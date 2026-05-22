// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    private static readonly string[] ProtocolForbiddenDependencies =
    {
        "ArcChat.Net",
        "ArcChat.Agent",
        "ArcChat.Tools",
        "ArcChat.Desktop",
        "ArcChat.UI",
        "ArcChat.Integrations",
        "ArcChat.LocalServices",
        "ArcChat.LocalPersistence",
        "ArcChat.ModelProviders",
    };

    private static readonly string[] NetCoreForbiddenDependencies =
    {
        "ArcChat.Agent",
        "ArcChat.Tools",
        "ArcChat.ModelProviders",
        "ArcChat.LocalServices",
        "ArcChat.Desktop",
    };

    private static readonly string[] AgentForbiddenDependencies =
    {
        "ArcChat.Desktop",
        "ArcChat.UI",
        "ArcChat.Integrations",
        "Avalonia",
    };

    private static readonly string[] ToolsForbiddenDependencies =
    {
        "ArcChat.Desktop",
        "Avalonia",
        "NetMQ",
        "System.Net.Http",
    };

    private static readonly string[] ModelProviderForbiddenDependencies =
    {
        "ArcChat.Desktop",
        "ArcChat.Agent",
        "Avalonia",
    };

    [Fact]
    public void ProtocolDoesNotDependOnOtherArcChatAssemblies()
    {
        AssertNoDependency("ArcChat.Protocol", ProtocolForbiddenDependencies);
    }

    [Fact]
    public void NetCoreDoesNotDependOnHigherLevelRuntimeAssemblies()
    {
        AssertNoDependency("ArcChat.Net", NetCoreForbiddenDependencies);
    }

    [Fact]
    public void AgentDoesNotDependOnDesktopUiOrIntegrations()
    {
        AssertNoDependency("ArcChat.Agent", AgentForbiddenDependencies);
    }

    [Fact]
    public void ToolsDoesNotDependOnDesktopAvaloniaNetMqOrRawHttpClient()
    {
        AssertNoDependency("ArcChat.Tools", ToolsForbiddenDependencies);
    }

    [Fact]
    public void ModelProvidersDoNotDependOnDesktopAgentOrAvalonia()
    {
        foreach (string assemblyName in LoadArcChatAssemblies().Where(name => name.StartsWith("ArcChat.ModelProviders.", StringComparison.Ordinal)))
        {
            AssertNoDependency(assemblyName, ModelProviderForbiddenDependencies);
        }
    }

    private static void AssertNoDependency(string assemblyName, string[] forbiddenDependencies)
    {
        Assembly assembly = Assembly.Load(assemblyName);
        TestResult result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        _ = result.IsSuccessful.Should().BeTrue($"{assemblyName} must not depend on {string.Join(", ", forbiddenDependencies)}");
    }

    private static string[] LoadArcChatAssemblies()
    {
        return Directory
            .EnumerateFiles(AppContext.BaseDirectory, "ArcChat.*.dll", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && !name.EndsWith(".Tests", StringComparison.Ordinal))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
