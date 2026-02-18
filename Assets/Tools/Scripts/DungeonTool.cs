using System;
using System.Collections.Generic;
using UnityEngine;

public enum DungeonToolMode
{
    Draw = 0,
    Edit = 1,
}

public sealed class DungeonTool : MonoBehaviour
{
    public static DungeonTool Instance { get; private set; }
    public static event Action ToolStateChanged;

    [SerializeField] private DungeonToolMapView _mapView;
    [SerializeField] private Camera _toolCamera;
    [SerializeField] private float _cameraMoveSpeed = 8f;

    public DungeonMapData CurrentMap { get; private set; }
    public DungeonToolMode Mode { get; private set; } = DungeonToolMode.Draw;
    public DungeonCellType DrawBrushCellType { get; private set; } = DungeonCellType.Wall;
    public DungeonPoint? SelectedCellPoint { get; private set; }
    public int MapRevision { get; private set; }

    private int _lastDragPaintedCellIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureMapView();
        EnsureToolCamera();
    }

    private void Update()
    {
        HandleCameraMovement();
        HandleToolInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public DungeonMapData CreateNewDungeon(int width, int height)
    {
        if (width < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 3.");
        }

        if (height < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 3.");
        }

        CurrentMap = BuildDungeonMap(width, height);
        SelectedCellPoint = null;
        _lastDragPaintedCellIndex = -1;
        IncrementMapRevision();
        RenderCurrentMap();
        NotifyToolStateChanged();
        return CurrentMap;
    }

    public DungeonMapData LoadDungeon(DungeonMapData mapData)
    {
        if (mapData == null)
        {
            throw new ArgumentNullException(nameof(mapData));
        }

        CurrentMap = mapData;
        SelectedCellPoint = null;
        _lastDragPaintedCellIndex = -1;
        IncrementMapRevision();
        RenderCurrentMap();
        NotifyToolStateChanged();
        return CurrentMap;
    }

    public void RenderCurrentMap()
    {
        if (CurrentMap == null)
        {
            throw new InvalidOperationException("Current map is not created yet.");
        }

        if (_mapView == null)
        {
            throw new InvalidOperationException("DungeonToolMapView reference is required.");
        }

        _mapView.SetMapData(CurrentMap);
        _mapView.SetSelection(SelectedCellPoint);
        _mapView.Render();
    }

    public void SetMode(DungeonToolMode mode)
    {
        Mode = mode;
        if (Mode == DungeonToolMode.Draw)
        {
            SelectedCellPoint = null;
            _mapView.SetSelection(null);
        }

        NotifyToolStateChanged();
    }

    public void SetDrawBrushCellType(DungeonCellType cellType)
    {
        if (!IsSupportedEditableCellType(cellType))
        {
            throw new ArgumentException("Unsupported cell type.", nameof(cellType));
        }

        DrawBrushCellType = cellType;
        NotifyToolStateChanged();
    }

    public bool TryGetSelectedCell(out DungeonPoint point, out DungeonCellData cellData)
    {
        point = default;
        cellData = null;
        if (!SelectedCellPoint.HasValue || CurrentMap == null)
        {
            return false;
        }

        point = SelectedCellPoint.Value;
        int index = point.ToIndex(CurrentMap.Width);
        cellData = CurrentMap.Cells[index];
        return true;
    }

    public bool TrySetSelectedCellType(DungeonCellType cellType)
    {
        if (!SelectedCellPoint.HasValue)
        {
            return false;
        }

        return TrySetCellTypeAt(SelectedCellPoint.Value, cellType);
    }

    public bool TrySetSelectedCellData(DungeonCellData cellData)
    {
        if (!SelectedCellPoint.HasValue || cellData == null || CurrentMap == null)
        {
            return false;
        }

        int index = SelectedCellPoint.Value.ToIndex(CurrentMap.Width);
        DungeonCellData currentCell = CurrentMap.Cells[index];
        if (currentCell.CellType != cellData.CellType)
        {
            return false;
        }

        return TrySetCellDataAt(SelectedCellPoint.Value, cellData);
    }

    private static DungeonMapData BuildDungeonMap(int width, int height)
    {
        var cells = new List<DungeonCellData>(width * height);
        var startPoint = new DungeonPoint(1, 1);
        var exitPoint = new DungeonPoint(width - 2, height - 2);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBorderWall = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                if (isBorderWall)
                {
                    cells.Add(new DungeonWallCellData());
                    continue;
                }

                var point = new DungeonPoint(x, y);
                if (point == startPoint)
                {
                    cells.Add(new DungeonStartCellData());
                    continue;
                }

                if (point == exitPoint)
                {
                    cells.Add(new DungeonEscapeAnchorCellData());
                    continue;
                }

                cells.Add(new DungeonEmptyCellData());
            }
        }

        return new DungeonMapData(width, height, cells);
    }

    private void EnsureMapView()
    {
        if (_mapView != null)
        {
            return;
        }

        _mapView = GetComponentInChildren<DungeonToolMapView>(true);
        if (_mapView != null)
        {
            return;
        }

        var viewObject = new GameObject("DungeonToolMapView");
        viewObject.transform.SetParent(transform, false);
        _mapView = viewObject.AddComponent<DungeonToolMapView>();
    }

    private void EnsureToolCamera()
    {
        if (_toolCamera != null)
        {
            return;
        }

        _toolCamera = Camera.main;
    }

    private void HandleCameraMovement()
    {
        if (_toolCamera == null || _cameraMoveSpeed <= 0f)
        {
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }

        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
        {
            return;
        }

        var direction = new Vector3(horizontal, vertical, 0f).normalized;
        _toolCamera.transform.position += direction * (_cameraMoveSpeed * Time.deltaTime);
    }

    private void HandleToolInput()
    {
        if (CurrentMap == null || _mapView == null || _toolCamera == null)
        {
            return;
        }

        if (Mode == DungeonToolMode.Draw)
        {
            HandleDrawModeInput();
            return;
        }

        HandleEditModeInput();
    }

    private void HandleDrawModeInput()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _lastDragPaintedCellIndex = -1;
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (!_mapView.TryGetCellPointFromScreenPosition(_toolCamera, Input.mousePosition, out DungeonPoint point))
        {
            return;
        }

        int index = point.ToIndex(CurrentMap.Width);
        if (_lastDragPaintedCellIndex == index)
        {
            return;
        }

        if (TrySetCellTypeAt(point, DrawBrushCellType))
        {
            _lastDragPaintedCellIndex = index;
        }
    }

    private void HandleEditModeInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!_mapView.TryGetCellPointFromScreenPosition(_toolCamera, Input.mousePosition, out DungeonPoint point))
        {
            return;
        }

        SelectedCellPoint = point;
        _mapView.SetSelection(SelectedCellPoint);
        NotifyToolStateChanged();
    }

    private bool TrySetCellTypeAt(DungeonPoint point, DungeonCellType cellType)
    {
        if (CurrentMap == null || !IsSupportedEditableCellType(cellType))
        {
            return false;
        }

        int index = point.ToIndex(CurrentMap.Width);
        if (index < 0 || index >= CurrentMap.Cells.Count)
        {
            return false;
        }

        DungeonCellData currentCell = CurrentMap.Cells[index];
        if (currentCell.CellType == cellType)
        {
            return false;
        }

        var cells = new List<DungeonCellData>(CurrentMap.Cells);
        cells[index] = CreateCellDataFromType(cellType);
        CurrentMap = new DungeonMapData(CurrentMap.Width, CurrentMap.Height, cells);
        IncrementMapRevision();
        RenderCurrentMap();
        NotifyToolStateChanged();
        return true;
    }

    private bool TrySetCellDataAt(DungeonPoint point, DungeonCellData cellData)
    {
        if (CurrentMap == null || cellData == null)
        {
            return false;
        }

        int index = point.ToIndex(CurrentMap.Width);
        if (index < 0 || index >= CurrentMap.Cells.Count)
        {
            return false;
        }

        DungeonCellData currentCell = CurrentMap.Cells[index];
        if (currentCell.CellType != cellData.CellType || AreSameCellData(currentCell, cellData))
        {
            return false;
        }

        var cells = new List<DungeonCellData>(CurrentMap.Cells);
        cells[index] = cellData;
        CurrentMap = new DungeonMapData(CurrentMap.Width, CurrentMap.Height, cells);
        IncrementMapRevision();
        RenderCurrentMap();
        NotifyToolStateChanged();
        return true;
    }

    private static bool AreSameCellData(DungeonCellData left, DungeonCellData right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left == null || right == null || left.CellType != right.CellType)
        {
            return false;
        }

        switch (left.CellType)
        {
            case DungeonCellType.Empty:
            case DungeonCellType.Wall:
            case DungeonCellType.Start:
                return true;
            case DungeonCellType.EnemySpawner:
            {
                var leftEncounter = left as DungeonEncounterCellData;
                var rightEncounter = right as DungeonEncounterCellData;
                return leftEncounter != null
                    && rightEncounter != null
                    && leftEncounter.EncounterDefId == rightEncounter.EncounterDefId
                    && leftEncounter.Tier == rightEncounter.Tier
                    && leftEncounter.IsMobile == rightEncounter.IsMobile;
            }
            case DungeonCellType.Loot:
            {
                var leftLoot = left as DungeonLootCellData;
                var rightLoot = right as DungeonLootCellData;
                return leftLoot != null
                    && rightLoot != null
                    && leftLoot.LootDefId == rightLoot.LootDefId
                    && leftLoot.LootTier == rightLoot.LootTier
                    && leftLoot.RequiresEliteClear == rightLoot.RequiresEliteClear;
            }
            case DungeonCellType.Information:
            {
                var leftInfo = left as DungeonInformationCellData;
                var rightInfo = right as DungeonInformationCellData;
                return leftInfo != null && rightInfo != null && leftInfo.InfoDefId == rightInfo.InfoDefId;
            }
            case DungeonCellType.Rest:
            {
                var leftRest = left as DungeonRestCellData;
                var rightRest = right as DungeonRestCellData;
                return leftRest != null && rightRest != null && leftRest.RestDefId == rightRest.RestDefId;
            }
            case DungeonCellType.Event:
            {
                var leftEvent = left as DungeonEventCellData;
                var rightEvent = right as DungeonEventCellData;
                return leftEvent != null && rightEvent != null && leftEvent.EventDefId == rightEvent.EventDefId;
            }
            case DungeonCellType.EscapeAnchor:
            {
                var leftAnchor = left as DungeonEscapeAnchorCellData;
                var rightAnchor = right as DungeonEscapeAnchorCellData;
                return leftAnchor != null && rightAnchor != null;
            }
            default:
                return false;
        }
    }

    private static DungeonCellData CreateCellDataFromType(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.Empty:
                return new DungeonEmptyCellData();
            case DungeonCellType.Wall:
                return new DungeonWallCellData();
            case DungeonCellType.EnemySpawner:
                return new DungeonEncounterCellData("encounter.default", DungeonEncounterTier.Normal, false);
            case DungeonCellType.Loot:
                return new DungeonLootCellData("loot.default", DungeonLootTier.Normal);
            case DungeonCellType.Information:
                return new DungeonInformationCellData("information.default");
            case DungeonCellType.Rest:
                return new DungeonRestCellData("rest.default");
            case DungeonCellType.Event:
                return new DungeonEventCellData("event.default");
            case DungeonCellType.Start:
                return new DungeonStartCellData();
            case DungeonCellType.EscapeAnchor:
                return new DungeonEscapeAnchorCellData();
            default:
                throw new ArgumentOutOfRangeException(nameof(cellType), "Unsupported cell type.");
        }
    }

    private static bool IsSupportedEditableCellType(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.Empty:
            case DungeonCellType.Wall:
            case DungeonCellType.EnemySpawner:
            case DungeonCellType.Loot:
            case DungeonCellType.Information:
            case DungeonCellType.Rest:
            case DungeonCellType.Event:
            case DungeonCellType.Start:
            case DungeonCellType.EscapeAnchor:
                return true;
            default:
                return false;
        }
    }

    private void IncrementMapRevision()
    {
        MapRevision++;
    }

    private static void NotifyToolStateChanged()
    {
        ToolStateChanged?.Invoke();
    }
}
