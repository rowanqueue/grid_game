using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class Token : MonoBehaviour
{
    public Logic.Token token;
    public SpriteRenderer spriteDisplay;
    public SpriteRenderer number;
    public SpriteRenderer shadow;
    public SpriteRenderer gnome;
    public TextMeshPro textDisplay;
    public ParticleSystem placementParticles;
    public ParticleSystem scoreParticles;

    float totalDeathMovement = 0.5f;
    Vector3 finalPos;

    public float liftSpeed = 0.9f;
    public float liftHeight = 0.75f;

    public GameObject shade;

    bool moving = false;
    public Vector2 handPos;
    bool initialized = false;
    bool wiggling = false;
    Coroutine wiggleAnim;

    bool beingSpaded = false;
    public bool lifted = false;
    public bool waitingToDie = false;
    public void Init(Logic.Token _token)
    {
        initialized = true;
        token = _token;
        SetTokenData(token.data);
        //spriteDisplay.color = Services.Visuals.tokenColors[(int)token.data.color];
    }
    public void UpgradeToken(Logic.Token _token)
    {
        initialized = true;
        token = _token;
        StartCoroutine(Upgrade());
    }
    IEnumerator Upgrade()
    {
        yield return new WaitForSeconds(0.08f);
        SetTokenData(token.data);
    }
    public void SetTokenData(Logic.TokenData tokenData)
    {
        gnome.enabled = tokenData.color == Logic.TokenColor.Gnome;
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
            if (tokenData.color == Logic.TokenColor.Spade && tokenData.temporary == false)
            {
                spriteDisplay.sprite = Services.Visuals.trainingSpade;
            }
            if (tokenData.color == Logic.TokenColor.Adder)
            {
                spriteDisplay.sprite = Services.Visuals.clippingSprites[tokenData.num];
            }
            if (tokenData.color == Logic.TokenColor.Adder && tokenData.temporary == false)
            {
                spriteDisplay.sprite = Services.Visuals.trainingAdder;
            }
            if (tokenData.color == Logic.TokenColor.Clipper && tokenData.temporary == false)
            {
                spriteDisplay.sprite = Services.Visuals.trainingClipper;
            }
            textDisplay.text = Services.GameController.ScoreToken(tokenData).ToString();
            textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + textDisplay.text;
            number.color = Services.Visuals.tokenColors[(int)tokenData.color];
            //textDisplay.color = number.color;
            number.enabled = false;
            if (tokenData.color == Logic.TokenColor.Clipper || tokenData.color == Logic.TokenColor.Spade || tokenData.color == Logic.TokenColor.Adder || tokenData.color == Logic.TokenColor.Gnome)
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
    public void ToolAnim(Token toolToken, int index)
    {
        if (toolToken.token.data.color == Logic.TokenColor.Spade)
        {
            beingSpaded = true;
        }
        
        StartCoroutine(ToolMovesTowards(toolToken));
    }
    IEnumerator ToolMovesTowards(Token tool)
    {
        Token newToken = GameObject.Instantiate(this, transform.parent).GetComponent<Token>();
        //newToken.UpdateLayer("TokenPlaced");
        newToken.token = token;
        newToken.SetTokenData(token.data);
        newToken.transform.localPosition = transform.localPosition;
        newToken.spriteDisplay.enabled = false;
        newToken.shadow.enabled = false;
        newToken.number.enabled = false;
        newToken.textDisplay.transform.parent = transform.parent;
        newToken.textDisplay.transform.localScale = Vector3.one * 1.4f;
        newToken.textDisplay.text = Services.GameController.ScoreToken(token.data).ToString();
        newToken.textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + newToken.textDisplay.text;
        //newToken.Die();
        //Services.GameController.dyingTokens.Add(newToken);
        while (Vector3.Distance(transform.position, tool.transform.position) > 0.05f)
        {
            tool.transform.position += (transform.position - tool.transform.position) * 0.15f;
            yield return new WaitForEndOfFrame();
        }
        newToken.StartKillNumber(0.05f);
        beingSpaded = false;
        GameObject.Destroy(tool.gameObject);
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
        transform.position = new Vector2(-2.5f, -8f);
        StartCoroutine(BagDraw(index * 0.2f));//*(1f/1.5f)));
    }
    IEnumerator BagDraw(float delay)
    {
        yield return new WaitForSeconds(delay);
        Services.GameController.tempDeckNumberForAnim -= 1;
        Services.AudioManager.PlayNewTileSound();
        while (Vector3.Distance(handPos, transform.position) > 0.05f)
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
        if (beingSpaded)
        {
            return;
        }
        if (initialized == false)
        {
            Init(token);
        }

        if (moving) { return; }
        float shadow_scale = Mathf.InverseLerp(0f, 0.5f, spriteDisplay.transform.localPosition.y);
        shadow.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.75f, shadow_scale);
        if (lifted)
        {
            spriteDisplay.transform.localPosition += (Vector3.up * 0.5f - spriteDisplay.transform.localPosition) * 1.5f * (Time.deltaTime / 0.16666f);
            //shadow.transform.localPosition = new Vector2(0, -0.5f);
        }
        else
        {
            spriteDisplay.transform.localPosition += (Vector3.zero - spriteDisplay.transform.localPosition) * 1.5f * (Time.deltaTime / 0.16666f);
            //shadow.transform.localPosition = Vector3.zero;
        }
        transform.position += ((Vector3)pos - transform.position) * 1.5f * (Time.deltaTime / 0.16666f);
        if (spriteDisplay.sortingLayerName == "TokenPlaced")
        {
            if (Services.GameController.lastTokenPlaced == this && Services.GameController.inputState != InputState.Wait)
            {
                if (wiggling == false)
                {
                    wiggleAnim = StartCoroutine(Wiggle());
                }
            }
            else
            {
                if (wiggling)
                {
                    wiggling = false;
                    StopCoroutine(wiggleAnim);
                }
                transform.localEulerAngles = Vector3.zero;
            }
        }

        //border.enabled = hover;
        if (spriteDisplay.sortingLayerName == "TokenMoving")
        {
            if (Vector2.Distance(pos, transform.position) < 0.1f && lifted)
            {
                lifted = false;
                StartCoroutine(LowerLift());
            }
        }
    }
    IEnumerator LowerLift()
    {
        placementParticles.Play();
        Debug.Log("Lowering lift");
        yield return new WaitForSeconds(0.3f);     
        UpdateLayer("TokenPlaced");
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
        scoreParticles.Play();
        textDisplay.text = Services.GameController.ScoreToken(token.data).ToString();
        textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + textDisplay.text;
        finalPos = transform.localPosition + Vector3.up * liftHeight;
        Services.AudioManager.PlayRemoveTileSound(1);
        StartCoroutine(Dying());
    }
    IEnumerator Wiggle()
    {
        wiggling = true;
        float speed = 0.225f * 1.3f;
        float targetAngle = 3f;
        yield return new WaitForSeconds(0.35f);

        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.z, targetAngle)) > 0.1f)
        {
            float angle = transform.localEulerAngles.z;
            angle = Mathf.LerpAngle(angle, targetAngle, speed);
            transform.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return new WaitForEndOfFrame();
        }
        transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        for (int i = 0; i < 5; i++)
        {
            targetAngle *= -1f;
            while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.z, targetAngle)) > 0.1f)
            {
                float angle = transform.localEulerAngles.z;
                angle = Mathf.LerpAngle(angle, targetAngle, speed);
                transform.localEulerAngles = new Vector3(0f, 0f, angle);
                yield return new WaitForEndOfFrame();
            }
            transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        }

        targetAngle = 0f;
        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.z, targetAngle)) > 0.1f)
        {
            float angle = transform.localEulerAngles.z;
            angle = Mathf.LerpAngle(angle, targetAngle, speed);
            transform.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return new WaitForEndOfFrame();
        }
        transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        wiggling = false;
    }
    IEnumerator Dying()
    {
        float speed = liftSpeed;
        float targetAngle = Random.Range(2.5f, 5f);
        if (Random.value < 0.5f)
        {
            targetAngle *= -1f;
        }
        Services.GameController.dyingTokens.Add(this);
        UpdateLayer("TokenMoving");
        while (Mathf.Abs(transform.localEulerAngles.z - targetAngle) < 0.1f)
        {
            float angle = transform.localEulerAngles.z;
            angle += (targetAngle - angle) * speed * 0.5f;
            transform.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return new WaitForEndOfFrame();
        }
        transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        finalPos = Vector3.up * liftHeight;
        waitingToDie = true;
        while (Vector2.Distance(finalPos, spriteDisplay.transform.localPosition) > 0.01f)
        {
            spriteDisplay.transform.localPosition += (finalPos - spriteDisplay.transform.localPosition) * speed;
            totalDeathMovement -= speed;
            if (Vector2.Distance(finalPos, spriteDisplay.transform.localPosition) < 0.25f)
            {
                var a = (float)spriteDisplay.color.a;
                a -= liftSpeed * 0.5f;
                spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, a);
                number.color = new Color(number.color.r, number.color.g, number.color.b, a);
                shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, Mathf.Lerp(0f, 0.5f, a));
            }

            yield return new WaitForEndOfFrame();
        }
        /*while (Vector2.Distance(finalPos,transform.localPosition) > 0.01f)
        {
            transform.localPosition += (finalPos - transform.localPosition) * speed;
            totalDeathMovement -= speed;
            if(Vector2.Distance(finalPos,transform.localPosition) < 0.25f)
            {
                var a = (float)spriteDisplay.color.a;
                a -= liftSpeed * 0.5f;
                spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, a);
                number.color = new Color(number.color.r, number.color.g, number.color.b, a);
            }
            
            yield return new WaitForEndOfFrame();
        }*/

        while (spriteDisplay.color.a > 0.05f)
        {
            var a = (float)spriteDisplay.color.a;
            a -= liftSpeed;
            spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, a);
            number.color = new Color(number.color.r, number.color.g, number.color.b, a);
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, Mathf.Lerp(0, 0.5f, a));
            yield return new WaitForEndOfFrame();
        }


        //this is right before number erases itself
        //make it so everything happens

        //yield return null;


    }
    public void StartKillNumber(float delay = 0f)
    {
        StartCoroutine(KillNumber(delay));
    }
    IEnumerator KillNumber(float delay)
    {
        yield return new WaitForSeconds(delay);
        float speed = liftSpeed;
        float waitTime = Random.Range(0.1f, 0.4f);//0.33f
        yield return new WaitForSeconds(waitTime);
        while (textDisplay.transform.localScale.x > 0.2f)
        {
            textDisplay.transform.localScale -= Vector3.one * speed * 0.95f;
            yield return new WaitForEndOfFrame();

        }
        //todo: make this the right amount of points
        //Services.GameController.dyingTokens.Remove(this);
        Services.GameController.scoreDelta += Services.GameController.ScoreToken(token.data);
        GameObject.Destroy(textDisplay.gameObject);
        GameObject.Destroy(gameObject);
    }
    public void ShowCrunchedDisplay(int num)
    {
        textDisplay.transform.position -= new Vector3(0, 0.5f);
        textDisplay.outlineWidth = 0.3f;
        textDisplay.outlineColor = Color.black;
        textDisplay.sortingLayerID = spriteDisplay.sortingLayerID;
        textDisplay.sortingOrder = spriteDisplay.sortingOrder + 5;
        textDisplay.text = num.ToString();
    }
}
