// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ArcChat.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace ArcChat.Desktop;

/// <summary>
/// Resolves view models to matching Avalonia views.
/// </summary>
[RequiresUnreferencedCode(
    "The default view locator uses reflection and can be affected by trimming.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? type = Type.GetType(name);

        if (type is not null)
        {
            object? view = Activator.CreateInstance(type);
            if (view is Control control)
            {
                return control;
            }
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
