using UnityEngine;
using TMPro;

public class HighScoreDisplay : MonoBehaviour
{
    public TextMeshPro textDisplay;
    public int place;
    public int score;
    public int difficulty;

    void Awake()
    {
        // Text is owned by HighScoreManager.RefreshUI; leave blank until then.
        if (textDisplay != null && score == 0)
        {
            textDisplay.text = "";
        }
    }

    public void SetScore(int newScore, int newDifficulty = 0)
    {
        score = newScore;
        difficulty = newDifficulty;
        if (textDisplay == null)
        {
            return;
        }

        string difficultyLabel = GetDifficultyLabel(difficulty);
        textDisplay.text = place + ".  " + FancyNum(score);
        if (!string.IsNullOrEmpty(difficultyLabel))
        {
            textDisplay.text += "  <size=70%>" + difficultyLabel + "</size>";
        }
    }

    /// <summary>
    /// Clears the display when there is no high score entry for this rank.
    /// </summary>
    public void SetEmpty()
    {
        score = 0;
        difficulty = 0;
        if (textDisplay != null)
        {
            textDisplay.text = "";
        }
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

    string FancyNum(int num)
    {
        return num.ToString("N0");
    }
}
