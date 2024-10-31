using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Token : MonoBehaviour
{
    public Logic.Token token;
    public SpriteRenderer spriteDisplay;
    public SpriteRenderer number;
    public TextMeshPro textDisplay;

    public void Init(Logic.Token _token)
    {
        token = _token;
        //spriteDisplay.color = Services.Visuals.tokenColors[(int)token.data.color];
        Debug.Log((int)token.data.color);
        spriteDisplay.sprite = Services.Visuals.tokenSprites[(int)token.data.color];
        textDisplay.text = token.data.num.ToString();
        number.color = Services.Visuals.tokenColors[(int)token.data.color];
        if (token.data.num >= 0)
        {
            number.sprite = Services.Visuals.numberSprites[(int)token.data.num];
        }
        
    }
    public void PlaceInHand(int index)
    {
        transform.position = Services.GameController.firstHandPos + (index * Services.GameController.handSeparation);
        transform.localEulerAngles = new Vector3(0, 0, Random.Range(-10f, 10f));
    }
    public void PlaceInTile(Tile tile)
    {
        transform.position = tile.transform.position;
        UpdateLayer("TokenPlaced");
    }
    public void Draw(Vector2 pos, bool hover = false)
    {
        Init(token);
        transform.position += ((Vector3)pos - transform.position) * 1.5f * (Time.deltaTime/0.16666f);
        if(Services.GameController.lastTokenPlaced == this)
        {
            float angle = Mathf.Sin(Time.time * 5f) * 3f;
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
        else
        {
            transform.localEulerAngles = Vector3.zero;
        }
        //border.enabled = hover;
        if(spriteDisplay.sortingLayerName == "TokenMoving")
        {
            if(Vector2.Distance(pos,transform.position) < 0.1f)
            {
                UpdateLayer("TokenPlaced");
            }
        }
    }
    public void UpdateLayer(string sortingLayer)
    {
        spriteDisplay.sortingLayerName = sortingLayer;
        number.sortingLayerName = sortingLayer;
        textDisplay.sortingLayerID = spriteDisplay.sortingLayerID;
    }
}
