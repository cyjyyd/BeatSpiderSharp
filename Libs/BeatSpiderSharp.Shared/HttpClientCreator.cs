using System.Net.Http.Headers;

namespace BeatSpiderSharp.Shared;

public static class HttpClientCreator
{
    public static HttpClient Create(HttpMessageHandler? handler = null, string? productName = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler);
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = "BeatSpiderSharp";
        }

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName,
            Constants.Version.ToNormalizedString()));

        return client;
    }
}
