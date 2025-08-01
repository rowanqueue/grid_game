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
    public Transform nextTokenParent;
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
        Services.AudioManager.PlayBagRustleSound();
        ClearBag();
        MakeBag();
        
    }
    public void NextBag()
    {
        /*if (Services.GameController.inTutorial && Services.GameController.tutorial.stage == TutorialStage.GreenBag)
        {
            Services.GameController.tutorial.IncrementStage();
        }*/
        if (showNextBag == true) { return; }
        showNextBag = true;
        Services.AudioManager.PlayBagRustleSound();
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
        bagContents = game.bag.GetCurrentBag();
        List<TokenData> uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1,t2) => t1.CompareTo(t2));
        int i = 0;
        int splitter = 4;
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            int amountTokens = bagContents[tokenData].x;
            if(amountTokens > splitter)
            {
                amountTokens = 1;
            }
            for (int j = 0; j < amountTokens; j++)
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
                if (bagContents[tokenData].x > splitter)
                {
                    token.ShowCrunchedDisplay(bagContents[tokenData].x);
                }
                Vector2 move = new Vector2(i % perRow * gridSeparation.x, i / perRow * gridSeparation.y);
                token.Draw(firstGridPos + move + (Vector2)transform.position);
                token.transform.position = firstGridPos + move + (Vector2)transform.position;
                i++;
                numTokens = i;
            }
        }
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            
            
            int leftover = bagContents[tokenData].y - bagContents[tokenData].x;
            int amountTokens = leftover;
            if (amountTokens > splitter)
            {
                amountTokens = 1;
            }
            for (int j = 0; j < amountTokens; j++)
            {
                Token token = GameObject.Instantiate(Services.GameController.tokenPrefab, tokenParent).GetComponent<Token>();
                token.Init(new Logic.Token(tokenData, true));
                token.gameObject.SetActive(true);
                token.UpdateLayer("UIToken");
                token.TurnShade();
                if (leftover > splitter)
                {
                    token.ShowCrunchedDisplay(leftover);
                }
                Vector2 move = new Vector2(i % perRow * gridSeparation.x, i / perRow * gridSeparation.y);
                token.Draw(firstGridPos + move + (Vector2)transform.position);
                token.transform.position = firstGridPos + move + (Vector2)transform.position;
                i++;
                numTokens = i;
            }
        }
        if (showNextBag == false)
        {
            
        }
        bagContents = game.bag.GetNextBag();
        uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1, t2) => t1.CompareTo(t2));
        i = 0;
        Vector2 miniTilePos = new Vector2(-2.25f, -2.5f);
        Vector2 miniTileSeparation = new Vector2(0.5f, -0.55f);
        int miniTilePerRow = 10;
;        foreach (Logic.TokenData tokenData in uniqueTokens)
        {

            for (int j = 0; j < bagContents[tokenData].x; j++)
            {
                MiniTile token = GameObject.Instantiate(Services.GameController.miniTilePrefab, tokenParent).GetComponent<MiniTile>();
                token.SetTile(tokenData);
                token.gameObject.SetActive(true);
                

                Vector2 move = new Vector2(i % miniTilePerRow * miniTileSeparation.x, i / miniTilePerRow * miniTileSeparation.y);
                //token.Draw(firstGridPos + move + (Vector2)transform.position);
                token.transform.position = miniTilePos + move + (Vector2)transform.position;
                i++;
                numTokens = i;
            }
        }

    }
}
