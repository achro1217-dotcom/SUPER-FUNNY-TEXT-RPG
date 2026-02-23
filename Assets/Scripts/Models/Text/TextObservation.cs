public readonly struct TextObservation
{
    public int MentalState { get; }
    public int StepsSinceText { get; }
    public float OpenRatioRecent { get; }
    public float WallContactRecent { get; }
    public int NewTilesRecent { get; }
    public float BacktrackRecent { get; }
    public float DepthNorm { get; }

    public TextObservation(
        int mentalState,
        int stepsSinceText,
        float openRatioRecent,
        float wallContactRecent,
        int newTilesRecent,
        float backtrackRecent,
        float depthNorm)
    {
        MentalState = mentalState;
        StepsSinceText = stepsSinceText;
        OpenRatioRecent = openRatioRecent;
        WallContactRecent = wallContactRecent;
        NewTilesRecent = newTilesRecent;
        BacktrackRecent = backtrackRecent;
        DepthNorm = depthNorm;
    }
}
