// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal interface IShareService
{
    Task<Uri?> ShareAsync(ShareGptRequest request, CancellationToken cancellationToken = default);
}
