using Logic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] Canvas nextBagCanvas;
    [SerializeField] RectTransform nextBagContent;
    [SerializeField] float nextBagEntryWidth = 0.9f;
    [SerializeField] float nextBagEntryHeight = 0.5f;
    [SerializeField] Vector2 nextBagCanvasSize = new Vector2(4.6f, 2.0f);
    [SerializeField] Vector3 nextBagLocalPos = new Vector3(0f, -2.5f, 0f);
    [SerializeField] float nextBagRowSpacing = 0.05f;
    [SerializeField] float nextBagIconCountSpacing = 0.06f;
    [SerializeField] float nextBagVerticalRowSpacing = 0.05f;
    [SerializeField] int nextBagMinPairsPerRow = 5;
    [SerializeField] float nextBagTopPadding = 0.25f;
    [SerializeField] float nextBagSidePadding = 0.2f;
    [SerializeField] float nextBagLabelGap = 0.15f;
    [SerializeField] float nextBagLabelHeight = 0.45f;
    [SerializeField] float nextBagLabelFontSize = 3.5f;
    [SerializeField] Color nextBagLabelColor = new Color(0.23f, 0.23f, 0.23f, 1f);
    [SerializeField] TMP_FontAsset nextBagLabelFont;

    TextMeshPro nextBagLabel;

    Coroutine emptyBagRoutine;

    void Start()
    {
        EnsureNextBagLayout();
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

        ClearNextBagLayoutChildren();
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

        PopulateNextBagStrip();

        Debug.Log($"[BagUI] MakeBag end showNext={showNextBag} spawned={tokenParent.childCount} t={Time.time:F3}");
    }

    void PopulateNextBagStrip()
    {
        EnsureNextBagLayout();
        ClearNextBagLayoutChildren();

        Dictionary<TokenData, Vector2Int> bagContents = game.bag.GetNextBag();
        List<TokenData> uniqueTokens = bagContents.Keys.ToList();
        uniqueTokens.Sort((t1, t2) => t1.CompareTo(t2));
        if (uniqueTokens.Count == 0)
        {
            ApplyNextBagCanvasSize(1);
            PositionNextBagLabel(GetNextBagCanvasSize(1).y);
            FitNextBagCanvasToScreen(GetNextBagCanvasSize(1));
            return;
        }

        float contentWidth = GetNextBagContentWidth();
        int pairsPerRow = CountPairsPerRow(contentWidth);

        // Disable VLG so it can't push rows below the floor; we place rows manually bottom-up.
        VerticalLayoutGroup vlg = nextBagContent != null ? nextBagContent.GetComponent<VerticalLayoutGroup>() : null;
        if (vlg != null)
        {
            vlg.enabled = false;
        }

        List<RectTransform> rows = new List<RectTransform>();
        RectTransform currentRow = null;
        int inRow = 0;
        foreach (TokenData tokenData in uniqueTokens)
        {
            if (currentRow == null || inRow >= pairsPerRow)
            {
                currentRow = CreateNextBagRow();
                rows.Add(currentRow);
                inRow = 0;
            }

            CreateNextBagEntry(currentRow, tokenData, bagContents[tokenData].x);
            inRow++;
            numTokens += 2;
        }

        Vector2 naturalSize = GetNextBagCanvasSize(rows.Count);
        ApplyNextBagCanvasSize(rows.Count);
        LayoutRowsBottomUp(rows, naturalSize.x);
        foreach (RectTransform row in rows)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }

        PositionNextBagLabel(naturalSize.y);
        FitNextBagCanvasToScreen(naturalSize);
    }

    void PositionNextBagLabel(float stripHeight)
    {
        EnsureNextBagLabel();
        if (nextBagLabel == null)
        {
            return;
        }

        RectTransform labelRect = nextBagLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(GetNextBagCanvasSize(1).x, nextBagLabelHeight);
        labelRect.anchoredPosition = new Vector2(0f, stripHeight + nextBagLabelGap);
        labelRect.localScale = Vector3.one;
        nextBagLabel.gameObject.SetActive(true);
    }

    void FitNextBagCanvasToScreen(Vector2 naturalSize)
    {
        if (nextBagCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = nextBagCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.localPosition = nextBagLocalPos;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.pivot = new Vector2(0.5f, 0f);

        float labelBlock = nextBagLabelGap + nextBagLabelHeight;
        float naturalHeight = naturalSize.y + labelBlock;
        float naturalWidth = naturalSize.x;

        float availableHeight = GetAvailableNextBagHeight();
        float availableWidth = GetAvailableNextBagWidth();

        float scale = 1f;
        if (naturalHeight > 0.01f && naturalHeight > availableHeight)
        {
            scale = Mathf.Min(scale, availableHeight / naturalHeight);
        }
        if (naturalWidth > 0.01f && naturalWidth > availableWidth)
        {
            scale = Mathf.Min(scale, availableWidth / naturalWidth);
        }

        scale = Mathf.Clamp(scale, 0.15f, 1f);
        canvasRect.localScale = new Vector3(scale, scale, 1f);
    }

    float GetAvailableNextBagHeight()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Mathf.Max(nextBagEntryHeight, nextBagCanvasSize.y);
        }

        Vector3 worldTop = cam.transform.position + new Vector3(0f, cam.orthographicSize, 0f);
        float safeTopLocalY = transform.InverseTransformPoint(worldTop).y - nextBagTopPadding;
        return Mathf.Max(nextBagEntryHeight + nextBagLabelGap + nextBagLabelHeight, safeTopLocalY - nextBagLocalPos.y);
    }

    float GetAvailableNextBagWidth()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Mathf.Max(nextBagCanvasSize.x, 1f);
        }

        float fullWidth = 2f * cam.orthographicSize * cam.aspect;
        return Mathf.Max(nextBagEntryWidth, fullWidth - nextBagSidePadding * 2f);
    }

    void LayoutRowsBottomUp(List<RectTransform> rows, float contentWidth)
    {
        if (rows == null || rows.Count == 0 || nextBagContent == null)
        {
            return;
        }

        float width = contentWidth > 0.01f ? contentWidth : GetNextBagCanvasSize(rows.Count).x;
        // Newest wrap row sits on the floor; older rows stack upward (oldest on top).
        float y = 0f;
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            RectTransform row = rows[i];
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.sizeDelta = new Vector2(width, nextBagEntryHeight);
            row.anchoredPosition = new Vector2(0f, y);
            row.localScale = Vector3.one;
            y += nextBagEntryHeight + nextBagVerticalRowSpacing;
        }
    }

    Vector2 GetNextBagCanvasSize(int rowCount = 1)
    {
        int minPairs = Mathf.Max(1, nextBagMinPairsPerRow);
        float widthForMinPairs = minPairs * nextBagEntryWidth
            + Mathf.Max(0, minPairs - 1) * nextBagRowSpacing;
        int rows = Mathf.Max(1, rowCount);
        float heightForRows = rows * nextBagEntryHeight
            + Mathf.Max(0, rows - 1) * nextBagVerticalRowSpacing;
        return new Vector2(
            Mathf.Max(nextBagCanvasSize.x, widthForMinPairs),
            Mathf.Max(nextBagEntryHeight, heightForRows));
    }

    void ApplyNextBagCanvasSize(int rowCount)
    {
        if (nextBagCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = nextBagCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.localPosition = nextBagLocalPos;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one;
        canvasRect.pivot = new Vector2(0.5f, 0f);
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = GetNextBagCanvasSize(rowCount);
    }

    float GetNextBagContentWidth()
    {
        Vector2 size = GetNextBagCanvasSize(1);
        if (nextBagContent != null && nextBagContent.rect.width > 0.01f)
        {
            return Mathf.Max(nextBagContent.rect.width, size.x);
        }
        return size.x;
    }

    int CountPairsPerRow(float contentWidth)
    {
        float stride = nextBagEntryWidth + nextBagRowSpacing;
        if (stride <= 0.01f)
        {
            return Mathf.Max(1, nextBagMinPairsPerRow);
        }
        // n * entryWidth + (n - 1) * spacing <= contentWidth
        int fitted = Mathf.FloorToInt((contentWidth + nextBagRowSpacing) / stride);
        return Mathf.Max(Mathf.Max(1, nextBagMinPairsPerRow), fitted);
    }

    void EnsureNextBagLayout()
    {
        // Parent under Bag (via nextTokenParent) so local bottom-center tracks the bag, not the camera.
        Transform parent = nextTokenParent != null ? nextTokenParent : transform;

        if (nextBagCanvas == null)
        {
            Transform existing = parent.Find("NextBagCanvas");
            if (existing != null)
            {
                nextBagCanvas = existing.GetComponent<Canvas>();
            }
        }

        if (nextBagCanvas == null)
        {
            GameObject canvasGo = new GameObject("NextBagCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.localPosition = nextBagLocalPos;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = GetNextBagCanvasSize(1);
            // Bottom-center pivot: nextBagLocalPos is the floor; strip only grows upward.
            canvasRect.pivot = new Vector2(0.5f, 0f);
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);

            nextBagCanvas = canvasGo.AddComponent<Canvas>();
            nextBagCanvas.renderMode = RenderMode.WorldSpace;
            nextBagCanvas.sortingOrder = 20;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Drop any leftover screen-anchor component from earlier experiments.
            AnchorGameObject leftoverAnchor = nextBagCanvas.GetComponent<AnchorGameObject>();
            if (leftoverAnchor != null)
            {
                Destroy(leftoverAnchor);
            }

            RectTransform canvasRect = nextBagCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localPosition = nextBagLocalPos;
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one;
                canvasRect.sizeDelta = GetNextBagCanvasSize(1);
                // Bottom-center pivot: nextBagLocalPos is the floor; strip only grows upward.
                canvasRect.pivot = new Vector2(0.5f, 0f);
                canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
                canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }

        if (nextBagContent == null && nextBagCanvas != null)
        {
            Transform existingContent = nextBagCanvas.transform.Find("NextBagContent");
            if (existingContent != null)
            {
                nextBagContent = existingContent as RectTransform;
            }
        }

        if (nextBagContent == null)
        {
            GameObject contentGo = new GameObject("NextBagContent", typeof(RectTransform));
            contentGo.transform.SetParent(nextBagCanvas.transform, false);
            nextBagContent = contentGo.GetComponent<RectTransform>();
            nextBagContent.anchorMin = Vector2.zero;
            nextBagContent.anchorMax = Vector2.one;
            nextBagContent.offsetMin = Vector2.zero;
            nextBagContent.offsetMax = Vector2.zero;
            nextBagContent.localScale = Vector3.one;

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0f;
            vlg.padding = new RectOffset(0, 0, 0, 0);
        }
        else
        {
            VerticalLayoutGroup vlg = nextBagContent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childAlignment = TextAnchor.LowerCenter;
                vlg.childForceExpandHeight = false;
            }
        }

        EnsureNextBagLabel();
    }

    void EnsureNextBagLabel()
    {
        if (nextBagCanvas == null)
        {
            return;
        }

        if (nextBagLabel == null)
        {
            Transform existing = nextBagCanvas.transform.Find("NextBagLabel");
            if (existing != null)
            {
                TextMeshProUGUI ugui = existing.GetComponent<TextMeshProUGUI>();
                if (ugui != null)
                {
                    Destroy(ugui);
                }
                nextBagLabel = existing.GetComponent<TextMeshPro>();
                if (nextBagLabel == null)
                {
                    nextBagLabel = existing.gameObject.AddComponent<TextMeshPro>();
                }
            }
        }

        if (nextBagLabelFont == null)
        {
#if UNITY_EDITOR
            nextBagLabelFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Nunito-Bold SDF.asset");
#endif
            if (nextBagLabelFont == null)
            {
                nextBagLabelFont = TMP_Settings.defaultFontAsset;
            }
        }

        if (nextBagLabel == null)
        {
            GameObject labelGo = new GameObject("NextBagLabel");
            labelGo.transform.SetParent(nextBagCanvas.transform, false);
            nextBagLabel = labelGo.AddComponent<TextMeshPro>();
        }

        nextBagLabel.text = "Next Bag";
        if (nextBagLabelFont != null)
        {
            nextBagLabel.font = nextBagLabelFont;
        }
        nextBagLabel.fontSize = nextBagLabelFontSize;
        nextBagLabel.color = nextBagLabelColor;
        nextBagLabel.alignment = TextAlignmentOptions.Center;
        nextBagLabel.enableWordWrapping = false;
        nextBagLabel.overflowMode = TextOverflowModes.Overflow;
        nextBagLabel.sortingOrder = 25;
        nextBagLabel.gameObject.SetActive(true);
    }

    void ClearNextBagLayoutChildren()
    {
        if (nextBagContent == null)
        {
            return;
        }

        List<GameObject> toDestroy = new List<GameObject>(nextBagContent.childCount);
        foreach (Transform child in nextBagContent)
        {
            toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy)
        {
            go.SetActive(false);
            GameObject.Destroy(go);
        }
    }

    RectTransform CreateNextBagRow()
    {
        GameObject rowGo = new GameObject("Row", typeof(RectTransform));
        rowGo.transform.SetParent(nextBagContent, false);
        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.localScale = Vector3.one;

        HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.LowerCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = nextBagRowSpacing;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        return rowRect;
    }

    void CreateNextBagEntry(RectTransform row, TokenData tokenData, int count)
    {
        GameObject entryGo = new GameObject("Entry", typeof(RectTransform));
        entryGo.transform.SetParent(row, false);
        RectTransform entryRect = entryGo.GetComponent<RectTransform>();
        entryRect.localScale = Vector3.one;

        HorizontalLayoutGroup hlg = entryGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.LowerCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = nextBagIconCountSpacing;

        LayoutElement entryLayout = entryGo.AddComponent<LayoutElement>();
        entryLayout.preferredWidth = nextBagEntryWidth;
        entryLayout.preferredHeight = nextBagEntryHeight;
        entryLayout.minWidth = nextBagEntryWidth;
        entryLayout.minHeight = nextBagEntryHeight;

        float innerWidth = Mathf.Max(0.01f, nextBagEntryWidth - nextBagIconCountSpacing);
        float countWidth = Mathf.Max(0.6f, innerWidth * 0.55f);
        float iconWidth = Mathf.Max(0.35f, innerWidth - countWidth);
        SpawnMiniTileInEntry(entryRect, tokenData, iconWidth);
        SpawnMiniTileInEntry(entryRect, new TokenData(TokenColor.Display, count), countWidth);
    }

    void SpawnMiniTileInEntry(RectTransform entry, TokenData data, float preferredWidth)
    {
        GameObject holderGo = new GameObject("MiniTileHolder", typeof(RectTransform));
        holderGo.transform.SetParent(entry, false);
        RectTransform holder = holderGo.GetComponent<RectTransform>();
        holder.localScale = Vector3.one;

        LayoutElement holderLayout = holderGo.AddComponent<LayoutElement>();
        holderLayout.preferredWidth = preferredWidth;
        holderLayout.preferredHeight = nextBagEntryHeight;
        holderLayout.minWidth = preferredWidth;
        holderLayout.minHeight = nextBagEntryHeight;

        MiniTile mini = GameObject.Instantiate(Services.GameController.miniTilePrefab, holder).GetComponent<MiniTile>();
        mini.SetTile(data);
        mini.gameObject.SetActive(true);
        mini.transform.localPosition = Vector3.zero;
        mini.transform.localRotation = Quaternion.identity;
        mini.transform.localScale = Vector3.one;
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
