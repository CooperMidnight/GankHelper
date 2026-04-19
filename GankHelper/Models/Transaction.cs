using System;
using System.Text.Json.Serialization;

namespace GankHelper.Models;

internal sealed class Transaction
{
    [JsonPropertyName("human_id")]
    public string Id { get; set; } = null!;
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("usecase")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("price")]
    public double PriceInDollars { get; set; }

    [JsonPropertyName("usecase_data")]
    public UsecaseData Usecase { get; set; } = null!;
    
    public sealed class UsecaseData
    {
        [JsonPropertyName("data")]
        public UsecaseImpl Data { get; set; } = null!;

        public sealed class UsecaseImpl
        {
            [JsonPropertyName("catalog_id")]
            public string ItemId { get; set; } = null!;

            [JsonPropertyName("catalog_title")]
            public string ItemTitle { get; set; } = null!;
        }
    }
}
