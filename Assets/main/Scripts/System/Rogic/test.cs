using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [Header("必須コンポーネント・アセット参照")]
    [SerializeField] private CSVLoader loader;         // CSVLoaderスクリプト
    [SerializeField] private AudioDataSO audioDataSO;   // 楽曲データ用SO
    [SerializeField] private NotsSpawn notsSpawn;       // お前の自動生成スクリプト

    [Header("実行設定")]
    [Tooltip("チェックを入れると、再生開始時（Start）に自動で読み込みと再生を行うぜ（テスト用）")]
    [SerializeField] private bool autoStartOnLaunch = true;

    private void Start()
    {
        // テスト時や、即座に楽曲を流したい場合は起動時に自動実行
        if (autoStartOnLaunch)
        {
            InitializeAndPlayChart();
        }
    }

    /// <summary>
    /// 🔥 【本編・テスト共通】譜面を読み込み、データを同期して、楽曲を再生するコアメソッド！
    /// </summary>
    [ContextMenu("🔗 Load And Play Now")]
    public void InitializeAndPlayChart()
    {
        // 1. 架け橋としての厳格なアタッチチェック
        if (loader == null || audioDataSO == null || audioDataSO.notesobjSO == null || notsSpawn == null)
        {
            Debug.LogError("<color=red>【架け橋エラー】コンポーネントのアタッチが足りないぜ、ブラザー！インスペクターを確認してくれ！</color>");
            return;
        }

        // 2. CSVLoaderにデータをセットして、CSVからUnityデータへ翻訳（パース）
        loader.audioDataSO = audioDataSO;
        List<NoteDate.Notes> parsedNotes = loader.LoadChartFromAudioData();

        if (parsedNotes == null || parsedNotes.Count == 0)
        {
            Debug.LogError("<color=red>【架け橋エラー】CSVの解析結果が空、またはヌルだぜ。譜面データが書き出されているか確認だ！</color>");
            return;
        }

        // 3. 翻訳データを ScriptableObject (譜面SO) へガチッと流し込む
        audioDataSO.notesobjSO.notes = parsedNotes;

        // エディタ上なら、変更をディスクに物理保存して忘れないようにする
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(audioDataSO.notesobjSO);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log($"<color=cyan>【架け橋データ同期完了】</color> 対象: <b>{audioDataSO.musicTitle}</b> | 総ノーツ数: <color=lime>{parsedNotes.Count}</color>");

        // 4. 🔥 お前の NotsSpawn に「データが新しくなったぞ！」と通知して同期させる！
        notsSpawn.RefreshChartData();

        // 5. 実際にゲームが動いているなら、曲を再生してノーツを流し始める！
        if (Application.isPlaying)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayActiveMusic();
                Debug.Log("<color=green>【ミュージックスタート！】さあ、ノーツが上から降ってくるぜ！プレイテスト開始だ！</color>");
            }
            else
            {
                Debug.LogWarning("AudioManager.Instance が見つからないぜ。ノーツの生成テストだけ進めるぜ！");
            }
        }
    }

    /// <summary>
    ///本編（製品版）用拡張メソッド】選曲画面から曲を切り替えてゲームを始める時に使うぜ！
    /// </summary>
    /// <param name="selectedMusic">選曲画面から渡された新しい曲のSO</param>
    public void SetupNewStage(AudioDataSO selectedMusic)
    {
        // 楽曲データを上書きして、生成スクリプト側にも教えてあげる
        audioDataSO = selectedMusic;
        notsSpawn.audioDataSO = selectedMusic;

        // あとはいつもの流れをドン！
        InitializeAndPlayChart();
    }
}
