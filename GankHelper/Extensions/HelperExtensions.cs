using System;
using System.Collections.Generic;
using System.Net.Http;

namespace GankHelper.Extensions;

internal static class HelperExtensions
{
    public static HttpClient GetGankClient(this IHttpClientFactory factory)
        => factory.CreateClient("Gank");

    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> addFactory, Func<TValue, TValue> updateFactory)
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            dictionary[key] = updateFactory(existingValue);
        }
        else
        {
            dictionary[key] = addFactory();
        }
    }
}
