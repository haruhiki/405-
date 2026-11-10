using UnityEngine;

public class ChartEditorGrid : MonoBehaviour
{
               [Header("参照")]
        [SerializeField] private ChartEditorManager editorManager;

        [Header("ガイドラインPrefab設定")]
        [SerializeField] private GameObject gridLinePrefab; // 白線ガイドのPrefab (GridLineCon付き)
        [SerializeField] private Transform spawnPoint;      // 生成開始位置 (レーン奥の中央)
        [SerializeField] private Transform targetPoint;     // 判定ライン中央位置

        [Header("グリッドスナップ設定")]
        public bool isSnapEnabled = true; // スナップ機能のON/OFF
        public bool isGridVisible = true; // グリッド線の表示/非表示
        public int beatDivision = 4;      // 現在の拍分割数 (4, 8, 16, 32)

        [Header("ガイド線の色設定")]
        [SerializeField] private Color colorQuarter = Color.white;       // 4分音符
        [SerializeField] private Color colorEighth = Color.cyan;         // 8分音符
        [SerializeField] private Color colorSixteenth = Color.yellow;    // 16分音符
        [SerializeField] private Color colorThirtySecond = Color.magenta;// 32分音符

        private float lastSpawnedBeatTime = -1f;

        private void Awake()
        {
            if (editorManager == null)
            {
                editorManager = GetComponent<ChartEditorManager>();
            }
        }

        private void Update()
        {
            if (!isGridVisible || gridLinePrefab == null || AudioManager.Instance == null) return;
            if (!AudioManager.Instance.IsBGMPlaying() || editorManager == null) return;

            float currentTime = AudioManager.Instance.GetCurrentTime();
            float bpm = editorManager.activeChartData.bpm;
            if (bpm <= 0) return;

            UpdateGridSpawning(currentTime, bpm);
        }

        /// <summary>
        /// 再生時間に合わせて未来の拍線を生成
        /// </summary>
        private void UpdateGridSpawning(float currentTime, float bpm)
        {
            float secondsPerBeat = 60.0f / bpm; // 1拍(4分音符)の長さ(秒)
            float step = secondsPerBeat / (beatDivision / 4.0f); // 選択分割に応じた最小ステップ時間
            float preSpawnTime = editorManager.PrespawnTime;

            // preSpawnTime 先までの時間範囲で拍を計算
            float horizonTime = currentTime + preSpawnTime;

            // 最後に生成した拍の次の拍タイミングを算出
            float startBeatTime = Mathf.Ceil(currentTime / step) * step;

            for (float t = startBeatTime; t <= horizonTime; t += step)
            {
                if (t > lastSpawnedBeatTime)
                {
                    Color lineColor = GetColorForBeatTime(t, secondsPerBeat);
                    SpawnGridLine(t, preSpawnTime, lineColor);
                    lastSpawnedBeatTime = t;
                }
            }
        }

        /// <summary>
        /// 白線オブジェクトのInstantiateおよび初期化
        /// </summary>
        private void SpawnGridLine(float beatTime, float preSpawnTime, Color lineColor)
        {
            Vector3 sPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Vector3 tPos = targetPoint != null ? targetPoint.position : Vector3.zero;

            GameObject obj = Instantiate(gridLinePrefab, sPos, Quaternion.identity);
            GridLineCon lineCon = obj.GetComponent<GridLineCon>();

            if (lineCon != null)
            {
                lineCon.Init(beatTime, preSpawnTime, sPos, tPos, lineColor);
            }
        }

        /// <summary>
        /// 拍タイミングの性質（4分拍か、8分拍か等）に応じて色を判定
        /// </summary>
        private Color GetColorForBeatTime(float time, float secondsPerBeat)
        {
            float beatPosition = time / secondsPerBeat;
            float epsilon = 0.001f;

            // 4分音符（整数の拍）
            if (Mathf.Abs(beatPosition - Mathf.Round(beatPosition)) < epsilon)
            {
                return colorQuarter;
            }

            // 8分音符（0.5刻み）
            if (Mathf.Abs((beatPosition * 2f) - Mathf.Round(beatPosition * 2f)) < epsilon)
            {
                return colorEighth;
            }

            // 16分音符（0.25刻み）
            if (Mathf.Abs((beatPosition * 4f) - Mathf.Round(beatPosition * 4f)) < epsilon)
            {
                return colorSixteenth;
            }

            // 32分音符
            return colorThirtySecond;
        }

        /// <summary>
        /// 指定時間を現在の拍分割ルールに基づいてスナップした時間に変換
        /// </summary>
        public float SnapTime(float time, float bpm)
        {
            if (!isSnapEnabled || bpm <= 0) return time;

            float secondsPerBeat = 60.0f / bpm;
            float step = secondsPerBeat / (beatDivision / 4.0f);

            float snapped = Mathf.Round(time / step) * step;
            return Mathf.Max(0f, snapped);
        }

        /// <summary>
        /// 拍分割数を順番に切り替える (4分 -> 8分 -> 16分 -> 32分 -> 4分)
        /// </summary>
        public void CycleDivision()
        {
            if (beatDivision == 4) beatDivision = 8;
            else if (beatDivision == 8) beatDivision = 16;
            else if (beatDivision == 16) beatDivision = 32;
            else beatDivision = 4;

            ResetGridState();
        }

        /// <summary>
        /// タイムスキップや巻き戻し発生時にガイド線の生成履歴をリセット
        /// </summary>
        public void ResetGridState()
        {
            lastSpawnedBeatTime = -1f;
        }
}
