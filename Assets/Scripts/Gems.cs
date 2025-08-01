using System;
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
    float nextSeedEarned;
    public float secondsBetweenSeeds;
    public SeedPopup seedPopup;
    public Dictionary<string, int> seedCosts = new Dictionary<string, int>()
    {
        {"earn",4 },
        {"newGame",5 },
        {"snapshot",3 },
        {"mulligan",2 },
        {"buySpade",2 },
        {"buyAdder",3 },
        {"buyClipper",4 }
    };
    private void Awake()
    {
        if (PlayerPrefs.HasKey("gems"))
        {
            numGems = PlayerPrefs.GetInt("gems");
        }
        else
        {
            numGems = 10;
            PlayerPrefs.SetInt("gems", numGems);
            PlayerPrefs.Save();
        }
        if (PlayerPrefs.HasKey("whenLeft"))
        {
            HandleAwayTime(PlayerPrefs.GetFloat("whenLeft"));
        }
        TimeSpan current = ((DateTime.UtcNow - new DateTime(1970, 1, 1)));
        float currentTime = (float)current.TotalSeconds;
        PlayerPrefs.SetFloat("whenLeft", currentTime);
        PlayerPrefs.Save();
        nextSeedEarned = Time.time + secondsBetweenSeeds;
        fakeAdVisual.SetActive(false);
        
    }
    void HandleAwayTime(float timeWhenLeft)
    {
        TimeSpan current = ((DateTime.UtcNow - new DateTime(1970, 1, 1)));
        float currentTime = (float)current.TotalSeconds;
        float span = currentTime - timeWhenLeft;
        span /= secondsBetweenSeeds;
        EarnGems(Mathf.FloorToInt(span));
    }
    private void Update()
    {
        if (PlayerPrefs.HasKey("whenLeft"))
        {
            TimeSpan current = ((DateTime.UtcNow - new DateTime(1970, 1, 1)));
            float currentTime = (float)current.TotalSeconds;
            if(currentTime > PlayerPrefs.GetFloat("whenLeft") + 60)
            {
                SaveCurrentTime();
            }
        }
        else
        {
            SaveCurrentTime();
        }
        if (Time.time > nextSeedEarned)
        {
            EarnGems(1);
            nextSeedEarned = Time.time + secondsBetweenSeeds;
        }
        foreach (var display in seedDisplays)
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
                EarnGems(seedCosts["earn"]);
            }
        }
        else
        {
            fakeAd.text = "";
        }
        
    }
    public void TooExpensive()
    {
        seedPopup.Open();
    }
    public bool CanAfford(string cost)
    {
        int num = seedCosts[cost];
        if(numGems >= num)
        {
            return true;
        }
        return false;
    }
    public void SpendGems(string cost)
    {
        int num = seedCosts[cost];
        numGems -= num;
        numGems = Mathf.Clamp(numGems, 0, maxGems);
        PlayerPrefs.SetInt("gems", numGems);
        PlayerPrefs.Save();
    }
    public void EarnGems(int num)
    {
        numGems += num;
        numGems = Mathf.Clamp(numGems, 0, maxGems);
        PlayerPrefs.SetInt("gems", numGems);
        PlayerPrefs.Save();
    }
    void SaveCurrentTime()
    {
        TimeSpan current = ((DateTime.UtcNow - new DateTime(1970, 1, 1)));
        float currentTime = (float)current.TotalSeconds;
        PlayerPrefs.SetFloat("whenLeft", currentTime);
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
