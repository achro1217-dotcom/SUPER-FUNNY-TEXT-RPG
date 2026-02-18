using System;

public readonly struct DungeonPoint : IEquatable<DungeonPoint>
{
    public int X { get; }
    public int Y { get; }

    public DungeonPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int ToIndex(int width)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        return (Y * width) + X;
    }

    public bool Equals(DungeonPoint other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is DungeonPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public static bool operator ==(DungeonPoint left, DungeonPoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DungeonPoint left, DungeonPoint right)
    {
        return !(left == right);
    }

}

public enum DungeonFogState
{
    Unknown = 0,
    Explored = 1,
    Visible = 2,
}

public enum DungeonCellType
{
    Empty = 0,
    Wall = 1,
    EnemySpawner = 2,
    Loot = 3,
    Information = 4,
    Rest = 5,
    Event = 6,
    Start = 7,
    EscapeAnchor = 8,
}

public enum DungeonEncounterTier
{
    Normal = 0,
    Elite = 1,
    Boss = 2,
}

public enum DungeonLootTier
{
    Normal = 0,
    High = 1,
}
