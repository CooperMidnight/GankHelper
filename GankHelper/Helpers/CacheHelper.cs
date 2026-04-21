using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GankHelper.Extensions;
using GankHelper.Models;

namespace GankHelper.Helpers;

internal sealed class CacheHelper
{
    private const string MeUrl = "https://api.ganknow.com/v1/users/me";
    private const string GetServicesUrlFormat = "https://api.ganknow.com/v1/catalogs/services?userId={0}&page={1}&per_page={2}&order_by=sort%20asc&order_by=createdAt%20desc";
    private const string GetTransactionsUrlFormat = "https://api-v2.ganknow.com/payment/transactions?page={0}&per_page={1}&usecases_client=WALLET_GANK_EARNING_TX";
    
    private readonly HttpClient _client;

    public CacheHelper(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient("Gank");
    }

    public async Task<List<JsonElement>> GetListingsAsync()
    {
        const string cacheFilePath = "./Listings.cache";
        
        var result = new List<JsonElement>();
        var loadedFromCache = false;
        
        if (File.Exists(cacheFilePath))
        {
            await using var input = File.OpenRead("./Listings.cache");
            var nodes = await JsonSerializer.DeserializeAsync<List<JsonElement>>(input) ?? [];
            result.AddRange(nodes);
            loadedFromCache = true;
        }

        var existingGankIds = result.Select(x => x.GetProperty("id").GetString()!).ToHashSet();
        var page = 0;
        // If there was already a cache file, then we probably don't need a page size of 50 to catch up with the latest listings.
        var pageSize = loadedFromCache ? 5 : 50;
        var self = await GetSelfAsync();
        
        while (true)
        {
            page++;
            
            var response = await _client.GetFromJsonAsync<JsonDocument>(String.Format(null, GetServicesUrlFormat, self.UserId, page, pageSize));

            if (response?.RootElement.TryGetProperty("data", out var property) != true)
                throw new InvalidOperationException("Unexpected server response");
            if (property.GetArrayLength() == 0)
                break;
            
            foreach (var item in property.EnumerateArray())
            {
                var gankId = item.GetProperty("id").GetString() ?? "";
                if (existingGankIds.Contains(gankId))
                    goto End;
                result.Add(item);
            }
        }
        
        End:
        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, result);
        return result;
    }

    public async Task<List<JsonElement>> GetTransactionsAsync()
    {
        const string cacheFilePath = "./Transactions.cache";

        var result = new List<JsonElement>();
        var loadedFromCache = false;

        if (File.Exists(cacheFilePath))
        {
            await using var input = File.OpenRead(cacheFilePath);
            var nodes = await JsonSerializer.DeserializeAsync<List<JsonElement>>(input) ?? [];
            result.AddRange(nodes);
            loadedFromCache = true;
        }

        var existingGankIds = result.Select(x => x.GetTransactionId()).ToHashSet();
        var page = 0;
        var pageSize = loadedFromCache ? 10 : 500;

        while (true)
        {
            page++;
            
            var response = await _client.GetFromJsonAsync<JsonDocument>(String.Format(null, GetTransactionsUrlFormat, page, pageSize));
            
            if (response?.RootElement.TryGetProperty("data", out var property) != true)
                throw new InvalidOperationException("Unexpected server response");
            if (property.GetArrayLength() == 0)
                break;
            
            foreach (var item in property.EnumerateArray())
            {
                var gankId = item.GetTransactionId();
                if (existingGankIds.Contains(gankId))
                    goto End;
                result.Add(item);
            }
        }
        
        End:
        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, result);
        return result;
    }
    
    private async Task<SelfInfo> GetSelfAsync()
    {
        const string cacheFilePath = "./Self.cache";
        
        if (File.Exists(cacheFilePath))
        {
            await using var input = File.OpenRead("./Self.cache");
            if (await JsonSerializer.DeserializeAsync<SelfInfo>(input) is SelfInfo info)
                return info;
        }

        if (await _client.GetFromJsonAsync<SingleItemResponse<SelfInfo>>(MeUrl) is not { Data: SelfInfo me })
            throw new InvalidOperationException("Couldn't get info from server.");

        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, me);
        return me;
    }
}
