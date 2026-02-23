using System;
using System.Collections.Generic;

public sealed class AmbientTextLineSelector
{
    private readonly IReadOnlyList<TextLine> _lines;
    private readonly Random _random;

    public AmbientTextLineSelector(IReadOnlyList<TextLine> lines, Random random = null)
    {
        _lines = lines ?? throw new ArgumentNullException(nameof(lines));
        _random = random ?? new Random();
    }

    public TextLine SelectTextLine(TextObservation observation, TextCooldownState cooldownState)
    {
        return TextLineSelectionRules.SelectWeightedMatch(_lines, observation, cooldownState, _random);
    }
}

