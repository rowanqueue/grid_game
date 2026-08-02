using Logic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

enum UpgradeType
{
    Unlock,
    Upgrade
}

public class UpgradePopup : MonoBehaviour
{
    public GameObject visual;
    public GameObject unlockParent;
    public GameObject[] unlockSubParents;
    public List<Token> unlockedTokens;
    public GameObject upgradeParent;
    public Token upgradedToken;
    public Token oldToken;
    public TextMeshPro title;
    public TextMeshPro content;
    public List<MiniTile> unlockReasons;

    [SerializeField] UpgradeType type;
    public List<string> previousUnlocks;

    [Header("Big panel slide (Visual local X only)")]
    [SerializeField] float slideInFromX = -6f;
    [SerializeField] float slideOpenX = 0f;
    [SerializeField] float slideOutToX = 6f;
    [SerializeField] float slideInDuration = 0.35f;
    [SerializeField] float slideOutDuration = 0.25f;

    [Header("Layout (SafeTopCenter anchor on this object)")]
    [Tooltip("Pulls panel down from safe top; ~half panel height + title (~4) clears notch.")]
    [SerializeField] Vector3 popupAnchorOffset = new Vector3(0f, -4f, 0f);

    public List<UpgradeTab> tinyTabs;
    public List<Vector3> tabPositions;
    public List<float> tinyTabX;

    Coroutine starting;
    Coroutine ending;
    bool isClosing;

    void Awake()
    {
        var anchor = GetComponent<AnchorGameObject>();
        if (anchor != null)
            anchor.anchorOffset = popupAnchorOffset;
    }

    void Start()
    {
        visual.SetActive(false);
        for (int i = 0; i < unlockSubParents.Length; i++)
            unlockSubParents[i].SetActive(false);
        unlockParent.SetActive(false);
        upgradeParent.SetActive(false);
        for (int i = 0; i < unlockReasons.Count; i++)
            unlockReasons[i].gameObject.SetActive(false);

        foreach (UpgradeTab tab in tinyTabs)
        {
            tab.popup = this;
            tab.Deactivate();
            tabPositions.Add(tab.transform.parent.localPosition);
        }
    }

    static Dictionary<TokenData, int> CopyContents(Dictionary<TokenData, int> contents)
    {
        return new Dictionary<TokenData, int>(contents);
    }

    static List<TokenData> RemoveTriggers(Dictionary<TokenData, int> contents)
    {
        List<TokenData> triggers = new List<TokenData>();
        foreach (TokenData token in contents.Keys)
        {
            if (contents[token] == 100)
                triggers.Add(token);
        }
        foreach (TokenData token in triggers)
            contents.Remove(token);
        return triggers;
    }

    static void GetUpgradeData(Dictionary<TokenData, int> contents, out TokenData oldData, out TokenData newData)
    {
        oldData = new TokenData();
        newData = new TokenData();
        foreach (TokenData data in contents.Keys)
        {
            if (contents[data] < 0) { oldData = data; }
            if (contents[data] > 0) { newData = data; }
        }
    }

    IEnumerator TinyActuallyCreate(Dictionary<TokenData, int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        UpgradeTab tinyTab = tinyTabs[0];
        if (tinyTabs[0].active == false)
        {
        }
        else if (tinyTabs[tinyTabs.Count - 1].active)
        {
            tinyTab = tinyTabs[tinyTabs.Count - 1];
            tinyTabs.Remove(tinyTab);
            tinyTabs.Insert(0, tinyTab);
        }
        else
        {
            for (int i = 1; i < tinyTabs.Count; i++)
            {
                if (tinyTabs[i].active == false)
                {
                    tinyTab = tinyTabs[i];
                    tinyTabs.Remove(tinyTab);
                    tinyTabs.Insert(0, tinyTab);
                    break;
                }
            }
        }
        for (int i = 0; i < tinyTabs.Count; i++)
            tinyTabs[i].SetHeight(tabPositions[i].y);
        tinyTab.Activate();

        Dictionary<TokenData, int> displayContents = CopyContents(contents);
        RemoveTriggers(displayContents);

        foreach (MiniTile tile in tinyTab.miniTiles)
            tile.gameObject.SetActive(false);

        int needed = 0;
        switch (type)
        {
            case UpgradeType.Upgrade:
                needed = 3;
                break;
            case UpgradeType.Unlock:
                TokenData unlockData = displayContents.Keys.ToList()[0];
                needed = Mathf.Clamp(displayContents[unlockData], 1, 3) + 1;
                break;
        }
        int tileCount = tinyTab.miniTiles.Count - needed;
        switch (type)
        {
            case UpgradeType.Upgrade:
                GetUpgradeData(displayContents, out TokenData oldData, out TokenData newData);
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(oldData);
                tileCount++;
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetArrow();
                tileCount++;
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(newData);
                tileCount++;
                break;
            case UpgradeType.Unlock:
                TokenData data = displayContents.Keys.ToList()[0];
                int count = displayContents[data];
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(data);
                tileCount++;
                if (count > 1)
                {
                    tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                    tinyTab.miniTiles[tileCount].SetTile(data);
                    tileCount++;
                }
                if (count > 2)
                {
                    tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                    tinyTab.miniTiles[tileCount].SetTile(data);
                    tileCount++;
                }
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetPlus();
                tileCount++;
                break;
        }
        float x = tinyTabX[needed - 1];
        float y = tinyTab.transform.localPosition.y;
        tinyTab.NewPosition(new Vector2(x, y));
    }

    IEnumerator ActuallyCreate(Dictionary<TokenData, int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        bool alreadyActive = visual.activeSelf;
        visual.SetActive(true);
        SetVisualSlideX(slideInFromX);
        if (alreadyActive == false)
        {
            starting = StartCoroutine(SlideFromLeft());
        }
        else if (ending != null)
        {
            StopCoroutine(ending);
            ending = null;
            starting = StartCoroutine(SlideFromLeft());
        }

        List<TokenData> triggers = RemoveTriggers(contents);
        int triggerCount = 0;
        foreach (TokenData token in triggers)
        {
            unlockReasons[triggerCount].gameObject.SetActive(true);
            unlockReasons[triggerCount].SetTile(token);
            triggerCount++;
        }
        switch (type)
        {
            case UpgradeType.Unlock:
                unlockParent.SetActive(true);
                title.text = "Tile Unlock";
                content.text = "A {0} tile has been added to your bag!";
                TokenData data = contents.Keys.ToList()[0];
                if (contents[data] == 1 || contents[data] == 3)
                {
                    unlockSubParents[0].SetActive(true);
                    if (contents[data] == 3)
                        unlockSubParents[2].SetActive(true);
                }
                else
                    unlockSubParents[1].SetActive(true);
                if (contents[data] > 1)
                {
                    string[] numWords = new string[] { "Zero", "one", "Two", "Three" };
                    content.text = numWords[contents[data]] + " {0} tiles have been added to your bag!";
                }
                string token_info = "";
                if (data.color == TokenColor.Adder)
                    token_info = "Watering Can";
                else if (data.color == TokenColor.Gnome)
                    token_info = "Gnome";
                else if (data.color == TokenColor.Spade || data.color == TokenColor.Clipper)
                    token_info = data.color.ToString();
                else
                    token_info = data.color.ToString() + " " + data.num.ToString();
                content.text = string.Format(content.text, new string[1] { token_info });
                for (int i = 0; i < unlockedTokens.Count; i++)
                {
                    unlockedTokens[i].SetTokenData(data);
                    unlockedTokens[i].UpdateLayer("UIToken");
                }
                break;
            case UpgradeType.Upgrade:
                upgradeParent.SetActive(true);
                title.text = "Tile Upgrade";
                GetUpgradeData(contents, out TokenData oldData, out TokenData newData);
                content.text = "A {0} tile has been upgraded from a {1} to a {2}!";
                content.text = string.Format(content.text, new string[3] { oldData.color.ToString(), oldData.num.ToString(), newData.num.ToString() });
                oldToken.SetTokenData(oldData);
                upgradedToken.SetTokenData(newData);
                upgradedToken.UpdateLayer("UIToken");
                oldToken.UpdateLayer("UIToken");
                break;
        }
    }

    public bool Create(Dictionary<TokenData, int> contents)
    {
        string id = GetContentsID(contents);
        bool repeat = false;
        Services.AudioManager.PlayTutorialNotificationSound();
        if (previousUnlocks.Contains(id))
            repeat = true;
        else
            previousUnlocks.Add(id);

        bool hasRemove = false;
        bool hasGain = false;
        foreach (int num in contents.Values)
        {
            if (num < 0) { hasRemove = true; }
            if (num > 0) { hasGain = true; }
        }
        if (hasGain == false) { return false; }
        type = hasRemove ? UpgradeType.Upgrade : UpgradeType.Unlock;
        if (repeat)
        {
            StartCoroutine(TinyActuallyCreate(contents));
            return false;
        }
        StartCoroutine(ActuallyCreate(contents));
        return true;
    }

    public void Close()
    {
        if (isClosing || !visual.activeSelf)
            return;

        Services.AudioManager.PlayButtonPressSound();
        unlockParent.SetActive(false);
        for (int i = 0; i < unlockSubParents.Length; i++)
            unlockSubParents[i].SetActive(false);
        for (int i = 0; i < unlockReasons.Count; i++)
            unlockReasons[i].gameObject.SetActive(false);
        upgradeParent.SetActive(false);

        if (starting != null)
        {
            StopCoroutine(starting);
            starting = null;
        }
        if (ending != null)
            StopCoroutine(ending);
        ending = StartCoroutine(SlideToRight());
    }

    public bool IsAnimatingClose => isClosing;

    public void TabClosed(UpgradeTab tab)
    {
        for (int i = 0; i < tinyTabs.Count; i++)
        {
            if (tinyTabs[i].active == false)
            {
                UpgradeTab _tab = tinyTabs[i];
                tinyTabs.Remove(_tab);
                tinyTabs.Add(_tab);
            }
        }
        for (int i = 0; i < tinyTabs.Count; i++)
            tinyTabs[i].SetHeight(tabPositions[i].y);
    }

    public string GetContentsID(Dictionary<TokenData, int> contents)
    {
        string s = "";
        foreach (TokenData t in contents.Keys)
        {
            s += t.ShortString();
            s += ":";
            s += contents[t].ToString();
        }
        return s;
    }

    void SetVisualSlideX(float x)
    {
        Vector3 p = visual.transform.localPosition;
        visual.transform.localPosition = new Vector3(x, p.y, p.z);
    }

    IEnumerator SlideFromLeft()
    {
        isClosing = false;
        yield return SlideHelper.SlideLocalX(visual.transform, slideOpenX, slideInDuration);
        starting = null;
    }

    IEnumerator SlideToRight()
    {
        isClosing = true;
        yield return SlideHelper.SlideLocalX(visual.transform, slideOutToX, slideOutDuration);
        visual.SetActive(false);
        isClosing = false;
        ending = null;
    }
}
