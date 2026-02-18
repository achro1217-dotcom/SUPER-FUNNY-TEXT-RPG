using System;
using UnityEngine;

public sealed class DungeonToolMapView : MonoBehaviour
{
    private struct TileInstanceData
    {
        public DungeonPoint Point;
        public GameObject Instance;
        public DungeonCellData CellData;

        public TileInstanceData(DungeonPoint point, GameObject instance, DungeonCellData cellData)
        {
            Point = point;
            Instance = instance;
            CellData = cellData;
        }
    }

    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _floorPrefab;
    [SerializeField] private Sprite _imageEnemy;
    [SerializeField] private Sprite _imageLoot;
    [SerializeField] private Sprite _imageInformation;
    [SerializeField] private Sprite _imageRest;
    [SerializeField] private Sprite _imageEvent;
    [SerializeField] private Sprite _imageStart;
    [SerializeField] private Sprite _imageEscape;
    
    [SerializeField] private float _cellSize = 1f;

    private DungeonMapData _mapData;
    private TileInstanceData[] _tileInstances;
    private Transform _tileRoot;
    private DungeonPoint? _selectedCellPoint;
    private GameObject _selectionVisual;
    private static Sprite _fallbackSprite;

    public void SetMapData(DungeonMapData mapData)
    {
        if (mapData == null)
        {
            throw new ArgumentNullException(nameof(mapData));
        }

        if (_cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(_cellSize), "Cell size must be greater than zero.");
        }

        if (ShouldRebuildAllTiles(mapData))
        {
            _mapData = mapData;
            RebuildAllTiles();
            UpdateSelectionVisual();
            return;
        }

        DungeonMapData previousMap = _mapData;
        _mapData = mapData;
        UpdateChangedTiles(previousMap, mapData);
        UpdateSelectionVisual();
    }

    public void Render()
    {
        if (_mapData == null)
        {
            throw new InvalidOperationException("Map data is not set.");
        }
        UpdateSelectionVisual();
    }

    public void Render(DungeonMapData mapData)
    {
        SetMapData(mapData);
        UpdateSelectionVisual();
    }

    public void SetSelection(DungeonPoint? selectedCellPoint)
    {
        _selectedCellPoint = selectedCellPoint;
        UpdateSelectionVisual();
    }

    public bool TryGetCellPointFromScreenPosition(Camera camera, Vector3 screenPosition, out DungeonPoint point)
    {
        point = default;
        if (camera == null || _mapData == null)
        {
            return false;
        }

        var plane = new Plane(transform.forward, transform.position);
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!plane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 worldPoint = ray.GetPoint(enter);
        return TryGetCellPointFromWorldPosition(worldPoint, out point);
    }

    public bool TryGetCellPointFromWorldPosition(Vector3 worldPosition, out DungeonPoint point)
    {
        point = default;
        if (_mapData == null || _cellSize <= 0f)
        {
            return false;
        }

        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        float xOffset = (_mapData.Width - 1) * _cellSize * 0.5f;
        float yOffset = (_mapData.Height - 1) * _cellSize * 0.5f;

        float xFloat = (localPosition.x + xOffset) / _cellSize;
        float yFloat = (localPosition.y + yOffset) / _cellSize;
        int x = Mathf.FloorToInt(xFloat + 0.5f);
        int y = Mathf.FloorToInt(yFloat + 0.5f);

        if (x < 0 || y < 0 || x >= _mapData.Width || y >= _mapData.Height)
        {
            return false;
        }

        float centerX = (x * _cellSize) - xOffset;
        float centerY = (y * _cellSize) - yOffset;
        float halfCellSize = _cellSize * 0.5f;
        if (Mathf.Abs(localPosition.x - centerX) > halfCellSize || Mathf.Abs(localPosition.y - centerY) > halfCellSize)
        {
            return false;
        }

        point = new DungeonPoint(x, y);
        return true;
    }

    private GameObject CreateCellInstance(DungeonCellData cell)
    {
        if (cell.CellType == DungeonCellType.Wall)
        {
            if (_wallPrefab != null)
            {
                return Instantiate(_wallPrefab);
            }

            return CreateFallbackTileInstance(cell.CellType);
        }

        GameObject baseInstance = _floorPrefab != null
            ? Instantiate(_floorPrefab)
            : CreateFallbackTileInstance(DungeonCellType.Empty);
        AttachOverlayIfNeeded(baseInstance.transform, cell.CellType);
        return baseInstance;
    }

    private static GameObject CreateFallbackTileInstance(DungeonCellType cellType)
    {
        var instance = new GameObject(cellType.ToString());
        var spriteRenderer = instance.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetFallbackSprite();
        return instance;
    }

    private void AttachOverlayIfNeeded(Transform tileTransform, DungeonCellType cellType)
    {
        if (!RequiresOverlay(cellType))
        {
            return;
        }

        GameObject overlayObject = new GameObject($"{cellType}_Overlay", typeof(SpriteRenderer));
        overlayObject.transform.SetParent(tileTransform, false);
        overlayObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = overlayObject.GetComponent<SpriteRenderer>();
        overlayRenderer.sprite = ResolveOverlaySprite(cellType) ?? GetFallbackSprite();
        overlayRenderer.sortingOrder = 10;
    }

    private static bool RequiresOverlay(DungeonCellType cellType)
    {
        return cellType != DungeonCellType.Empty && cellType != DungeonCellType.Wall;
    }

    private Sprite ResolveOverlaySprite(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.Empty:
            case DungeonCellType.Wall:
                return null;
            case DungeonCellType.EnemySpawner:
                return _imageEnemy;
            case DungeonCellType.Loot:
                return _imageLoot;
            case DungeonCellType.Information:
                return _imageInformation;
            case DungeonCellType.Rest:
                return _imageRest;
            case DungeonCellType.Event:
                return _imageEvent;
            case DungeonCellType.Start:
                return _imageStart;
            case DungeonCellType.EscapeAnchor:
                return _imageEscape;
            default:
                return null;
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (_fallbackSprite != null)
        {
            return _fallbackSprite;
        }

        _fallbackSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return _fallbackSprite;
    }

    private bool ShouldRebuildAllTiles(DungeonMapData mapData)
    {
        if (_mapData == null || _tileInstances == null)
        {
            return true;
        }

        if (_mapData.Width != mapData.Width || _mapData.Height != mapData.Height)
        {
            return true;
        }

        return _tileInstances.Length != mapData.Cells.Count;
    }

    private void RebuildAllTiles()
    {
        EnsureTileRoot();
        ClearTileInstances();
        _tileInstances = new TileInstanceData[_mapData.Cells.Count];

        for (int y = 0; y < _mapData.Height; y++)
        {
            for (int x = 0; x < _mapData.Width; x++)
            {
                var point = new DungeonPoint(x, y);
                int index = point.ToIndex(_mapData.Width);
                DungeonCellData cell = _mapData.Cells[index];
                GameObject instance = CreateAndPlaceTileInstance(point, cell);
                _tileInstances[index] = new TileInstanceData(point, instance, cell);
            }
        }
    }

    private void UpdateChangedTiles(DungeonMapData previousMap, DungeonMapData nextMap)
    {
        for (int index = 0; index < nextMap.Cells.Count; index++)
        {
            DungeonCellData previousCell = previousMap.Cells[index];
            DungeonCellData nextCell = nextMap.Cells[index];
            if (AreSameCell(previousCell, nextCell))
            {
                continue;
            }

            ReplaceTileInstance(index, nextCell);
        }
    }

    private static bool AreSameCell(DungeonCellData left, DungeonCellData right)
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
            default:
                return false;
        }
    }

    private void ReplaceTileInstance(int index, DungeonCellData nextCell)
    {
        TileInstanceData tileData = _tileInstances[index];
        DestroyTileInstance(tileData.Instance);
        GameObject nextInstance = CreateAndPlaceTileInstance(tileData.Point, nextCell);
        _tileInstances[index] = new TileInstanceData(tileData.Point, nextInstance, nextCell);
    }

    private GameObject CreateAndPlaceTileInstance(DungeonPoint point, DungeonCellData cell)
    {
        GameObject instance = CreateCellInstance(cell);
        instance.name = $"{cell.CellType}_{point.X}_{point.Y}";
        instance.transform.SetParent(_tileRoot, false);
        instance.transform.localPosition = GetLocalCellPosition(point);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private Vector3 GetLocalCellPosition(DungeonPoint point)
    {
        float xOffset = (_mapData.Width - 1) * _cellSize * 0.5f;
        float yOffset = (_mapData.Height - 1) * _cellSize * 0.5f;
        return new Vector3((point.X * _cellSize) - xOffset, (point.Y * _cellSize) - yOffset, 0f);
    }

    private void EnsureTileRoot()
    {
        if (_tileRoot != null)
        {
            return;
        }

        var root = new GameObject("Tiles");
        root.transform.SetParent(transform, false);
        _tileRoot = root.transform;
    }

    private void ClearTileInstances()
    {
        if (_tileInstances == null)
        {
            if (_tileRoot == null)
            {
                return;
            }

            for (int i = _tileRoot.childCount - 1; i >= 0; i--)
            {
                DestroyTileInstance(_tileRoot.GetChild(i).gameObject);
            }

            return;
        }

        for (int i = 0; i < _tileInstances.Length; i++)
        {
            DestroyTileInstance(_tileInstances[i].Instance);
        }

        _tileInstances = null;
    }

    private void DestroyTileInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(instance);
            return;
        }

        DestroyImmediate(instance);
    }

    private void UpdateSelectionVisual()
    {
        if (_mapData == null || !_selectedCellPoint.HasValue)
        {
            SetSelectionVisualActive(false);
            return;
        }

        EnsureSelectionVisual();
        DungeonPoint point = _selectedCellPoint.Value;
        float xOffset = (_mapData.Width - 1) * _cellSize * 0.5f;
        float yOffset = (_mapData.Height - 1) * _cellSize * 0.5f;
        _selectionVisual.transform.localPosition = new Vector3((point.X * _cellSize) - xOffset, (point.Y * _cellSize) - yOffset, -0.05f);
        _selectionVisual.transform.localRotation = Quaternion.identity;
        _selectionVisual.transform.localScale = Vector3.one * _cellSize;
        SetSelectionVisualActive(true);
    }

    private void EnsureSelectionVisual()
    {
        if (_selectionVisual != null)
        {
            return;
        }

        _selectionVisual = new GameObject("Selection");
        _selectionVisual.transform.SetParent(transform, false);

        var renderer = _selectionVisual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        renderer.sortingOrder = 999;
    }

    private void SetSelectionVisualActive(bool isActive)
    {
        if (_selectionVisual == null)
        {
            return;
        }

        _selectionVisual.SetActive(isActive);
    }
}
