public abstract class DungeonPoiRuntimeData
{
}

public sealed class DungeonEncounterPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsCleared { get; set; }
    public bool IsAlive { get; set; }

    public DungeonEncounterPoiRuntimeData(bool isAlive = true)
    {
        IsAlive = isAlive;
        IsCleared = !isAlive;
    }
}

public sealed class DungeonLootPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsOpened { get; set; }
    public bool IsConsumed { get; set; }
}

public sealed class DungeonInformationPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsUsed { get; set; }
}

public sealed class DungeonRestPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsUsed { get; set; }
}

public sealed class DungeonEventPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsConsumed { get; set; }
}

public sealed class DungeonStartPoiRuntimeData : DungeonPoiRuntimeData
{
}

public sealed class DungeonEscapeAnchorPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsActivated { get; set; }

    public DungeonEscapeAnchorPoiRuntimeData(bool isActivated = true)
    {
        IsActivated = isActivated;
    }
}

public sealed class DungeonExitPoiRuntimeData : DungeonPoiRuntimeData
{
    public bool IsUnlocked { get; set; }

    public DungeonExitPoiRuntimeData(bool isUnlocked = true)
    {
        IsUnlocked = isUnlocked;
    }
}
