using System;
using System.Net.Http.Headers;
using GankHelper.Commands;
using GankHelper.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var provider = new ServiceCollection()
    .Configure<GetSalesOptions>(configuration.GetRequiredSection("GetSales"))
    .Configure<ReorderListingsOptions>(configuration.GetRequiredSection("ReorderListings"))
    .Configure<GeneralOptions>(configuration.GetRequiredSection("General"))
    .AddTransient<GetSalesCommand>()
    .AddTransient<ReorderListingsCommand>()
    .AddLogging(b =>
    {
        _ = b
            .AddConsole()
            .AddConfiguration(configuration)
            // NOTE: Seems to ignore json configuration values, so setting it manually. 
            .AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
    })
    .AddHttpClient("Gank", (sp, client) =>
    {
        var generalOptions = sp.GetRequiredService<IOptions<GeneralOptions>>().Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", generalOptions.BearerToken);
    })
    .Services
    .BuildServiceProvider();

Console.WriteLine("[1] Get sales");
Console.WriteLine("[2] Reorder listings");
Console.Write("What would you like to do? ");

var choice = Console.ReadKey().Key;
Console.WriteLine();

switch (choice)
{
    case ConsoleKey.D1:
        await provider.GetRequiredService<GetSalesCommand>().ExecuteAsync();
        break;
    case ConsoleKey.D2:
        await provider.GetRequiredService<ReorderListingsCommand>().ExecuteAsync();
        break;
    default:
        return;
}
