using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Textbox : MonoBehaviour
{
    public TextMeshPro textDisplay;
    public SpriteRenderer background;

    public void SetText(string text)
    {
        if (textDisplay != null)
            textDisplay.text = text;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
