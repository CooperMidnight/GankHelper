using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using GankHelper.Constants;
using GankHelper.Extensions;
using GankHelper.Models;
using GankHelper.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GankHelper.Commands;

internal sealed class GetSalesCommand : CommandBase
{
    private const string UrlFormat = "https://api-v2.ganknow.com/payment/transactions?page={0}&per_page={1}&usecases_client=WALLET_GANK_EARNING_TX";
    
    private readonly IHttpClientFactory _clientFactory;
    private readonly IOptions<GetSalesOptions> _options;
    private readonly ILogger<GetSalesCommand> _logger;

    public GetSalesCommand(IHttpClientFactory clientFactory, IOptions<GetSalesOptions> options, ILogger<GetSalesCommand> logger)
    {
        _clientFactory = clientFactory;
        _options = options;
        _logger = logger;
    }

    public override async Task ExecuteAsync()
    {
        var client = _clientFactory.GetGankClient();
        var pageSize = _options.Value.PageSize;
        var page = 0;
        var earningsViaItems = 0;
        var earningsViaTips = 0;
        var withdrawnTotal = 0;
        var listingInfo = new Dictionary<string, ListingInfo>();
        var transactionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            page++;
            _logger.LogDebug("Getting page {Page}.", page);
            
            var response = await client.GetFromJsonAsync<ListResponse<Transaction>>(String.Format(null, UrlFormat, page, pageSize));

            if (response is not { Data: { Count: > 0 } transactions })
            {
                _logger.LogInformation("No more data.");
                break;
            }

            foreach (var transaction in transactions)
            {
                if (!transactionIds.Add(transaction.Id))
                {
                    // This can happen if someone buys an item while you're running this app.
                    _logger.LogDebug("Found duplicate transaction {Id}. Skipping.", transaction.Id);
                    continue;
                }

                var priceInDollars = (int)transaction.PriceInDollars;

                switch (transaction.Type)
                {
                    case TransactionTypes.ProfileTip or TransactionTypes.PostTip:
                        earningsViaTips += priceInDollars;
                        break;
                    case TransactionTypes.Withdrawal:
                        withdrawnTotal += priceInDollars;
                        break;
                    case TransactionTypes.DigitalGoodsPurchase:
                    {
                        earningsViaItems += priceInDollars;
                        var title = transaction.Usecase.Data.ItemTitle;
                        var itemId = transaction.Usecase.Data.ItemId;

                        listingInfo.AddOrUpdate(
                            itemId,
                            () => new ListingInfo(title, 1, priceInDollars),
                            old => old with { SaleCount = old.SaleCount + 1, TotalEarnings = old.TotalEarnings + priceInDollars }
                        );
                        break;
                    }
                    default:
                        _logger.LogWarning("Transaction type {Type} not implemented. Skipping.", transaction.Type);
                        break;
                }
            }
        }
        
        _logger.LogInformation("Done.");
        _logger.LogInformation("Earnings via digital items: {DigitalItemTotal}", earningsViaItems);
        _logger.LogInformation("Earnings via tips: {TipTotal}", earningsViaTips);
        _logger.LogInformation("Total withdrawn: {Withdrawn}", withdrawnTotal);

        var dateComparer = Comparer<string>.Create(static (a, b) =>
        {
            var firstDate = TryGetDate(a);
            var secondDate = TryGetDate(b);

            return firstDate.HasValue && secondDate.HasValue
                ? firstDate.Value.CompareTo(secondDate.Value)
                : firstDate.HasValue
                    ? 1
                    : secondDate.HasValue
                        ? -1
                        : 0;
        });

        var listings = listingInfo.Values
            .OrderByDescending(x => x.Name, dateComparer)
            .ThenBy(x => x.Name)
            .ToList();

        if (_options.Value.ShouldWriteToCsvFile)
        {
            await using var writer = new StreamWriter(_options.Value.CsvFilePath!);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(listings);
            _logger.LogInformation("Wrote data to {Path}", _options.Value.CsvFilePath);
        }
    }
    
    private sealed record ListingInfo(string Name, int SaleCount, int TotalEarnings);

    private static int? TryGetDate(string input)
    {
        return Regex.Match(input, "[0-9]{6}") is { Success: true } match
            ? Int32.Parse(match.Value, null)
            : null;
    }
}
