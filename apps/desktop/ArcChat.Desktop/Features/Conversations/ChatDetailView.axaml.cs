// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;

namespace ArcChat.Desktop.Features.Conversations;

public partial class ChatDetailView : UserControl
{
    public ChatDetailView()
    {
        this.InitializeComponent();
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
    }
}
