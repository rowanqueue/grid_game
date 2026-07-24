using System.Collections;
using System.Collections.Generic;
using Logic;
using UnityEngine;

public class PolaroidToken : MonoBehaviour
{
    public SpriteRenderer tileSprite;
    public SpriteRenderer numberSprite;

    public void ResetDisplay()
    {
        tileSprite.enabled = true;
        numberSprite.enabled = true;
        tileSprite.sprite = null;
        numberSprite.sprite = null;
        this.gameObject.SetActive(false);
    }

    public void InitializeDisplay(TokenData tokenData)
    {
        int maxTileNum = 8;
        if (Services.GameController != null && Services.GameController.game is Logic.TripleGame tripleGame)
        {
            maxTileNum = tripleGame.maxTileNum;
        }
        if (tokenData.num >= maxTileNum)
        {
            tileSprite.sprite = Services.Visuals.tokenMax[(int)tokenData.color];
            numberSprite.enabled = false;
        }
        else
        {
            tileSprite.sprite = Services.Visuals.tokenSprites[(int)tokenData.color];
            if (tokenData.color == Logic.TokenColor.Spade && tokenData.temporary == false)
            {
                tileSprite.sprite = Services.Visuals.trainingSpade;
            }
            if (tokenData.color == Logic.TokenColor.Adder)
            {
                tileSprite.sprite = Services.Visuals.clippingSprites[tokenData.num];
            }
            if (tokenData.color == Logic.TokenColor.Adder && tokenData.temporary == false)
            {
                tileSprite.sprite = Services.Visuals.trainingAdder;
            }
            if (tokenData.color == Logic.TokenColor.Clipper && tokenData.temporary == false)
            {
                tileSprite.sprite = Services.Visuals.trainingClipper;
            }
            numberSprite.color = Services.Visuals.tokenColors[(int)tokenData.color];
            //textDisplay.color = number.color;
            numberSprite.enabled = false;
            if (tokenData.color == Logic.TokenColor.Clipper || tokenData.color == Logic.TokenColor.Spade || tokenData.color == Logic.TokenColor.Adder || tokenData.color == Logic.TokenColor.Gnome)
            {
                return;
            }
            if (tokenData.num >= 0)
            {
                numberSprite.enabled = true;
                numberSprite.sprite = Services.Visuals.numberSprites[(int)tokenData.num];
                if (Services.GameController != null && Services.GameController.diceMode)
                {
                    numberSprite.sprite = Services.Visuals.altNumberSprites[(int)tokenData.num];
                }

            }
        }
    }
}
