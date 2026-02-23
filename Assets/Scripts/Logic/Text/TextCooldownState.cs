using System.Collections.Generic;

public sealed class TextCooldownState
{
    private readonly Dictionary<string, int> _cooldownRemaining;

    public TextCooldownState()
    {
        _cooldownRemaining = new Dictionary<string, int>();
    }

    public IReadOnlyDictionary<string, int> CooldownRemaining => _cooldownRemaining;

    public void AdvanceTurn()
    {
        var keys = new List<string>(_cooldownRemaining.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            int next = _cooldownRemaining[key] - 1;
            if (next <= 0)
            {
                _cooldownRemaining.Remove(key);
                continue;
            }

            _cooldownRemaining[key] = next;
        }
    }

    public bool IsOnCooldown(string lineId)
    {
        return _cooldownRemaining.TryGetValue(lineId, out int remaining) && remaining > 0;
    }

    public void Apply(TextLine line)
    {
        _cooldownRemaining[line.Id] = line.CooldownTurns;
    }
}
