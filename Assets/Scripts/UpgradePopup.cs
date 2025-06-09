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

    //tiny popup
    public List<UpgradeTab> tinyTabs;
    public List<Vector3> tabPositions;
    //where should it be depending on how many you have
    public List<float> tinyTabX;

    Coroutine starting;
    Coroutine ending;
    // Start is called before the first frame update
    void Start()
    {
        visual.SetActive(false);
        for (int i = 0; i < unlockSubParents.Length; i++)
        {
            unlockSubParents[i].SetActive(false);
        }
        unlockParent.SetActive(false);
        upgradeParent.SetActive(false);
        for(int i = 0; i < unlockReasons.Count; i++)
        {
            unlockReasons[i].gameObject.SetActive(false);
        }

        //tiny
        foreach(UpgradeTab tab in tinyTabs)
        {
            tab.popup = this;
            tab.Deactivate();
            tabPositions.Add(tab.transform.parent.localPosition);
        }
    }
    IEnumerator TinyActuallyCreate(Dictionary<TokenData,int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        UpgradeTab tinyTab = tinyTabs[0];
        if (tinyTabs[0].active == false)
        {
            //no need to worry, just put it in the first slot
        }
        else if (tinyTabs[tinyTabs.Count - 1].active)
        {
            //all tabs are full! push them all back, then put the last one in the first slot
            tinyTab = tinyTabs[tinyTabs.Count - 1];
            tinyTabs.Remove(tinyTab);
            tinyTabs.Insert(0, tinyTab);
        }
        else
        {
            //some slots at end are empty
            for(int i = 1; i < tinyTabs.Count; i++)
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
        for(int i = 0; i < tinyTabs.Count; i++)
        {
            tinyTabs[i].SetHeight(tabPositions[i].y);
        }
        tinyTab.Activate();
        //unlock triggers/reasons
        List<TokenData> triggers = new List<TokenData>();
        foreach (TokenData token in contents.Keys)
        {
            if (contents[token] == 100)
            {
                triggers.Add(token);
            }
        }
        int triggerCount = 0;
        foreach (TokenData token in triggers)
        {
            unlockReasons[triggerCount].gameObject.SetActive(true);
            unlockReasons[triggerCount].SetTile(token);
            contents.Remove(token);
            triggerCount++;
        }
        foreach (MiniTile tile in tinyTab.miniTiles)
        {
            tile.gameObject.SetActive(false);
        }
        int tileCount = 0;
        switch (type)
        {
            case UpgradeType.Upgrade:
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                tileCount++;
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetArrow();
                tileCount++;
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(contents.Keys.ToList()[1]);
                tileCount++;
                break;
            case UpgradeType.Unlock:
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                tileCount++;
                if(contents.Values.ToList()[0] > 1)
                {
                    tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                    tinyTab.miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                    tileCount++;
                }
                if (contents.Values.ToList()[0] > 2)
                {
                    tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                    tinyTab.miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                    tileCount++;
                }
                tinyTab.miniTiles[tileCount].gameObject.SetActive(true);
                tinyTab.miniTiles[tileCount].SetPlus();
                tileCount++;
                break;
        }
        float x = tinyTabX[tileCount - 1];
        float y = tinyTab.transform.localPosition.y;
        tinyTab.NewPosition(new Vector2(x, y));
    }
    IEnumerator ActuallyCreate(Dictionary<TokenData,int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        bool alreadyActive = visual.activeSelf;
        visual.SetActive(true);
        visual.transform.localPosition = new Vector2(-6f, 0f);
        if(alreadyActive == false)
        {
            starting = StartCoroutine(SlideFromLeft());
        }
        else
        {
            if(ending != null)
            {
                StopCoroutine(ending);
                ending = null;
                starting = StartCoroutine(SlideFromLeft());
            }
        }
        
        //unlock triggers/reasons
        List<TokenData> triggers = new List<TokenData>();
        foreach(TokenData token in contents.Keys)
        {
            if (contents[token] == 100)
            {
                triggers.Add(token);
            }
        }
        int triggerCount = 0;
        foreach (TokenData token in triggers)
        {
            unlockReasons[triggerCount].gameObject.SetActive(true);
            unlockReasons[triggerCount].SetTile(token);
            contents.Remove(token);
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
                    {
                        unlockSubParents[2].SetActive(true);
                    }
                }
                else
                {
                    unlockSubParents[1].SetActive(true);
                }
                if (contents[data] > 1)
                {
                    string[] numWords = new string[]{"Zero", "one", "Two", "Three" };
                    content.text = numWords[contents[data]] + " {0} tiles have been added to your bag!";
                }
                string token_info = "";
                if (data.color == TokenColor.Adder)
                {
                    token_info = "Watering Can";
                }
                else if(data.color == TokenColor.Gnome)
                {
                    token_info = "Gnome";
                }
                else if(data.color == TokenColor.Spade || data.color == TokenColor.Clipper)
                {
                    token_info = data.color.ToString();
                }
                else
                {
                    token_info = data.color.ToString() + " " + data.num.ToString();
                }
                content.text = string.Format(content.text, new string[1] { token_info });
                for(int i = 0; i < unlockedTokens.Count; i++)
                {
                    unlockedTokens[i].SetTokenData(data);
                    unlockedTokens[i].UpdateLayer("UIToken");
                }
                
                break;
            case UpgradeType.Upgrade:
                upgradeParent.SetActive(true);
                title.text = "Tile Upgrade";
                TokenData oldData = new TokenData();
                TokenData newData = new TokenData();
                foreach (TokenData _data in contents.Keys)
                {
                    if (contents[_data] < 0) { oldData = _data; }
                    if (contents[_data] > 0) { newData = _data; }
                }
                content.text = "A {0} tile has been upgraded from a {1} to a {2}!";
                content.text = string.Format(content.text, new string[3] { oldData.color.ToString(), oldData.num.ToString(), newData.num.ToString() });
                oldToken.SetTokenData(oldData);
                upgradedToken.SetTokenData(newData);
                upgradedToken.UpdateLayer("UIToken");
                oldToken.UpdateLayer("UIToken");
                break;
        }
    }
    public bool Create(Dictionary<TokenData,int> contents)
    {
        string id = GetContentsID(contents);
        bool repeat = false;
        if (previousUnlocks.Contains(id))
        {
            repeat = true;
        }
        else
        {
            previousUnlocks.Add(id);
        }
       
        bool hasRemove = false;
        bool hasGain = false;
        foreach (int num in contents.Values)
        {
            if (num < 0) { hasRemove = true; }
            if (num > 0) { hasGain = true; }
        }
        if (hasGain == false) { return false; }
        if (hasRemove)
        {
            type = UpgradeType.Upgrade;
        }
        else
        {
            type = UpgradeType.Unlock;
        }
        if (repeat)
        {
            StartCoroutine(TinyActuallyCreate(contents));
            return false;
        }
        else
        {
            StartCoroutine(ActuallyCreate(contents));
            return true;
        }
        
        
    }
    public void Close()
    {
        unlockParent.SetActive(false);
        for(int i = 0; i < unlockSubParents.Length;i++)
        {
            unlockSubParents[i].SetActive(false);
        }
        for (int i = 0; i < unlockReasons.Count; i++)
        {
            unlockReasons[i].gameObject.SetActive(false);
        }
        upgradeParent.SetActive(false);
        if(ending == null)
        {
            if(starting != null)
            {
                StopAllCoroutines();
            }
            ending = StartCoroutine(SlideToRight());
        }
        else
        {
            visual.gameObject.SetActive(false);
            StopCoroutine(ending);
        }
        
    }

    public void TabClosed(UpgradeTab tab)
    {
        //end of list anyway

        for(int i = 0; i < tinyTabs.Count; i++)
        {
            if (tinyTabs[i].active == false)
            {
                UpgradeTab _tab = tinyTabs[i];
                tinyTabs.Remove(_tab);
                tinyTabs.Add(_tab);
            }
        }
        for (int i = 0; i < tinyTabs.Count; i++)
        {
            tinyTabs[i].SetHeight(tabPositions[i].y);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public string GetContentsID(Dictionary<TokenData,int> contents)
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
    IEnumerator SlideFromLeft()
    {
        while (Mathf.Abs(0 - visual.transform.localPosition.x) > 0.1f)
        {
            visual.transform.localPosition += (Vector3.zero - visual.transform.localPosition) * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        visual.transform.localPosition = Vector3.zero;
        starting = null;
    }
    IEnumerator SlideToRight()
    {
        while (Mathf.Abs(6f - visual.transform.localPosition.x) > 0.1f)
        {
            visual.transform.localPosition += (new Vector3(6f,0f) - visual.transform.localPosition) * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        visual.gameObject.SetActive(false);
        ending = null;
    }
}
