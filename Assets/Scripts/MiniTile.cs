using Logic;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MiniTile : MonoBehaviour
{
    public SpriteRenderer backColor;
    public SpriteRenderer edgeColor;
    public TextMeshPro number;
    public SpriteRenderer spriteDisplay;
    public Sprite arrow;
    public Sprite plus;
    public Sprite adder;
    public Sprite spade;
    public Sprite clipper;
    // Start is called before the first frame update
    public void SetTile(TokenData token)
    {
        if(token.color == TokenColor.Adder || token.color == TokenColor.Spade || token.color == TokenColor.Clipper)
        {
            SetTileWeird(token);
            return;
        }
        backColor.enabled = true;
        edgeColor.enabled = true;
        Color c = Services.Visuals.tokenColors[(int)token.color];
        backColor.color = c;
        edgeColor.color = Color.white;
        number.color = c;
        number.text = token.num.ToString();
        spriteDisplay.enabled = false;

    }
    void SetTileWeird(TokenData token)
    {
        backColor.enabled = true;
        edgeColor.enabled = true;
        backColor.color = Color.gray;
        edgeColor.color = Color.black;
        number.text = "";
        spriteDisplay.enabled = true;
        if(token.color == TokenColor.Adder)
        {
            spriteDisplay.sprite = adder;
        }
        if (token.color == TokenColor.Spade)
        {
            spriteDisplay.sprite = spade;
        }
        if (token.color == TokenColor.Clipper)
        {
            spriteDisplay.sprite = clipper;
        }
    }
    public void SetArrow()
    {
        backColor.enabled = false;
        edgeColor.enabled = false;
        number.text = "";
        spriteDisplay.enabled = true;
        spriteDisplay.sprite = arrow;
    }
    public void SetPlus()
    {
        backColor.enabled = false;
        edgeColor.enabled = false;
        number.text = "";
        spriteDisplay.enabled = true;
        spriteDisplay.sprite = plus;
    }
}
