using System.Collections.Generic;
using UnityEngine;

public class ChartEditorGrid : MonoBehaviour
{
    [Header("連携設定")]
    [SerializeField] private ChartEditorManager editorManager;
    [SerializeField] private Transform gridSpawnPoint;
    [SerializeField] private Transform gridJudgePoint;

    [Header("拍線プレハブ設定（縦線｜のSprite / Quad等。空欄なら表示なし）")]
    [SerializeField] private GameObject beatLinePrefab;
    [SerializeField] private Color quarterBeatColor = new Color(1f, 1f, 1f, 0.7f);
    [SerializeField] private Color eighthBeatColor = new Color(0.4f, 0.8f, 1f, 0.45f);
    [SerializeField] private Color sixteenthBeatColor = new Color(1f, 0.9f, 0.3f, 0.25f);

    [Header("グリッド・スナップ設定")]
    public bool isGridVisible = true;
    public bool isSnapEnabled = true;
    [Tooltip("1小節あたりの分割数 (4=4分, 8=8分, 16=16分, 32=32分)")]
    public int beatDivision = 16;
    [SerializeField] private float songOffsetSeconds = 0f;

    private List<GameObject> linePool = new List<GameObject>();
    private readonly int[] availableDivisions = new int[] { 4, 8, 16, 32 };

    private void Awake()
    {
        if (editorManager == null)
        {
            editorManager = GetComponent<ChartEditorManager>();
        }
    }

    private void Update()
    {
        UpdateGridLines();
    }

    public void ResetGridState()
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            if (linePool[i] != null) linePool[i].SetActive(false);
        }
    }

    public void CycleDivision()
    {
        int currentIndex = System.Array.IndexOf(availableDivisions, beatDivision);
        int nextIndex = (currentIndex + 1) % availableDivisions.Length;
        beatDivision = availableDivisions[nextIndex];
    }

    /// <summary>
    /// 入力時刻を最寄りの拍分割タイミングへ吸着(スナップ)させる
    /// </summary>
    public float SnapTime(float rawTime, float bpm)
    {
        if (bpm <= 0f || beatDivision <= 0) return rawTime;

        float quarterDuration = 60f / bpm;
        float stepDuration = quarterDuration * (4f / beatDivision);
        float adjustedTime = rawTime - songOffsetSeconds;
        float snapped = Mathf.Round(adjustedTime / stepDuration) * stepDuration + songOffsetSeconds;

        return Mathf.Max(0f, snapped);
    }

    private void UpdateGridLines()
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            if (linePool[i] != null) linePool[i].SetActive(false);
        }

        if (!isGridVisible || beatLinePrefab == null || gridSpawnPoint == null || gridJudgePoint == null) return;
        if (editorManager == null || !editorManager.IsAudioReady || AudioManager.Instance == null) return;

        float bpm = editorManager.activeChartData != null ? editorManager.activeChartData.bpm : 120f;
        if (bpm <= 0f) return;

        float currentTime = AudioManager.Instance.GetCurrentTime();
        float preSpawn = editorManager.PrespawnTime;

        float quarterDuration = 60f / bpm;
        float stepDuration = quarterDuration * (4f / beatDivision);
        if (stepDuration <= 0.005f) return;

        int startStep = Mathf.FloorToInt((currentTime - songOffsetSeconds) / stepDuration);
        int endStep = Mathf.CeilToInt((currentTime + preSpawn - songOffsetSeconds) / stepDuration);

        int poolIndex = 0;
        for (int step = startStep; step <= endStep; step++)
        {
            if (step < 0) continue;

            float beatTime = step * stepDuration + songOffsetSeconds;
            if (beatTime < currentTime - 0.1f || beatTime > currentTime + preSpawn) continue;

            GameObject lineObj = GetOrCreateLine(poolIndex++);
            lineObj.SetActive(true);

            float progress = 1f - ((beatTime - currentTime) / preSpawn);
            lineObj.transform.position = Vector3.LerpUnclamped(gridSpawnPoint.position, gridJudgePoint.position, progress);

            ApplyBeatColor(lineObj, step);
        }
    }

    private void ApplyBeatColor(GameObject lineObj, int stepIndex)
    {
        int stepsPerQuarter = Mathf.Max(1, beatDivision / 4);
        int stepsPerEighth = Mathf.Max(1, beatDivision / 8);

        Color targetColor = sixteenthBeatColor;
        if (stepIndex % stepsPerQuarter == 0)
        {
            targetColor = quarterBeatColor;
        }
        else if (stepIndex % stepsPerEighth == 0)
        {
            targetColor = eighthBeatColor;
        }

        if (lineObj.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = targetColor;
        }
        else if (lineObj.TryGetComponent<Renderer>(out var rend))
        {
            rend.material.color = targetColor;
        }
    }

    private GameObject GetOrCreateLine(int index)
    {
        while (linePool.Count <= index)
        {
            GameObject obj = Instantiate(beatLinePrefab, transform);
            linePool.Add(obj);
        }
        return linePool[index];
    }
}