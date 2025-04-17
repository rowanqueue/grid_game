using Logic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BagDisplay : MonoBehaviour
{
    public bool bagUpdated = true;
    public Logic.Game game;
    public Vector2 firstGridPos;
    public Vector2 gridSeparation;
    public Transform tokenParent;
    public int perRow;
    public int numTokens = 0;
    public bool showNextBag = false;

    public SpriteRenderer bagSwitchSprite;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bagSwitchSprite.flipX = showNextBag;
        
    }
    public void CurrentBag()
    {
        if (showNextBag == false) { return; }
        showNextBag = false;
        ClearBag();
        MakeBag();
    }
    public void NextBag()
    {
        if (Services.GameController.inTutorial && Services.GameController.tutorial.stage == TutorialStage.GreenBag)
        {
            Services.GameController.tutorial.IncrementStage();
        }
        if (showNextBag == true) { return; }
        showNextBag = true;
        ClearBag();
        MakeBag();
    }
    public void ClearBag()
    {
        foreach(Transform child in tokenParent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    public void ClearBagAfterDelay()
    {
        StartCoroutine(EmptyBag());
    }
    IEnumerator EmptyBag()
    {
        yield return new WaitForSeconds(1f);
        if(Services.GameController.gameState != GameState.Bag)
        {
            ClearBag();
        }
        
    }
    public void MakeBag()
    {
        Dictionary<Logic.TokenData, Vector2Int> bagContents = new Dictionary<Logic.TokenData, Vector2Int>();
        if (showNextBag)
        {
            bagContents = game.bag.GetNextBag();
        }
        else
        {
            bagContents = game.bag.GetCurrentBag();
        }
        List<TokenData> uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1,t2) => t1.CompareTo(t2));
        int i = 0;
        foreach(Logic.TokenData tokenData in uniqueTokens)
        {

            for (int j = 0; j < bagContents[tokenData].x; j++)
            {
                Token token = GameObject.Instantiate(Services.GameController.tokenPrefab, tokenParent).GetComponent<Token>();
                token.Init(new Logic.Token(tokenData, true));
                token.gameObject.SetActive(true);
                token.UpdateLayer("UIToken");
                if(showNextBag == false)
                {
                    /*if (j >= bagContents[tokenData].x)
                    {
                        token.TurnShade();
                    }*/
                }

                Vector2 move = new Vector2(i % perRow * gridSeparation.x, i / perRow * gridSeparation.y);
                token.Draw(firstGridPos + move + (Vector2)transform.position);
                token.transform.position = firstGridPos + move + (Vector2)transform.position;
                i++;
                numTokens = i;
            }
        }
        if(showNextBag == false)
        {
            foreach (Logic.TokenData tokenData in uniqueTokens)
            {
                int leftover = bagContents[tokenData].y - bagContents[tokenData].x;
                for (int j = 0; j < leftover; j++)
                {
                    Token token = GameObject.Instantiate(Services.GameController.tokenPrefab, tokenParent).GetComponent<Token>();
                    token.Init(new Logic.Token(tokenData, true));
                    token.gameObject.SetActive(true);
                    token.UpdateLayer("UIToken");
                    token.TurnShade();

                    Vector2 move = new Vector2(i % perRow * gridSeparation.x, i / perRow * gridSeparation.y);
                    token.Draw(firstGridPos + move + (Vector2)transform.position);
                    token.transform.position = firstGridPos + move + (Vector2)transform.position;
                    i++;
                    numTokens = i;
                }
            }
        }
        
    }
}
