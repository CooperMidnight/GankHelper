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
        const string cacheFileName = "Listings.cache";

        var (result, loadedFromCache) = await TryLoadFromCacheFileAsync(cacheFileName);
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

        await WriteCacheFileAsync(result, cacheFileName);
        return result;
    }

    public async Task<List<JsonElement>> GetTransactionsAsync()
    {
        const string cacheFileName = "Transactions.cache";

        var (result, loadedFromCache) = await TryLoadFromCacheFileAsync(cacheFileName);
        var existingGankIds = result.Select(x => x.GetTransactionId()).ToHashSet();
        var pageSize = loadedFromCache ? 10 : 500;
        
        await foreach (var listing in _client.GetAllTransactionsAsync(pageSize))
        {
            var gankId = listing.GetProperty("human_id").GetString() ?? "";
            if (existingGankIds.Contains(gankId))
                break;
            result.Add(listing);
        }

        await WriteCacheFileAsync(result, cacheFileName);
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

    private static async Task<(List<JsonElement> Elements, bool LoadedFromCache)> TryLoadFromCacheFileAsync(string cacheFileName)
    {
        var cacheFilePath = Path.Combine(Directory.GetCurrentDirectory(), cacheFileName);

        if (!File.Exists(cacheFilePath))
            return ([], false);
        
        await using var input = File.OpenRead(cacheFilePath);
        var elements = await JsonSerializer.DeserializeAsync<List<JsonElement>>(input) ?? [];
        return (elements, true);
    }

    private static async Task WriteCacheFileAsync<T>(T data, string cacheFileName)
    {
        var cacheFilePath = Path.Combine(Directory.GetCurrentDirectory(), cacheFileName);
        await using var output = File.OpenWrite(cacheFilePath);
        await JsonSerializer.SerializeAsync(output, data);
    }
}
