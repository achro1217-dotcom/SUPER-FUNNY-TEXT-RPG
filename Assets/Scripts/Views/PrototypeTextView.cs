using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PrototypeTextView : MonoBehaviour
{
    private const float MaxContentHeight = 220f;

    [SerializeField] private TMP_Text _lineTemplate;
    [SerializeField] private string _lineObjectName = "TextLine";
    [SerializeField] private float _typewriterCharIntervalSeconds = 0.03f;

    public void AppendText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        TrimOldestLinesToHeightBudget(text, MaxContentHeight);

        TMP_Text line = CreateLineInstance();
        line.text = text;
        line.transform.SetAsLastSibling();
    }

    public IEnumerator AppendTextTypewriter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        TrimOldestLinesToHeightBudget(text, MaxContentHeight);

        TMP_Text line = CreateLineInstance();
        line.text = text;
        line.maxVisibleCharacters = 0;
        line.transform.SetAsLastSibling();
        line.ForceMeshUpdate();

        int totalVisibleCharacters = line.textInfo.characterCount;
        if (totalVisibleCharacters <= 0)
        {
            yield break;
        }

        if (_typewriterCharIntervalSeconds <= 0f)
        {
            line.maxVisibleCharacters = totalVisibleCharacters;
            yield break;
        }

        var wait = new WaitForSeconds(_typewriterCharIntervalSeconds);
        for (int visibleCount = 1; visibleCount <= totalVisibleCharacters; visibleCount++)
        {
            line.maxVisibleCharacters = visibleCount;
            yield return wait;
        }
    }

    public void ClearLines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (_lineTemplate != null && child == _lineTemplate.transform)
            {
                continue;
            }

            DestroyLineObject(child.gameObject);
        }
    }

    private TMP_Text CreateLineInstance()
    {
        TMP_Text instance = Instantiate(_lineTemplate, transform);
        instance.gameObject.SetActive(true);
        instance.gameObject.name = ResolveLineObjectName();

        return instance;
    }

    private void TrimOldestLinesToHeightBudget(string nextText, float maxHeight)
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        float projectedHeight = CalculateProjectedHeight(nextText, root);
        while (projectedHeight > maxHeight)
        {
            TMP_Text oldestLine = FindOldestLine();
            if (oldestLine == null)
            {
                return;
            }

            DestroyLineObject(oldestLine.gameObject);
            if (root != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }

            projectedHeight = CalculateProjectedHeight(nextText, root);
        }
    }

    private float CalculateProjectedHeight(string nextText, RectTransform root)
    {
        float currentHeight = root == null ? 0f : root.rect.height;
        if (_lineTemplate == null)
        {
            return currentHeight;
        }

        Vector2 preferredSize = _lineTemplate.GetPreferredValues(nextText);
        float nextLineHeight = Mathf.Max(0f, preferredSize.y);
        return currentHeight + nextLineHeight;
    }

    private TMP_Text FindOldestLine()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (TryGetLineText(child, out TMP_Text line))
            {
                return line;
            }
        }

        return null;
    }

    private bool TryGetLineText(Transform child, out TMP_Text line)
    {
        line = null;
        if (child == null)
        {
            return false;
        }

        if (_lineTemplate != null && child == _lineTemplate.transform)
        {
            return false;
        }

        line = child.GetComponent<TMP_Text>();
        return line != null;
    }

    private string ResolveLineObjectName()
    {
        return string.IsNullOrWhiteSpace(_lineObjectName) ? "TextLine" : _lineObjectName;
    }

    private static void DestroyLineObject(GameObject lineObject)
    {
        if (lineObject == null)
        {
            return;
        }

        lineObject.transform.SetParent(null, false);

        Destroy(lineObject);
    }
}
