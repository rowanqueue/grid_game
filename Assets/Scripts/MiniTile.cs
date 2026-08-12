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
        if(token.color == TokenColor.Gold)
        {
            edgeColor.color = Color.black;
        }
        number.color = c;
        number.text = token.num.ToString();
        spriteDisplay.enabled = false;
        if (token.color == Logic.TokenColor.Display)
        {
            backColor.enabled = false;
            edgeColor.enabled = false;
            number.text = "<size=35%>x</size><u>" + number.text + "</u>";
            number.enableWordWrapping = false;
            number.overflowMode = TextOverflowModes.Overflow;
            number.rectTransform.sizeDelta = new Vector2(1f, 1f);
        }

    }
    void SetTileWeird(TokenData token)
    {
        backColor.enabled = true;
        edgeColor.enabled = true;
        if (token.temporary)
        {
            backColor.color = Color.gray;
            edgeColor.color = Color.black;
            spriteDisplay.color = Color.white;
        }
        else
        {
            backColor.color = Color.white;
            edgeColor.color = Color.white;
            spriteDisplay.color = Color.gray;
        }
        number.text = "";
        spriteDisplay.enabled = true;
        if (token.color == TokenColor.Adder)
        {
            spriteDisplay.sprite = adder;
        }
        else if (token.color == TokenColor.Spade)
        {
            spriteDisplay.sprite = spade;
        }
        else if (token.color == TokenColor.Clipper)
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
