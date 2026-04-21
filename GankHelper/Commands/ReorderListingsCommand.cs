using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using GankHelper.Extensions;
using GankHelper.Helpers;
using GankHelper.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GankHelper.Commands;

internal sealed class ReorderListingsCommand : CommandBase
{
    private const string SortUrl = "https://api.ganknow.com/v1/catalogs/services/sort";
    
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<ReorderListingsCommand> _logger;
    private readonly IOptions<ReorderListingsOptions> _options;
    private readonly CacheHelper _cacheHelper;

    public ReorderListingsCommand(IHttpClientFactory clientFactory, ILogger<ReorderListingsCommand> logger, IOptions<ReorderListingsOptions> options, CacheHelper cacheHelper)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _options = options;
        _cacheHelper = cacheHelper;
    }

    public override async Task ExecuteAsync()
    {
        var listings = await _cacheHelper.GetListingsAsync();

        if (_options.Value.OrderingSubstrings.Length > 0)
        {
            var enumerable = listings.OrderBy(x => DoesNameContainSubstring(x, _options.Value.OrderingSubstrings[0]));
        
            foreach (var substring in _options.Value.OrderingSubstrings.Skip(1))
            {
                enumerable = enumerable.ThenBy(x => DoesNameContainSubstring(x, substring));
            }
            
            enumerable = enumerable.ThenBy(GetName);
            listings = enumerable.Reverse().ToList();
        }
        
        var ids = listings.Select(GetId).ToArray();
        var client = _clientFactory.GetGankClient();
        using var _ = await client.PutAsJsonAsync(SortUrl, ids);
    }

    private static bool DoesNameContainSubstring(JsonElement element, string substring)
        => GetName(element).Contains(substring);

    private static string GetName(JsonElement element)
        => element.GetListingName();

    private static string GetId(JsonElement element)
        => element.GetListingId();
}
