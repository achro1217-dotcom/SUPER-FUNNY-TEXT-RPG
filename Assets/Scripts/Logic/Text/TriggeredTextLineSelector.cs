using System;
using System.Collections.Generic;

public sealed class TriggeredTextLineSelector
{
    private readonly Dictionary<TextTriggerType, List<TextLine>> _linesByTrigger;
    private readonly Random _random;

    public TriggeredTextLineSelector(
        IReadOnlyList<TextLine> allLines,
        IReadOnlyList<TextTriggerBinding> bindings,
        Random random = null)
    {
        if (allLines == null)
        {
            throw new ArgumentNullException(nameof(allLines));
        }

        if (bindings == null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        _linesByTrigger = BuildLinesByTrigger(allLines, bindings);
        _random = random ?? new Random();
    }

    public TextLine SelectTextLine(TextTriggerType triggerType, TextObservation observation, TextCooldownState cooldownState)
    {
        if (!_linesByTrigger.TryGetValue(triggerType, out List<TextLine> lines))
        {
            return null;
        }

        return TextLineSelectionRules.SelectWeightedMatch(lines, observation, cooldownState, _random);
    }

    private static Dictionary<TextTriggerType, List<TextLine>> BuildLinesByTrigger(
        IReadOnlyList<TextLine> allLines,
        IReadOnlyList<TextTriggerBinding> bindings)
    {
        var lineById = new Dictionary<string, TextLine>(StringComparer.Ordinal);
        for (int i = 0; i < allLines.Count; i++)
        {
            TextLine line = allLines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.Id))
            {
                continue;
            }

            lineById[line.Id] = line;
        }

        var result = new Dictionary<TextTriggerType, List<TextLine>>();

        for (int i = 0; i < bindings.Count; i++)
        {
            TextTriggerBinding binding = bindings[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.TextLineId))
            {
                continue;
            }

            if (!lineById.TryGetValue(binding.TextLineId, out TextLine line))
            {
                continue;
            }

            if (!result.TryGetValue(binding.TriggerType, out List<TextLine> bucket))
            {
                bucket = new List<TextLine>();
                result[binding.TriggerType] = bucket;
            }

            bucket.Add(line);
        }

        return result;
    }
}
