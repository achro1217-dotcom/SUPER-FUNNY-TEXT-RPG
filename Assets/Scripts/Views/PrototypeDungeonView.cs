using System;
using UnityEngine;

public sealed class PrototypeDungeonView : MonoBehaviour
{
    private static readonly Color VisibleColor = Color.white;
    private static readonly Color UnknownColor = new Color(1f, 1f, 1f, 0f);
    private static readonly Color ExploredOverlayColor = new Color(32f / 255f, 29f / 255f, 24f / 255f, 1f);
    private const float ExploredOverlayStrength = 0.4f;

    [SerializeField] private Sprite _roomVisibleSprite;
    [SerializeField] private Sprite _roomInvisibleSprite;
    [SerializeField] private Sprite _roomEmptySprite;
    [SerializeField] private Sprite _imageEnemy;
    [SerializeField] private Sprite _imageLoot;
    [SerializeField] private Sprite _imageInformation;
    [SerializeField] private Sprite _imageRest;
    [SerializeField] private Sprite _imageEvent;
    [SerializeField] private Sprite _imageEscape;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private int _renderWindowWidth = 5;
    [SerializeField] private int _renderWindowHeight = 5;

    private DungeonMapData _mapData;
    private DungeonMapRuntimeData _runtimeMapData;
    private DungeonPoint _renderCenterPoint;
    private Transform _tileRoot;
    private int _renderedWidth;
    private int _renderedHeight;
    private static Sprite _fallbackSprite;

    public void SetMapData(DungeonMapData mapData, DungeonMapRuntimeData runtimeMapData, DungeonPoint renderCenterPoint)
    {
        if (mapData == null)
        {
            throw new ArgumentNullException(nameof(mapData));
        }

        if (_cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(_cellSize), "Cell size must be greater than zero.");
        }

        if (_renderWindowWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_renderWindowWidth), "Render window width must be greater than zero.");
        }

        if (_renderWindowHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_renderWindowHeight), "Render window height must be greater than zero.");
        }

        if (runtimeMapData == null)
        {
            throw new ArgumentNullException(nameof(runtimeMapData));
        }

        if (runtimeMapData.Cells.Count != mapData.Cells.Count)
        {
            throw new ArgumentException("Runtime map cell count must match static map cell count.", nameof(runtimeMapData));
        }

        _mapData = mapData;
        _runtimeMapData = runtimeMapData;
        _renderCenterPoint = renderCenterPoint;
        RebuildAllTiles();
    }

    public void Render()
    {
        if (_mapData == null)
        {
            throw new InvalidOperationException("Map data is not set.");
        }
    }

    public void Render(DungeonMapData mapData)
    {
        DungeonPoint centerPoint = FindStartPointOrCenter(mapData);
        DungeonMapRuntimeData runtimeMapData = CreateVisibleRuntimeMap(mapData.Cells.Count);
        SetMapData(mapData, runtimeMapData, centerPoint);
    }

    public void Render(DungeonMapData mapData, DungeonMapRuntimeData runtimeMapData, DungeonPoint renderCenterPoint)
    {
        SetMapData(mapData, runtimeMapData, renderCenterPoint);
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
        Vector3 localPosition = transform.InverseTransformPoint(worldPoint);
        float xOffset = (_renderedWidth - 1) * _cellSize * 0.5f;
        float yOffset = (_renderedHeight - 1) * _cellSize * 0.5f;

        float xFloat = (localPosition.x + xOffset) / _cellSize;
        float yFloat = (localPosition.y + yOffset) / _cellSize;
        int renderX = Mathf.FloorToInt(xFloat + 0.5f);
        int renderY = Mathf.FloorToInt(yFloat + 0.5f);
        if (renderX < 0 || renderY < 0 || renderX >= _renderedWidth || renderY >= _renderedHeight)
        {
            return false;
        }

        int halfWidthLeft = _renderWindowWidth / 2;
        int halfHeightBottom = _renderWindowHeight / 2;
        int minX = _renderCenterPoint.X - halfWidthLeft;
        int minY = _renderCenterPoint.Y - halfHeightBottom;
        int sourceX = minX + renderX;
        int sourceY = minY + renderY;
        if (sourceX < 0 || sourceY < 0 || sourceX >= _mapData.Width || sourceY >= _mapData.Height)
        {
            return false;
        }

        point = new DungeonPoint(sourceX, sourceY);
        return true;
    }

    public bool TryGetLocalPositionForCell(DungeonPoint point, out Vector3 localPosition)
    {
        localPosition = default;
        if (_mapData == null)
        {
            return false;
        }

        int halfWidthLeft = _renderWindowWidth / 2;
        int halfHeightBottom = _renderWindowHeight / 2;
        int minX = _renderCenterPoint.X - halfWidthLeft;
        int minY = _renderCenterPoint.Y - halfHeightBottom;
        int renderX = point.X - minX;
        int renderY = point.Y - minY;
        if (renderX < 0 || renderY < 0 || renderX >= _renderedWidth || renderY >= _renderedHeight)
        {
            return false;
        }

        localPosition = GetLocalCellPosition(renderX, renderY);
        return true;
    }

    public DungeonFogState GetFogStateAt(DungeonPoint point)
    {
        if (_mapData == null || _runtimeMapData == null)
        {
            return DungeonFogState.Unknown;
        }

        if (point.X < 0 || point.Y < 0 || point.X >= _mapData.Width || point.Y >= _mapData.Height)
        {
            return DungeonFogState.Unknown;
        }

        return _runtimeMapData.Cells[point.ToIndex(_mapData.Width)].FogState;
    }

    private static GameObject CreateSpriteTileInstance(DungeonCellType cellType, Sprite sprite)
    {
        var instance = new GameObject(cellType.ToString(), typeof(SpriteRenderer));
        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite ?? GetFallbackSprite();
        renderer.sortingOrder = 0;
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
        return cellType != DungeonCellType.Empty && cellType != DungeonCellType.Wall && cellType != DungeonCellType.Start;
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
                return null;
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

    private void RebuildAllTiles()
    {
        EnsureTileRoot();
        ClearTileInstances();
        int halfWidthLeft = _renderWindowWidth / 2;
        int halfHeightBottom = _renderWindowHeight / 2;
        int minX = _renderCenterPoint.X - halfWidthLeft;
        int minY = _renderCenterPoint.Y - halfHeightBottom;
        _renderedWidth = _renderWindowWidth;
        _renderedHeight = _renderWindowHeight;

        for (int renderY = 0; renderY < _renderedHeight; renderY++)
        {
            for (int renderX = 0; renderX < _renderedWidth; renderX++)
            {
                int sourceX = minX + renderX;
                int sourceY = minY + renderY;
                DungeonCellData cellData = ResolveRenderCell(sourceX, sourceY);
                DungeonFogState fogState = ResolveFogState(sourceX, sourceY);
                var sourcePoint = new DungeonPoint(sourceX, sourceY);
                CreateAndPlaceTileInstance(sourcePoint, renderX, renderY, cellData, fogState);
            }
        }
    }

    private static DungeonPoint FindStartPointOrCenter(DungeonMapData mapData)
    {
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            if (mapData.Cells[i].CellType != DungeonCellType.Start)
            {
                continue;
            }

            int x = i % mapData.Width;
            int y = i / mapData.Width;
            return new DungeonPoint(x, y);
        }

        return new DungeonPoint(mapData.Width / 2, mapData.Height / 2);
    }

    private DungeonCellData ResolveRenderCell(int sourceX, int sourceY)
    {
        if (sourceX < 0 || sourceY < 0 || sourceX >= _mapData.Width || sourceY >= _mapData.Height)
        {
            return new DungeonWallCellData();
        }

        int sourceIndex = (sourceY * _mapData.Width) + sourceX;
        return _mapData.Cells[sourceIndex];
    }

    private DungeonFogState ResolveFogState(int sourceX, int sourceY)
    {
        if (sourceX < 0 || sourceY < 0 || sourceX >= _mapData.Width || sourceY >= _mapData.Height)
        {
            return DungeonFogState.Unknown;
        }

        int sourceIndex = (sourceY * _mapData.Width) + sourceX;
        return _runtimeMapData.Cells[sourceIndex].FogState;
    }

    private GameObject CreateAndPlaceTileInstance(DungeonPoint sourcePoint, int renderX, int renderY, DungeonCellData cell, DungeonFogState fogState)
    {
        GameObject instance = CreateCellInstance(cell, fogState);
        instance.name = $"{cell.CellType}_{sourcePoint.X}_{sourcePoint.Y}";
        instance.transform.SetParent(_tileRoot, false);
        instance.transform.localPosition = GetLocalCellPosition(renderX, renderY);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private GameObject CreateCellInstance(DungeonCellData cell, DungeonFogState fogState)
    {
        Sprite baseSprite = ResolveBaseSprite(cell, fogState);
        GameObject baseInstance = CreateSpriteTileInstance(cell.CellType, baseSprite);
        ApplyFogColor(baseInstance, fogState);

        if (fogState == DungeonFogState.Visible || fogState == DungeonFogState.Explored)
        {
            AttachOverlayIfNeeded(baseInstance.transform, cell.CellType);
            if (fogState == DungeonFogState.Explored && cell.IsWalkable && cell.CellType != DungeonCellType.Wall)
            {
                ApplyExploredTintToAllRenderers(baseInstance);
            }
        }

        return baseInstance;
    }

    private Sprite ResolveBaseSprite(DungeonCellData cell, DungeonFogState fogState)
    {
        if (cell.CellType == DungeonCellType.Wall || !cell.IsWalkable)
        {
            return _roomEmptySprite;
        }

        if (fogState == DungeonFogState.Invisible)
        {
            return _roomInvisibleSprite;
        }

        if (fogState == DungeonFogState.Visible || fogState == DungeonFogState.Explored)
        {
            return _roomVisibleSprite;
        }

        return _roomInvisibleSprite;
    }

    private static void ApplyFogColor(GameObject tileInstance, DungeonFogState fogState)
    {
        SpriteRenderer renderer = tileInstance.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        switch (fogState)
        {
            case DungeonFogState.Unknown:
                renderer.color = UnknownColor;
                return;
            case DungeonFogState.Invisible:
            case DungeonFogState.Visible:
                renderer.color = VisibleColor;
                return;
            case DungeonFogState.Explored:
                renderer.color = VisibleColor;
                return;
            default:
                renderer.color = VisibleColor;
                return;
        }
    }

    private static void ApplyExploredTintToAllRenderers(GameObject tileInstance)
    {
        SpriteRenderer[] renderers = tileInstance.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            renderer.color = Color.Lerp(renderer.color, ExploredOverlayColor, ExploredOverlayStrength);
        }
    }

    private static DungeonMapRuntimeData CreateVisibleRuntimeMap(int cellCount)
    {
        var runtimeCells = new DungeonCellRuntimeData[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            runtimeCells[i] = new DungeonCellRuntimeData(DungeonFogState.Visible, null);
        }

        return new DungeonMapRuntimeData(runtimeCells, cellCount);
    }

    private Vector3 GetLocalCellPosition(int renderX, int renderY)
    {
        float xOffset = (_renderedWidth - 1) * _cellSize * 0.5f;
        float yOffset = (_renderedHeight - 1) * _cellSize * 0.5f;
        return new Vector3((renderX * _cellSize) - xOffset, (renderY * _cellSize) - yOffset, 0f);
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
        if (_tileRoot == null)
        {
            return;
        }

        for (int i = _tileRoot.childCount - 1; i >= 0; i--)
        {
            DestroyTileInstance(_tileRoot.GetChild(i).gameObject);
        }
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

}
