using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialStage
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
    Finish
}
public class Tutorial : MonoBehaviour
{
    bool active = false;
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
        GameObject.Destroy(gameObject);
    }
    void EnterStage(TutorialStage newStage)
    {
        ExitStage();
        stage = newStage;
        if ((int)stage >= stageParents.Count)
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
                highlight = stageParent.transform.GetChild(1).gameObject;
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0,2),
                };
                Services.GameController.CreateHand();
                break;
            case TutorialStage.HandRefill:
            case TutorialStage.FreeSlot:
                placingRule = true;
                choosingRule = false;
                break;
            case TutorialStage.WeirdSet:
                placingRule = true;
                highlight = stageParent.transform.GetChild(1).gameObject;
                highlight.SetActive(true);
                allowedPlaces = new List<Vector2Int>()
                {
                    new Vector2Int(0,3),
                    new Vector2Int(1,3),
                };
                choosingRule = true;
                allowedColor = Logic.TokenColor.Blue;
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
                highlight = stageParent.transform.GetChild(1).gameObject;
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
        }
    }
    void ExitStage()
    {
        stageParents[(int)stage].SetActive(false);
    }
    public void IncrementStage()
    {
        TutorialStage newStage = (TutorialStage)((int)stage + 1);
        if(newStage == TutorialStage.BagIntro)
        {
            newStage = (TutorialStage)((int)newStage + 1);
        }
        EnterStage(newStage);
    }
    public void StageUpdate()
    {
        stagePhase += 1;
        Debug.Log(stagePhase);
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
                    allowedPlaces.Add(new Vector2Int(1, 2));
                    highlight.SetActive(false);
                    highlight = stageParent.transform.GetChild(2).gameObject;
                    highlight.SetActive(true);
                }
                break;
            case TutorialStage.FirstRed:
                if(stagePhase == 2)
                {
                    IncrementStage();
                }
                break;
            case TutorialStage.EmptyHand:
                if (stagePhase == 2)
                {
                    allowedColor = Logic.TokenColor.Red;
                    allowedPlaces.Clear();
                    highlight.SetActive(false);
                    highlight = stageParent.transform.GetChild(2).gameObject;
                    highlight.SetActive(true);
                }
                if(stagePhase == 3)
                {
                    IncrementStage();
                }
                break;
        }
    }
    // Update is called once per frame
    void Update()
    {
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
                case TutorialStage.FirstRed:
                case TutorialStage.BagIntro:
                case TutorialStage.Red2:
                case TutorialStage.EmptyHand:
                case TutorialStage.TeachMulligan:
                case TutorialStage.ThirdBlue2:
                    break;
                default:
                    if (Input.anyKeyDown)
                    {
                        IncrementStage();
                    }
                    break;
            }
            
        }
    }
}
