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
    //public List<MiniTile> unlockReasons;


    [SerializeField] UpgradeType type;
    public List<string> previousUnlocks;

    //tiny popup
    public GameObject tinyTab;
    public List<MiniTile> miniTiles;
    //where should it be depending on how many you have
    public List<float> tinyTabX;
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

        //tiny
        tinyTab.SetActive(false);
    }
    IEnumerator TinyActuallyCreate(Dictionary<TokenData,int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        tinyTab.SetActive(true);
        foreach(MiniTile tile in miniTiles)
        {
            tile.gameObject.SetActive(false);
        }
        int tileCount = 0;
        switch (type)
        {
            case UpgradeType.Upgrade:
                miniTiles[tileCount].gameObject.SetActive(true);
                miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                tileCount++;
                miniTiles[tileCount].gameObject.SetActive(true);
                miniTiles[tileCount].SetArrow();
                tileCount++;
                miniTiles[tileCount].gameObject.SetActive(true);
                miniTiles[tileCount].SetTile(contents.Keys.ToList()[1]);
                tileCount++;
                break;
            case UpgradeType.Unlock:
                miniTiles[tileCount].gameObject.SetActive(true);
                miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                tileCount++;
                if(contents.Values.ToList()[0] > 1)
                {
                    miniTiles[tileCount].gameObject.SetActive(true);
                    miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                    tileCount++;
                }
                if (contents.Values.ToList()[0] > 2)
                {
                    miniTiles[tileCount].gameObject.SetActive(true);
                    miniTiles[tileCount].SetTile(contents.Keys.ToList()[0]);
                    tileCount++;
                }
                miniTiles[tileCount].gameObject.SetActive(true);
                miniTiles[tileCount].SetPlus();
                tileCount++;
                break;
        }
        tinyTab.transform.localPosition = new Vector2(tinyTabX[tileCount-1], tinyTab.transform.localPosition.y);
    }
    IEnumerator ActuallyCreate(Dictionary<TokenData,int> contents)
    {
        yield return new WaitForSeconds(0.0f);
        visual.SetActive(true);
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
        upgradeParent.SetActive(false);
        visual.SetActive(false);
        tinyTab.SetActive(false);
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
}
