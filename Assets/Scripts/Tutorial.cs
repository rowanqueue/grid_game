using Logic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum TutorialStage
{
    Intro,//1hand: 4 blues
    Placing,//3 places until you make Blue2
    Blue2,
    FreeSlot,
    HandRefill,//2hand: 2 blues 2 reds
    WeirdSet,//leading up to undo
    Undo,
    FirstRed,//3hand: 2 reds 2 blues
    RedScoring,
    Blue3,//place 2 blues, then reserve 1 red 4hand: blue,red,2 greens
    SecondRed,
    LearnGreen,//5hand: blue,red,green,red
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
    TutorialStep currentStep;
    bool greenLearnt;
    bool purpleLearnt;
    public List<Transform> tokenTransforms;
    public List<Token> purpleTokens;
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
    // Start is called before the first frame update
    public void StartTutorial()
    {
        active = true;
        EnterStage(TutorialStage.Intro);
    }
    public void ExitTutorial()
    {
        PlayerPrefs.SetInt("tutorialComplete", 1);
        active = false;
    }
    public void StartSpecialTutorial(TutorialStage stage)
    {
        active = true;
        EnterStage(stage);
    }
    void EnterStage(TutorialStage newStage)
    {
        currentStep = null;
        ExitStage();
        stage = newStage;
        if (stage == TutorialStage.ActuallyFinishDefault)
        {
            ExitTutorial();
            return;
        }
        stageParents[(int)stage].SetActive(true);
        stagePhase = 0;
        placingRule = true;
        choosingRule = true;
        allowedColor = Logic.TokenColor.Gold;
        changed = true;
        //create a bunch
        switch (stage)
        {
            case TutorialStage.Placing:
                placingRule = true;
                choosingRule = false;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0,2),
                };
                Services.GameController.CreateHand();
                break;
            case TutorialStage.FreeSlot:
                placingRule = true;
                choosingRule = false;
                break;
            case TutorialStage.WeirdSet:
                placingRule = true;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                allowedPlaces = new List<Vector2Int>()
                    {
                        new Vector2Int(0,3),
                        new Vector2Int(1,3),
                    };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Blue;
                break;
            case TutorialStage.Undo:
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
            case TutorialStage.FirstRed:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(4,2),
                            new Vector2Int(4,1)
                        };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Red;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
            case TutorialStage.Blue3:
                placingRule = true;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                allowedPlaces = new List<Vector2Int>()
                    {
                        new Vector2Int(0,3),
                        new Vector2Int(1,3),
                    };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Blue;
                break;
            case TutorialStage.SecondRed:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(2,4),
                            new Vector2Int(2,3)
                        };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Red;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
            case TutorialStage.LearnGreen:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(3,3),
                            new Vector2Int(4,3)
                        };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Green;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
            case TutorialStage.Green2:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(4,4)
                };
                choosingRule = true;
                allowedColor = TokenColor.Green;

                break;
            case TutorialStage.CleanUp:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0,1)
                };
                choosingRule = true;
                allowedColor = TokenColor.Blue;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
            case TutorialStage.Purple:
                placingRule = true;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(1,1)
                };
                choosingRule = true;
                allowedColor = TokenColor.Red;
                currentStep = stageParent.GetComponent<TutorialStep>();
                highlight = currentStep.highlights[0].gameObject;
                highlight.SetActive(true);
                break;
                /*
                    case TutorialStage.HandRefill:
                        choosingRule = true;
                        break;


                    case TutorialStage.FirstRed:
                        placingRule = true;
                        allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(4,2),
                            new Vector2Int(4,1)
                        };
                        choosingRule = true;
                        allowedColor = Logic.TokenColor.Red;
                        break;
                    case TutorialStage.Red2:
                        placingRule = true;
                        allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(4,3)
                        };
                        choosingRule = true;
                        allowedColor = Logic.TokenColor.Red;
                        break;
                    case TutorialStage.EmptyHand:
                        placingRule = true;
                        currentStep = stageParent.GetComponent<TutorialStep>();
                        highlight = currentStep.highlights[0].gameObject;
                        highlight.SetActive(true);
                        allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(0,3),
                            new Vector2Int(1,3),
                        };
                        choosingRule = true;
                        allowedColor = Logic.TokenColor.Blue;
                        break;
                    case TutorialStage.ThirdBlue2:
                        placingRule = true;
                        allowedPlaces = new List<Vector2Int>()
                        {
                            new Vector2Int(2,3),
                        };
                        choosingRule = true;
                        allowedColor = Logic.TokenColor.Blue;
                        break;
                    case TutorialStage.GreenStart:
                    case TutorialStage.GreenBag:
                    case TutorialStage.GreenNextBag:
                    case TutorialStage.PurpleStart:
                        placingRule = true;
                        choosingRule = true;
                        break;
                    */
        }
    }
    void ExitStage()
    {
        stageParents[(int)stage].SetActive(false);
    }
    public void IncrementStage()
    {
        TutorialStage newStage = (TutorialStage)((int)stage + 1);
        /*if(newStage == TutorialStage.BagIntro)
        {
            newStage = (TutorialStage)((int)newStage + 1);
        }*/
        EnterStage(newStage);
    }
    public void StageUpdate()
    {
        stagePhase += 1;
        switch (stage)
        {
            case TutorialStage.Placing:
                
                if (stagePhase < 3)
                {
                    allowedPlaces[0] = new Vector2Int(stagePhase, 2);
                    highlight.transform.position += Vector3.right*Services.GameController.gridSeparation.x;
                }
                else
                {
                    allowedPlaces.Clear();
                    highlight.SetActive(false);
                }
                break;
            case TutorialStage.WeirdSet:
                if(stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(1, 1));
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[1].gameObject;
                    highlight.SetActive(true);
                }
                if(stagePhase == 3)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.Undo:
                if(stagePhase == 1)
                {
                    placingRule = true;
                    allowedColor = Logic.TokenColor.Blue;
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(1, 2));
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[2].gameObject;
                    highlight.SetActive(true);
                }
                break;
            case TutorialStage.FirstRed:
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(3, 2));
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[1].gameObject;
                    highlight.SetActive(true);
                }
                if(stagePhase == 3)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.Blue3:
                if (stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(0, 2));
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[1].gameObject;
                    allowedColor = Logic.TokenColor.Red;
                    placingRule = false;
                }
                if (stagePhase == 3)
                {
                    placingRule = true;
                    allowedPlaces.Clear();
                    allowedPlaces.Add(new Vector2Int(0, 2));
                    highlight.SetActive(true);
                    allowedColor = Logic.TokenColor.Blue;
                }
                if(stagePhase == 4)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.SecondRed:
                if (stagePhase == 2)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.LearnGreen:
                if (stagePhase == 2)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.CleanUp:
                if(stagePhase == 1)
                {
                    allowedPlaces[0] = new Vector2Int(2, 2);
                    allowedColor = TokenColor.Red;
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[1].gameObject;
                    highlight.SetActive(true);
                }
                if(stagePhase == 2)
                {
                    allowedPlaces.Clear();
                    highlight.SetActive(false);
                }
                if(stagePhase == 3)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.Purple:
                if (stagePhase == 1)
                {
                    allowedPlaces[0] = new Vector2Int(1, 2);
                    allowedColor = TokenColor.Purple;
                    highlight.SetActive(false);
                    highlight = currentStep.highlights[1].gameObject;
                    highlight.SetActive(true);
                }
                if (stagePhase == 2)
                {
                    IncrementStage();
                }
                break;
                /*
                case TutorialStage.EmptyHand:
                    if (stagePhase == 2)
                    {
                        allowedColor = Logic.TokenColor.Red;
                        allowedPlaces.Clear();
                        highlight.SetActive(false);
                        currentStep.highlights[2].gameObject.SetActive(true);
                    }
                    if(stagePhase == 3)
                    {
                        IncrementStage();
                    }
                    break;
                */
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(false && active == false)
        {
            if(greenLearnt == false)
            {
                if(Services.GameController.inputState == InputState.Choose)
                {
                    foreach (TokenData token in Services.GameController.game.bag.bagContents.Keys)
                    {
                        if (token.color == TokenColor.Green)
                        {
                            //StartSpecialTutorial(TutorialStage.GreenStart);
                            return;
                        }
                    }
                }
                
            }
            if (purpleLearnt == false)
            {
                /*for (int i = 0; i < Services.GameController.hand.Count; i++)
                {
                    Token t = Services.GameController.hand[i];
                    if (t != null)
                    {
                        Logic.TokenColor color = t.token.data.color;
                        if (color == Logic.TokenColor.Purple)
                        {
                            StartSpecialTutorial(TutorialStage.PurpleStart);
                            return;
                        }
                    }
                }*/
            }
        }
        
        if (changed)
        {
            changed = false;
            return;
        }
        if (active)
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
                    break;
                default:
                    if (Input.anyKeyDown)
                    {
                        Services.AudioManager.PlayTutorialNotificationSound();
                        Debug.Log("start tutorial");
                        IncrementStage();
                    }
                    break;
            }
            
        }
    }
}
