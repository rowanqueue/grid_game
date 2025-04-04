using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HighScoreDisplay : MonoBehaviour
{
    public TextMeshPro textDisplay;
    public int place;
    public int score;
    int spaces = 25;
    public void Start()
    {
        int sofar = spaces;
        textDisplay.text = "<mspace=0em>";
        textDisplay.text += place.ToString() + ".";
        sofar -= place.ToString().Length + 1;
        string num = FancyNum(score);
        sofar -= num.Length;
        for(int i = 0; i < sofar; i++)
        {
            textDisplay.text += " ";
        }
        textDisplay.text += num;
    }
    string FancyNum(int num)
    {
        string s = string.Empty;
        string _number = num.ToString("N0");
        for (int i = 0; i < _number.Length; i++)
        {
            if (i < _number.Length - 1 && _number[i + 1] == ',')
            {
                //s += "<cspace=-0.2em>";
            }
            s += _number[i];
            if (i > 0 && _number[i - 1] == ',')
            {
                //s += "</cspace>";
            }
        }
        return s;
    }
}
