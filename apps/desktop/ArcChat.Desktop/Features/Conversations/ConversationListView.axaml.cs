// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ArcChat.Desktop.Features.Conversations;

public partial class ConversationListView : UserControl
{
    public ConversationListView()
    {
        this.InitializeComponent();
        DragDrop.AddDragOverHandler(this.ConversationList, this.OnDragOver);
        DragDrop.AddDropHandler(this.ConversationList, this.OnDrop);
    }

    private static ConversationListItem? FindConversationItem(object? source)
    {
        Avalonia.Visual? current = source as Avalonia.Visual;
        while (current is not null)
        {
            if (current is Control { DataContext: ConversationListItem item })
            {
                return item;
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Avalonia owns IDataTransfer lifetime after DoDragDropAsync starts.")]
    private async void OnConversationPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed || FindConversationItem(sender) is not { } item)
        {
            return;
        }

        DataTransfer dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.CreateText(item.Id));
        _ = await DragDrop.DoDragDropAsync(e, dataTransfer, DragDropEffects.Move).ConfigureAwait(true);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetText() is not null)
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        string? conversationId = e.DataTransfer.TryGetText();
        if (conversationId is null || this.DataContext is not ConversationListViewModel viewModel)
        {
            return;
        }

        ConversationListItem? target = FindConversationItem(e.Source);
        int targetIndex = target is null
            ? viewModel.Conversations.Count - 1
            : viewModel.Conversations.IndexOf(target);
        await viewModel.MoveAsync(conversationId, targetIndex, CancellationToken.None).ConfigureAwait(true);
        e.Handled = true;
    }
}
