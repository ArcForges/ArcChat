// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ArcChat.Agent;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence.Repositories;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using ArcChat.Protocol.Settings;
using CommunityToolkit.Mvvm.Input;
using SettingsRepository = ArcChat.LocalServices.Settings.ISettingsRepository;

namespace ArcChat.Desktop.Features.Masks;

internal sealed class NewChatViewModel : ViewModelBase
{
    private readonly IConversationRepository? conversationRepository;
    private readonly SettingsRepository? settingsRepository;
    private readonly IAppNavigator? navigator;
    private ProviderOption? selectedProvider;
    private ModelOption? selectedModel;
    private ModelConfig baseModelConfig = ModelConfig.NextChatDefault;
    private string statusMessage = string.Empty;
    private bool isBusy;

    public NewChatViewModel()
        : this(null, null, null, true)
    {
    }

    internal NewChatViewModel(
        IConversationRepository conversationRepository,
        SettingsRepository settingsRepository,
        IAppNavigator navigator)
        : this((IConversationRepository?)conversationRepository, settingsRepository, navigator, false)
    {
    }

    private NewChatViewModel(
        IConversationRepository? conversationRepository,
        SettingsRepository? settingsRepository,
        IAppNavigator? navigator,
        bool designMode)
    {
        _ = designMode;
        this.conversationRepository = conversationRepository;
        this.settingsRepository = settingsRepository;
        this.navigator = navigator;
        foreach (RecommendedMaskItem mask in RecommendedMaskCatalog.Load())
        {
            this.RecommendedMasks.Add(mask);
        }

        this.ApplySettings(SettingsDefaults.Create());
        this.ReturnHomeCommand = new RelayCommand(() => this.navigator?.Navigate(new Home()));
        this.MoreMasksCommand = new RelayCommand(() => this.navigator?.Navigate(new Navigation.Masks()));
        this.StartBlankCommand = new AsyncRelayCommand(this.StartBlankAsync);
        this.StartWithMaskCommand = new AsyncRelayCommand<RecommendedMaskItem>(this.StartWithMaskAsync);
    }

    public ObservableCollection<RecommendedMaskItem> RecommendedMasks { get; } = new ObservableCollection<RecommendedMaskItem>();

    public ObservableCollection<ProviderOption> Providers { get; } = new ObservableCollection<ProviderOption>();

    public ObservableCollection<ModelOption> Models { get; } = new ObservableCollection<ModelOption>();

    public IRelayCommand ReturnHomeCommand { get; }

    public IRelayCommand MoreMasksCommand { get; }

    public IAsyncRelayCommand StartBlankCommand { get; }

    public IAsyncRelayCommand<RecommendedMaskItem> StartWithMaskCommand { get; }

    public ProviderOption? SelectedProvider
    {
        get => this.selectedProvider;
        set
        {
            if (this.SetProperty(ref this.selectedProvider, value))
            {
                this.RefreshModels(value, this.SelectedModel?.Id);
            }
        }
    }

    public ModelOption? SelectedModel
    {
        get => this.selectedModel;
        set => this.SetProperty(ref this.selectedModel, value);
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    public bool IsBusy
    {
        get => this.isBusy;
        private set => this.SetProperty(ref this.isBusy, value);
    }

    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (this.settingsRepository is null)
        {
            return;
        }

        SettingsSnapshot snapshot = await this.settingsRepository.LoadAsync(cancellationToken).ConfigureAwait(true);
        this.ApplySettings(snapshot);
    }

    private async Task StartBlankAsync()
    {
        await this.StartConversationAsync(null, true, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task StartWithMaskAsync(RecommendedMaskItem? mask)
    {
        if (mask is null)
        {
            return;
        }

        await this.StartConversationAsync(mask.Mask, false, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task StartConversationAsync(Mask? recommendedMask, bool pinBlank, CancellationToken cancellationToken)
    {
        this.IsBusy = true;
        try
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string conversationId = "chat-" + Guid.NewGuid().ToString("N");
            ModelConfig activeModelConfig = this.CreateActiveModelConfig();
            Mask mask = recommendedMask is null
                ? NewChatConversationFactory.CreateBlankMask(conversationId, now, activeModelConfig)
                : NewChatConversationFactory.ApplyActiveProvider(recommendedMask, activeModelConfig, now);
            Conversation conversation = new Conversation(
                conversationId,
                recommendedMask?.Name ?? ConversationTitler.DefaultTopic,
                string.Empty,
                ImmutableArray<Message>.Empty,
                new ChatStat(0, 0, 0),
                now,
                0,
                null,
                mask);

            if (this.conversationRepository is not null)
            {
                await this.conversationRepository.UpsertAsync(conversation, cancellationToken).ConfigureAwait(true);
                if (pinBlank)
                {
                    await this.conversationRepository.SetPinnedAsync(conversationId, true, cancellationToken).ConfigureAwait(true);
                }
            }

            this.StatusMessage = conversation.Topic;
            this.navigator?.Navigate(new Chat(conversationId));
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private void ApplySettings(SettingsSnapshot snapshot)
    {
        this.baseModelConfig = snapshot.ModelConfig;
        ImmutableArray<ProviderOption> providerOptions = this.CreateProviderOptions(snapshot);
        this.ReplaceProviders(providerOptions);
        ProviderOption? provider = providerOptions.FirstOrDefault(option =>
                string.Equals(option.ProviderName, snapshot.ModelConfig.ProviderName, StringComparison.Ordinal)
                || string.Equals(option.Id, snapshot.ModelConfig.ProviderName, StringComparison.OrdinalIgnoreCase))
            ?? providerOptions.FirstOrDefault();
        this.SelectedProvider = provider;
        this.RefreshModels(provider, snapshot.ModelConfig.Model);
    }

    private ImmutableArray<ProviderOption> CreateProviderOptions(SettingsSnapshot snapshot)
    {
        ImmutableArray<ProviderConfig> providerConfigs = snapshot.Providers.ProviderConfigs.IsDefaultOrEmpty
            ? SettingsDefaults.Create().Providers.ProviderConfigs
            : snapshot.Providers.ProviderConfigs;
        ImmutableArray<ProviderOption>.Builder providers = ImmutableArray.CreateBuilder<ProviderOption>(providerConfigs.Length);
        foreach (ProviderConfig provider in providerConfigs)
        {
            ImmutableArray<ModelOption> models = this.CreateModelOptions(provider, snapshot);
            providers.Add(new ProviderOption(provider.Id, provider.ProviderName, provider.ProviderName, models));
        }

        return providers.ToImmutable();
    }

    private ImmutableArray<ModelOption> CreateModelOptions(ProviderConfig provider, SettingsSnapshot snapshot)
    {
        Dictionary<string, ModelOption> modelsById = new Dictionary<string, ModelOption>(StringComparer.Ordinal);
        foreach (ModelDescriptor model in provider.Models.Where(static model => model.Available))
        {
            modelsById[model.Id] = new ModelOption(model.Id, model.DisplayName);
        }

        AddModel(snapshot.Providers.DefaultModel);
        AddModel(snapshot.ModelConfig.Model);
        foreach (RecommendedMaskItem mask in this.RecommendedMasks)
        {
            AddModel(mask.Model);
        }

        if (modelsById.Count == 0)
        {
            AddModel(ModelConfig.NextChatDefault.Model);
        }

        return modelsById.Values.ToImmutableArray();

        void AddModel(string modelId)
        {
            if (!string.IsNullOrWhiteSpace(modelId))
            {
                modelsById[modelId] = new ModelOption(modelId, modelId);
            }
        }
    }

    private void RefreshModels(ProviderOption? provider, string? preferredModelId)
    {
        this.ReplaceModels(provider?.Models ?? ImmutableArray<ModelOption>.Empty);
        this.SelectedModel = this.Models.FirstOrDefault(model => string.Equals(model.Id, preferredModelId, StringComparison.Ordinal))
            ?? this.Models.FirstOrDefault();
    }

    private ModelConfig CreateActiveModelConfig()
    {
        ProviderOption? provider = this.SelectedProvider ?? this.Providers.FirstOrDefault();
        ModelOption? model = this.SelectedModel ?? provider?.Models.FirstOrDefault();
        return NewChatConversationFactory.CreateActiveModelConfig(this.baseModelConfig, provider?.ProviderName, model?.Id);
    }

    private void ReplaceProviders(IEnumerable<ProviderOption> values)
    {
        this.Providers.Clear();
        foreach (ProviderOption value in values)
        {
            this.Providers.Add(value);
        }
    }

    private void ReplaceModels(IEnumerable<ModelOption> values)
    {
        this.Models.Clear();
        foreach (ModelOption value in values)
        {
            this.Models.Add(value);
        }
    }
}
