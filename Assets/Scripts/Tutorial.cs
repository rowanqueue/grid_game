using Logic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public enum TutorialStage
{
    Intro,//1hand: 4 blues
    Placing,//3 places until you make Blue2
    Blue2,
    FreeSlot,
    HandRefill,//2hand: 2 blues 2 reds
    WeirdSet,//leading up to undo
    Undo,
    FirstRed,//3hand: 2 blues 2 reds
    RedScoring,
    Blue3,//place 2 blues, then reserve 1 red; 4hand: blue,red,2 greens
    SecondRed,
    LearnGreen,//5hand: blue,red,purple,green
    Green2,
    GreenScoring,
    CleanUp,
    Purple, //6hand: blue,red,green,purple
    Shop,
    Snapshot,
    Mulligan,
    HighestScore,
    ActuallyFinishDefault

}
public enum TutStage
{
    Intro,
    Shelf,
    Placing,
    Combining,
    Scoring,
    FreeSlot,
    HandRefill,
    WeirdSet,
    FirstRed,
    BagIntro,
    Red2,
    RedScoring,
    EmptyHand,
    TeachMulligan,
    ThirdBlue2,
    Blue3Appears,
    ActualBlue3,
    MoreScoring,
    Finish,
    ActuallyFinishDefault,
    GreenStart,
    GreenBag,
    GreenNextBag,
    GreenFinish,
    PurpleStart,
    PurpleFinish
}

public enum IncrementTrigger
{
    IntroKey,
    AfterUpgrade,
    AfterPlace,
    AfterUndo,
}

public class Tutorial : MonoBehaviour
{
    public bool active = false;
    public bool changed = false;
    public TutorialStage stage;
    public int stagePhase;
    public List<GameObject> stageParents;
    GameObject stageParent => stageParents[(int)stage];
    public bool placingRule = false;
    public List<Vector2Int> allowedPlaces;
    public bool choosingRule = false;
    public Logic.TokenColor allowedColor;
    GameObject highlight;
    GameObject arrow;
    TutorialStageData currentStageData;
    bool greenLearnt;
    bool purpleLearnt;
    public List<Transform> tokenTransforms;
    public List<Token> purpleTokens;

    [Header("Presentation Timing")]
    [SerializeField] float introSettleTime = 0.25f;
    [SerializeField] float blue3FormViewHold = 0.75f;

    public float Blue3FormViewHold => blue3FormViewHold;

    bool isPresenting;
    Coroutine activeTransition;
    bool presentationRunsOnController;
    IncrementTrigger? deferredIncrementTrigger;

    public bool CanAcceptTutorialInput => active && !isPresenting;

    bool ShouldAllowGameplayDuringPresentation()
    {
        return UsesStageData(stage) || !IsIntroSlideStage();
    }

    void ReleaseGameplayInputIfReady()
    {
        if (ShouldAllowGameplayDuringPresentation())
        {
            isPresenting = false;
        }
    }

    void EnsureHierarchyActive()
    {
        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
            }
            t = t.parent;
        }
    }

    void StopActivePresentation()
    {
        if (activeTransition != null)
        {
            if (presentationRunsOnController)
            {
                Services.GameController.StopCoroutine(activeTransition);
            }
            else if (gameObject.activeInHierarchy)
            {
                StopCoroutine(activeTransition);
            }
            activeTransition = null;
            presentationRunsOnController = false;
        }
        isPresenting = false;
    }

    bool WasAdvanceInputPressed()
    {
        return Input.anyKeyDown
            || Input.GetMouseButtonDown(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    bool IsIntroSlideStage()
    {
        switch (stage)
        {
            case TutorialStage.Placing:
            case TutorialStage.FreeSlot:
            case TutorialStage.WeirdSet:
            case TutorialStage.Undo:
            case TutorialStage.FirstRed:
            case TutorialStage.Blue3:
            case TutorialStage.SecondRed:
            case TutorialStage.LearnGreen:
            case TutorialStage.Green2:
            case TutorialStage.CleanUp:
            case TutorialStage.Purple:
                return false;
            default:
                return true;
        }
    }

    bool UsesStageData(TutorialStage s)
    {
        switch (s)
        {
            case TutorialStage.Placing:
            case TutorialStage.WeirdSet:
            case TutorialStage.Undo:
            case TutorialStage.FirstRed:
            case TutorialStage.Blue3:
            case TutorialStage.LearnGreen:
            case TutorialStage.CleanUp:
            case TutorialStage.Purple:
                return true;
            default:
                return false;
        }
    }

    void BindHighlightFromStageData(int phaseIndex = 0)
    {
        if (currentStageData == null) { return; }
        if (currentStageData.stagePhase.Count <= phaseIndex) { return; }
        StagePhase phase = currentStageData.stagePhase[phaseIndex];
        if (phase.highlights != null && phase.highlights.Count > 0 && phase.highlights[0] != null)
        {
            highlight = phase.highlights[0].gameObject;
        }
        if (phase.arrows != null && phase.arrows.Count > 0 && phase.arrows[0] != null)
        {
            arrow = phase.arrows[0].gameObject;
        }
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("greenLearnt"))
        {
            greenLearnt = true;
        }
        if (PlayerPrefs.HasKey("purpleLearnt"))
        {
            purpleLearnt = true;
        }
    }

    public void StartTutorial()
    {
        EnsureHierarchyActive();
        enabled = true;
        active = true;
        isPresenting = false;
        EnterStage(TutorialStage.Intro);
    }

    public void ExitTutorial(bool refillHand = false)
    {
        active = false;
        isPresenting = false;
        StopActivePresentation();
        ExitStage();
        Services.GameController.OnTutorialEnded(refillHand);
        PlayerPrefs.SetInt("tutorialComplete", 1);
    }

    public void StartSpecialTutorial(TutorialStage stage)
    {
        EnsureHierarchyActive();
        active = true;
        EnterStage(stage);
        BeginPresentation(FinishEnterPresentation());
    }

    void BeginPresentation(IEnumerator routine)
    {
        StopActivePresentation();
        EnsureHierarchyActive();
        enabled = true;
        isPresenting = true;
        activeTransition = Services.GameController.StartCoroutine(PresentationWrapper(routine));
        presentationRunsOnController = true;
    }

    IEnumerator PresentationWrapper(IEnumerator routine)
    {
        try
        {
            yield return routine;
        }
        finally
        {
            isPresenting = false;
            activeTransition = null;
            presentationRunsOnController = false;
            EnsureHierarchyActive();
        }
    }

    public void RequestIncrementStage(IncrementTrigger trigger)
    {
        if (!active) { return; }
        if (trigger == IncrementTrigger.IntroKey && IsIntroSlideStage())
        {
            StopActivePresentation();
        }
        else if (isPresenting)
        {
            return;
        }
        BeginPresentation(IncrementStageRoutine(trigger));
    }

    public void RequestStageUpdate()
    {
        if (!active || isPresenting) { return; }
        BeginPresentation(StageUpdateRoutine());
    }

    public void RequestStageUpdateAfterPlacement(Token placedToken)
    {
        if (!active) { return; }
        Services.GameController.StartCoroutine(WaitForPlacementThenStageUpdate(placedToken));
    }

    public void RequestIncrementStageAfterPlacement(Token placedToken, IncrementTrigger trigger)
    {
        if (!active) { return; }
        Services.GameController.StartCoroutine(WaitForPlacementThenIncrementStage(placedToken, trigger));
    }

    IEnumerator WaitForPlacementThenStageUpdate(Token placedToken)
    {
        if (placedToken != null)
        {
            yield return placedToken.WaitForPlacementComplete();
        }
        RequestStageUpdate();
    }

    IEnumerator WaitForPlacementThenIncrementStage(Token placedToken, IncrementTrigger trigger)
    {
        if (placedToken != null)
        {
            yield return placedToken.WaitForPlacementComplete();
        }
        RequestIncrementStage(trigger);
    }

    public void TryHandleIntroAdvanceInput()
    {
        if (!Services.GameController.CanProcessIntroTutorialInput)
        {
            return;
        }
        if (!active || !IsIntroSlideStage() || !WasAdvanceInputPressed())
        {
            return;
        }
        AdvanceIntroSlideFromPointer();
    }

    public void AdvanceIntroSlideFromPointer()
    {
        if (!Services.GameController.CanProcessIntroTutorialInput)
        {
            return;
        }
        if (!active || !IsIntroSlideStage())
        {
            return;
        }
        Services.AudioManager.PlayTutorialNotificationSound();
        RequestIncrementStage(IncrementTrigger.IntroKey);
    }

    void EnsureIntroClickForwarder()
    {
        if (!IsIntroSlideStage()) { return; }
        TutorialIntroClickForwarder forwarder = stageParent.GetComponent<TutorialIntroClickForwarder>();
        if (forwarder == null)
        {
            forwarder = stageParent.AddComponent<TutorialIntroClickForwarder>();
        }
        forwarder.Bind(this);
    }

    IEnumerator WaitForGridSettled()
    {
        while (Services.GameController.game.gridUpdating)
        {
            yield return null;
        }
    }

    IEnumerator IncrementStageRoutine(IncrementTrigger trigger, bool nested = false)
    {
        if (trigger == IncrementTrigger.AfterPlace || trigger == IncrementTrigger.AfterUndo)
        {
            yield return WaitForGridSettled();
        }

        if (currentStageData != null && UsesStageData(stage))
        {
            int lastPhase = currentStageData.stagePhase.Count - 1;
            if (lastPhase >= 0)
            {
                yield return currentStageData.AnimateOutStageCoroutine(
                    Mathf.Min(stagePhase, lastPhase));
            }
        }

        IncrementStageInternal();
        ReleaseGameplayInputIfReady();

        yield return FinishEnterPresentation();
    }

    void DeferIncrementStage(IncrementTrigger trigger)
    {
        deferredIncrementTrigger = trigger;
    }

    IEnumerator FinishEnterPresentation()
    {
        if (currentStageData != null && UsesStageData(stage) && currentStageData.HasPhase(0))
        {
            yield return currentStageData.RunEnterPresentation();
            BindHighlightFromStageData(0);
        }
        else
        {
            yield return new WaitForSecondsRealtime(introSettleTime);
        }

        if (stage == TutorialStage.HandRefill || stage == TutorialStage.LearnGreen)
        {
            Services.GameController.PresentTutorialHandAfterIntro();
        }
        else if (stage == TutorialStage.Mulligan)
        {
            Services.GameController.PresentBagHandForMulliganBeat();
        }
    }

    void EnterStage(TutorialStage newStage)
    {
        currentStageData = null;
        ExitStage();
        stage = newStage;
        if (stage == TutorialStage.ActuallyFinishDefault)
        {
            ExitTutorial(refillHand: true);
            return;
        }
        int stageIndex = (int)stage;
        if (stageIndex < 0 || stageIndex >= stageParents.Count)
        {
            Debug.LogError($"Tutorial stage index out of range: {stage} ({stageIndex})");
            return;
        }
        stageParents[stageIndex].SetActive(true);
        stagePhase = 0;
        placingRule = true;
        choosingRule = true;
        allowedColor = Logic.TokenColor.Gold;
        changed = true;
        if (!stageParent.TryGetComponent(out currentStageData))
        {
            currentStageData = null;
        }

        switch (stage)
        {
            case TutorialStage.Placing:
                placingRule = true;
                choosingRule = false;
                if (currentStageData != null)
                {
                    // Hide overlays until RunEnterPresentation animates them in (avoids a one-frame flash).
                    currentStageData.ActivateStage(0, false);
                }
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0, 2),
                };
                Services.GameController.CreateHand();
                break;
            case TutorialStage.FreeSlot:
                placingRule = true;
                choosingRule = false;
                break;
            case TutorialStage.HandRefill:
                break;
            case TutorialStage.WeirdSet:
                placingRule = true;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0, 3),
                    new Vector2Int(1, 3),
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Blue;
                break;
            case TutorialStage.Undo:
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                break;
            case TutorialStage.FirstRed:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(4, 2),
                    new Vector2Int(4, 1)
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Red;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                break;
            case TutorialStage.Blue3:
                placingRule = true;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0, 3),
                    new Vector2Int(1, 3),
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Blue;
                break;
            case TutorialStage.SecondRed:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(2, 4),
                    new Vector2Int(2, 3)
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Red;
                break;
            case TutorialStage.LearnGreen:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(3, 3),
                    new Vector2Int(4, 3)
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Green;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                break;
            case TutorialStage.Green2:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(4, 4)
                };
                choosingRule = true;
                allowedColor = TokenColor.Green;
                break;
            case TutorialStage.CleanUp:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0, 1)
                };
                choosingRule = true;
                allowedColor = TokenColor.Blue;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                break;
            case TutorialStage.Purple:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(1, 1)
                };
                choosingRule = true;
                allowedColor = TokenColor.Purple;
                currentStageData = stageParent.GetComponent<TutorialStageData>();
                BindHighlightFromStageData(0);
                break;
        }

        EnsureIntroClickForwarder();
    }

    void ExitStage()
    {
        int index = (int)stage;
        if (index < 0 || index >= stageParents.Count) { return; }
        GameObject stageObject = stageParents[index];
        if (stageObject != null)
        {
            stageObject.SetActive(false);
        }
    }

    void IncrementStageInternal()
    {
        TutorialStage newStage = (TutorialStage)((int)stage + 1);
        EnterStage(newStage);
    }

    IEnumerator StageUpdateRoutine()
    {
        isPresenting = true;
        stagePhase += 1;

        if (stage == TutorialStage.Placing)
        {
            ApplyPlacingStageUpdate();
            isPresenting = false;
            activeTransition = null;
            yield break;
        }

        yield return WaitForGridSettled();
        ReleaseGameplayInputIfReady();
        deferredIncrementTrigger = null;
        yield return ApplyAnimatedStageUpdate();

        if (deferredIncrementTrigger.HasValue)
        {
            IncrementTrigger trigger = deferredIncrementTrigger.Value;
            deferredIncrementTrigger = null;
            yield return IncrementStageRoutine(trigger, nested: true);
        }

        isPresenting = false;
        activeTransition = null;
    }

    void ApplyPlacingStageUpdate()
    {
        if (stagePhase < 3)
        {
            allowedPlaces[0] = new Vector2Int(stagePhase, 2);
            if (highlight != null)
            {
                highlight.transform.position += Vector3.right * Services.GameController.gridSeparation.x;
            }
            if (arrow != null)
            {
                arrow.transform.position += Vector3.right * Services.GameController.gridSeparation.x;
            }
        }
        else
        {
            // Third tile placed; merge runs next. Keep highlight/arrow until AfterUpgrade advances to Blue2.
            allowedPlaces.Clear();
        }
    }

    IEnumerator ApplyAnimatedStageUpdate()
    {
        switch (stage)
        {
            case TutorialStage.WeirdSet:
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(1, 1));
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(0, 1);
                        BindHighlightFromStageData(1);
                    }
                }
                if (stagePhase == 3)
                {
                    DeferIncrementStage(IncrementTrigger.AfterPlace);
                }
                break;
            case TutorialStage.Undo:
                if (stagePhase == 1)
                {
                    placingRule = true;
                    allowedColor = Logic.TokenColor.Blue;
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(1, 2));
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(0, 1);
                        BindHighlightFromStageData(1);
                    }
                }
                break;
            case TutorialStage.FirstRed:
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(3, 2));
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(0, 1);
                        BindHighlightFromStageData(1);
                    }
                }
                if (stagePhase == 3)
                {
                    DeferIncrementStage(IncrementTrigger.AfterPlace);
                }
                break;
            case TutorialStage.Blue3:
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(0, 2));
                    allowedColor = Logic.TokenColor.Red;
                    placingRule = false;
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(0, 1);
                        BindHighlightFromStageData(1);
                    }
                }
                if (stagePhase == 3)
                {
                    placingRule = true;
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(0, 2));
                    allowedColor = Logic.TokenColor.Blue;
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(1, 2);
                        BindHighlightFromStageData(2);
                    }
                }
                if (stagePhase == 4)
                {
                    // Final blue placement merges to blue 3; keep highlight until AfterUpgrade.
                    allowedPlaces.Clear();
                }
                break;
            case TutorialStage.SecondRed:
                if (stagePhase == 2)
                {
                    DeferIncrementStage(IncrementTrigger.AfterPlace);
                }
                break;
            case TutorialStage.LearnGreen:
                if (stagePhase == 2)
                {
                    DeferIncrementStage(IncrementTrigger.AfterPlace);
                }
                break;
            case TutorialStage.CleanUp:
                if (stagePhase == 1)
                {
                    allowedPlaces[0] = new Vector2Int(2, 1);
                    allowedColor = TokenColor.Red;
                    if (currentStageData != null)
                    {
                        yield return currentStageData.TransitionPhase(0, 1);
                        BindHighlightFromStageData(1);
                    }
                }
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    if (currentStageData != null)
                    {
                        yield return currentStageData.AnimateOutStageCoroutine(1);
                    }
                    DeferIncrementStage(IncrementTrigger.AfterPlace);
                }
                break;
            case TutorialStage.Purple:
                DeferIncrementStage(IncrementTrigger.AfterPlace);
                break;
        }
    }

    void Update()
    {
        if (false && active == false)
        {
            if (greenLearnt == false)
            {
                if (Services.GameController.inputState == InputState.Choose)
                {
                    foreach (TokenData token in Services.GameController.game.bag.bagContents.Keys)
                    {
                        if (token.color == TokenColor.Green)
                        {
                            return;
                        }
                    }
                }
            }
        }

        if (changed)
        {
            changed = false;
        }

        TryHandleIntroAdvanceInput();
    }
}
