using UnityEngine;

public class SampleDungeonView : MonoBehaviour
{
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _floorPrefab;
    [SerializeField] private float _cellSize = 1f;

    private DungeonMapData _mapData;

    public void SetMapData(DungeonMapData mapData)
    {
        _mapData = mapData;
    }

    public void Render()
    {
        if (_mapData == null)
        {
            throw new System.InvalidOperationException("Map data is not set.");
        }

        Render(_mapData);
    }

    public void Render(DungeonMapData mapData)
    {
        if (mapData == null)
        {
            throw new System.ArgumentNullException(nameof(mapData));
        }

        if (_wallPrefab == null || _floorPrefab == null)
        {
            throw new System.InvalidOperationException("Wall/Floor prefab must be assigned.");
        }

        if (_cellSize <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(_cellSize), "Cell size must be greater than zero.");
        }

        _mapData = mapData;
        ClearChildren();

        for (int y = 0; y < mapData.Height; y++)
        {
            for (int x = 0; x < mapData.Width; x++)
            {
                int index = (y * mapData.Width) + x;
                var cell = mapData.Cells[index];
                var prefab = cell.IsWalkable ? _floorPrefab : _wallPrefab;
                var instance = Instantiate(prefab, transform);
                instance.name = $"{cell.CellType}_{x}_{y}";
                instance.transform.localPosition = new Vector3(x * _cellSize, y * _cellSize, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
