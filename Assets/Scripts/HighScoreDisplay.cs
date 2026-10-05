using System;
using System.Globalization;
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

    public void SetScore(int newScore, int newDifficulty = 0, string timestamp = null)
    {
        score = newScore;
        difficulty = newDifficulty;
        if (textDisplay == null)
        {
            return;
        }

        string line = place + ".  " + FancyNum(score);
        string date = FormatDate(timestamp);
        if (string.IsNullOrEmpty(date))
        {
            textDisplay.text = line;
            return;
        }

        string dateMarkup = "<size=65%>" + date + "</size>";
        if (textDisplay.textInfo == null)
        {
            textDisplay.text = line + dateMarkup;
            return;
        }

        textDisplay.text = line + dateMarkup;
        textDisplay.ForceMeshUpdate(true);
        if (!TryGetRightEdgePercent(date.Length, out float percent))
        {
            textDisplay.text = line + dateMarkup;
            return;
        }

        textDisplay.text = line + PosTag(percent) + dateMarkup;
        textDisplay.ForceMeshUpdate(true);
        if (TryGetRightEdgePercent(date.Length, out float adjusted) && Mathf.Abs(adjusted - percent) > 0.5f)
        {
            textDisplay.text = line + PosTag(adjusted) + dateMarkup;
        }
    }

    static string PosTag(float percent)
    {
        return "<pos=" + percent.ToString("0.##", CultureInfo.InvariantCulture) + "%>";
    }

    bool TryGetRightEdgePercent(int trailingCharCount, out float percent)
    {
        percent = 0f;
        float containerWidth = textDisplay.rectTransform.rect.width;
        if (containerWidth <= 0.01f || !TryMeasureTrailingWidth(trailingCharCount, out float dateWidth))
        {
            return false;
        }

        float start = Mathf.Clamp(containerWidth - dateWidth, 0f, containerWidth);
        percent = start / containerWidth * 100f;
        return true;
    }

    bool TryMeasureTrailingWidth(int charCount, out float width)
    {
        width = 0f;
        TMP_TextInfo info = textDisplay.textInfo;
        if (info == null || info.characterInfo == null || charCount <= 0)
        {
            return false;
        }

        int count = info.characterCount;
        if (count == 0)
        {
            return false;
        }

        int start = Mathf.Max(0, count - charCount);
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = start; i < count; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            minX = Mathf.Min(minX, character.origin);
            maxX = Mathf.Max(maxX, character.xAdvance);
        }

        if (minX == float.MaxValue)
        {
            return false;
        }

        width = Mathf.Max(0f, maxX - minX);
        return true;
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

    static string FormatDate(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
        {
            return "";
        }

        if (!DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return "";
        }

        return parsed.ToLocalTime().ToString("M/d/yy", CultureInfo.InvariantCulture);
    }
}
