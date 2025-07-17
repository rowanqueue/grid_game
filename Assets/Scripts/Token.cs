using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using EZ.Haptics;
using Logic;
using System.Collections.Generic;

public class Token : MonoBehaviour
{
    public Logic.Token token;
    public SpriteRenderer spriteDisplay;
    public SpriteRenderer number;
    public SpriteRenderer shadow;
    public SpriteRenderer gnome;
    public TextMeshPro textDisplay;
    public ParticleSystem placementParticles;
    public ParticleSystem dirtParticles;
    public ParticleSystem sparkleParticles;
    // Small droplets of water that appear when the watering can rotates
    public ParticleSystem adderWaterSquirtParticles;
    public ParticleSystem clippingSlashParticles;
    public FlowerBurstParticleController flowerParticles;
    // Small burst of flowers when clippings add to a token
    public FlowerBurstParticleController adderClippingParticles;

    float totalDeathMovement = 0.5f;
    Vector3 finalPos;

    public float liftSpeed = 0.9f;
    public float liftHeight = 0.75f;
    public float toolLiftSpeed = 0.5f;

    public GameObject shade;

    bool moving = false;
    public Vector2 handPos;
    bool initialized = false;
    bool wiggling = false;
    Coroutine wiggleAnim;

    bool beingSpaded = false;
    public bool lifted = false;
    public bool waitingToDie = false;

    [Header("Clipper Tool Animation Values")]
    [SerializeField] private float Clipper_MoveToTileTime = 0.25f;
    [SerializeField] private float Clipper_SlashTime = 0.25f;
    [SerializeField] private Vector3 ClipperPositionOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private float ClipperSlashXLength = 1f;

    [Header("Spade Tool Animation Values")]
    [SerializeField] private float Spade_MoveToTileTime = 0.5f;
    [SerializeField] private float Spade_DigTime = 0.5f;
    [SerializeField] private Vector3 Spade_PositionOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private Vector3 Spade_DigDestination = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private float Spade_StartingRotation = 30f;
    [SerializeField] private float Spade_EndRotation = 30f;



    public void Init(Logic.Token _token)
    {
        initialized = true;
        token = _token;
        SetTokenData(token.data);
        //spriteDisplay.color = Services.Visuals.tokenColors[(int)token.data.color];
    }

    /// <summary>
    /// Changes the display of the token to match the given data. If this change increases the token number, it will play the upgrade animation
    /// A tool is passed it if it was used to change the token, so that animation timing can be changes
    /// </summary>
    /// <param name="_token"></param>
    public void UpgradeToken(Logic.Token _token, Logic.Token usedTool = null, bool useHaptics = false)
    {
        initialized = true;
        Logic.Token oldToken = token;
        token = _token;

        // Animate the token only when the token's number increases
        if (usedTool != null)
        {
            switch (usedTool.data.color)
            {
                case Logic.TokenColor.Adder:
                    print("Using adder tool to upgrade token");
                    Services.GameController.waiting += 1f;
                    StartCoroutine(AdderUpgradeRoutine(useHaptics, usedTool.data.num != 0));
                    break;
                case Logic.TokenColor.Clipper:
                    print("Using clipper tool to upgrade token");
                    Services.GameController.waiting += 1f;
                    StartCoroutine(ClipperUpgradeRoutine(useHaptics, oldToken.data));
                    break;
            }
        }
        else
        {
            StartCoroutine(DefaultUpgradeRoutine(useHaptics));
        }
    }

    /// <summary>
    /// Plays the upgrade animation for the token
    /// </summary>
    /// <returns></returns>
    IEnumerator DefaultUpgradeRoutine(bool useHaptics)
    {
        print("Default");
        // Delay before starting the upgrade animation
        yield return new WaitForSeconds(0.1f);

        //Services.AudioManager.PlayUpgradeTileSound();
        if (useHaptics)
        {
            Haptics.PlayTransient(1f, .5f);
        }
        

        // Playing the flower burst animation
        StartCoroutine(flowerParticles.PlayFlowerBurstCoroutine(0f, token.data.color));

        // Changing token data (number)
        SetTokenData(token.data);

        // Ending flower burst animation
        flowerParticles.StopFlowerBurst(token.data.color);
    }

    /// <summary>
    /// Upgrade animation for clipping and watering can 
    /// </summary>
    /// <param name="useHaptics"></param>
    /// <returns></returns>
    IEnumerator AdderUpgradeRoutine(bool useHaptics, bool isClipping)
    {
        // Delay for audio to trigger
        yield return new WaitForSeconds(0.6f);

        //Services.AudioManager.PlayUpgradeTileSound();
        if (useHaptics)
        {
            Haptics.PlayTransient(1f, .5f);
        }

        // Playing the flower burst animation
        if (isClipping)
        {
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(flowerParticles.PlayFlowerBurstCoroutine(0f, token.data.color));
        }
        else
        {
            StartCoroutine(flowerParticles.PlayFlowerBurstCoroutine(0f, Logic.TokenColor.Adder));
        }

        // Changing token data (number)
        SetTokenData(token.data);

        // Ending flower burst animation
        flowerParticles.StopFlowerBurst(token.data.color);
    }

    /// <summary>
    /// Occurs after a clipping tool is used, which reduces the token to a lower number
    /// </summary>
    /// <param name="useHaptics"></param>
    /// <returns></returns>
    IEnumerator ClipperUpgradeRoutine(bool useHaptics, Logic.TokenData oldTokenData)
    {
        // Delay waiting for tool animations
        if (oldTokenData.num == 0)
        {
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Changing token data (number)
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

        StartCoroutine(ToolUseAnimation(toolToken));
    }

    IEnumerator ToolUseAnimation(Token tool)
    {
        switch (tool.token.data.color)
        {
            case Logic.TokenColor.Clipper:
                yield return ClipperUseAnimation(tool);
                break;
            case Logic.TokenColor.Spade:
                yield return SpadeUseAnimation(tool);
                break;
            case Logic.TokenColor.Adder:
                if (tool.token.data.num != 0)
                {
                    yield return AdderClippingAnimation(tool);
                }
                else
                {
                    yield return AdderUseAnimation(tool);
                }
                break;
            default:
                yield return ClipperUseAnimation(tool);
                break;
        }
    }

    /// <summary>
    /// Spade animation after a target tile is selected
    /// Moves the tool to the token, then plays effects and destroys the tool
    /// The target token displays a score number at its original position which is then removed using the StartKillNumber method
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="newToken"></param>
    /// <returns></returns>
    public IEnumerator SpadeUseAnimation(Token tool)
    {
        // Creating new token to display score post shovel
        Token newToken = GameObject.Instantiate(this, transform.parent).GetComponent<Token>();
        newToken.token = token;
        newToken.SetTokenData(token.data);
        newToken.transform.localPosition = transform.localPosition;
        newToken.spriteDisplay.enabled = false;
        newToken.shadow.enabled = false;
        newToken.number.enabled = false;

        // Move to tile
        tool.transform.DORotate(Vector3.forward * Spade_StartingRotation, Spade_MoveToTileTime).SetEase(Ease.OutQuint).Play();
        yield return tool.transform.DOMove(transform.position + Spade_PositionOffset, Spade_MoveToTileTime).SetEase(Ease.OutQuint).WaitForCompletion();

        // Show text
        newToken.textDisplay.transform.parent = transform.parent;
        newToken.textDisplay.transform.localScale = Vector3.one * 1.4f;
        newToken.textDisplay.text = Services.GameController.ScoreToken(token.data).ToString();
        newToken.textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + newToken.textDisplay.text;

        // spade scoops
        Services.AudioManager.PlaySpadeSound();
        tool.transform.DORotate(Vector3.forward * Spade_EndRotation, Spade_DigTime).SetEase(Ease.InOutSine).Play();
        yield return tool.transform.DOMove(transform.position + Spade_DigDestination, Spade_DigTime).SetEase(Ease.InOutSine).WaitForCompletion();

        // spade fades away
        Sequence dyingSequence = DOTween.Sequence();
        tool.textDisplay.gameObject.SetActive(false);
        dyingSequence.Append(tool.spriteDisplay.DOFade(0f, Spade_DigTime + 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.number.DOFade(0f, Spade_DigTime + 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.shadow.DOFade(0f, Spade_DigTime + 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Play();

        // dirt particles spawn from new score
        newToken.dirtParticles.Play();
        newToken.sparkleParticles.Play();
        StartCoroutine(newToken.flowerParticles.PlayFlowerBurstCoroutine(0f, Logic.TokenColor.Spade));

        // delete new token
        newToken.StartKillNumber(0.5f);
        beingSpaded = false;
        GameObject.Destroy(tool.gameObject);
    }

    /// <summary>
    /// Clipper animation after a target tile is selected
    /// Moves the tool to the token, then destroys the tool
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="newToken"></param>
    /// <returns></returns>
    public IEnumerator ClipperUseAnimation(Token tool)
    {
        // Move towards token at an angle
        tool.transform.DORotate(Vector3.forward * 30f, Clipper_MoveToTileTime).SetEase(Ease.OutQuint).Play();
        yield return tool.transform.DOMove(transform.position + ClipperPositionOffset, Clipper_MoveToTileTime).SetEase(Ease.OutQuint).WaitForCompletion();
        tool.clippingSlashParticles.Play();

        // Slash across the tile
        Sequence dyingSequence = DOTween.Sequence();
        tool.textDisplay.gameObject.SetActive(false);
        dyingSequence.Append(tool.spriteDisplay.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.number.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.shadow.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Play();
        Services.AudioManager.PlayShearsSound();
        yield return tool.transform.DOMoveX(transform.position.x - ClipperSlashXLength, Clipper_SlashTime).SetEase(Ease.OutQuint).WaitForCompletion();
        tool.clippingSlashParticles.Stop();

        beingSpaded = false;
        GameObject.Destroy(tool.gameObject);
    }

    /// <summary>
    /// Adder (watering can) animation after a target tile is selected
    /// Plays the adding animation, moves the tool to the token, then destroys the tool
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="newToken"></param>
    /// <returns></returns>
    public IEnumerator AdderUseAnimation(Token tool)
    {
        // Move tool towards the tile
        while (Vector3.Distance(transform.position, tool.transform.position) > 0.05f)
        {
            tool.transform.position += (transform.position - tool.transform.position) * 0.15f;
            yield return new WaitForEndOfFrame();
        }

        
        Sequence adderSequence = DOTween.Sequence();
        float adderRotationTime = 0.4f;
        float waterSquirtTime = 0.3f;

        // Rotate the watering can
        adderSequence.Append(tool.transform.DORotate(Vector3.forward * -30f, adderRotationTime).SetEase(Ease.OutCirc));
        adderSequence.Join(tool.transform.DOMoveX(transform.position.x - 0.6f, adderRotationTime).SetEase(Ease.OutSine));
        adderSequence.Play();

        // watering can dips down while that is happening
        Sequence adderDipSequence = DOTween.Sequence();
        adderDipSequence.Append(tool.transform.DOMoveY(transform.position.y - 0.05f, adderRotationTime).SetEase(Ease.InCirc));
        adderDipSequence.Append(tool.transform.DOMoveY(transform.position.y + 0.1f, adderRotationTime).SetEase(Ease.InOutCubic));
        adderDipSequence.Play();
        Services.AudioManager.PlayWateringCanSound();

        // Wait for animations to finish with slight added delay
        //tool.adderWaterSquirtParticles.Play();
        yield return new WaitForSeconds(adderRotationTime + waterSquirtTime);

        // Tool fades away
        yield return tool.ToolDyingRoutine(false);

        GameObject.Destroy(tool.gameObject);
    }

    /// <summary>
    /// Adder (clipping) animation after a target tile is selected
    /// Plays the adding animation, moves the tool to the token, then destroys the tool
    /// </summary>
    /// <param name="tool"></param>
    /// <param name="newToken"></param>
    /// <returns></returns>
    public IEnumerator AdderClippingAnimation(Token tool)
    {
        // Move tool towards the tile
        while (Vector3.Distance(transform.position, tool.transform.position) > 0.05f)
        {
            tool.transform.position += (transform.position - tool.transform.position) * 0.15f;
            yield return new WaitForEndOfFrame();
        }

        // Adder clipping straightens out and dips down on to the tile
        float adderDipTime = 0.4f;

        tool.transform.DORotate(Vector3.zero, adderDipTime).Play();
        yield return tool.transform.DOMoveY(transform.position.y - 0.4f, adderDipTime).SetEase(Ease.InOutSine).WaitForCompletion();

        // Small flower burst plays
        StartCoroutine(tool.adderClippingParticles.PlayFlowerBurstCoroutine(0, token.data.color));

        yield return new WaitForSeconds(0.1f);

        //Token fades away 
        UpdateLayer("TokenMoving");
        Sequence dyingSequence = DOTween.Sequence();
        tool.textDisplay.gameObject.SetActive(false);
        dyingSequence.Append(tool.spriteDisplay.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.number.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.shadow.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        yield return dyingSequence.WaitForCompletion();
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
        yield return new WaitForSeconds(0.1f);
        placementParticles.Play();
        UpdateLayer("TokenPlaced");
    }
    public void UpdateLayer(string sortingLayer)
    {
        spriteDisplay.sortingLayerName = sortingLayer;
        number.sortingLayerName = sortingLayer;
        textDisplay.sortingLayerID = spriteDisplay.sortingLayerID;
    }
    public void Die(Logic.Token toolData = null)
    {
        Debug.Log("Die");
        StartCoroutine(Dying(toolData));
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
    IEnumerator Dying(Logic.Token toolToken)
    {
        float speed = liftSpeed;
        float targetAngle = Random.Range(2.5f, 5f);
        if (Random.value < 0.5f)
        {
            targetAngle *= -1f;
        }
        Services.GameController.dyingTokens.Add(this);

        if (toolToken != null && toolToken.data.color == Logic.TokenColor.Clipper)
        {
            yield return new WaitForSeconds(0.5f);
        }

        textDisplay.transform.parent = transform.parent;
        textDisplay.transform.localScale = Vector3.one * 1.4f;
        dirtParticles.Play();
        sparkleParticles.Play();
        textDisplay.text = Services.GameController.ScoreToken(token.data).ToString();
        textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + textDisplay.text;
        finalPos = transform.localPosition + Vector3.up * liftHeight;
        Services.AudioManager.PlayRemoveTileSound(1);

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
    public IEnumerator ToolDyingRoutine(bool destroyTool)
    {
        UpdateLayer("TokenMoving");
        Sequence dyingSequence = DOTween.Sequence();
        textDisplay.gameObject.SetActive(false);
        dyingSequence.Append(transform.DOMove(transform.position + Vector3.up * liftHeight, toolLiftSpeed).SetEase(Ease.OutCubic));
        dyingSequence.Join(spriteDisplay.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(number.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(shadow.DOFade(0f, toolLiftSpeed * 0.5f).SetEase(Ease.InCubic));
        yield return dyingSequence.WaitForCompletion();
        if (destroyTool)
        {
            GameObject.Destroy(gameObject);
        }
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
    public void StartInvalidAnim()
    {
        if (wiggling == false)
        {
            StartCoroutine(InvalidWiggle());
        }

    }
    IEnumerator InvalidWiggle()
    {
        wiggling = true;
        float speed = 0.225f * 2.3f;
        float targetAngle = 10f;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.z, targetAngle)) > 0.1f)
        {
            float angle = transform.localEulerAngles.z;
            angle = Mathf.LerpAngle(angle, targetAngle, speed);
            transform.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return new WaitForEndOfFrame();
        }
        transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        for (int i = 0; i < 1; i++)
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
}
