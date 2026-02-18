using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class PrototypeDungeonSceneController : MonoBehaviour
{
    [SerializeField] private PrototypeDungeonView _dungeonView;
    [SerializeField] private PrototypeDungeonEntityView _entityView;
    [SerializeField] private Camera _inputCamera;
    [SerializeField] private string _defaultDungeonJsonPath = "GameData/Dungeon/proto-dungeon.json";
    [SerializeField] private bool _loadOnStart = true;

    public DungeonSessionData CurrentSession { get; private set; }
    public DungeonPoint PlayerPoint { get; private set; }
    public int TurnCount { get; private set; }

    private void Start()
    {
        if (_loadOnStart)
        {
            LoadDefaultSession();
        }
    }

    private void Update()
    {
        if (CurrentSession == null)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!TryGetClickedCellPoint(out DungeonPoint clickedPoint))
        {
            return;
        }

        if (!TryMovePlayerTowards(clickedPoint))
        {
            return;
        }

        TurnCount++;
        RecalculateFog();
        RenderSession();
    }

    public void LoadDefaultSession()
    {
        LoadSessionFromJsonPath(_defaultDungeonJsonPath);
    }

    public void LoadSessionFromJsonPath(string relativeJsonPath)
    {
        if (string.IsNullOrWhiteSpace(relativeJsonPath))
        {
            throw new ArgumentException("Json path is required.", nameof(relativeJsonPath));
        }

        EnsureView();
        string normalizedPath = relativeJsonPath.Replace('\\', '/');
        string assetRelativePath = normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath
            : $"Assets/{normalizedPath}";

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetRelativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Dungeon json file not found: {assetRelativePath}", fullPath);
        }

        DungeonMapData mapData = DungeonMapDataJsonSerializer.LoadFromFile(fullPath);
        LoadSession(CreateSessionFromMap(mapData));
    }

    public void LoadSession(DungeonSessionData sessionData)
    {
        if (sessionData == null)
        {
            throw new ArgumentNullException(nameof(sessionData));
        }

        EnsureView();
        CurrentSession = sessionData;
        PlayerPoint = FindStartPointOrCenter(CurrentSession.StaticMap);
        TurnCount = 0;
        ResetFogToUnknown();
        RecalculateFog();
        RenderSession();
    }

    private void EnsureView()
    {
        if (_dungeonView != null)
        {
            EnsureEntityView();
            EnsureInputCamera();
            return;
        }

        _dungeonView = GetComponentInChildren<PrototypeDungeonView>(true);
        if (_dungeonView == null)
        {
            throw new InvalidOperationException("PrototypeDungeonView reference is required.");
        }

        EnsureEntityView();
        EnsureInputCamera();
    }

    private void RenderSession()
    {
        _dungeonView.Render(CurrentSession.StaticMap, CurrentSession.RuntimeMap, PlayerPoint);
        RenderPlayerEntity();
    }

    private void EnsureEntityView()
    {
        if (_entityView != null)
        {
            return;
        }

        _entityView = GetComponentInChildren<PrototypeDungeonEntityView>(true);
        if (_entityView == null)
        {
            throw new InvalidOperationException("PrototypeDungeonEntityView reference is required.");
        }
    }

    private void RenderPlayerEntity()
    {
        if (_entityView == null)
        {
            return;
        }

        if (!_dungeonView.TryGetLocalPositionForCell(PlayerPoint, out Vector3 localPosition))
        {
            _entityView.HidePlayer();
            return;
        }

        bool isVisible = _dungeonView.GetFogStateAt(PlayerPoint) == DungeonFogState.Visible;
        _entityView.RenderPlayer(localPosition, isVisible);
    }

    private void EnsureInputCamera()
    {
        if (_inputCamera != null)
        {
            return;
        }

        _inputCamera = Camera.main;
    }

    private bool TryGetClickedCellPoint(out DungeonPoint point)
    {
        point = default;
        if (_inputCamera == null)
        {
            return false;
        }

        return _dungeonView.TryGetCellPointFromScreenPosition(_inputCamera, Input.mousePosition, out point);
    }

    private bool TryMovePlayerTowards(DungeonPoint targetPoint)
    {
        int deltaX = targetPoint.X - PlayerPoint.X;
        int deltaY = targetPoint.Y - PlayerPoint.Y;
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }

        int stepX = Math.Sign(deltaX);
        int stepY = Math.Sign(deltaY);
        bool preferY = Mathf.Abs(deltaY) >= Mathf.Abs(deltaX);

        if (preferY)
        {
            if (TryMovePlayerByStep(0, stepY))
            {
                return true;
            }

            return TryMovePlayerByStep(stepX, 0);
        }

        if (TryMovePlayerByStep(stepX, 0))
        {
            return true;
        }

        return TryMovePlayerByStep(0, stepY);
    }

    private bool TryMovePlayerByStep(int stepX, int stepY)
    {
        if (stepX == 0 && stepY == 0)
        {
            return false;
        }

        int nextX = PlayerPoint.X + stepX;
        int nextY = PlayerPoint.Y + stepY;
        if (nextX < 0 || nextY < 0 || nextX >= CurrentSession.StaticMap.Width || nextY >= CurrentSession.StaticMap.Height)
        {
            return false;
        }

        var nextPoint = new DungeonPoint(nextX, nextY);
        int index = nextPoint.ToIndex(CurrentSession.StaticMap.Width);
        if (!CurrentSession.StaticMap.Cells[index].IsWalkable)
        {
            return false;
        }

        PlayerPoint = nextPoint;
        return true;
    }

    private void ResetFogToUnknown()
    {
        IReadOnlyList<DungeonCellRuntimeData> runtimeCells = CurrentSession.RuntimeMap.Cells;
        for (int i = 0; i < runtimeCells.Count; i++)
        {
            runtimeCells[i].FogState = DungeonFogState.Unknown;
        }
    }

    private void RecalculateFog()
    {
        IReadOnlyList<DungeonCellRuntimeData> runtimeCells = CurrentSession.RuntimeMap.Cells;
        for (int i = 0; i < runtimeCells.Count; i++)
        {
            if (runtimeCells[i].FogState == DungeonFogState.Visible)
            {
                runtimeCells[i].FogState = DungeonFogState.Explored;
            }
        }

        int width = CurrentSession.StaticMap.Width;
        int height = CurrentSession.StaticMap.Height;
        for (int deltaY = -1; deltaY <= 1; deltaY++)
        {
            for (int deltaX = -1; deltaX <= 1; deltaX++)
            {
                int x = PlayerPoint.X + deltaX;
                int y = PlayerPoint.Y + deltaY;
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    continue;
                }

                var point = new DungeonPoint(x, y);
                runtimeCells[point.ToIndex(width)].FogState = DungeonFogState.Visible;
            }
        }
    }
    private static DungeonPoint FindStartPointOrCenter(DungeonMapData mapData)
    {
        int width = mapData.Width;
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            if (mapData.Cells[i].CellType != DungeonCellType.Start)
            {
                continue;
            }

            return new DungeonPoint(i % width, i / width);
        }

        return new DungeonPoint(mapData.Width / 2, mapData.Height / 2);
    }

    private static DungeonSessionData CreateSessionFromMap(DungeonMapData staticMap)
    {
        if (staticMap == null)
        {
            throw new ArgumentNullException(nameof(staticMap));
        }

        List<DungeonCellRuntimeData> runtimeCells = BuildRuntimeCells(staticMap.Cells);
        var runtimeMap = new DungeonMapRuntimeData(runtimeCells, runtimeCells.Count);
        var generation = BuildDefaultGeneration(staticMap.Cells.Count);
        return new DungeonSessionData(staticMap, runtimeMap, generation);
    }

    private static List<DungeonCellRuntimeData> BuildRuntimeCells(IReadOnlyList<DungeonCellData> staticCells)
    {
        var runtimeCells = new List<DungeonCellRuntimeData>(staticCells.Count);
        for (int i = 0; i < staticCells.Count; i++)
        {
            runtimeCells.Add(new DungeonCellRuntimeData(DungeonFogState.Unknown, CreatePoiRuntimeData(staticCells[i].CellType)));
        }

        return runtimeCells;
    }

    private static DungeonPoiRuntimeData CreatePoiRuntimeData(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.EnemySpawner:
                return new DungeonEncounterPoiRuntimeData(isAlive: true);
            case DungeonCellType.Loot:
                return new DungeonLootPoiRuntimeData();
            case DungeonCellType.Information:
                return new DungeonInformationPoiRuntimeData();
            case DungeonCellType.Rest:
                return new DungeonRestPoiRuntimeData();
            case DungeonCellType.Event:
                return new DungeonEventPoiRuntimeData();
            case DungeonCellType.Start:
                return new DungeonStartPoiRuntimeData();
            case DungeonCellType.EscapeAnchor:
                return new DungeonEscapeAnchorPoiRuntimeData(isActivated: true);
            default:
                return null;
        }
    }

    private static DungeonGenerationResultData BuildDefaultGeneration(int cellCount)
    {
        var distances = new List<int>(cellCount);
        for (int i = 0; i < cellCount; i++)
        {
            distances.Add(-1);
        }

        var seed = new DungeonGenerationSeedData(0, 0);
        var connectivity = new DungeonConnectivityData(false, 0, 0);
        var distanceMap = new DungeonDistanceMapData(new DungeonPoint(0, 0), distances, cellCount);
        var deadEnds = new List<DungeonPoint>();
        var branches = new List<DungeonPoint>();
        return new DungeonGenerationResultData(seed, connectivity, distanceMap, deadEnds, branches);
    }
}
