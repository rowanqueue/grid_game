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

        textDisplay.text = place + ".  " + FancyNum(score);
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

    string FancyNum(int num)
    {
        return num.ToString("N0");
    }
}
