using UnityEngine;

public class EditorInput : MonoBehaviour
{
   
    [RequireComponent(typeof(ChartEditorManager))]
    public class ChartEditorInput : MonoBehaviour
    {
        private ChartEditorManager editorManager;
        [SerializeField] private ChartEditorGrid gridSystem;

        [Header("キー割り当て設定")]
        [SerializeField] private KeyCode lane0Key = KeyCode.F; // レーン0（左）
        [SerializeField] private KeyCode lane1Key = KeyCode.J; // レーン1（右）

        [Header("入力パラメータ")]
        [SerializeField] private float longPressThreshold = 0.15f; // 単押し/ロング判定のしきい値(秒)

        // リアルタイム打ち込み管理（レーンごとの保持データ）
        private NotesCon[] activeRecordingNotes = new NotesCon[2];
        private float[] holdStartTimes = new float[2];

        private void Awake()
        {
            // 同一GameObjectからManager参照を自動取得
            editorManager = GetComponent<ChartEditorManager>();

            // GridSystemが未設定の場合は同じオブジェクトから取得を試みる
            if (gridSystem == null)
            {
                gridSystem = GetComponent<ChartEditorGrid>();
            }
        }

        private void Update()
        {
            if (AudioManager.Instance == null) return;
            float currentTime = AudioManager.Instance.GetCurrentTime();

            // 1. ショートカットキー入力の監視
            HandleShortcutInputs();

            // 2. レーン別のノーツ打ち込み処理（Fキー / Jキー）
            ProcessKeyRec(0, lane0Key, currentTime);
            ProcessKeyRec(1, lane1Key, currentTime);
        }

        /// <summary>
        /// ショートカットキー（保存・グリッド切り替え）の処理
        /// </summary>
        private void HandleShortcutInputs()
        {
            // Ctrl + S : 譜面データのJSON保存
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
            {
                editorManager.SaveChartData();
            }

            // Gキー : グリッドスナップの ON / OFF 切替
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (gridSystem != null)
                {
                    gridSystem.isSnapEnabled = !gridSystem.isSnapEnabled;
                    Debug.Log($"[EditorInput] グリッドスナップ: {(gridSystem.isSnapEnabled ? "ON" : "OFF")}");
                }
            }

            // Tabキー : 拍分割数の切り替え (4分 -> 8分 -> 16分 -> 32分)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (gridSystem != null)
                {
                    gridSystem.CycleDivision();
                    Debug.Log($"[EditorInput] 拍分割を変更: {gridSystem.beatDivision}分音符");
                }
            }
        }

        /// <summary>
        /// ノーツの入力監視・リアルタイム伸長・スナップ確定処理
        /// </summary>
        private void ProcessKeyRec(int lane, KeyCode key, float currentTime)
        {
            float bpm = editorManager.activeChartData.bpm;

            // ----------------------------------------------------
            // 1. 押した瞬間：開始時間を記録（＋スナップ）して仮ロングを生成
            // ----------------------------------------------------
            if (Input.GetKeyDown(key))
            {
                // グリッドが有効なら現在時間を最寄りの拍へスナップ
                float startTime = (gridSystem != null) ? gridSystem.SnapTime(currentTime, bpm) : currentTime;
                holdStartTimes[lane] = startTime;

                // 打ち込み中のリアルタイムプレビュー用オブジェクトを生成
                activeRecordingNotes[lane] = editorManager.SpawnHandlePreview(lane, startTime);
            }

            // ----------------------------------------------------
            // 2. 押している間：入力時間に応じてリアルタイムにお尻を伸ばす
            // ----------------------------------------------------
            if (Input.GetKey(key) && activeRecordingNotes[lane] != null)
            {
                float currentDuration = currentTime - holdStartTimes[lane];
                float initialTargetTime = holdStartTimes[lane] + editorManager.PrespawnTime;

                // リアルタイム表示の更新
                activeRecordingNotes[lane].SetEndTime(initialTargetTime + currentDuration);
            }

            // ----------------------------------------------------
            // 3. 離した瞬間：時間判定と最終スナップを行って確定
            // ----------------------------------------------------
            if (Input.GetKeyUp(key))
            {
                float rawDuration = currentTime - holdStartTimes[lane];

                if (rawDuration < longPressThreshold)
                {
                    // 単押し（Shortノーツ）判定
                    // 仮生成していたロング用オブジェクトを破棄
                    if (activeRecordingNotes[lane] != null)
                    {
                        Destroy(activeRecordingNotes[lane].gameObject);
                        activeRecordingNotes[lane] = null;
                    }

                    // type: 0 (Short), length: 0
                    editorManager.AddNote(holdStartTimes[lane], lane, 0, 0f);
                }
                else
                {
                    // ロングノーツ確定判定
                    // 離した時間もグリッドにスナップさせて正確な長さを計算
                    float snappedEndTime = (gridSystem != null) ? gridSystem.SnapTime(currentTime, bpm) : currentTime;
                    float snappedDuration = Mathf.Max(0.1f, snappedEndTime - holdStartTimes[lane]);

                    // type: 1 (Long), length: snappedDuration
                    editorManager.AddNote(holdStartTimes[lane], lane, 1, snappedDuration);

                    // リアルタイムで伸びていたオブジェクトを寿命設定して解放
                    if (activeRecordingNotes[lane] != null)
                    {
                        float destroyDelay = editorManager.PrespawnTime + snappedDuration + 0.5f;
                        Destroy(activeRecordingNotes[lane].gameObject, destroyDelay);
                        activeRecordingNotes[lane] = null;
                    }
                }
            }
        }
    }

}
