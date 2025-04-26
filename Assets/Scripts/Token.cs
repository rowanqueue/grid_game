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

    float totalDeathMovement = 0.5f;
    Vector3 finalPos;

    public float liftSpeed = 0.9f;
    public float liftHeight = 0.75f;

    public GameObject shade;

    bool moving = false;
    public Vector2 handPos;
    public void Init(Logic.Token _token)
    {
        token = _token;
        SetTokenData(token.data);
        //spriteDisplay.color = Services.Visuals.tokenColors[(int)token.data.color];
    }
    public void SetTokenData(Logic.TokenData tokenData)
    {
        if (tokenData.num >= ((Logic.TripleGame)Services.GameController.game).maxTileNum)
        {
            spriteDisplay.sprite = Services.Visuals.tokenMax[(int)tokenData.color];
            if (Services.GameController.diceMode)
            {
                number.sprite = Services.Visuals.altNumberSprites[(int)tokenData.num];
            }
            number.enabled = false;
        }
        else
        {
            spriteDisplay.sprite = Services.Visuals.tokenSprites[(int)tokenData.color];
            if (tokenData.color == Logic.TokenColor.Adder)
            {
                spriteDisplay.sprite = Services.Visuals.clippingSprites[tokenData.num];
            }
            textDisplay.text = Services.GameController.ScoreToken(tokenData).ToString();
            textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + textDisplay.text;
            number.color = Services.Visuals.tokenColors[(int)tokenData.color];
            //textDisplay.color = number.color;
            number.enabled = false;
            if (tokenData.color == Logic.TokenColor.Clipper || tokenData.color == Logic.TokenColor.Spade || tokenData.color == Logic.TokenColor.Adder)
            {
                return;
            }
            if (tokenData.num >= 0)
            {
                number.enabled = true;
                number.sprite = Services.Visuals.numberSprites[(int)tokenData.num];
                if (Services.GameController.diceMode)
                {
                    number.sprite = Services.Visuals.altNumberSprites[(int)tokenData.num];
                }
                
            }
        }
    }
    public void PlaceInHand(int index)
    {
        transform.position = Services.GameController.firstHandPos + (index * Services.GameController.handSeparation);
        transform.localEulerAngles = new Vector3(0, 0, Random.Range(-10f, 10f));
    }
    public void TurnShade()
    {
        shade.SetActive(true);
    }
    public void PlaceInTile(Tile tile)
    {
        transform.position = tile.transform.position;
        UpdateLayer("TokenPlaced");
    }
    public void DrawFromBag(int index)
    {
        moving = true;
        transform.position = Services.GameController.bagButtonTransform.position;
        StartCoroutine(BagDraw(index * 0.2f));//*(1f/1.5f)));
    }
    IEnumerator BagDraw(float delay)
    {
        yield return new WaitForSeconds(delay);
        Services.GameController.tempDeckNumberForAnim -= 1;
        Services.AudioManager.PlayNewTileSound();
        while (Vector3.Distance(handPos,transform.position) > 0.05f)
        {
            transform.position += ((Vector3)handPos - transform.position) * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        /*while (Mathf.Abs(handPos.y - transform.position.y) > 0.1f)
        {
            transform.position += (new Vector3(0, handPos.y > transform.position.y ? 1f : -1f)) * 0.1f*1.5f;
            yield return new WaitForEndOfFrame();
        }
        transform.position = new Vector3(transform.position.x, handPos.y);
        while (Mathf.Abs(handPos.x - transform.position.x) > 0.1f)
        {
            transform.position += (new Vector3(handPos.x > transform.position.x ? 1f : -1f,0)) * 0.1f*1.5f;
            yield return new WaitForEndOfFrame();
        }*/
        transform.position = handPos;
        moving = false;
    }
    public void Draw(Vector2 pos, bool hover = false)
    {
        Init(token);
        if (moving) { return; }
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
    public void Die()
    {
        textDisplay.transform.parent = transform.parent;
        textDisplay.transform.localScale = Vector3.one * 1.4f;
        finalPos = transform.localPosition + Vector3.up * liftHeight;
        Services.AudioManager.PlayRemoveTileSound(1);
        StartCoroutine(Dying());
    }
    IEnumerator Dying()
    {
        float speed = liftSpeed;
        UpdateLayer("TokenMoving");
        while(Vector2.Distance(finalPos,transform.localPosition) > 0.01f)
        {
            transform.localPosition += (finalPos - transform.localPosition) * speed;
            totalDeathMovement -= speed;
            yield return new WaitForEndOfFrame();
        }
        while(spriteDisplay.color.a > 0.05f)
        {
            var a = (float)spriteDisplay.color.a;
            a -= liftSpeed;
            spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, a);
            number.color = new Color(number.color.r, number.color.g, number.color.b, a);
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(0.33f);
        while (textDisplay.transform.localScale.x > 0.2f)
        {
            textDisplay.transform.localScale -= Vector3.one * speed * 0.95f;
            yield return new WaitForEndOfFrame();
        }
        GameObject.Destroy(textDisplay.gameObject);
        GameObject.Destroy(gameObject);
        //yield return null;

    }
}
