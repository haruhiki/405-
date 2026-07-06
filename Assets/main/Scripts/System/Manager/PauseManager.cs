using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;       // Full pause UI panel (contains buttons)
    [SerializeField] private GameObject slidersGroup;     // Group that contains sliders (show only when paused)
    [SerializeField] private GameObject countdownPanel;   // Optional separate countdown panel
    [SerializeField] private TextMeshProUGUI countdownTMP; // Countdown text shown when resuming

    [Header("Countdown")]
    [SerializeField] private int startCount = 3;

    public event Action OnPaused;
    public event Action OnResumed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        // Do not mark DontDestroyOnLoad here; we assume PauseManager is scene-scoped by design
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (slidersGroup != null) slidersGroup.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (countdownTMP != null) countdownTMP.gameObject.SetActive(false);

        EnsureCountdownUI();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (slidersGroup != null) slidersGroup.SetActive(true);

        // Stop game time and audio
        Time.timeScale = 0f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAll();
        }
        else
        {
            AudioListener.pause = true;
        }

        OnPaused?.Invoke();
    }

    // Called by UI when closing pause panel and wanting to resume with countdown
    public void ResumeWithCountdown()
    {
        if (!IsPaused) return;

        // Hide pause UI immediately; sliders should hide as well
        if (pausePanel != null) pausePanel.SetActive(false);
        if (slidersGroup != null) slidersGroup.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(true);
        else if (countdownTMP != null) countdownTMP.gameObject.SetActive(true);

        StartCoroutine(CountdownAndResume());
    }

    // Immediate resume without countdown (useful for debugging)
    public void ResumeImmediate()
    {
        if (!IsPaused) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (slidersGroup != null) slidersGroup.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (countdownTMP != null) countdownTMP.gameObject.SetActive(false);

        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeAll();
        }
        else
        {
            AudioListener.pause = false;
        }
        IsPaused = false;

        OnResumed?.Invoke();
    }

    private IEnumerator CountdownAndResume()
    {
        if (countdownTMP != null)
        {
            countdownTMP.gameObject.SetActive(true);
            for (int i = startCount; i >= 1; i--)
            {
                countdownTMP.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            countdownTMP.text = "GO!";
            yield return new WaitForSecondsRealtime(0.5f);
        }

        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (countdownTMP != null) countdownTMP.gameObject.SetActive(false);
        else
        {
            // Ensure a short wait so the user sees the transition
            yield return new WaitForSecondsRealtime(0.1f);
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        OnResumed?.Invoke();
    }

    // UI hooks
    public void OnPauseButtonPressed() => Pause();
    public void OnResumeButtonPressed() => ResumeWithCountdown();
    public void OnResumeImmediateButtonPressed() => ResumeImmediate();

    private void Update()
    {
        // Toggle pause with Escape key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!IsPaused)
            {
                Pause();
            }
            else
            {
                // Close pause UI and resume with countdown
                ResumeWithCountdown();
            }
        }
    }

    private void EnsureCountdownUI()
    {
        if (countdownTMP != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("PauseCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (countdownPanel == null)
        {
            countdownPanel = new GameObject("CountdownPanel");
            countdownPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = countdownPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(300f, 180f);
            countdownPanel.AddComponent<CanvasRenderer>();
            Image panelImage = countdownPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.5f);
            countdownPanel.SetActive(false);
        }

        if (countdownTMP == null)
        {
            GameObject textGO = new GameObject("CountdownText");
            textGO.transform.SetParent(countdownPanel.transform, false);
            countdownTMP = textGO.AddComponent<TextMeshProUGUI>();
            countdownTMP.alignment = TextAlignmentOptions.Center;
            countdownTMP.fontSize = 80;
            countdownTMP.color = Color.white;
            RectTransform textRect = countdownTMP.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            countdownTMP.gameObject.SetActive(false);
        }
    }
}
