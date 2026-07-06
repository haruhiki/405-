using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreUIController : MonoBehaviour
{
    public static ScoreUIController Instance { get; private set; }

    [Header("References")]
    public Canvas uiCanvas;
    public TextMeshProUGUI totalScoreTMP;
    public TextMeshProUGUI comboTMP;
    public GameObject hitPopupPrefab; // Prefab should contain a Text component at root

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureUICanvas();
        EnsureFixedScoreUI();
    }

    private void EnsureUICanvas()
    {
        if (uiCanvas != null) return;

        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasGO = new GameObject("ScoreCanvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureFixedScoreUI()
    {
        if (uiCanvas == null) return;

        if (totalScoreTMP == null)
        {
            var scoreGO = new GameObject("TotalScoreText");
            scoreGO.transform.SetParent(uiCanvas.transform, false);
            totalScoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            totalScoreTMP.alignment = TextAlignmentOptions.TopLeft;
            totalScoreTMP.fontSize = 36;
            totalScoreTMP.color = Color.white;
            RectTransform rect = totalScoreTMP.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(400f, 80f);
        }

        if (comboTMP == null)
        {
            var comboGO = new GameObject("ComboText");
            comboGO.transform.SetParent(uiCanvas.transform, false);
            comboTMP = comboGO.AddComponent<TextMeshProUGUI>();
            comboTMP.alignment = TextAlignmentOptions.TopRight;
            comboTMP.fontSize = 32;
            comboTMP.color = Color.yellow;
            RectTransform rect = comboTMP.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(400f, 80f);
        }
    }

    public void UpdateTotalScore(long total)
    {
        var s = total.ToString();
        if (totalScoreTMP != null) totalScoreTMP.text = s;
    }

    public void UpdateCombo(int combo)
    {
        var s = combo > 0 ? $"COMBO {combo}" : "";
        if (comboTMP != null) comboTMP.text = s;
    }

    public void ShowHitPopup(string text, Vector3 worldPos)
    {
        if (uiCanvas == null)
        {
            EnsureUICanvas();
            if (uiCanvas == null) return;
        }

        GameObject go;
        if (hitPopupPrefab != null)
        {
            go = Instantiate(hitPopupPrefab, uiCanvas.transform, false);
        }
        else
        {
            go = new GameObject("HitPopup");
            go.transform.SetParent(uiCanvas.transform, false);
            var tmpFallback = go.AddComponent<TextMeshProUGUI>();
            tmpFallback.alignment = TextAlignmentOptions.Center;
            tmpFallback.fontSize = 28;
            tmpFallback.color = Color.white;
        }

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (tmp != null)
        {
            tmp.text = text;
        }
        else
        {
            Debug.LogWarning("hitPopupPrefab must contain a TextMeshProUGUI component on root or child.");
        }

        StartCoroutine(AnimatePopup(go, worldPos));
    }

    public void ShowComboBreak()
    {
        // Optional: play a small animation or sound for combo break
        if (comboTMP != null)
        {
            comboTMP.text = "";
        }
    }

    private IEnumerator AnimatePopup(GameObject go, Vector3 worldPos)
    {
        var rect = go.GetComponent<RectTransform>();
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 anchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)uiCanvas.transform, screenPos, uiCanvas.worldCamera, out anchored);
        rect.anchoredPosition = anchored;

        float t = 0f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogWarning("AnimatePopup: missing TextMeshProUGUI on popup.");
            yield break;
        }
        Color orig = tmp.color;

        while (t < 0.8f)
        {
            t += Time.deltaTime;
            rect.anchoredPosition += new Vector2(0, Time.deltaTime * 40f);
            var c = orig;
            c.a = Mathf.Lerp(1f, 0f, t / 0.8f);
            tmp.color = c;
            yield return null;
        }

        Destroy(go);
    }
}
