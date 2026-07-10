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
    float nextSeedEarned;
    bool rewardGrantedFromAd;
    public float secondsBetweenSeeds;
    public SeedPopup seedPopup;
    public Dictionary<string, int> seedCosts = new Dictionary<string, int>()
    {
        {"earn",4 },
        {"newGame",5 },
        {"takeSnapshot", 0},
        {"buySpade",2 },
        {"buyAdder",3 },
        {"buyClipper",4 }
    };
    [SerializeField] string _androidGameId = "";
    [SerializeField] string _iOSGameId = "";
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    [SerializeField] bool _testMode = false;
    string _adUnitId = null;
    string _gameId = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool watchingFakeAd;
    float fakeAdDuration;
#endif

    const string AdUnavailableMessage = "Ad unavailable.\nTry again later.";

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
        if (fakeAdVisual != null)
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
        GameLog.Log("Unity Ads initialized.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        GameLog.LogWarning($"Unity Ads init failed: {error} - {message}");
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (watchingFakeAd && fakeAd != null)
        {
            fakeAdDuration -= Time.deltaTime;
            fakeAd.text = fakeAdDuration.ToString("F0");
            if (fakeAd.text == "0")
                fakeAd.text = "+" + seedCosts["earn"] + " Seeds";
            if (fakeAdDuration <= 0)
            {
                if (fakeAdVisual != null)
                    fakeAdVisual.SetActive(false);
                watchingFakeAd = false;
                if (!rewardGrantedFromAd)
                {
                    rewardGrantedFromAd = true;
                    EarnGems(seedCosts["earn"]);
                }
            }
        }
        else if (fakeAd != null)
        {
            fakeAd.text = "";
        }
#endif
    }

    public void TooExpensive()
    {
        seedPopup.Open();
    }

    public int GetCost(string key)
    {
        return (seedCosts != null && seedCosts.TryGetValue(key, out int value)) ? value : 0;
    }

    public bool CanAfford(string cost)
    {
        int num = seedCosts[cost];
        return numGems >= num;
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
            HandleAdUnavailable();
        }
    }

    public void LoadAd()
    {
        GameLog.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd()
    {
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        GameLog.Log("Ad Loaded: " + adUnitId);
        if (adUnitId.Equals(_adUnitId))
            ShowAd();
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED && !rewardGrantedFromAd)
        {
            GameLog.Log("Unity Ads Rewarded Ad Completed");
            rewardGrantedFromAd = true;
            EarnGems(seedCosts["earn"]);
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        GameLog.LogWarning($"Error loading Ad Unit {adUnitId}: {error} - {message}");
        HandleAdUnavailable();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        GameLog.LogWarning($"Error showing Ad Unit {adUnitId}: {error} - {message}");
        HandleAdUnavailable();
    }

    void HandleAdUnavailable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        StartFakeAdFallback();
#else
        if (seedPopup != null)
            seedPopup.Open(AdUnavailableMessage);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void StartFakeAdFallback()
    {
        if (fakeAdVisual == null)
        {
            if (seedPopup != null)
                seedPopup.Open(AdUnavailableMessage);
            return;
        }
        fakeAdVisual.SetActive(true);
        fakeAdDuration = 10f;
        watchingFakeAd = true;
    }
#endif

    public void OnUnityAdsShowStart(string adUnitId)
    {
        if (fakeAdVisual != null && fakeAdVisual.activeSelf)
            fakeAdVisual.SetActive(false);
    }

    public void OnUnityAdsShowClick(string adUnitId) { }
}
