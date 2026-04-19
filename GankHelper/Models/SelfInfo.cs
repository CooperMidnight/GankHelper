using System.Text.Json.Serialization;

namespace GankHelper.Models;

internal sealed class SelfInfo
{
    [JsonPropertyName("id")]
    public string UserId { get; set; } = null!;
}
