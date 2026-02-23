using System;
using System.Globalization;
using Newtonsoft.Json;

public sealed class TextLine
{
    public string Id { get; set; }
    public int Weight { get; set; }
    public int CooldownTurns { get; set; }
    public string Text { get; set; }

    public string Mental { get; set; }
    public string StepsSinceText { get; set; }
    public string OpenRatioRecent { get; set; }
    public string WallContactRecent { get; set; }
    public string NewTilesRecent { get; set; }
    public string BacktrackRecent { get; set; }
    public string DepthNorm { get; set; }

    [JsonIgnore] public NumericRange? MentalRange { get; private set; }
    [JsonIgnore] public NumericRange? StepsSinceTextRange { get; private set; }
    [JsonIgnore] public NumericRange? OpenRatioRecentRange { get; private set; }
    [JsonIgnore] public NumericRange? WallContactRecentRange { get; private set; }
    [JsonIgnore] public NumericRange? NewTilesRecentRange { get; private set; }
    [JsonIgnore] public NumericRange? BacktrackRecentRange { get; private set; }
    [JsonIgnore] public NumericRange? DepthNormRange { get; private set; }
    [JsonIgnore] public bool IsConditionCacheReady { get; private set; }

    public void BuildConditionCache()
    {
        MentalRange = ParseRangeOrNull(Mental);
        StepsSinceTextRange = ParseRangeOrNull(StepsSinceText);
        OpenRatioRecentRange = ParseRangeOrNull(OpenRatioRecent);
        WallContactRecentRange = ParseRangeOrNull(WallContactRecent);
        NewTilesRecentRange = ParseRangeOrNull(NewTilesRecent);
        BacktrackRecentRange = ParseRangeOrNull(BacktrackRecent);
        DepthNormRange = ParseRangeOrNull(DepthNorm);
        IsConditionCacheReady = true;
    }

    public bool IsMatch(in TextObservation observation)
    {
        if (!IsConditionCacheReady)
        {
            throw new InvalidOperationException($"TextLine '{Id ?? "(null)"}' condition cache is not ready. BuildConditionCache() must be called first.");
        }

        return IsInRange(MentalRange, observation.MentalState)
            && IsInRange(StepsSinceTextRange, observation.StepsSinceText)
            && IsInRange(OpenRatioRecentRange, observation.OpenRatioRecent)
            && IsInRange(WallContactRecentRange, observation.WallContactRecent)
            && IsInRange(NewTilesRecentRange, observation.NewTilesRecent)
            && IsInRange(BacktrackRecentRange, observation.BacktrackRecent)
            && IsInRange(DepthNormRange, observation.DepthNorm);
    }

    private static bool IsInRange(NumericRange? range, int value)
    {
        return IsInRange(range, (float)value);
    }

    private static bool IsInRange(NumericRange? range, float value)
    {
        if (!range.HasValue)
        {
            return true;
        }

        return value >= range.Value.Min && value <= range.Value.Max;
    }

    private static NumericRange? ParseRangeOrNull(string conditionText)
    {
        if (string.IsNullOrWhiteSpace(conditionText))
        {
            return null;
        }

        string[] tokens = conditionText.Split('-');
        if (tokens.Length != 2)
        {
            throw new FormatException($"Condition range must be 'min-max': {conditionText}");
        }

        if (!float.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float min))
        {
            throw new FormatException($"Condition min is invalid: {conditionText}");
        }

        if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float max))
        {
            throw new FormatException($"Condition max is invalid: {conditionText}");
        }

        if (min > max)
        {
            throw new FormatException($"Condition range min cannot exceed max: {conditionText}");
        }

        return new NumericRange(min, max);
    }
}

public readonly struct NumericRange
{
    public float Min { get; }
    public float Max { get; }

    public NumericRange(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
