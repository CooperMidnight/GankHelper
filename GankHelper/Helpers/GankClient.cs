using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GankHelper.Extensions;

namespace GankHelper.Helpers;

internal sealed class GankClient
{
    private const string MeUrl = "https://api.ganknow.com/v1/users/me";
    private const string GetServicesUrlFormat = "https://api.ganknow.com/v1/catalogs/services?userId={0}&page={1}&per_page={2}&order_by=sort%20asc&order_by=createdAt%20desc";
    private const string GetTransactionsUrlFormat = "https://api-v2.ganknow.com/payment/transactions?page={0}&per_page={1}&usecases_client=WALLET_GANK_EARNING_TX";
    
    private readonly HttpClient _client;

    public GankClient(IHttpClientFactory factory)
    {
        _client = factory.GetGankClient();
    }
    
    // TODO: The two next functions are nearly identical. The services URL takes an extra format item.

    public async IAsyncEnumerable<JsonElement> GetAllListingsAsync(string userId, int pageSize)
    {
        var page = 0;
        
        while (true)
        {
            page++;
            
            var response = await _client.GetFromJsonAsync<JsonDocument>(String.Format(null, GetServicesUrlFormat, userId, page, pageSize));
            
            if (response?.RootElement.TryGetProperty("data", out var property) != true)
                throw new InvalidOperationException("Unexpected server response");
            if (property.GetArrayLength() == 0)
                break;
            
            foreach (var item in property.EnumerateArray())
            {
                yield return item;
            }
            
            if (property.GetArrayLength() < pageSize)
                yield break;
        }
    }

    public async IAsyncEnumerable<JsonElement> GetAllTransactionsAsync(int pageSize)
    {
        var page = 0;
        
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
                yield return item;
            }
            
            if (property.GetArrayLength() < pageSize)
                yield break;
        }
    }
    
    public async Task<JsonElement> GetSelfAsync()
    {
        return await _client.GetFromJsonAsync<JsonDocument>(MeUrl) is not JsonDocument document
            ? throw new InvalidOperationException("Couldn't get info from server.")
            : document.RootElement.GetProperty("data");
    }
}
