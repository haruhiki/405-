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
    
    private Coroutine _comboFadeCouroutine;
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

    /// <summary>
    /// スコアの合計
    /// </summary>
    /// <param name="total"></param>
    public void UpdateTotalScore(long total)
    {
        var s = total.ToString();
        if (totalScoreTMP != null) totalScoreTMP.text = s;
    }

    /// <summary>
    ///  コンボの更新と表示
    /// </summary>
    /// <param name="combo"></param>
    public void UpdateCombo(int combo)
    {
       if (comboTMP == null) return;
    
        if (_comboFadeCouroutine != null) StopCoroutine(_comboFadeCouroutine);

        if (combo > 0)
        {
            comboTMP.text = $"COMBO {combo}";
            comboTMP.alpha = 1f; // 表示
            // 2秒後に非表示にするコルーチンを開始
            _comboFadeCouroutine = StartCoroutine(FadeComboAfterDelay(2f));
        }
        else
        {
            comboTMP.text = "";
        }
    }

    // コンボの表示を時間経過でフェードアウト
    private IEnumerator FadeComboAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        float t = 0f;
        while (t < 0.5f) { // 0.5秒かけてフェードアウト
            t += Time.deltaTime;
            comboTMP.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }
        comboTMP.text = "";
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
        // ... (位置計算の既存処理) ...

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        
        if (tmp == null) 
        {
            Destroy(go); // TMPがなければ即削除して終了
            yield break;
        }

        float duration = 0.8f; // 表示時間
        float t = 0f;
        Color orig = tmp.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            // 上に移動
            rect.anchoredPosition += new Vector2(0, Time.deltaTime * 40f);
            
            // フェードアウト
            var c = orig;
            c.a = Mathf.Lerp(1f, 0f, t / duration);
            tmp.color = c;
            
            yield return null;
        }

        // ループ終了後に確実に破棄
        Destroy(go);
    }
}
