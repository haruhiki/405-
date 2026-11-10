using System;
using UnityEngine;
using System.Collections.Generic;   
using System.IO;
using UnityEngine.Rendering;


/// <summary> /// 楽曲ディレクトリの情報を保持するクラス /// </summary>
public class SongDirectory
{
        public string folderName;
        public string folderPath;
        public string audioPath;
        public string jsonPath;

}

/// <summary> /// 楽曲ディレクトリの管理を行う静的クラス /// </summary>
public static class SongDirectoryManager
{

   public static List<SongDirectory> GetSongDirectories()
    {
        List<SongDirectory> list = new List<SongDirectory>();
        
        // Assets/Songs フォルダの絶対パスを指定
        string songsPath = Path.Combine(Application.dataPath, "Songs");

        // Songs フォルダが存在しない場合は自動作成して抜ける
        if (!Directory.Exists(songsPath))
        {
            Directory.CreateDirectory(songsPath);
            Debug.LogWarning($"[SongDirectoryManager] Songsフォルダが存在しなかったため作成しました: {songsPath}");
            return list;
        }

        // Assets/Songs/ の中にあるサブフォルダのみを取得
        string[] directories = Directory.GetDirectories(songsPath);
        foreach (string dir in directories)
        {
            string folderName = Path.GetFileName(dir);

            // 「Audio」や「.」から始まるシステムフォルダをスキップ
            if (folderName.Equals("Audio", StringComparison.OrdinalIgnoreCase) || folderName.StartsWith("."))
            {
                continue;
            }

            SongDirectory songDir = new SongDirectory
            {
                folderName = folderName,
                folderPath = dir,
                jsonPath = Path.Combine(dir, "chart.json")
            };

            // フォルダ内の音源ファイル (.wav / .mp3) を検索
            string[] files = Directory.GetFiles(dir);
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".wav" || ext == ".mp3")
                {
                    songDir.audioPath = file;
                    break;
                }
            }

            // 音源ファイルが見つかったフォルダのみリストに追加
            if (!string.IsNullOrEmpty(songDir.audioPath))
            {
                list.Add(songDir);
            }
        }

        return list;
    }

}







