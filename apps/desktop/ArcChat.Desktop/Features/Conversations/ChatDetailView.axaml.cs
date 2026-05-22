// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace ArcChat.Desktop.Features.Conversations;

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:ElementsMustAppearInTheCorrectOrder", Justification = "Static drag helpers are kept before event handlers.")]
public partial class ChatDetailView : UserControl
{
    private static IStorageFile[] GetImageFiles(IDataTransfer dataTransfer)
    {
        IReadOnlyList<IStorageItem>? files = dataTransfer.TryGetFiles();
        if (files is null)
        {
            return Array.Empty<IStorageFile>();
        }

        return files
            .OfType<IStorageFile>()
            .Where(file => ImageAttachmentService.IsImageName(file.Name))
            .ToArray();
    }

    public ChatDetailView()
    {
        this.InitializeComponent();
        DragDrop.AddDragOverHandler(this.Composer, this.OnComposerDragOver);
        DragDrop.AddDropHandler(this.Composer, this.OnComposerDrop);
    }

    private void OnComposerKeyDown(object? sender, KeyEventArgs e)
    {
        if (this.DataContext is not ChatDetailViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.SendCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            viewModel.AbortCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control) && viewModel.CanAddMoreAttachments)
        {
            _ = viewModel.PasteImageAsync(new AvaloniaClipboardService(TopLevel.GetTopLevel(this)), CancellationToken.None);
        }
    }

    private void OnComposerDragOver(object? sender, DragEventArgs e)
    {
        if (this.DataContext is not ChatDetailViewModel viewModel || !viewModel.CanAddMoreAttachments)
        {
            return;
        }

        if (GetImageFiles(e.DataTransfer).Length > 0)
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void OnComposerDrop(object? sender, DragEventArgs e)
    {
        if (this.DataContext is not ChatDetailViewModel viewModel)
        {
            return;
        }

        IStorageFile[] files = GetImageFiles(e.DataTransfer);
        if (files.Length == 0)
        {
            return;
        }

        foreach (IStorageFile file in files)
        {
            using Stream input = await file.OpenReadAsync().ConfigureAwait(true);
            using MemoryStream buffer = new MemoryStream();
            await input.CopyToAsync(buffer, CancellationToken.None).ConfigureAwait(true);
            await viewModel.AddImageBytesAsync(
                buffer.ToArray(),
                ImageAttachmentService.GuessMimeTypeFromName(file.Name),
                CancellationToken.None).ConfigureAwait(true);
            if (!viewModel.CanAddMoreAttachments)
            {
                break;
            }
        }

        e.Handled = true;
    }
}
