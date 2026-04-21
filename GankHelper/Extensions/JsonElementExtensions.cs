using System.Text.Json;

namespace GankHelper.Extensions;

internal static class JsonElementExtensions
{
    extension(JsonElement element)
    {
        public string GetListingName()
            => element.GetProperty("name").GetString()!;

        public string GetListingId()
            => element.GetProperty("id").GetString()!;

        public string GetTransactionId()
            => element.GetProperty("human_id").GetString()!;
    }
}
