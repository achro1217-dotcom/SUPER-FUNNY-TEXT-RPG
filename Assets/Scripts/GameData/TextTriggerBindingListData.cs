using System.Collections.Generic;

public sealed class TextTriggerBindingListData : IdIndexedListData<TextTriggerBinding>
{
    private readonly Dictionary<TextTriggerType, List<TextTriggerBinding>> _bindingsByTriggerType;

    public IReadOnlyList<TextTriggerBinding> Bindings => Items;

    public TextTriggerBindingListData(IReadOnlyList<TextTriggerBinding> bindings)
        : base(bindings, binding => binding.Id, "TextTriggerBinding")
    {
        _bindingsByTriggerType = BuildBindingsByTriggerType(bindings);
    }

    public IReadOnlyList<TextTriggerBinding> GetByTriggerType(TextTriggerType triggerType)
    {
        if (_bindingsByTriggerType.TryGetValue(triggerType, out List<TextTriggerBinding> bucket))
        {
            return bucket;
        }

        return EmptyReadOnlyList<TextTriggerBinding>.Instance;
    }

    private static Dictionary<TextTriggerType, List<TextTriggerBinding>> BuildBindingsByTriggerType(IReadOnlyList<TextTriggerBinding> bindings)
    {
        var bindingsByTriggerType = new Dictionary<TextTriggerType, List<TextTriggerBinding>>();

        for (int i = 0; i < bindings.Count; i++)
        {
            TextTriggerBinding binding = bindings[i];
            if (binding == null)
            {
                continue;
            }

            if (!bindingsByTriggerType.TryGetValue(binding.TriggerType, out List<TextTriggerBinding> bucket))
            {
                bucket = new List<TextTriggerBinding>();
                bindingsByTriggerType[binding.TriggerType] = bucket;
            }

            bucket.Add(binding);
        }

        return bindingsByTriggerType;
    }
}

public static class EmptyReadOnlyList<T>
{
    public static IReadOnlyList<T> Instance { get; } = new T[0];
}
