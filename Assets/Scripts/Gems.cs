using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Gems : MonoBehaviour
{
    public int numGems;
    public int maxGems = 25;
    public TextMeshPro[] seedDisplays;
    public TextMeshPro fakeAd;
    public GameObject fakeAdVisual;
    bool watchingFakeAd;
    float fakeAdDuration;
    private void Awake()
    {
        fakeAdVisual.SetActive(false);
        if (PlayerPrefs.HasKey("gems"))
        {
            numGems = PlayerPrefs.GetInt("gems");
        }
        else
        {
            PlayerPrefs.SetInt("gems", numGems);
            PlayerPrefs.Save();
        }
    }
    private void Update()
    {
        foreach(var display in seedDisplays)
        {
            display.text = numGems.ToString() + "/" + maxGems.ToString();
        }
        if (watchingFakeAd)
        {
            fakeAdDuration -= Time.deltaTime;
            fakeAd.text = (fakeAdDuration).ToString("F0");
            if(fakeAd.text == "0")
            {
                fakeAd.text = "+5 Seeds";
            }
            if(fakeAdDuration <= 0)
            {
                fakeAdVisual.SetActive(false);
                watchingFakeAd = false;
                EarnGems(5);
            }
        }
        else
        {
            fakeAd.text = "";
        }
        
    }
    public bool CanAfford(int num)
    {
        if(numGems >= num)
        {
            return true;
        }
        return false;
    }
    public void SpendGems(int num)
    {
        numGems -= num;
        PlayerPrefs.SetInt("gems", numGems);
        PlayerPrefs.Save();
    }
    public void EarnGems(int num)
    {
        numGems += num;
        numGems %= maxGems;
        PlayerPrefs.SetInt("gems", numGems);
        PlayerPrefs.Save();
    }
    public void WatchAd()
    {
        fakeAdVisual.SetActive(true);
        fakeAdDuration = 10f;
        watchingFakeAd = true;
    }
    /*IEnumerator FakeAd()
    {
        float fakeAdDuration = 5f;
        while(fakeAdDuration > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(0.5f);
        EarnGems(5);
    }*/
}
