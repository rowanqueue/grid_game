using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using EZ.Haptics;
using Logic;
using System.Collections.Generic;
using UnityEditor;

public struct FinishTilePacing
{
    public float scoreHoldDuration;
}

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
    public const float DrawFollowSpeed = 1.5f;
    public const float DragDrawFollowSpeed = DrawFollowSpeed * 1.25f;
    public float toolLiftSpeed = 0.5f;

    public GameObject shade;

    bool moving = false;
    public Vector2 handPos;
    bool initialized = false;
    bool wiggling = false;
    Coroutine wiggleAnim;

    bool beingSpaded = false;
    /// <summary>True while a dug Spade tile is lerping from the board into the hand.</summary>
    public bool travelingFromSpade = false;
    int spadeReturnHandIndex = -1;
    bool loggedSpadeDrawBlock = false;
    bool finishDying = false;
    bool finishScoreApplied = false;
    bool finishScoreLabelDone = false;
    public bool lifted = false;
    public bool waitingToDie = false;
    public bool mergeDeathVisualComplete = false;

    [Header("Clipper Tool Animation Values")]
    [SerializeField] private float Clipper_MoveToTileTime = 0.25f;
    [SerializeField] private float Clipper_SlashTime = 0.25f;
    [SerializeField] private Vector3 ClipperPositionOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private float ClipperSlashXLength = 1f;

    [Header("Spade Tool Animation Values")]
    [SerializeField] private float Spade_MoveToTileTime = 0.5f;
    [SerializeField] private float Spade_DigTime = 0.5f;
    [SerializeField] private float Spade_FadeTime = 0.35f;
    [SerializeField] private Vector3 Spade_PositionOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private Vector3 Spade_DigDestination = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private float Spade_StartingRotation = 30f;
    [SerializeField] private float Spade_EndRotation = 30f;

    [Header("Undo Animation Values")]
    [SerializeField] private float UndoInAnimation = 0.5f;
    [SerializeField] private float UndoOutAnimation = 0.5f;

    [Header("Tutorial")]
    [SerializeField] private float upgradePresentationHold = 0.4f;
    [SerializeField] private float bagDrawStaggerSeconds = 0.2f;
    [SerializeField] private float placementSettleSeconds = 0.12f;
    [SerializeField] private float killNumberWaitMin = 0.1f;
    [SerializeField] private float killNumberWaitMax = 0.4f;
    [SerializeField] private float gnomeFinishHoldDuration = 0.55f;
    [SerializeField] private float lastTileHoldBonus = 0.15f;

    public SpriteRenderer crunchCircle;
    public TextMeshPro crunchText;

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
    public void UpgradeToken(Logic.Token _token, Logic.Token usedTool = null, bool useHaptics = false, System.Action onPresentationComplete = null)
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
            StartCoroutine(DefaultUpgradeRoutine(useHaptics, onPresentationComplete));
        }
    }

    /// <summary>
    /// Plays the upgrade animation for the token
    /// </summary>
    /// <returns></returns>
    IEnumerator DefaultUpgradeRoutine(bool useHaptics, System.Action onPresentationComplete = null)
    {
        print("Default");
        // Delay before starting the upgrade animation
        yield return new WaitForSeconds(Services.GameController.UpgradeStartDelay);

        if(token.data.num < 8)
        {
            Services.AudioManager.PlayUpgradeTileSound();
        }
        else
        {
            Services.AudioManager.PlayFinalUpgradeTileSound();
        }
        
        if (useHaptics)
        {
            Haptics.PlayTransient(1f, .5f);
        }


        // FlowerBurst lives under Text. SetTokenData→HideScoreLabel deactivates Text the
        // same frame as Play, which kills the burst + its WatchBurst coroutine. Detach first.
        DetachFlowerBurstForPlay();
        yield return flowerParticles.PlayFlowerBurstCoroutine(0f, token.data.color);

        // Changing token data (number)
        SetTokenData(token.data);

        onPresentationComplete?.Invoke();
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

        if (token.data.num < 8)
        {
            Services.AudioManager.PlayUpgradeTileSound();
        }
        else
        {
            Services.AudioManager.PlayFinalUpgradeTileSound();
        }
        if (useHaptics)
        {
            Haptics.PlayTransient(1f, .5f);
        }

        // Detach FlowerBurst so SetTokenData→HideScoreLabel can't kill it mid-play.
        DetachFlowerBurstForPlay();
        if (isClipping)
        {
            yield return new WaitForSeconds(0.4f);
            yield return flowerParticles.PlayFlowerBurstCoroutine(0f, token.data.color);
        }
        else
        {
            yield return flowerParticles.PlayFlowerBurstCoroutine(0f, Logic.TokenColor.Adder);
        }

        // Changing token data (number)
        SetTokenData(token.data);
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
        HideGnomeRevealSprite();
        if (tokenData.num >= ((Logic.TripleGame)Services.GameController.game).maxTileNum)
        {
            spriteDisplay.sprite = Services.Visuals.tokenMax[(int)tokenData.color];
            if (Services.GameController.diceMode)
            {
                number.sprite = Services.Visuals.altNumberSprites[(int)tokenData.num];
            }
            number.enabled = false;
            HideScoreLabel();
            return;
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
                HideScoreLabel();
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
        HideScoreLabel();
    }

    void HideScoreLabel()
    {
        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// DirtPuff and FlowerBurst live under the score Text GO. HideScoreLabel() deactivates
    /// that GO, so activate it before Play (TMP stays off until score show). Sparkles stay off.
    /// </summary>
    void EnsureDeathParticleHierarchyActive()
    {
        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(true);
            // Keep score digits hidden until ShowDeathScoreLabel; particles still simulate.
            if (!IsGnomeToken)
            {
                textDisplay.enabled = false;
            }
        }
        if (dirtParticles != null)
        {
            dirtParticles.gameObject.SetActive(true);
        }
        if (flowerParticles != null)
        {
            flowerParticles.gameObject.SetActive(true);
        }
        Debug.Log(
            $"[ParticleLife] EnsureDeathParticleHierarchyActive " +
            $"textActive={(textDisplay != null && textDisplay.gameObject.activeInHierarchy)} " +
            $"dirtActive={(dirtParticles != null && dirtParticles.gameObject.activeInHierarchy)} " +
            $"flowerActive={(flowerParticles != null && flowerParticles.gameObject.activeInHierarchy)} " +
            $"token={name} t={Time.time:F3}");
    }

    /// <summary>
    /// Move FlowerBurst off the score Text GO so HideScoreLabel() cannot deactivate it
    /// (and stop its coroutines) on the same frame as Play.
    /// </summary>
    void DetachFlowerBurstForPlay()
    {
        if (flowerParticles == null)
        {
            return;
        }

        Transform keepAliveParent = transform;
        string prevParent = flowerParticles.transform.parent != null
            ? flowerParticles.transform.parent.name
            : "null";
        flowerParticles.transform.SetParent(keepAliveParent, true);
        flowerParticles.gameObject.SetActive(true);

        Debug.Log(
            $"[ParticleLife] DetachFlowerBurstForPlay " +
            $"from={prevParent} to={keepAliveParent.name} " +
            $"active={flowerParticles.gameObject.activeInHierarchy} " +
            $"worldPos={flowerParticles.transform.position} token={name} t={Time.time:F3}");
    }

    bool IsGnomeToken => token != null && token.data.color == Logic.TokenColor.Gnome;

    void HideGnomeRevealSprite()
    {
        if (gnome != null)
        {
            gnome.enabled = false;
        }
    }

    void ShowGnomeRevealForScoring()
    {
        if (gnome == null || !IsGnomeToken)
        {
            return;
        }

        gnome.enabled = true;
        gnome.transform.SetParent(transform.parent, true);
        gnome.transform.localScale = Vector3.one * 1.4f;
        gnome.transform.localRotation = Quaternion.identity;
        gnome.color = Color.white;
    }

    void ShowDeathScoreLabel()
    {
        if (IsGnomeToken)
        {
            ShowGnomeRevealForScoring();
            return;
        }

        textDisplay.gameObject.SetActive(true);
        textDisplay.enabled = true;
        textDisplay.transform.localScale = Vector3.one * 1.4f;
        textDisplay.text = Services.GameController.ScoreToken(token.data).ToString();
        textDisplay.text = "<size=70%><voffset=0.2em>+</voffset></size>" + textDisplay.text;
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
            travelingFromSpade = true;
            spadeReturnHandIndex = index;
            loggedSpadeDrawBlock = false;
            GameLog.Log($"[Spade] ToolAnim: beingSpaded=true travelingFromSpade=true dug={name} id={GetInstanceID()} handIndex={index} pos={transform.position}");
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
        GameLog.Log($"[Spade] Dig start dug={name} id={GetInstanceID()} pos={transform.position}");

        // Creating new token to display score post shovel
        Token newToken = GameObject.Instantiate(this, transform.parent).GetComponent<Token>();
        newToken.token = token;
        newToken.SetTokenData(token.data);
        newToken.transform.localPosition = transform.localPosition;
        newToken.spriteDisplay.enabled = false;
        newToken.shadow.enabled = false;
        newToken.number.enabled = false;
        // Score clone must not freeze Draw or keep traveling-from-spade state.
        newToken.beingSpaded = false;
        newToken.travelingFromSpade = false;
        newToken.moving = false;

        // Move to tile
        tool.transform.DORotate(Vector3.forward * Spade_StartingRotation, Spade_MoveToTileTime).SetEase(Ease.OutQuint).Play();
        yield return tool.transform.DOMove(transform.position + Spade_PositionOffset, Spade_MoveToTileTime).SetEase(Ease.OutQuint).WaitForCompletion();

        // spade scoops — start dug-tile lift with the scoop so it isn't late
        Services.AudioManager.PlaySpadeSound();
        beingSpaded = false;
        GameLog.Log($"[Spade] Scoop start: dug-tile lift dug={name} id={GetInstanceID()} pos={transform.position}");
        StartCoroutine(SpadeDugTileTravelToHand());

        tool.transform.DORotate(Vector3.forward * Spade_EndRotation, Spade_DigTime).SetEase(Ease.InOutSine).Play();
        tool.transform.DOMove(transform.position + Spade_DigDestination, Spade_DigTime).SetEase(Ease.InOutSine).Play();

        // dirt + flower burst with the scoop (sparkles intentionally unused).
        // EnsureDeathParticleHierarchyActive disables TMP; ShowDeathScoreLabel turns +N back on.
        newToken.EnsureDeathParticleHierarchyActive();
        newToken.DetachFlowerBurstForPlay();
        newToken.dirtParticles.Play();
        StartCoroutine(newToken.flowerParticles.PlayFlowerBurstCoroutine(0f, Logic.TokenColor.Spade));
        newToken.textDisplay.transform.SetParent(transform.parent, true);
        newToken.ShowDeathScoreLabel();
        newToken.StartKillNumber(0.5f);

        // spade fades away while dig finishes — wait for full fade before destroy
        Sequence dyingSequence = DOTween.Sequence();
        tool.textDisplay.gameObject.SetActive(false);
        dyingSequence.Append(tool.spriteDisplay.DOFade(0f, Spade_FadeTime).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.number.DOFade(0f, Spade_FadeTime).SetEase(Ease.InCubic));
        dyingSequence.Join(tool.shadow.DOFade(0f, Spade_FadeTime).SetEase(Ease.InCubic));
        yield return dyingSequence.WaitForCompletion();
        GameObject.Destroy(tool.gameObject);
    }

    /// <summary>
    /// Lifts the dug flower off the board, then moves it into the hand/free-slot.
    /// Uses <see cref="moving"/> so Draw does not fight the animation.
    /// </summary>
    IEnumerator SpadeDugTileTravelToHand()
    {
        moving = true;
        travelingFromSpade = true;
        lifted = true;
        UpdateLayer("TokenMoving");

        Vector3 liftTarget = transform.position + Vector3.up * liftHeight;
        GameLog.Log($"[Spade] Lift tween start from={transform.position} to={liftTarget} liftHeight={liftHeight} speed={toolLiftSpeed}");
        yield return transform.DOMove(liftTarget, toolLiftSpeed).SetEase(Ease.OutCubic).WaitForCompletion();
        GameLog.Log($"[Spade] Lift tween done pos={transform.position}");

        // Raise sprite while traveling (same hover offset as held tiles).
        spriteDisplay.transform.localPosition = Vector3.up * 0.5f;

        Vector3 HandTarget()
        {
            var gc = Services.GameController;
            if (spadeReturnHandIndex >= gc.game.hand.handSize)
            {
                return gc.freeSlot != null ? gc.freeSlot.transform.position : transform.position;
            }
            if (spadeReturnHandIndex >= 0 && spadeReturnHandIndex < gc.handTransforms.Count)
            {
                Vector3 p = gc.handTransforms[spadeReturnHandIndex].position;
                if (gc.game.hand.handSize == 1)
                {
                    p.x = 0f;
                }
                return p;
            }
            return transform.position;
        }

        Vector3 target = HandTarget();
        GameLog.Log($"[Spade] Travel to hand start target={target} handIndex={spadeReturnHandIndex}");
        while (Vector3.Distance(target, transform.position) > 0.05f)
        {
            target = HandTarget();
            transform.position += (target - transform.position) * 0.15f;
            yield return new WaitForEndOfFrame();
        }

        transform.position = HandTarget();
        // Leave sprite hover offset; Draw lerps it down when lifted=false (avoids snap).
        travelingFromSpade = false;
        lifted = false;
        moving = false;
        UpdateLayer("TokenHand");
        GameLog.Log($"[Spade] Arrived in hand dug={name} id={GetInstanceID()} pos={transform.position}");
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

    public void SetBagUsedAppearance()
    {
        shade.SetActive(false);
        if (shadow != null)
            shadow.enabled = false;
        const float fade = 0.45f;
        const float desat = 0.55f;
        spriteDisplay.color = new Color(desat, desat, desat, fade);
        if (number.enabled)
        {
            Color c = number.color;
            number.color = new Color(c.r * desat, c.g * desat, c.b * desat, fade);
        }
        if (crunchCircle.enabled)
        {
            crunchCircle.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            crunchText.color = Color.white;
        }
    }
    public void PlaceInTile(Tile tile, bool undoAnimation = false)
    {
        transform.position = tile.transform.position;
        placementAnimating = false;
        placementSettling = true;
        UpdateLayer("TokenPlaced");
        if (undoAnimation)
        {
            UndoSpawn();
        }
    }

    public void DrawFromBag(int index)
    {
        moving = true;
        transform.position = Services.GameController.bagButtonTransform.position;
        transform.position = new Vector2(-2.5f, -8f);
        StartCoroutine(BagDraw(index * bagDrawStaggerSeconds));
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

    public void PlaceInBag()
    {
        moving = true;
        StartCoroutine(PlaceInBagRoutine());
    }

    public IEnumerator PlaceInBagRoutine()
    {
        yield return transform.DOMove(new Vector2(-2.5f, -8f), 0.4f).SetEase(Ease.InSine).WaitForCompletion();
        Destroy(gameObject);
    }

    bool placementSettling;
    bool placementAnimating;

    public void BeginPlacementAnimation()
    {
        placementAnimating = true;
        placementSettling = false;
    }

    public void Draw(Vector2 pos, bool hover = false, float followSpeed = DrawFollowSpeed)
    {
        if (finishDying)
        {
            return;
        }
        if (beingSpaded)
        {
            if (!loggedSpadeDrawBlock)
            {
                loggedSpadeDrawBlock = true;
                GameLog.Log($"[Spade] Draw blocked by beingSpaded dug={name} id={GetInstanceID()}");
            }
            return;
        }
        if (initialized == false)
        {
            Init(token);
        }
        if (lifted)
        {
            spriteDisplay.transform.localPosition += (Vector3.up * 0.5f - spriteDisplay.transform.localPosition) * followSpeed * (Time.deltaTime / 0.16666f);
            //shadow.transform.localPosition = new Vector2(0, -0.5f);
        }
        else
        {
            spriteDisplay.transform.localPosition += (Vector3.zero - spriteDisplay.transform.localPosition) * followSpeed * (Time.deltaTime / 0.16666f);
            //shadow.transform.localPosition = Vector3.zero;
        }
        if (moving) { return; }
        float shadow_scale = Mathf.InverseLerp(0f, 0.5f, spriteDisplay.transform.localPosition.y);
        shadow.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.75f, shadow_scale);

        transform.position += ((Vector3)pos - transform.position) * followSpeed * (Time.deltaTime / 0.16666f);

        // Spade travel is driven by SpadeDugTileTravelToHand (moving=true); Draw must not clear it early.
        if (travelingFromSpade && !moving)
        {
            float spadeDist = Vector2.Distance(pos, transform.position);
            if (spadeDist > 0.15f)
            {
                lifted = true;
            }
            else
            {
                travelingFromSpade = false;
                lifted = false;
                GameLog.Log($"[Spade] Arrived in hand (Draw settle) dug={name} id={GetInstanceID()}");
            }
        }

        if (spriteDisplay.sortingLayerName == "TokenPlaced")
        {
            if (Services.GameController.lastTokenPlaced == this && Services.GameController.inputState == InputState.Choose)
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
        if (placementAnimating)
        {
            float dist = Vector2.Distance(pos, transform.position);
            if (dist < 0.1f && !placementSettling)
            {
                placementSettling = true;
                lifted = false;
                StartCoroutine(LowerLift());
            }
        }
    }
    IEnumerator LowerLift()
    {
        yield return new WaitForSeconds(Services.GameController.LowerLiftDelay);
        placementParticles.Play();
        placementAnimating = false;
        UpdateLayer("TokenPlaced");
    }

    public bool IsPlacementAnimating => placementAnimating;

    public bool IsMoving => moving;

    public IEnumerator WaitForPlacementComplete()
    {
        while (IsPlacementAnimating)
        {
            yield return null;
        }
        yield return new WaitForSeconds(Services.GameController.PlacementSettleSeconds);
    }
    public void UpdateLayer(string sortingLayer)
    {
        if (sortingLayer == "TokenMoving")
        {
            placementSettling = false;
        }
        spriteDisplay.sortingLayerName = sortingLayer;
        number.sortingLayerName = sortingLayer;
        textDisplay.sortingLayerID = spriteDisplay.sortingLayerID;
        if (sortingLayer == "UIToken")
        {
            if (shadow != null)
            {
                shadow.sortingLayerName = sortingLayer;
                shadow.enabled = false;
            }
            if (crunchCircle != null)
            {
                crunchCircle.sortingLayerName = sortingLayer;
            }
            if (crunchText != null)
            {
                crunchText.sortingLayerID = spriteDisplay.sortingLayerID;
            }
        }
    }

    public void PrepareForFinishParade()
    {
        if (wiggling)
        {
            wiggling = false;
            if (wiggleAnim != null)
            {
                StopCoroutine(wiggleAnim);
                wiggleAnim = null;
            }
        }
        finishDying = false;
        finishScoreApplied = false;
        finishScoreLabelDone = false;
        lifted = false;
        placementAnimating = false;
        placementSettling = true;
        transform.localEulerAngles = Vector3.zero;
        if (token != null && token.tile != null
            && Services.GameController.tiles.TryGetValue(token.pos, out Tile gridTile))
        {
            transform.position = gridTile.transform.position;
        }
        if (spriteDisplay != null)
        {
            spriteDisplay.transform.localPosition = Vector3.zero;
        }
        if (shadow != null)
        {
            shadow.transform.localScale = Vector3.one;
        }
        UpdateLayer("TokenPlaced");
    }

    public void Die(Logic.Token toolData = null)
    {
        GameLog.Log("Die");
        StartCoroutine(Dying(toolData, forFinish: false));
    }

    public IEnumerator PlayFinishTileRoutine(FinishTilePacing pacing, bool isLastTile)
    {
        finishDying = true;
        finishScoreApplied = false;
        finishScoreLabelDone = false;
        if (Services.GameController != null)
        {
            Services.GameController.finishLiftsInFlight++;
        }
        StartCoroutine(FinishLiftThenDestroy());

        float hold = pacing.scoreHoldDuration;
        if (IsGnomeToken)
        {
            hold = Mathf.Max(hold, gnomeFinishHoldDuration);
        }
        if (isLastTile)
        {
            hold += lastTileHoldBonus;
        }
        yield return new WaitForSeconds(hold);
        ApplyFinishScore();
    }

    IEnumerator FinishLiftThenDestroy()
    {
        yield return Dying(null, forFinish: true);
        while (!finishScoreApplied)
        {
            yield return null;
        }
        while (!finishScoreLabelDone)
        {
            yield return null;
        }
        if (Services.GameController != null)
        {
            Services.GameController.finishLiftsInFlight = Mathf.Max(0, Services.GameController.finishLiftsInFlight - 1);
        }
        ReleasePlayingDeathParticles();
        GameObject.Destroy(gameObject);
    }

    void ApplyFinishScore()
    {
        if (finishScoreApplied)
        {
            return;
        }
        finishScoreApplied = true;
        // Visual HUD roll only — no score loop (one pop at lift start is the audio beat).
        if (Services.GameController != null && token != null)
        {
            Services.GameController.scoreDelta += Services.GameController.ScoreToken(token.data);
            if (token.tile != null)
            {
                token.tile.token = null;
            }
        }
        StartCoroutine(ShrinkFinishScoreLabel());
    }

    IEnumerator ShrinkFinishScoreLabel()
    {
        // Match KillNumber's disappear for +N. Gnomes leave their reveal image behind.
        if (IsGnomeToken)
        {
            if (textDisplay != null)
            {
                GameObject.Destroy(textDisplay.gameObject);
                textDisplay = null;
            }
            finishScoreLabelDone = true;
            yield break;
        }

        float speed = liftSpeed;
        float waitTime = Random.Range(killNumberWaitMin, killNumberWaitMax);
        yield return new WaitForSeconds(waitTime);
        Transform scoringVisual = textDisplay != null ? textDisplay.transform : null;
        while (scoringVisual != null && scoringVisual.localScale.x > 0.2f)
        {
            scoringVisual.localScale -= Vector3.one * speed * 0.95f;
            yield return new WaitForEndOfFrame();
        }
        if (textDisplay != null)
        {
            GameObject.Destroy(textDisplay.gameObject);
            textDisplay = null;
        }
        finishScoreLabelDone = true;
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
    IEnumerator Dying(Logic.Token toolToken, bool forFinish)
    {
        mergeDeathVisualComplete = false;
        float speed = (forFinish
            ? liftSpeed * Services.GameController.finishLiftSpeedMultiplier
            : liftSpeed) * Services.GameController.DeathAnimationSpeed;
        float fadeSpeed = (forFinish
            ? liftSpeed * Services.GameController.finishLiftSpeedMultiplier
            : liftSpeed) * Services.GameController.DeathAnimationSpeed;
        float targetAngle = Random.Range(2.5f, 5f);
        if (Random.value < 0.5f)
        {
            targetAngle *= -1f;
        }
        if (!forFinish)
        {
            Services.GameController.dyingTokens.Add(this);
        }

        if (toolToken != null && toolToken.data.color == Logic.TokenColor.Clipper)
        {
            yield return new WaitForSeconds(0.5f);
        }

        textDisplay.transform.parent = transform.parent;
        // DirtPuff/FlowerBurst live under Text. HideScoreLabel() deactivates that GO, so
        // Play() before activation emits nothing. Wake the hierarchy (TMP stays off until score show).
        EnsureDeathParticleHierarchyActive();
        LogDeathParticle("dirt", "Play_call", dirtParticles);
        if (dirtParticles != null)
        {
            dirtParticles.Play();
            LogDeathParticle("dirt", "AfterPlay", dirtParticles);
            if (!dirtParticles.isPlaying && dirtParticles.particleCount == 0)
            {
                Debug.LogWarning($"[ParticleLife] dirt NEVER_STARTED after Play() token={name} t={Time.time:F3}");
            }
        }
        else
        {
            Debug.Log($"[ParticleLife] dirt Play SKIPPED null ref token={name} t={Time.time:F3}");
        }
        StartCoroutine(WatchDeathParticles());
        Services.AudioManager.PlayRemoveTileSound(1);

        UpdateLayer("TokenMoving");
        if (forFinish)
        {
            transform.localEulerAngles = Vector3.zero;
            finalPos = spriteDisplay.transform.localPosition + Vector3.up * liftHeight;
            ShowDeathScoreLabel();
            if (IsGnomeToken && Services.GameController.useHaptics)
            {
                Haptics.PlayTransient(1f, 0.5f);
            }
        }
        else
        {
            // Buncha effective behavior: snap tilt (no multi-frame lean), mark pop, then lift
            // immediately so dirt + float are visible before score KillNumber can destroy us.
            transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
            finalPos = spriteDisplay.transform.localPosition + Vector3.up * liftHeight;
            ShowDeathScoreLabel();
            waitingToDie = true;
        }
        while (Vector2.Distance(finalPos, spriteDisplay.transform.localPosition) > 0.01f)
        {
            spriteDisplay.transform.localPosition += (finalPos - spriteDisplay.transform.localPosition) * speed;
            totalDeathMovement -= speed;
            if (Vector2.Distance(finalPos, spriteDisplay.transform.localPosition) < 0.25f)
            {
                var a = (float)spriteDisplay.color.a;
                a -= fadeSpeed * 0.5f;
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
            a -= fadeSpeed;
            spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, a);
            number.color = new Color(number.color.r, number.color.g, number.color.b, a);
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, Mathf.Lerp(0, 0.5f, a));
            yield return new WaitForEndOfFrame();
        }


        if (!forFinish)
        {
            mergeDeathVisualComplete = true;
        }

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
        Debug.Log($"[ParticleLife] KillNumber START delay={delay:F2} token={name} " +
                  $"dirtPlaying={(dirtParticles != null && dirtParticles.isPlaying)} " +
                  $"mergeDeathVisualComplete={mergeDeathVisualComplete} t={Time.time:F3}");
        StartCoroutine(KillNumber(delay));
    }

    void LogDeathParticle(string which, string action, ParticleSystem ps)
    {
        if (ps == null)
        {
            Debug.Log($"[ParticleLife] {which} {action} ps=NULL token={name} t={Time.time:F3}");
            return;
        }
        var main = ps.main;
        Debug.Log(
            $"[ParticleLife] {which} {action} " +
            $"name={ps.name} playing={ps.isPlaying} paused={ps.isPaused} count={ps.particleCount} " +
            $"duration={main.duration:F2} startLife={main.startLifetime.constantMax:F2} " +
            $"active={ps.gameObject.activeInHierarchy} enabled={ps.gameObject.activeSelf} " +
            $"emission={ps.emission.enabled} renderer={(ps.GetComponent<ParticleSystemRenderer>() != null && ps.GetComponent<ParticleSystemRenderer>().enabled)} " +
            $"parent={(ps.transform.parent != null ? ps.transform.parent.name : "null")} " +
            $"worldPos={ps.transform.position} token={name} t={Time.time:F3}");
    }

    IEnumerator WatchDeathParticles()
    {
        float start = Time.time;
        for (int i = 0; i < 40; i++)
        {
            yield return new WaitForSeconds(0.1f);
            if (this == null)
            {
                Debug.Log($"[ParticleLife] WatchDeathParticles TOKEN_DESTROYED after={Time.time - start:F2}s t={Time.time:F3}");
                yield break;
            }
            LogDeathParticle("dirt", $"tick{i}", dirtParticles);
            bool dirtDone = dirtParticles == null || (!dirtParticles.isPlaying && dirtParticles.particleCount == 0);
            if (dirtDone && i > 2)
            {
                Debug.Log($"[ParticleLife] death particles IDLE after={Time.time - start:F2}s token={name} t={Time.time:F3}");
                yield break;
            }
        }
    }

    void ReleasePlayingDeathParticles()
    {
        Debug.Log($"[ParticleLife] ReleasePlayingDeathParticles BEGIN token={name} t={Time.time:F3}");
        Transform keepAliveParent = transform.parent != null
            ? transform.parent
            : Services.GameController != null ? Services.GameController.gridTransform : null;

        ReleaseParticleSystem("dirt", dirtParticles, keepAliveParent);
        dirtParticles = null;
    }

    void ReleaseParticleSystem(string which, ParticleSystem ps, Transform keepAliveParent)
    {
        if (ps == null)
        {
            Debug.Log($"[ParticleLife] {which} Release SKIPPED null t={Time.time:F3}");
            return;
        }

        LogDeathParticle(which, "BeforeRelease", ps);
        GameObject go = ps.gameObject;
        if (keepAliveParent != null)
        {
            go.transform.SetParent(keepAliveParent, true);
        }
        else
        {
            go.transform.SetParent(null, true);
        }

        if (!ps.isPlaying)
        {
            Debug.Log($"[ParticleLife] {which} Release re-Play (was not playing) t={Time.time:F3}");
            ps.Play();
        }

        // Clean up after the burst finishes playing out.
        float lifetime = 2f;
        var main = ps.main;
        if (main.duration > 0f)
        {
            lifetime = main.duration + main.startLifetime.constantMax + 0.5f;
        }
        LogDeathParticle(which, $"AfterRelease destroyIn={lifetime:F2}s", ps);
        Debug.Log($"[ParticleLife] {which} SCHEDULED_DESTROY go={go.name} in={lifetime:F2}s t={Time.time:F3}");
        GameObject.Destroy(go, lifetime);
    }

    IEnumerator KillNumber(float delay)
    {
        yield return new WaitForSeconds(delay);
        float speed = liftSpeed;
        float waitTime = Random.Range(killNumberWaitMin, killNumberWaitMax);
        yield return new WaitForSeconds(waitTime);
        Transform scoringVisual = IsGnomeToken && gnome != null && gnome.enabled ? gnome.transform : textDisplay.transform;
        while (scoringVisual != null && scoringVisual.localScale.x > 0.2f)
        {
            scoringVisual.localScale -= Vector3.one * speed * 0.95f;
            yield return new WaitForEndOfFrame();

        }
        //todo: make this the right amount of points
        //Services.GameController.dyingTokens.Remove(this);
        if(Services.GameController.scoreDelta == 0)
        {
            Services.GameController.StartScoreRolling();
        }
        Services.GameController.scoreDelta += Services.GameController.ScoreToken(token.data);
        if (IsGnomeToken && gnome != null && gnome.enabled)
        {
            GameObject.Destroy(gnome.gameObject);
        }
        else if (textDisplay != null)
        {
            GameObject.Destroy(textDisplay.gameObject);
        }

        // Let lift/fade finish so dirt is visible, then detach particles before destroy.
        float elapsed = 0f;
        const float deathVisualTimeout = 3f;
        Debug.Log($"[ParticleLife] KillNumber wait mergeDeathVisualComplete={mergeDeathVisualComplete} token={name} t={Time.time:F3}");
        while (!mergeDeathVisualComplete && elapsed < deathVisualTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[ParticleLife] KillNumber deathVisual done complete={mergeDeathVisualComplete} waited={elapsed:F2}s token={name} t={Time.time:F3}");

        ReleasePlayingDeathParticles();
        Debug.Log($"[ParticleLife] KillNumber Destroy(token) CAUSE=KillNumber token={name} t={Time.time:F3}");
        GameObject.Destroy(gameObject);
    }

    void OnDestroy()
    {
        bool dirtStillChild = dirtParticles != null && dirtParticles.transform.IsChildOf(transform);
        Debug.Log(
            $"[ParticleLife] Token OnDestroy CAUSE=OnDestroy name={name} " +
            $"dirtNull={dirtParticles == null} dirtStillChild={dirtStillChild} " +
            $"dirtPlaying={(dirtParticles != null && dirtParticles.isPlaying)} " +
            $"t={Time.time:F3}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
    }

    public void ShowCrunchedDisplay(int num)
    {
        if (crunchCircle == null || crunchText == null)
        {
            return;
        }

        // BagNumberText lives under the score Text GO, which HideScoreLabel deactivates.
        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(true);
            textDisplay.enabled = false;
        }
        crunchText.gameObject.SetActive(true);

        crunchCircle.enabled = true;
        crunchText.enabled = true;
        crunchText.text = $"x{num}";
        crunchCircle.color = Services.Visuals.tokenColors[(int)token.data.color];

        crunchCircle.sortingLayerName = spriteDisplay.sortingLayerName;
        crunchCircle.sortingOrder = spriteDisplay.sortingOrder + 1;
        crunchText.sortingLayerID = spriteDisplay.sortingLayerID;
        crunchText.sortingOrder = spriteDisplay.sortingOrder + 2;
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

    public void UndoInit(Logic.Token _token)
    {
        initialized = true;
        token = _token;
        SetTokenData(token.data);
        UndoSpawn();
    }

    public void UndoDestroy()
    {
        StartCoroutine(UndoDestroyAnimation());
    }

    public IEnumerator UndoDestroyAnimation()
    {
        Sequence dyingSequence = DOTween.Sequence();
        textDisplay.gameObject.SetActive(false);
        GameLog.Log("Undo Destroy Animation");
        dyingSequence.Append(transform.DOMove(transform.position + Vector3.up * liftHeight, UndoOutAnimation).SetEase(Ease.OutCubic));
        dyingSequence.Join(spriteDisplay.DOFade(0f, UndoOutAnimation * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(number.DOFade(0f, UndoOutAnimation * 0.5f).SetEase(Ease.InCubic));
        dyingSequence.Join(shadow.DOFade(0f, UndoOutAnimation * 0.5f).SetEase(Ease.InCubic));
        yield return dyingSequence.WaitForCompletion();
        GameLog.Log("Undo Destroy Animation Complete");
        GameObject.Destroy(gameObject);
    }

    public void UndoSpawn()
    {
        textDisplay.gameObject.SetActive(false);
        spriteDisplay.color = new Color(spriteDisplay.color.r, spriteDisplay.color.g, spriteDisplay.color.b, 0f);
        number.color = new Color(number.color.r, number.color.g, number.color.b, 0f);
        shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0f);
        number.gameObject.SetActive(false);
        StartCoroutine(UndoSpawnAnimation());
    }

    public IEnumerator UndoSpawnAnimation()
    {
        yield return new WaitForEndOfFrame();
        number.gameObject.SetActive(true);
        number.color = new Color(number.color.r, number.color.g, number.color.b, 0f);
        transform.position += Vector3.up * liftHeight;
        Sequence spawnSequence = DOTween.Sequence();
        spawnSequence.Append(transform.DOMove(transform.position - Vector3.up * liftHeight, UndoInAnimation).SetEase(Ease.OutCubic));
        spawnSequence.Join(spriteDisplay.DOFade(1f, UndoInAnimation * 0.5f).SetEase(Ease.InCubic));
        spawnSequence.Join(number.DOFade(1f, UndoInAnimation * 0.5f).SetEase(Ease.InCubic));
        spawnSequence.Join(shadow.DOFade(1f, UndoInAnimation * 0.5f).SetEase(Ease.InCubic));
        yield return spawnSequence.WaitForCompletion();
    }
}
