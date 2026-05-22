# NC00 Settings Schema Source Inventory

Source commit: `C:\MyFile\DevAll\QmlSharp\NextChat` at `89b8f26ff8f03a0c5b98fc3026d980721495227e`.

This file is the NC00 field/default inventory required by `settings-schema-migration-matrix.md`. Later implementation steps replace planned DTO, persistence, binding, and test names with exact shipped names. Secrets marked `keychain` must migrate to `keychain://` references in NC15.08.

## Store Metadata

| Source | Store key | Version | Migrations found | Owner |
| --- | --- | ---: | --- | --- |
| `app/store/access.ts` | `Access` | 2 | `< 2` | NC15.05 / NC15.08 |
| `app/store/config.ts` | `Config` | 4.1 | `< 3.4`, `< 3.5`, `< 3.6`, `< 3.7`, `< 3.8`, `< 3.9`, `< 4.1` | NC02 / NC03 / NC15 |
| `app/store/chat.ts` | `Chat` | 3.3 | `< 2`, `< 3`, `< 3.1`, `< 3.2`, `< 3.3` | NC02 / NC04 |
| `app/store/mask.ts` | `Mask` | 3.1 | `< 3`, `< 3.1` | NC07.00-02 |
| `app/store/plugin.ts` | `Plugin` | 1 | none | NC07.04-06 |
| `app/store/prompt.ts` | `Prompt` | 3 | `< 3` | NC07.03 |
| `app/store/sd.ts` | `SdList` | 1.0 | none | NC13 |
| `app/store/sync.ts` | `Sync` | 1.2 | `< 1.1`, `< 1.2` | NC14 |
| `app/store/update.ts` | `Update` | 1 | none | NC15.06 |
| `app/mcp/mcp_config.default.json` | MCP seed | n/a | n/a | NC09 |

## Field Rows

| Source path | Source field | C# DTO field | Persistence location | AXAML binding | Migration test | Manual acceptance path | Final gate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `app/store/access.ts` | `accessCode` | `AccessState.AccessCode` | settings JSON; secret-like value moved to keychain/external proxy mode | Auth access code field | `AccessStoreMigrationTests` planned | Settings > Auth | FG.08 |
| `app/store/access.ts` | `useCustomConfig` | `AccessState.UseCustomConfig` | settings JSON | Settings > Access custom config toggle | planned | Settings > Providers | FG.08 |
| `app/store/access.ts` | `provider` | `AccessState.Provider` | settings JSON | provider selector | planned | Settings > Model Provider | FG.08 |
| `app/store/access.ts` | `openaiUrl` | `ProviderSettings.OpenAi.BaseUrl` | settings JSON | OpenAI endpoint text box | planned | Settings > OpenAI | FG.08 |
| `app/store/access.ts` | `openaiApiKey` | `ProviderSettings.OpenAi.ApiKeyRef` | keychain | OpenAI API key input | planned | Settings > OpenAI | FG.08 |
| `app/store/access.ts` | `azureUrl` | `ProviderSettings.Azure.BaseUrl` | settings JSON | Azure endpoint text box | planned | Settings > Azure | FG.08 |
| `app/store/access.ts` | `azureApiKey` | `ProviderSettings.Azure.ApiKeyRef` | keychain | Azure API key input | planned | Settings > Azure | FG.08 |
| `app/store/access.ts` | `azureApiVersion` | `ProviderSettings.Azure.ApiVersion` | settings JSON | Azure API version input | planned | Settings > Azure | FG.08 |
| `app/store/access.ts` | `googleUrl` | `ProviderSettings.Google.BaseUrl` | settings JSON | Google endpoint text box | planned | Settings > Google | FG.08 |
| `app/store/access.ts` | `googleApiKey` | `ProviderSettings.Google.ApiKeyRef` | keychain | Google API key input | planned | Settings > Google | FG.08 |
| `app/store/access.ts` | `googleApiVersion` | `ProviderSettings.Google.ApiVersion` | settings JSON | Google API version selector/input | planned | Settings > Google | FG.08 |
| `app/store/access.ts` | `googleSafetySettings` | `ProviderSettings.Google.SafetyThreshold` | settings JSON | Google safety selector | planned | Settings > Google | FG.08 |
| `app/store/access.ts` | `anthropicUrl` | `ProviderSettings.Anthropic.BaseUrl` | settings JSON | Anthropic endpoint text box | planned | Settings > Anthropic | FG.08 |
| `app/store/access.ts` | `anthropicApiKey` | `ProviderSettings.Anthropic.ApiKeyRef` | keychain | Anthropic API key input | planned | Settings > Anthropic | FG.08 |
| `app/store/access.ts` | `anthropicApiVersion` | `ProviderSettings.Anthropic.ApiVersion` | settings JSON | Anthropic API version input | planned | Settings > Anthropic | FG.08 |
| `app/store/access.ts` | `baiduUrl` | `ProviderSettings.Baidu.BaseUrl` | settings JSON | Baidu endpoint display/input | planned | Settings > Baidu | FG.08 |
| `app/store/access.ts` | `baiduApiKey` | `ProviderSettings.Baidu.ApiKeyRef` | keychain | Baidu API key input | planned | Settings > Baidu | FG.08 |
| `app/store/access.ts` | `baiduSecretKey` | `ProviderSettings.Baidu.SecretKeyRef` | keychain | Baidu secret key input | planned | Settings > Baidu | FG.08 |
| `app/store/access.ts` | `bytedanceUrl` | `ProviderSettings.ByteDance.BaseUrl` | settings JSON | ByteDance endpoint text box | planned | Settings > ByteDance | FG.08 |
| `app/store/access.ts` | `bytedanceApiKey` | `ProviderSettings.ByteDance.ApiKeyRef` | keychain | ByteDance API key input | planned | Settings > ByteDance | FG.08 |
| `app/store/access.ts` | `alibabaUrl` | `ProviderSettings.Alibaba.BaseUrl` | settings JSON | Alibaba endpoint text box | planned | Settings > Alibaba | FG.08 |
| `app/store/access.ts` | `alibabaApiKey` | `ProviderSettings.Alibaba.ApiKeyRef` | keychain | Alibaba API key input | planned | Settings > Alibaba | FG.08 |
| `app/store/access.ts` | `moonshotUrl` | `ProviderSettings.Moonshot.BaseUrl` | settings JSON | Moonshot endpoint text box | planned | Settings > Moonshot | FG.08 |
| `app/store/access.ts` | `moonshotApiKey` | `ProviderSettings.Moonshot.ApiKeyRef` | keychain | Moonshot API key input | planned | Settings > Moonshot | FG.08 |
| `app/store/access.ts` | `stabilityUrl` | `ImageProviderSettings.Stability.BaseUrl` | settings JSON | Stability endpoint text box | planned | Settings > Stability / SD | FG.08 |
| `app/store/access.ts` | `stabilityApiKey` | `ImageProviderSettings.Stability.ApiKeyRef` | keychain | Stability API key input | planned | Settings > Stability / SD | FG.08 |
| `app/store/access.ts` | `tencentUrl` | `ProviderSettings.Tencent.BaseUrl` | settings JSON | Tencent endpoint display/input | planned | Settings > Tencent | FG.08 |
| `app/store/access.ts` | `tencentSecretKey` | `ProviderSettings.Tencent.SecretKeyRef` | keychain | Tencent secret key input | planned | Settings > Tencent | FG.08 |
| `app/store/access.ts` | `tencentSecretId` | `ProviderSettings.Tencent.SecretIdRef` | keychain | Tencent secret id input | planned | Settings > Tencent | FG.08 |
| `app/store/access.ts` | `iflytekUrl` | `ProviderSettings.Iflytek.BaseUrl` | settings JSON | iFlytek endpoint text box | planned | Settings > iFlytek | FG.08 |
| `app/store/access.ts` | `iflytekApiKey` | `ProviderSettings.Iflytek.ApiKeyRef` | keychain | iFlytek API key input | planned | Settings > iFlytek | FG.08 |
| `app/store/access.ts` | `iflytekApiSecret` | `ProviderSettings.Iflytek.ApiSecretRef` | keychain | iFlytek API secret input | planned | Settings > iFlytek | FG.08 |
| `app/store/access.ts` | `deepseekUrl` | `ProviderSettings.DeepSeek.BaseUrl` | settings JSON | DeepSeek endpoint text box | planned | Settings > DeepSeek | FG.08 |
| `app/store/access.ts` | `deepseekApiKey` | `ProviderSettings.DeepSeek.ApiKeyRef` | keychain | DeepSeek API key input | planned | Settings > DeepSeek | FG.08 |
| `app/store/access.ts` | `xaiUrl` | `ProviderSettings.Xai.BaseUrl` | settings JSON | xAI endpoint text box | planned | Settings > xAI | FG.08 |
| `app/store/access.ts` | `xaiApiKey` | `ProviderSettings.Xai.ApiKeyRef` | keychain | xAI API key input | planned | Settings > xAI | FG.08 |
| `app/store/access.ts` | `chatglmUrl` | `ProviderSettings.Glm.BaseUrl` | settings JSON | ChatGLM endpoint text box | planned | Settings > ChatGLM | FG.08 |
| `app/store/access.ts` | `chatglmApiKey` | `ProviderSettings.Glm.ApiKeyRef` | keychain | ChatGLM API key input | planned | Settings > ChatGLM | FG.08 |
| `app/store/access.ts` | `siliconflowUrl` | `ProviderSettings.SiliconFlow.BaseUrl` | settings JSON | SiliconFlow endpoint text box | planned | Settings > SiliconFlow | FG.08 |
| `app/store/access.ts` | `siliconflowApiKey` | `ProviderSettings.SiliconFlow.ApiKeyRef` | keychain | SiliconFlow API key input | planned | Settings > SiliconFlow | FG.08 |
| `app/store/access.ts` | `ai302Url` | `ProviderSettings.Ai302.BaseUrl` | settings JSON | 302.AI endpoint text box | planned | Settings > 302.AI | FG.08 |
| `app/store/access.ts` | `ai302ApiKey` | `ProviderSettings.Ai302.ApiKeyRef` | keychain | 302.AI API key input | planned | Settings > 302.AI | FG.08 |
| `app/store/access.ts` | `needCode` | `AccessState.NeedCode` | settings JSON | Auth gate toggle/external proxy mode | planned | Auth | FG.08 |
| `app/store/access.ts` | `hideUserApiKey` | `AccessState.HideUserApiKey` | settings JSON | server-config compatibility only | planned | Settings > Access | FG.08 |
| `app/store/access.ts` | `hideBalanceQuery` | `AccessState.HideBalanceQuery` | settings JSON | usage query visibility | planned | Settings > Usage | FG.08 |
| `app/store/access.ts` | `disableGPT4` | `AccessState.DisableGpt4` | settings JSON | model availability compatibility | planned | Settings > Models | FG.08 |
| `app/store/access.ts` | `disableFastLink` | `AccessState.DisableFastLink` | settings JSON | cut or settings compatibility row | planned | Settings > Access | FG.08 |
| `app/store/access.ts` | `customModels` | `ProviderSettings.CustomModels` | settings JSON | custom models text box | planned | Settings > Models | FG.08 |
| `app/store/access.ts` | `defaultModel` | `ProviderSettings.DefaultModel` | settings JSON | default model selector | planned | Settings > Models | FG.08 |
| `app/store/access.ts` | `visionModels` | `ProviderSettings.VisionModelsOverride` | settings JSON | advanced model settings | planned | Settings > Models | FG.08 |
| `app/store/access.ts` | `edgeTTSVoiceName` | `TtsSettings.EdgeVoiceName` | settings JSON | TTS voice selector | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `lastUpdate` | `SettingsSnapshot.LastUpdate` | settings JSON | not directly bound | planned | Settings import/export | FG.08 |
| `app/store/config.ts` | `submitKey` | `UiSettings.SubmitKey` | settings JSON | send-key selector | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `avatar` | `UiSettings.Avatar` | settings JSON | avatar picker | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `fontSize` | `UiSettings.FontSize` | settings JSON | font-size slider | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `fontFamily` | `UiSettings.FontFamily` | settings JSON | font-family text box | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `theme` | `UiSettings.Theme` | settings JSON | theme selector | planned | Settings > Theme | FG.03 / FG.08 |
| `app/store/config.ts` | `tightBorder` | `UiSettings.TightBorder` | settings JSON | tight-border toggle | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `sendPreviewBubble` | `UiSettings.SendPreviewBubble` | settings JSON | preview-bubble toggle | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `enableAutoGenerateTitle` | `ConversationSettings.EnableAutoGenerateTitle` | settings JSON | auto-title toggle | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `sidebarWidth` | `UiSettings.SidebarWidth` | settings JSON | persisted shell width | planned | Shell | FG.03 |
| `app/store/config.ts` | `enableArtifacts` | `ConversationSettings.EnableArtifacts` | settings JSON | artifacts toggle | planned | Settings > App / Mask config | FG.07 / FG.08 |
| `app/store/config.ts` | `enableCodeFold` | `MarkdownSettings.EnableCodeFold` | settings JSON | code-fold toggle | planned | Settings > App / Mask config | FG.04 / FG.08 |
| `app/store/config.ts` | `disablePromptHint` | `PromptSettings.DisablePromptHint` | settings JSON | prompt hint toggle | planned | Settings > Prompt | FG.08 |
| `app/store/config.ts` | `dontShowMaskSplashScreen` | `MaskSettings.DontShowSplashScreen` | settings JSON | new-chat splash toggle | planned | New Chat | FG.04 / FG.08 |
| `app/store/config.ts` | `hideBuiltinMasks` | `MaskSettings.HideBuiltinMasks` | settings JSON | hide builtin masks toggle | planned | Settings > Masks | FG.08 |
| `app/store/config.ts` | `customModels` | `ProviderSettings.CustomModels` | settings JSON | custom models text box | planned | Settings > Models | FG.08 |
| `app/store/config.ts` | `models` | `ProviderSettings.Models` | settings JSON plus provider refresh cache | model list | planned | Settings > Models | FG.06 / FG.08 |
| `app/store/config.ts` | `modelConfig.model` | `ModelConfig.Model` | settings JSON and per-conversation override | model selector | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.providerName` | `ModelConfig.ProviderName` | settings JSON and per-conversation override | provider selector | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.temperature` | `ModelConfig.Temperature` | settings JSON and per-conversation override | temperature slider | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.top_p` | `ModelConfig.TopP` | settings JSON and per-conversation override | top-p slider | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.max_tokens` | `ModelConfig.MaxTokens` | settings JSON and per-conversation override | max tokens input | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.presence_penalty` | `ModelConfig.PresencePenalty` | settings JSON and per-conversation override | presence penalty slider | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.frequency_penalty` | `ModelConfig.FrequencyPenalty` | settings JSON and per-conversation override | frequency penalty slider | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.sendMemory` | `ModelConfig.SendMemory` | settings JSON and per-conversation override | memory toggle | planned | Model Config | FG.04 / FG.08 |
| `app/store/config.ts` | `modelConfig.historyMessageCount` | `ModelConfig.HistoryMessageCount` | settings JSON and per-conversation override | history count stepper | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.compressMessageLengthThreshold` | `ModelConfig.CompressMessageLengthThreshold` | settings JSON and per-conversation override | compress threshold input | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.compressModel` | `ModelConfig.CompressModel` | settings JSON and per-conversation override | summary model selector | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.compressProviderName` | `ModelConfig.CompressProviderName` | settings JSON and per-conversation override | summary provider selector | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.enableInjectSystemPrompts` | `ModelConfig.EnableInjectSystemPrompts` | settings JSON and per-conversation override | inject system prompt toggle | planned | Settings > App | FG.08 |
| `app/store/config.ts` | `modelConfig.template` | `ModelConfig.Template` | settings JSON and per-conversation override | input template text area | planned | Model Config | FG.08 |
| `app/store/config.ts` | `modelConfig.size` | `ImageModelConfig.Size` | settings JSON and per-conversation override | image size selector | planned | SD/Image settings | FG.07 / FG.08 |
| `app/store/config.ts` | `modelConfig.quality` | `ImageModelConfig.Quality` | settings JSON and per-conversation override | image quality selector | planned | SD/Image settings | FG.07 / FG.08 |
| `app/store/config.ts` | `modelConfig.style` | `ImageModelConfig.Style` | settings JSON and per-conversation override | image style selector | planned | SD/Image settings | FG.07 / FG.08 |
| `app/store/config.ts` | `ttsConfig.enable` | `TtsSettings.Enable` | settings JSON | TTS enable toggle | planned | Settings > TTS | FG.07 / FG.08 |
| `app/store/config.ts` | `ttsConfig.autoplay` | `TtsSettings.Autoplay` | settings JSON | TTS autoplay toggle | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `ttsConfig.engine` | `TtsSettings.Engine` | settings JSON | TTS engine selector | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `ttsConfig.model` | `TtsSettings.Model` | settings JSON | TTS model selector | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `ttsConfig.voice` | `TtsSettings.Voice` | settings JSON | TTS voice selector | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `ttsConfig.speed` | `TtsSettings.Speed` | settings JSON | TTS speed slider | planned | Settings > TTS | FG.08 |
| `app/store/config.ts` | `realtimeConfig.enable` | `RealtimeSettings.Enable` | settings JSON | realtime enable toggle | planned | Settings > Realtime | FG.07 / FG.08 |
| `app/store/config.ts` | `realtimeConfig.provider` | `RealtimeSettings.Provider` | settings JSON | realtime provider selector | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.model` | `RealtimeSettings.Model` | settings JSON | realtime model selector | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.apiKey` | `RealtimeSettings.ApiKeyRef` | keychain | realtime API key input | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.azure.endpoint` | `RealtimeSettings.Azure.Endpoint` | settings JSON | Azure realtime endpoint input | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.azure.deployment` | `RealtimeSettings.Azure.Deployment` | settings JSON | Azure realtime deployment input | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.temperature` | `RealtimeSettings.Temperature` | settings JSON | realtime temperature slider | planned | Settings > Realtime | FG.08 |
| `app/store/config.ts` | `realtimeConfig.voice` | `RealtimeSettings.Voice` | settings JSON | realtime voice selector | planned | Settings > Realtime | FG.08 |
| `app/store/chat.ts` | `sessions` | `ConversationSnapshot.Sessions` | SQLite conversations/messages plus import/export JSON | conversation list/detail | planned | Chat | FG.04 |
| `app/store/chat.ts` | `currentSessionIndex` | `ConversationUiState.CurrentSessionId` | settings JSON or local UI state | selected conversation | planned | Chat | FG.04 |
| `app/store/chat.ts` | `lastInput` | `ComposerState.LastInput` | local UI state/settings JSON | composer draft | planned | Chat composer | FG.04 |
| `app/store/mask.ts` | `masks` | `MaskSnapshot.Masks` | SQLite masks plus seed JSON | mask gallery/editor | planned | Masks | FG.04 |
| `app/store/mask.ts` | `language` | `MaskSnapshot.Language` | settings JSON | mask language filter | planned | Masks | FG.04 |
| `app/store/plugin.ts` | `plugins` | `PluginSnapshot.Plugins` | SQLite/plugin repository | plugin list/editor | planned | Plugins | FG.05 |
| `app/store/prompt.ts` | `counter` | `PromptSnapshot.Counter` | settings JSON or not persisted if only invalidation | not bound | planned | Prompt search | FG.04 |
| `app/store/prompt.ts` | `prompts` | `PromptSnapshot.Prompts` | SQLite/prompt repository | prompt list/editor | planned | Prompt search | FG.04 |
| `app/store/sd.ts` | `currentId` | `SdState.CurrentId` | SQLite/image-generation state | SD panel | planned | SD | FG.07 |
| `app/store/sd.ts` | `draw` | `SdState.DrawHistory` | SQLite image history | SD gallery | planned | SD | FG.07 |
| `app/store/sd.ts` | `currentModel` | `SdState.CurrentModel` | settings JSON | SD model selector | planned | SD | FG.07 |
| `app/store/sd.ts` | `currentParams` | `SdState.CurrentParams` | settings JSON | SD parameter form | planned | SD | FG.07 |
| `app/store/sync.ts` | `provider` | `SyncSettings.Provider` | settings JSON | sync provider selector | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `useProxy` | `SyncSettings.UseProxy` | settings JSON; desktop direct sync should default false after migration | sync proxy toggle | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `proxyUrl` | `SyncSettings.ProxyUrl` | settings JSON | sync proxy URL | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `webdav.endpoint` | `WebDavSettings.Endpoint` | settings JSON | WebDAV endpoint input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `webdav.username` | `WebDavSettings.Username` | settings JSON | WebDAV username input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `webdav.password` | `WebDavSettings.PasswordRef` | keychain | WebDAV password input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `upstash.endpoint` | `UpstashSettings.Endpoint` | settings JSON | Upstash endpoint input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `upstash.username` | `UpstashSettings.BackupName` | settings JSON | Upstash backup name input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `upstash.apiKey` | `UpstashSettings.ApiKeyRef` | keychain | Upstash token input | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `lastSyncTime` | `SyncState.LastSyncTime` | settings JSON/SQLite sync meta | sync status label | planned | Sync settings | FG.08 |
| `app/store/sync.ts` | `lastProvider` | `SyncState.LastProvider` | settings JSON/SQLite sync meta | sync status label | planned | Sync settings | FG.08 |
| `app/store/update.ts` | `versionType` | `UpdateSettings.VersionType` | settings JSON | update settings/internal | planned | Update banner | FG.08 |
| `app/store/update.ts` | `lastUpdate` | `UpdateSettings.LastUpdateCheck` | settings JSON | update status | planned | Update banner | FG.08 |
| `app/store/update.ts` | `version` | `UpdateSettings.CurrentVersion` | settings JSON/runtime info | update status | planned | Update banner | FG.08 |
| `app/store/update.ts` | `remoteVersion` | `UpdateSettings.RemoteVersion` | settings JSON | update status | planned | Update banner | FG.08 |
| `app/store/update.ts` | `used` | `UsageState.Used` | settings/cache | usage balance | planned | Settings > Usage | FG.08 |
| `app/store/update.ts` | `subscription` | `UsageState.Subscription` | settings/cache | usage balance | planned | Settings > Usage | FG.08 |
| `app/store/update.ts` | `lastUpdateUsage` | `UsageState.LastUpdateUsage` | settings/cache | usage balance | planned | Settings > Usage | FG.08 |
| `app/mcp/mcp_config.default.json` | `mcpServers` | `McpSettings.Servers` | seed JSON plus settings repository | MCP market/config | planned | MCP market | FG.05 |

## Cut Or Desktop-Reinterpreted Fields

No field is silently cut in NC00. Browser/server-only semantics are reinterpreted as desktop settings where needed: `useProxy` and `proxyUrl` are user-network proxy settings, not ArcChat server CORS proxy settings; `needCode`, `hideUserApiKey`, `hideBalanceQuery`, `disableGPT4`, and `disableFastLink` survive only for external proxy/server-compatible deployments.
