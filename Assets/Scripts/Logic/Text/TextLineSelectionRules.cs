using System;
using System.Collections.Generic;

public static class TextLineSelectionRules
{
    public static TextLine SelectWeightedMatch(
        IReadOnlyList<TextLine> lines,
        TextObservation observation,
        TextCooldownState cooldownState,
        Random random)
    {
        if (lines == null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (cooldownState == null)
        {
            throw new ArgumentNullException(nameof(cooldownState));
        }

        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        var candidates = new List<TextLine>();
        int totalWeight = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            TextLine line = lines[i];
            if (!CanUseLine(line))
            {
                continue;
            }

            if (cooldownState.IsOnCooldown(line.Id))
            {
                continue;
            }

            if (!line.IsMatch(observation))
            {
                continue;
            }

            candidates.Add(line);
            totalWeight += line.Weight;
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return PickWeightedRandom(candidates, totalWeight, random);
    }

    private static bool CanUseLine(TextLine line)
    {
        if (line == null)
        {
            return false;
        }

        return line.Weight > 0
            && line.CooldownTurns >= 0
            && !string.IsNullOrWhiteSpace(line.Id)
            && !string.IsNullOrWhiteSpace(line.Text);
    }

    private static TextLine PickWeightedRandom(IReadOnlyList<TextLine> candidates, int totalWeight, Random random)
    {
        int roll = random.Next(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            TextLine candidate = candidates[i];
            cumulativeWeight += candidate.Weight;

            if (roll < cumulativeWeight)
            {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }
}
