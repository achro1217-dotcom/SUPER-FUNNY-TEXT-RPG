using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public abstract class DungeonCellData
{
    public DungeonCellType CellType { get; }
    [JsonIgnore] public bool IsWalkable { get; }
    [JsonIgnore] public bool BlocksVision { get; }

    protected DungeonCellData(
        DungeonCellType cellType,
        bool isWalkable,
        bool blocksVision)
    {
        CellType = cellType;
        IsWalkable = isWalkable;
        BlocksVision = blocksVision;
    }
}

public sealed class DungeonEmptyCellData : DungeonCellData
{
    public DungeonEmptyCellData()
        : base(DungeonCellType.Empty, true, false)
    {
    }
}

public sealed class DungeonWallCellData : DungeonCellData
{
    public DungeonWallCellData()
        : base(DungeonCellType.Wall, false, true)
    {
    }
}

public sealed class DungeonEncounterCellData : DungeonCellData
{
    public string EncounterDefId { get; }
    public DungeonEncounterTier Tier { get; }
    public bool IsMobile { get; }

    public DungeonEncounterCellData(string encounterDefId, DungeonEncounterTier tier, bool isMobile)
        : base(DungeonCellType.EnemySpawner, true, false)
    {
        if (string.IsNullOrWhiteSpace(encounterDefId))
        {
            throw new ArgumentException("Encounter definition id is required.", nameof(encounterDefId));
        }

        EncounterDefId = encounterDefId;
        Tier = tier;
        IsMobile = isMobile;
    }
}

public sealed class DungeonLootCellData : DungeonCellData
{
    public string LootDefId { get; }
    public DungeonLootTier LootTier { get; }
    public bool RequiresEliteClear { get; }

    public DungeonLootCellData(string lootDefId, DungeonLootTier lootTier, bool requiresEliteClear = false)
        : base(DungeonCellType.Loot, true, false)
    {
        if (string.IsNullOrWhiteSpace(lootDefId))
        {
            throw new ArgumentException("Loot definition id is required.", nameof(lootDefId));
        }

        LootDefId = lootDefId;
        LootTier = lootTier;
        RequiresEliteClear = requiresEliteClear;
    }
}

public sealed class DungeonInformationCellData : DungeonCellData
{
    public string InfoDefId { get; }

    public DungeonInformationCellData(string infoDefId)
        : base(DungeonCellType.Information, true, false)
    {
        if (string.IsNullOrWhiteSpace(infoDefId))
        {
            throw new ArgumentException("Information definition id is required.", nameof(infoDefId));
        }

        InfoDefId = infoDefId;
    }
}

public sealed class DungeonRestCellData : DungeonCellData
{
    public string RestDefId { get; }

    public DungeonRestCellData(string restDefId)
        : base(DungeonCellType.Rest, true, false)
    {
        if (string.IsNullOrWhiteSpace(restDefId))
        {
            throw new ArgumentException("Rest definition id is required.", nameof(restDefId));
        }

        RestDefId = restDefId;
    }
}

public sealed class DungeonEventCellData : DungeonCellData
{
    public string EventDefId { get; }

    public DungeonEventCellData(string eventDefId)
        : base(DungeonCellType.Event, true, false)
    {
        if (string.IsNullOrWhiteSpace(eventDefId))
        {
            throw new ArgumentException("Event definition id is required.", nameof(eventDefId));
        }

        EventDefId = eventDefId;
    }
}

public sealed class DungeonStartCellData : DungeonCellData
{
    public DungeonStartCellData()
        : base(DungeonCellType.Start, true, false)
    {
    }
}

public sealed class DungeonEscapeAnchorCellData : DungeonCellData
{
    public DungeonEscapeAnchorCellData()
        : base(DungeonCellType.EscapeAnchor, true, false)
    {
    }
}

public sealed class DungeonCellRuntimeData
{
    public DungeonFogState FogState { get; set; }
    public DungeonPoiRuntimeData Poi { get; set; }

    public DungeonCellRuntimeData(DungeonFogState fogState = DungeonFogState.Unknown, DungeonPoiRuntimeData poi = null)
    {
        FogState = fogState;
        Poi = poi;
    }
}

public sealed class DungeonMapData
{
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<DungeonCellData> Cells { get; }

    public DungeonMapData(
        int width,
        int height,
        IReadOnlyList<DungeonCellData> cells)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (cells.Count != width * height)
        {
            throw new ArgumentException("Cell count must equal width * height.", nameof(cells));
        }

        Width = width;
        Height = height;
        Cells = cells;
    }
}

public sealed class DungeonMapRuntimeData
{
    public IReadOnlyList<DungeonCellRuntimeData> Cells { get; }

    public DungeonMapRuntimeData(IReadOnlyList<DungeonCellRuntimeData> cells, int expectedCellCount)
    {
        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (expectedCellCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCellCount));
        }

        if (cells.Count != expectedCellCount)
        {
            throw new ArgumentException("Runtime cell count must match static map cell count.", nameof(cells));
        }

        Cells = cells;
    }
}
