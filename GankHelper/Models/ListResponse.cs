using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GankHelper.Models;

internal sealed class ListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = null!;
}
