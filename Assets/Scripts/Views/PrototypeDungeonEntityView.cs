using UnityEngine;

public sealed class PrototypeDungeonEntityView : MonoBehaviour
{
    [SerializeField] private Sprite _playerSprite;
    [SerializeField] private Color _playerColor = Color.white;
    [SerializeField] private int _sortingOrder = 100;
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 0f, -0.2f);

    private GameObject _playerInstance;

    public void RenderPlayer(Vector3 localPosition, bool isVisible)
    {
        EnsurePlayerInstance();
        _playerInstance.transform.localPosition = localPosition + _localOffset;
        _playerInstance.transform.localRotation = Quaternion.identity;
        _playerInstance.transform.localScale = Vector3.one;
        _playerInstance.SetActive(isVisible);
    }

    public void HidePlayer()
    {
        if (_playerInstance == null)
        {
            return;
        }

        _playerInstance.SetActive(false);
    }

    private void EnsurePlayerInstance()
    {
        if (_playerInstance != null)
        {
            return;
        }

        _playerInstance = new GameObject("PlayerEntity", typeof(SpriteRenderer));
        _playerInstance.transform.SetParent(transform, false);
        SpriteRenderer renderer = _playerInstance.GetComponent<SpriteRenderer>();
        renderer.sprite = _playerSprite ?? GetFallbackSprite();
        renderer.color = _playerColor;
        renderer.sortingOrder = _sortingOrder;
    }

    private static Sprite GetFallbackSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
