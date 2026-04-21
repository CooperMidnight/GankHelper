using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GankHelper.Extensions;

namespace GankHelper.Helpers;

internal sealed class CacheHelper
{
    private readonly GankClient _client;

    public CacheHelper(GankClient client)
    {
        _client = client;
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
        // If there was already a cache file, then we probably don't need a page size of 50 to catch up with the latest listings.
        var pageSize = loadedFromCache ? 5 : 50;
        var userId = await GetUserIdAsync();

        await foreach (var listing in _client.GetAllListingsAsync(userId, pageSize))
        {
            var gankId = listing.GetProperty("id").GetString() ?? "";
            if (existingGankIds.Contains(gankId))
                break;
            result.Add(listing);
        }
        
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
        var pageSize = loadedFromCache ? 10 : 500;
        
        await foreach (var listing in _client.GetAllTransactionsAsync(pageSize))
        {
            var gankId = listing.GetProperty("human_id").GetString() ?? "";
            if (existingGankIds.Contains(gankId))
                break;
            result.Add(listing);
        }
        
        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, result);
        return result;
    }
    
    private async Task<string> GetUserIdAsync()
    {
        const string cacheFilePath = "./Self.cache";
        
        if (File.Exists(cacheFilePath))
        {
            await using var input = File.OpenRead("./Self.cache");
            if (await JsonSerializer.DeserializeAsync<JsonDocument>(input) is JsonDocument document)
                return document.RootElement.GetProperty("id").GetString()!;
        }
        
        var self = await _client.GetSelfAsync();
        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, self);
        return self.GetProperty("id").GetString()!;
    }
}
