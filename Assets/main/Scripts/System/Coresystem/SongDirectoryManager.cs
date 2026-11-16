using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SongDirectory
{
    public string folderName;
    public string folderPath;
    public string audioPath;
    public string jsonPath;
}

public static class SongDirectoryManager
{
    public static List<SongDirectory> GetSongDirectories()
    {
        List<SongDirectory> list = new List<SongDirectory>();
        string songsPath = Path.Combine(Application.dataPath, "Songs");

        if (!Directory.Exists(songsPath))
        {
            Directory.CreateDirectory(songsPath);
            Debug.LogWarning($"[SongDirectoryManager] Songsフォルダを作成しました: {songsPath}");
            return list;
        }

        string[] directories = Directory.GetDirectories(songsPath);
        foreach (string dir in directories)
        {
            string folderName = Path.GetFileName(dir);

            // 「Audio」フォルダや隠しフォルダを誤検出しないよう除外
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

            // フォルダ内の音源ファイル (.wav / .mp3 / .ogg) を検索
            string[] files = Directory.GetFiles(dir);
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                {
                    songDir.audioPath = file;
                    break;
                }
            }

            // 音源ファイルが含まれているフォルダのみ有効な曲として登録
            if (!string.IsNullOrEmpty(songDir.audioPath))
            {
                list.Add(songDir);
            }
        }

        return list;
    }
}