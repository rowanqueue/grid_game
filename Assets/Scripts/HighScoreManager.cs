using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Manages local high scores and populates HighScoreDisplay UI elements.
/// Attach this to a GameObject in any scene that uses a high score screen.
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance;

    [Serializable]
    public class HighScoreEntry
    {
        public int score;
        public int difficulty;
        public string timestamp;
    }

    [Serializable]
    class HighScoreData
    {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }

    const string PlayerPrefsKey = "highscores";
    public int maxEntries = 5;

    [Tooltip("Optional explicit list of HighScoreDisplay components. If empty, they will be discovered in the scene.")]
    public List<HighScoreDisplay> displays = new List<HighScoreDisplay>();

    HighScoreData data = new HighScoreData();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    /// <summary>
    /// Adds a score to the high score table if it qualifies.
    /// </summary>
    public void SubmitScore(int score, int difficulty)
    {
        if (score <= 0)
        {
            return;
        }

        if (data == null)
        {
            data = new HighScoreData();
        }

        HighScoreEntry entry = new HighScoreEntry
        {
            score = score,
            difficulty = difficulty,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        data.entries.Add(entry);
        data.entries = data.entries
            .OrderByDescending(e => e.score)
            .ThenBy(e => e.timestamp)
            .Take(Mathf.Max(1, maxEntries))
            .ToList();

        Save();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (displays == null || displays.Count == 0)
        {
            // Include inactive objects so the manager can live elsewhere in the scene hierarchy.
            displays = FindObjectsOfType<HighScoreDisplay>(true)
                .OrderBy(d => d.place)
                .ToList();
        }

        if (displays == null || displays.Count == 0)
        {
            return;
        }

        for (int i = 0; i < displays.Count; i++)
        {
            if (data != null && data.entries != null && i < data.entries.Count)
            {
                displays[i].SetScore(data.entries[i].score);
            }
            else
            {
                displays[i].SetEmpty();
            }
        }
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            data = new HighScoreData();
            return;
        }

        try
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey);
            if (string.IsNullOrEmpty(json))
            {
                data = new HighScoreData();
                return;
            }

            data = JsonConvert.DeserializeObject<HighScoreData>(json);
            if (data == null || data.entries == null)
            {
                data = new HighScoreData();
            }
        }
        catch (Exception)
        {
            // If anything goes wrong with deserialization, reset the table.
            data = new HighScoreData();
        }
    }

    void Save()
    {
        if (data == null)
        {
            data = new HighScoreData();
        }

        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }
}

