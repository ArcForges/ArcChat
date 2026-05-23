// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using ArcChat.Net.Factory;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ShareGptShareService : IShareService
{
    private static readonly Uri ShareEndpoint = new Uri("https://sharegpt.com/api/conversations");
    private readonly INetCoreFactory netCoreFactory;

    public ShareGptShareService(INetCoreFactory netCoreFactory)
    {
        this.netCoreFactory = netCoreFactory ?? throw new ArgumentNullException(nameof(netCoreFactory));
    }

    public async Task<Uri?> ShareAsync(ShareGptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        HttpClient client = this.netCoreFactory.GetClient(NetClientProfileNames.Default);
        string body = JsonSerializer.Serialize(request, ConversationExportJsonContext.Default.ShareGptRequest);
        using StringContent content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(ShareEndpoint, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        ShareGptResponse? shareResponse = await JsonSerializer
            .DeserializeAsync(stream, ConversationExportJsonContext.Default.ShareGptResponse, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(shareResponse?.Id)
            ? null
            : new Uri("https://shareg.pt/" + shareResponse.Id);
    }
}
