using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using GankHelper.Constants;
using GankHelper.Extensions;
using GankHelper.Helpers;
using Microsoft.Extensions.Logging;

namespace GankHelper.Commands;

internal sealed class GetSalesCommand : CommandBase
{
    private readonly CacheHelper _cacheHelper;
    private readonly ILogger<GetSalesCommand> _logger;

    public GetSalesCommand(ILogger<GetSalesCommand> logger, CacheHelper cacheHelper)
    {
        _logger = logger;
        _cacheHelper = cacheHelper;
    }

    public override async Task ExecuteAsync()
    {
        var earningsViaItems = 0m;
        var earningsViaTips = 0m;
        var withdrawnTotal = 0m;
        var listingInfo = new Dictionary<string, ListingInfo>();
        var listings = (await _cacheHelper.GetListingsAsync()).ToDictionary(x => x.GetListingId());
        var transactions = await _cacheHelper.GetTransactionsAsync();
        var transactionIds = new HashSet<string>();

        foreach (var transaction in transactions)
        {
            if (!transactionIds.Add(transaction.GetTransactionId()))
            {
                _logger.LogDebug("Found duplicate transaction {Id}. Skipping.", transaction.GetTransactionId());
                continue;
            }

            var amountInDollars = Decimal.Parse(transaction.GetProperty("price").ToString(), CultureInfo.InvariantCulture);
            var type = transaction.GetProperty("usecase").GetString()!;

            switch (type)
            {
                case TransactionTypes.ProfileTip or TransactionTypes.PostTip:
                    earningsViaTips += amountInDollars;
                    break;
                case TransactionTypes.Withdrawal:
                    withdrawnTotal += amountInDollars;
                    break;
                case TransactionTypes.DigitalGoodsPurchase:
                    earningsViaItems += amountInDollars;
                    var usecaseData = transaction.GetProperty("usecase_data").GetProperty("data");
                    var itemId = usecaseData.GetProperty("catalog_id").ToString()!;
                    var itemName = listings.TryGetValue(itemId, out var existingListing)
                        ? existingListing.GetListingName()
                        : usecaseData.GetProperty("catalog_title").GetString()!;
                    
                    listingInfo.AddOrUpdate(
                        itemId,
                        () => new ListingInfo(itemId, itemName, 1, amountInDollars),
                        old => old with { SaleCount = old.SaleCount + 1, TotalEarnings = old.TotalEarnings + amountInDollars }
                    );
                    break;
                default:
                    _logger.LogWarning("Transaction type {Type} not implemented. Skipping", type);
                    break;
            }
        }

        _logger.LogInformation("Done.");
        _logger.LogInformation("Earnings via digital items: {DigitalItemTotal}", earningsViaItems);
        _logger.LogInformation("Earnings via tips: {TipTotal}", earningsViaTips);
        _logger.LogInformation("Total withdrawn: {Withdrawn}", withdrawnTotal);

        var ordered = listingInfo.Values
            .OrderByDescending(x =>
            {
                return listings.TryGetValue(x.GankId, out var existingListing)
                    ? existingListing.GetProperty("createdAt").GetDateTimeOffset()
                    : DateTimeOffset.MinValue;
            })
            .ThenBy(x => x.Name)
            .Select(x => new { x.Name, x.SaleCount, x.TotalEarnings })
            .ToArray();
        
        var csvFilePath = Path.Combine("./", "earnings_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss", null) + ".csv");
        await using var writer = new StreamWriter(csvFilePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(ordered);
        _logger.LogInformation("Wrote data to {Path}", csvFilePath);
    }
    
    private sealed record ListingInfo(string GankId, string Name, int SaleCount, decimal TotalEarnings);
}
