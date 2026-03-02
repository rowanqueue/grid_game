using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Advertisements;

public class Gems : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public int numGems;
    public int maxGems = 25;
    public TextMeshPro[] seedDisplays;
    public TextMeshPro fakeAd;
    public GameObject fakeAdVisual;
    bool watchingFakeAd;
    float fakeAdDuration;
    float nextSeedEarned;
    bool rewardGrantedFromAd; // Prevents double-grant when real ad completes
    public float secondsBetweenSeeds;
    public SeedPopup seedPopup;
    public Dictionary<string, int> seedCosts = new Dictionary<string, int>()
    {
        {"earn",4 },
        {"newGame",5 },
        {"snapshot",5 },
        {"takeSnapshot", 2},
        {"mulligan",2 },
        {"buySpade",2 },
        {"buyAdder",3 },
        {"buyClipper",4 }
    };
    [SerializeField] string _androidGameId = ""; // Set in Inspector from Unity Ads dashboard
    [SerializeField] string _iOSGameId = "";   // Set in Inspector from Unity Ads dashboard
    [SerializeField] string _androidAdUnitId = "Rewarded_Android"; // Replace with real ad unit ID
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";       // Replace with real ad unit ID
    [SerializeField] bool _testMode = true; // Set false for production
    string _adUnitId = null;
    string _gameId = null;

    private void Awake()
    {
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
        _gameId = _iOSGameId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
        _gameId = _androidGameId;
#endif
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

        InitializeAds();
    }

    void InitializeAds()
    {
        if (string.IsNullOrEmpty(_gameId) || !Advertisement.isSupported) return;
        if (!Advertisement.isInitialized)
            Advertisement.Initialize(_gameId, _testMode, this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialized.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity Ads init failed: {error} - {message}");
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
            if (fakeAd.text == "0")
                fakeAd.text = "+" + seedCosts["earn"] + " Seeds";
            if (fakeAdDuration <= 0)
            {
                fakeAdVisual.SetActive(false);
                watchingFakeAd = false;
                if (!rewardGrantedFromAd)
                {
                    rewardGrantedFromAd = true;
                    EarnGems(seedCosts["earn"]);
                }
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
    /// <summary>
    /// Returns the gem cost for the given key, or 0 if the key is missing.
    /// </summary>
    public int GetCost(string key)
    {
        return (seedCosts != null && seedCosts.TryGetValue(key, out int value)) ? value : 0;
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
        rewardGrantedFromAd = false;
        if (Advertisement.isInitialized && !string.IsNullOrEmpty(_adUnitId))
        {
            LoadAd();
        }
        else
        {
            // Fallback when Ads not initialized (e.g. missing game ID): show fake overlay
            StartFakeAdFallback();
        }
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
    // Call this public method when you want to get an ad ready to show.
    public void LoadAd()
    {
        // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);
        if (adUnitId.Equals(_adUnitId))
            ShowAd();
    }

    // Implement a method to execute when the user clicks the button:
    public void ShowAd()
    {
        // Disable the button:
        //_showAdButton.interactable = false;
        // Then show the ad:
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED && !rewardGrantedFromAd)
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            rewardGrantedFromAd = true;
            EarnGems(seedCosts["earn"]);
        }
    }

    // Implement Load and Show Listener error callbacks:
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit {adUnitId}: {error} - {message}");
        StartFakeAdFallback();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error} - {message}");
        StartFakeAdFallback();
    }

    void StartFakeAdFallback()
    {
        fakeAdVisual.SetActive(true);
        fakeAdDuration = 10f;
        watchingFakeAd = true;
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
        // Real ad is showing; hide fake overlay if it was shown as loading indicator
        if (fakeAdVisual.activeSelf && !watchingFakeAd)
            fakeAdVisual.SetActive(false);
    }
    public void OnUnityAdsShowClick(string adUnitId) { }
}
