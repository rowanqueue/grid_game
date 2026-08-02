using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
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
    const int MinViewDifficulty = 1;

    public int maxEntries = 10;

    [Tooltip("Optional explicit list of HighScoreDisplay components. If empty, they will be discovered in the scene.")]
    public List<HighScoreDisplay> displays = new List<HighScoreDisplay>();

    [Tooltip("Difficulty currently shown on the high score screen (Novice=1 .. Sage). Tutorial/0 is never shown.")]
    public int viewDifficulty = MinViewDifficulty;

    public TextMeshPro difficultyNameDisplay;
    public flora.Button lowerDifficultyButton;
    public flora.Button higherDifficultyButton;

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
    /// Tutorial (0) maps to Novice (1). Clamps into the navigable difficulty range.
    /// </summary>
    public int NormalizeDifficulty(int difficulty)
    {
        int max = GetMaxViewDifficulty();
        if (difficulty <= 0)
        {
            difficulty = MinViewDifficulty;
        }
        return Mathf.Clamp(difficulty, MinViewDifficulty, max);
    }

    public void SetViewDifficulty(int difficulty)
    {
        viewDifficulty = NormalizeDifficulty(difficulty);
        RefreshUI();
    }

    public void HigherViewDifficulty()
    {
        SetViewDifficulty(viewDifficulty + 1);
    }

    public void LowerViewDifficulty()
    {
        SetViewDifficulty(viewDifficulty - 1);
    }

    /// <summary>
    /// Adds a score to the high score table if it qualifies for that difficulty's top N.
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
        if (data.entries == null)
        {
            data.entries = new List<HighScoreEntry>();
        }

        int normalized = NormalizeDifficulty(difficulty);
        HighScoreEntry entry = new HighScoreEntry
        {
            score = score,
            difficulty = normalized,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        data.entries.Add(entry);
        TrimToMaxPerDifficulty();
        Save();
        RefreshUI();
    }

    public bool WouldBeNewBest(int score, int difficulty)
    {
        if (score <= 0)
        {
            return false;
        }
        if (data == null || data.entries == null || data.entries.Count == 0)
        {
            return true;
        }

        int normalized = NormalizeDifficulty(difficulty);
        int bestForDifficulty = data.entries
            .Where(e => e.difficulty == normalized)
            .Select(e => e.score)
            .DefaultIfEmpty(0)
            .Max();
        return score > bestForDifficulty;
    }

    public void RefreshUI()
    {
        viewDifficulty = NormalizeDifficulty(viewDifficulty);
        UpdateDifficultyChrome();

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

        List<HighScoreEntry> filtered = GetEntriesForDifficulty(viewDifficulty);

        for (int i = 0; i < displays.Count; i++)
        {
            if (i < filtered.Count)
            {
                HighScoreEntry entry = filtered[i];
                displays[i].SetScore(entry.score, entry.difficulty);
            }
            else
            {
                displays[i].SetEmpty();
            }
        }
    }

    void UpdateDifficultyChrome()
    {
        if (difficultyNameDisplay != null)
        {
            difficultyNameDisplay.text = GetDifficultyLabel(viewDifficulty);
        }

        int max = GetMaxViewDifficulty();
        if (lowerDifficultyButton != null)
        {
            lowerDifficultyButton.disabled = viewDifficulty <= MinViewDifficulty;
        }
        if (higherDifficultyButton != null)
        {
            higherDifficultyButton.disabled = viewDifficulty >= max;
        }
    }

    List<HighScoreEntry> GetEntriesForDifficulty(int difficulty)
    {
        if (data == null || data.entries == null)
        {
            return new List<HighScoreEntry>();
        }

        return data.entries
            .Where(e => e.difficulty == difficulty)
            .OrderByDescending(e => e.score)
            .ThenBy(e => e.timestamp)
            .Take(Mathf.Max(1, maxEntries))
            .ToList();
    }

    int GetMaxViewDifficulty()
    {
        if (Services.GameController != null && Services.GameController.difficultyNames != null
            && Services.GameController.difficultyNames.Count > MinViewDifficulty)
        {
            return Services.GameController.difficultyNames.Count - 1;
        }
        return 4;
    }

    static string GetDifficultyLabel(int difficultyIndex)
    {
        if (Services.GameController == null || Services.GameController.difficultyNames == null)
        {
            return string.Empty;
        }
        if (difficultyIndex < 0 || difficultyIndex >= Services.GameController.difficultyNames.Count)
        {
            return string.Empty;
        }

        string raw = Services.GameController.difficultyNames[difficultyIndex];
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        int br = raw.IndexOf('<');
        return br >= 0 ? raw.Substring(0, br).Trim() : raw.Trim();
    }

    void TrimToMaxPerDifficulty()
    {
        if (data == null || data.entries == null)
        {
            return;
        }

        int keep = Mathf.Max(1, maxEntries);
        data.entries = data.entries
            .GroupBy(e => e.difficulty)
            .SelectMany(g => g
                .OrderByDescending(e => e.score)
                .ThenBy(e => e.timestamp)
                .Take(keep))
            .ToList();
    }

    void MigrateTutorialEntries()
    {
        if (data == null || data.entries == null)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (data.entries[i].difficulty == 0)
            {
                data.entries[i].difficulty = MinViewDifficulty;
                changed = true;
            }
        }

        if (changed)
        {
            TrimToMaxPerDifficulty();
            Save();
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
            else
            {
                MigrateTutorialEntries();
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
