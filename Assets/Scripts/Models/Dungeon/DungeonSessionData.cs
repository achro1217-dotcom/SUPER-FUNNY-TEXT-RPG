using System;

public sealed class DungeonSessionData
{
    public DungeonMapData StaticMap { get; }
    public DungeonMapRuntimeData RuntimeMap { get; }
    public DungeonGenerationResultData Generation { get; }

    public DungeonSessionData(
        DungeonMapData staticMap,
        DungeonMapRuntimeData runtimeMap,
        DungeonGenerationResultData generation)
    {
        if (staticMap == null)
        {
            throw new ArgumentNullException(nameof(staticMap));
        }

        if (runtimeMap == null)
        {
            throw new ArgumentNullException(nameof(runtimeMap));
        }

        if (generation == null)
        {
            throw new ArgumentNullException(nameof(generation));
        }

        if (staticMap.Cells.Count != runtimeMap.Cells.Count)
        {
            throw new ArgumentException("Static/runtime map size mismatch.");
        }

        StaticMap = staticMap;
        RuntimeMap = runtimeMap;
        Generation = generation;
    }
}
