using Logic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
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

    [SerializeField] float nextBagLabelGap = 0.1f;
    [SerializeField] TMP_Text nextBagDisplay;

    static readonly Vector2 miniTilePos = new Vector2(-2.5f, -2.5f);
    static readonly Vector2 miniTileSeparation = new Vector2(0.40f, -0.55f);
    const int miniTilePerRow = 15;
    const float countWidth = 1f;
    const float miniTileHalfHeight = 0.64f;

    TMP_Text nextBagLabel;

    Coroutine emptyBagRoutine;

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
        int count = tokenParent != null ? tokenParent.childCount : 0;
        StringBuilder childDump = new StringBuilder();
        List<GameObject> children = new List<GameObject>(count);
        if (tokenParent != null)
        {
            foreach (Transform child in tokenParent)
            {
                children.Add(child.gameObject);
                childDump.Append($" [{child.name} id={child.GetInstanceID()} active={child.gameObject.activeSelf}]");
            }
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
        const int usedSplitter = 2;
        int remainingSplitter = 4;
        int fiveColCapacity = perRow * 4;
        int sixColCapacity = 6 * 4;
        bool showUsed = true;

        while (CountBagDisplaySlots(bagContents, uniqueTokens, remainingSplitter, usedSplitter, showUsed) > fiveColCapacity
               && remainingSplitter > 2)
        {
            remainingSplitter--;
        }
        if (CountBagDisplaySlots(bagContents, uniqueTokens, remainingSplitter, usedSplitter, showUsed) > sixColCapacity)
        {
            showUsed = false;
        }

        int actualPerRow = perRow;
        Vector2 realFirstPos = firstGridPos;
        Vector2 realGridSeparation = gridSeparation;
        if (CountBagDisplaySlots(bagContents, uniqueTokens, remainingSplitter, usedSplitter, showUsed) > fiveColCapacity)
        {
            actualPerRow = 6;
            realFirstPos.x -= gridSeparation.x * 0.26f;
            realGridSeparation.x = 0.85f;
        }

        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            int amountTokens = bagContents[tokenData].x;
            if (amountTokens >= remainingSplitter)
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
                if (bagContents[tokenData].x >= remainingSplitter)
                {
                    token.ShowCrunchedDisplay(bagContents[tokenData].x);
                }
                Vector2 move = new Vector2(i % actualPerRow * realGridSeparation.x, i / actualPerRow * realGridSeparation.y);
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
                if (amountTokens >= usedSplitter)
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
                    if (leftover >= usedSplitter)
                    {
                        token.ShowCrunchedDisplay(leftover);
                    }
                    token.SetBagUsedAppearance();
                    Vector2 move = new Vector2(i % actualPerRow * realGridSeparation.x, i / actualPerRow * realGridSeparation.y);
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
        foreach (Logic.TokenData tokenData in uniqueTokens)
        {
            int count = bagContents[tokenData].x;
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

        PlaceNextBagLabel();

        Debug.Log($"[BagUI] MakeBag end showNext={showNextBag} spawned={tokenParent.childCount} t={Time.time:F3}");
    }

    void PlaceNextBagLabel()
    {
        EnsureNextBagLabel();
        if (nextBagLabel == null)
        {
            return;
        }

        RectTransform labelRect = nextBagLabel.rectTransform;
        if (labelRect == null)
        {
            return;
        }

        float halfH = Mathf.Abs(labelRect.sizeDelta.y) * 0.5f;
        if (halfH < 0.01f)
        {
            halfH = 0.5f;
        }

        float bottomInset = Mathf.Max(0f, nextBagLabel.margin.w);
        float rowTop = miniTilePos.y + miniTileHalfHeight;

        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, rowTop + nextBagLabelGap + halfH - bottomInset);
        labelRect.localScale = Vector3.one;
        nextBagLabel.gameObject.SetActive(true);
    }

    void EnsureNextBagLabel()
    {
        if (nextBagLabel == null)
        {
            nextBagLabel = nextBagDisplay;
        }

        if (nextBagLabel == null)
        {
            Transform sceneLabel = transform.Find("NextBagDisplay");
            if (sceneLabel != null)
            {
                nextBagLabel = sceneLabel.GetComponent<TMP_Text>();
            }
        }

        if (nextBagLabel != null)
        {
            nextBagLabel.gameObject.SetActive(true);
        }
    }

    static int SlotsForCount(int count, int splitter)
    {
        if (count <= 0)
        {
            return 0;
        }
        return count >= splitter ? 1 : count;
    }

    static int CountBagDisplaySlots(
        Dictionary<TokenData, Vector2Int> bagContents,
        List<TokenData> uniqueTokens,
        int remainingSplitter,
        int usedSplitter,
        bool includeUsed)
    {
        int slots = 0;
        foreach (TokenData tokenData in uniqueTokens)
        {
            slots += SlotsForCount(bagContents[tokenData].x, remainingSplitter);
            if (includeUsed)
            {
                int leftover = bagContents[tokenData].y - bagContents[tokenData].x;
                slots += SlotsForCount(leftover, usedSplitter);
            }
        }
        return slots;
    }
}
