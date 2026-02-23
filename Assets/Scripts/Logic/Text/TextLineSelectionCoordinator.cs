using System;
using System.Collections.Generic;

public sealed class TextLineSelectionCoordinator
{
    private readonly TriggeredTextLineSelector _triggeredSelector;
    private readonly AmbientTextLineSelector _ambientSelector;
    private readonly TextCooldownState _cooldownState;

    public TextLineSelectionCoordinator(
        IReadOnlyList<TextLine> lines,
        IReadOnlyList<TextTriggerBinding> triggerBindings,
        Random ambientRandom = null,
        Random triggerRandom = null)
    {
        if (lines == null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (triggerBindings == null)
        {
            throw new ArgumentNullException(nameof(triggerBindings));
        }

        _triggeredSelector = new TriggeredTextLineSelector(lines, triggerBindings, triggerRandom);
        _ambientSelector = new AmbientTextLineSelector(lines, ambientRandom);
        _cooldownState = new TextCooldownState();
    }

    public IReadOnlyDictionary<string, int> CooldownRemaining => _cooldownState.CooldownRemaining;

    public TextLine SelectTextLine(TextObservation observation, TextTriggerType? triggerType = null)
    {
        _cooldownState.AdvanceTurn();

        if (triggerType.HasValue)
        {
            TextLine triggeredLine = _triggeredSelector.SelectTextLine(triggerType.Value, observation, _cooldownState);
            if (triggeredLine != null)
            {
                _cooldownState.Apply(triggeredLine);
                return triggeredLine;
            }
        }

        TextLine ambientLine = _ambientSelector.SelectTextLine(observation, _cooldownState);
        if (ambientLine != null)
        {
            _cooldownState.Apply(ambientLine);
            return ambientLine;
        }

        return null;
    }
}
