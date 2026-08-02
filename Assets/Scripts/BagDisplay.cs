using Logic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    Coroutine emptyBagRoutine;

    void Start()
    {
    }

    void Update()
    {
        bagSwitchSprite.flipX = showNextBag;
    }

    public void CurrentBag()
    {
        if (showNextBag == false) { return; }
        showNextBag = false;
        Services.AudioManager.PlayBagRustleSound();
        MakeBag();
    }

    public void NextBag()
    {
        if (showNextBag == true) { return; }
        showNextBag = true;
        Services.AudioManager.PlayBagRustleSound();
        MakeBag();
    }

    public void ClearBag()
    {
        int count = tokenParent.childCount;
        StringBuilder childDump = new StringBuilder();
        List<GameObject> children = new List<GameObject>(count);
        foreach (Transform child in tokenParent)
        {
            children.Add(child.gameObject);
            childDump.Append($" [{child.name} id={child.GetInstanceID()} active={child.gameObject.activeSelf}]");
        }
        Debug.Log($"[BagUI] ClearBag children={count} t={Time.time:F3}{childDump}");

        foreach (GameObject go in children)
        {
            go.SetActive(false);
            GameObject.Destroy(go);
        }
    }

    public void ClearBagAfterDelay()
    {
        gameObject.SetActive(true);
        CancelDelayedClear();
        emptyBagRoutine = StartCoroutine(EmptyBag());
        Debug.Log($"[BagUI] ClearBagAfterDelay scheduled children={tokenParent.childCount} t={Time.time:F3}");
    }

    void CancelDelayedClear()
    {
        if (emptyBagRoutine != null)
        {
            StopCoroutine(emptyBagRoutine);
            emptyBagRoutine = null;
            Debug.Log($"[BagUI] CancelDelayedClear t={Time.time:F3}");
        }
    }

    IEnumerator EmptyBag()
    {
        yield return new WaitForSeconds(1f);
        emptyBagRoutine = null;
        bool willClear = Services.GameController.gameState != GameState.Bag;
        Debug.Log($"[BagUI] EmptyBag willClear={willClear} gameState={Services.GameController.gameState} children={tokenParent.childCount} t={Time.time:F3}");
        if (willClear)
        {
            ClearBag();
        }
    }

    public void MakeBag()
    {
        CancelDelayedClear();
        int staleCount = tokenParent.childCount;
        string staleTag = staleCount > 0 ? " (STALE)" : "";
        Debug.Log($"[BagUI] MakeBag start showNext={showNextBag} children={staleCount}{staleTag} t={Time.time:F3}");
        ClearBag();

        Dictionary<Logic.TokenData, Vector2Int> bagContents;
        if (showNextBag)
        {
            bagContents = game.bag.GetNextBag();
        }
        else
        {
            bagContents = game.bag.GetCurrentBag();
        }

        List<TokenData> uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1, t2) => t1.CompareTo(t2));
        int i = 0;
        int splitter = 4;
        int totalUniqueTokens = 0;
        int usedTokens = 0;
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            if (bagContents[tokenData].x > splitter)
            {
                totalUniqueTokens += 1;
            }
            else
            {
                totalUniqueTokens += bagContents[tokenData].x;
            }
            int leftover = bagContents[tokenData].y - bagContents[tokenData].x;
            if (leftover > splitter)
            {
                totalUniqueTokens += 1;
                usedTokens += 1;
            }
            else
            {
                totalUniqueTokens += leftover;
                usedTokens += leftover;
            }
        }
        int actualPerRow = perRow;
        Vector2 realFirstPos = firstGridPos;
        Vector2 realGridSeperation = gridSeparation;
        bool showUsed = true;
        if (totalUniqueTokens > 6 * 4)
        {
            showUsed = false;
            totalUniqueTokens -= usedTokens;
        }
        if (totalUniqueTokens > 5 * 4)
        {
            actualPerRow = 6;
            realFirstPos.x -= gridSeparation.x * 0.26f;
            realGridSeperation.x = 0.85f;
        }
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            int amountTokens = bagContents[tokenData].x;
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
                token.shadow.enabled = false;
                if (bagContents[tokenData].x > splitter)
                {
                    token.ShowCrunchedDisplay(bagContents[tokenData].x);
                }
                Vector2 move = new Vector2(i % actualPerRow * realGridSeperation.x, i / actualPerRow * realGridSeperation.y);
                token.Draw(realFirstPos + move + (Vector2)transform.position);
                token.transform.position = realFirstPos + move + (Vector2)transform.position;
                i++;
                numTokens = i;
            }
        }
        if (showUsed)
        {
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
                    token.shadow.enabled = false;
                    if (leftover > splitter)
                    {
                        token.ShowCrunchedDisplay(leftover);
                    }
                    token.SetBagUsedAppearance();
                    Vector2 move = new Vector2(i % actualPerRow * realGridSeperation.x, i / actualPerRow * realGridSeperation.y);
                    token.Draw(realFirstPos + move + (Vector2)transform.position);
                    token.transform.position = realFirstPos + move + (Vector2)transform.position;
                    i++;
                    numTokens = i;
                }
            }
        }

        bagContents = game.bag.GetNextBag();
        uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1, t2) => t1.CompareTo(t2));
        float cursor = 0f;
        int row = 0;
        Vector2 miniTilePos = new Vector2(-2.5f, -2.5f);
        Vector2 miniTileSeparation = new Vector2(0.48f, -0.55f);
        int miniTilePerRow = 12;
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            int count = bagContents[tokenData].x;
            float countWidth = count >= 10 ? 1.35f : 1f;
            float pairWidth = 1f + countWidth;
            if (cursor > 0f && cursor + pairWidth > miniTilePerRow)
            {
                cursor = 0f;
                row++;
            }

            MiniTile token = GameObject.Instantiate(Services.GameController.miniTilePrefab, tokenParent).GetComponent<MiniTile>();
            token.SetTile(tokenData);
            token.gameObject.SetActive(true);
            Vector2 move = new Vector2(cursor * miniTileSeparation.x, row * miniTileSeparation.y);
            token.transform.position = miniTilePos + move + (Vector2)transform.position;
            cursor += 1f;
            numTokens++;

            token = GameObject.Instantiate(Services.GameController.miniTilePrefab, tokenParent).GetComponent<MiniTile>();
            token.SetTile(new TokenData(Logic.TokenColor.Display, count));
            token.gameObject.SetActive(true);
            move = new Vector2(cursor * miniTileSeparation.x, row * miniTileSeparation.y);
            token.transform.position = miniTilePos + move + (Vector2)transform.position;
            cursor += countWidth;
            numTokens++;
        }

        Debug.Log($"[BagUI] MakeBag end showNext={showNextBag} spawned={tokenParent.childCount} t={Time.time:F3}");
    }
}
