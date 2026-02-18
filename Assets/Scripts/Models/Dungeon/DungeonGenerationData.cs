using System;
using System.Collections.Generic;

public sealed class DungeonGenerationSeedData
{
    public int RunSeed { get; }
    public int FloorSeed { get; }

    public DungeonGenerationSeedData(int runSeed, int floorSeed)
    {
        RunSeed = runSeed;
        FloorSeed = floorSeed;
    }
}

public sealed class DungeonConnectivityData
{
    public bool IsExitReachable { get; }
    public int ReachableCellCount { get; }
    public int TotalWalkableCellCount { get; }

    public DungeonConnectivityData(bool isExitReachable, int reachableCellCount, int totalWalkableCellCount)
    {
        if (reachableCellCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reachableCellCount));
        }

        if (totalWalkableCellCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalWalkableCellCount));
        }

        if (reachableCellCount > totalWalkableCellCount)
        {
            throw new ArgumentException("Reachable cell count cannot exceed total walkable cell count.");
        }

        IsExitReachable = isExitReachable;
        ReachableCellCount = reachableCellCount;
        TotalWalkableCellCount = totalWalkableCellCount;
    }
}

public sealed class DungeonDistanceMapData
{
    public DungeonPoint OriginPoint { get; }
    public IReadOnlyList<int> Distances { get; }

    public DungeonDistanceMapData(DungeonPoint originPoint, IReadOnlyList<int> distances, int expectedCellCount)
    {
        if (distances == null)
        {
            throw new ArgumentNullException(nameof(distances));
        }

        if (expectedCellCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCellCount));
        }

        if (distances.Count != expectedCellCount)
        {
            throw new ArgumentException("Distance map cell count must match map cell count.", nameof(distances));
        }

        OriginPoint = originPoint;
        Distances = distances;
    }
}

public sealed class DungeonGenerationResultData
{
    public DungeonGenerationSeedData Seed { get; }
    public DungeonConnectivityData Connectivity { get; }
    public DungeonDistanceMapData DistanceMap { get; }
    public IReadOnlyList<DungeonPoint> DeadEndPoints { get; }
    public IReadOnlyList<DungeonPoint> BranchPoints { get; }

    public DungeonGenerationResultData(
        DungeonGenerationSeedData seed,
        DungeonConnectivityData connectivity,
        DungeonDistanceMapData distanceMap,
        IReadOnlyList<DungeonPoint> deadEndPoints,
        IReadOnlyList<DungeonPoint> branchPoints)
    {
        Seed = seed ?? throw new ArgumentNullException(nameof(seed));
        Connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        DistanceMap = distanceMap ?? throw new ArgumentNullException(nameof(distanceMap));
        DeadEndPoints = deadEndPoints ?? throw new ArgumentNullException(nameof(deadEndPoints));
        BranchPoints = branchPoints ?? throw new ArgumentNullException(nameof(branchPoints));
    }
}
