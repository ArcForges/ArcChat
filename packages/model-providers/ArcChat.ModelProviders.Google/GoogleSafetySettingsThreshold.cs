// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.ModelProviders.Google;

/// <summary>
/// Google Gemini safety filtering threshold mapped from NextChat GoogleSafetySettingsThreshold.
/// </summary>
public enum GoogleSafetySettingsThreshold
{
    /// <summary>
    /// Always show regardless of unsafe-content probability.
    /// </summary>
    BlockNone = 0,

    /// <summary>
    /// Block when unsafe-content probability is high.
    /// </summary>
    BlockOnlyHigh = 1,

    /// <summary>
    /// Block when unsafe-content probability is medium or high.
    /// </summary>
    BlockMediumAndAbove = 2,

    /// <summary>
    /// Block when unsafe-content probability is low, medium, or high.
    /// </summary>
    BlockLowAndAbove = 3,
}
