using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GankHelper.Extensions;
using GankHelper.Models;
using GankHelper.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GankHelper.Commands;

internal sealed class ReorderListingsCommand : CommandBase
{
    // NOTE: 50 is the max page size, so currently no point making it configurable.
    private const int PageSize = 50;
    private const string SortUrl = "https://api.ganknow.com/v1/catalogs/services/sort";
    private const string GetServicesUrlFormat = "https://api.ganknow.com/v1/catalogs/services?userId={0}8&page={1}&per_page={2}";
    private const string MeUrl = "https://api.ganknow.com/v1/users/me";
    
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<ReorderListingsCommand> _logger;
    private readonly IOptions<ReorderListingsOptions> _options;

    public ReorderListingsCommand(IHttpClientFactory clientFactory, ILogger<ReorderListingsCommand> logger, IOptions<ReorderListingsOptions> options)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _options = options;
    }

    public override async Task ExecuteAsync()
    {
        var page = 0;
        var client = _clientFactory.GetGankClient();
        var listings = new List<Service>();

        if (await client.GetFromJsonAsync<SingleItemResponse<SelfInfo>>(MeUrl) is not { Data: SelfInfo me })
        {
            _logger.LogError("Couldn't get user info.");
            return;
        }

        var userId = me.UserId;
        
        while (true)
        {
            page++;
            _logger.LogDebug("Getting page {Page}.", page);
            
            var response = await client.GetFromJsonAsync<ListResponse<Service>>(String.Format(null, GetServicesUrlFormat, userId, page, PageSize));

            if (response is not { Data: { Count: > 0 } services })
            {
                _logger.LogInformation("No more data.");
                break;
            }
            
            listings.AddRange(services);

            if (response.Data.Count < PageSize)
            {
                _logger.LogInformation("No more data.");
                break;
            }
        }

        if (_options.Value.OrderingSubstrings.Length > 0)
        {
            var enumerable = listings.OrderBy(x => x.Name.Contains(_options.Value.OrderingSubstrings[0]));

            foreach (var substring in _options.Value.OrderingSubstrings.Skip(1))
            {
                enumerable = enumerable.ThenBy(x => x.Name.Contains(substring));
            }

            enumerable = enumerable.ThenBy(x => x.Name);

            listings = enumerable.Reverse().ToList();
        }

        var ids = listings.Select(x => x.Id).ToArray();
        using var _ = await client.PutAsJsonAsync(SortUrl, ids);
    }
}
