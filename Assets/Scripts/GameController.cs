using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Logic;
using Save;
using EZ.Haptics;
using System;
using flora;

public enum GameType
{
    Triple,
    Bubble
}
public enum InputState
{
    Choose,//waiting to pick up a token
    Place,//waiting to place a token
    Wait,// waiting for game to update
    Popup,
    Snapshot, //waiting to dismiss snapshot
    Finish,
    TapToRestart
}
public enum GameState
{
    Gameplay,
    Settings,
    Start,
    ToolShop,
    Seeds,
    Credits,
    HighScore,
    Help,
    Bag,
    SelectDifficulty,
    Snapshot,

}
public class GameController : MonoBehaviour
{
    public TextAsset gameJson;
    public int difficulty = 0;
    public List<TextAsset> difficulties = new List<TextAsset>();
    public List<string> difficultyNames = new List<string>();
    public List<bool> difficultyUnlocked = new List<bool>();
    public List<int> scoreNeededToUnlock = new List<int>();
    public TextMeshPro difficultyName;
    public List<Button> difficultyButtons = new List<Button>();
    public GameObject difficultyParent;
    public GameType whichGame;
    public GameState gameState = GameState.Gameplay;
    GameState lastState;
    public bool runTutorial;
    //once tutorial is done, it should be created from a prefab
    public Tutorial tutorial;
    public bool inTutorial => tutorial.active;
    public bool IsTutorialSession => runTutorial || pendingTutorialStart || inTutorial;
    bool pendingTutorialStart;
    bool introTutorialInputEnabled;
    public bool CanProcessIntroTutorialInput => introTutorialInputEnabled;
    public List<flora.Screen> stateScreens = new List<flora.Screen>();
    bool movingToScreen = false;
    public Logic.Game game;
    //public TextInput.SimInput simInput;
    public TextMeshPro display;
    public TextMeshPro bagDisplay;

    public GameObject tokenPrefab;
    public GameObject tilePrefab;
    public GameObject miniTilePrefab;
    //bad
    public Vector2 firstHandPos;
    public Vector2 handSeparation;

    public Vector2 firstGridPos;
    public Vector2 gridSeparation;

    public Vector2 freeSlotPos;
    Vector2Int freeSlotChoice = new Vector2Int(-20, -20);

    public BagDisplay deckDisplay;
    public TextMeshPro deckNumberDisplay;
    public int tempDeckNumberForAnim;
    public TextMeshPro debugBagDisplay;

    //gameplay
    public InputState inputState = InputState.Choose;
    public List<Token> hand = new List<Token>();
    public bool draggingTile = false;
    public bool holdingClick = false;
    public float clickHoldDuration;
    public int chosenIndex = -1;
    public Token chosenToken => (chosenIndex < game.hand.handSize ? hand[chosenIndex] : freeSlot.token);
    public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
    public Tile freeSlot;
    public GameObject freeSlotVisual;
    public Vector2Int chosenPos = Vector2Int.left;
    public float waitTime = 0.1f;
    [SerializeField] float normalWaitTime = 0.25f;
    [SerializeField] float fastWaitTime = 0.1f;
    [SerializeField] float tokenWaitMultiplier = 1.5f;
    [SerializeField] float winHoldDuration = 1.2f;
    [Header("Finish Sequence")]
    [SerializeField] float finishBaseDelay = 0.04f;
    [SerializeField] float finishDelayPerScore = 0.003f;
    [SerializeField] int finishLastTileCount = 3;
    [SerializeField] float finishLastTileSlowdown = 1.6f;
    [SerializeField] int finishFlowerBase = 3;
    [SerializeField] int finishFlowerPerNum = 2;
    [SerializeField] float finishScoreHoldBase = 0.06f;
    public float finishLiftSpeedMultiplier = 2f;
    [SerializeField] float cameraPanDuration = 0.45f;
    [SerializeField] float mulliganStaggerSeconds = 0.1f;
    [SerializeField] float mulliganPauseBeforeDraw = 0.5f;
    [SerializeField] float placementSnapRadiusScale = 0.5f;
    [SerializeField] float tapPlacementSnapRadiusScale = 0.72f;
    [SerializeField] bool snapDragVisualToTile = true;
    public float waiting = 0f;
    bool mergeUpgradeBlocking;
    public Token lastTokenPlaced;
    public bool holdingClipper = false;
    public bool holdingSpade = false;
    public bool holdingAdder = false;

    //data
    public int score = 0;
    public int scoreDelta = 0;

    public Logic.History.Turn currentSave;

    public Logic.History.Turn snapshotSave;

    public UpgradePopup upgradePopup;
    public bool popupopen = false;

    public List<GameObject> flowerPrefabs;
    public Dictionary<Tile, List<Flower>> flowers;
    public int numFlowers;

    public Transform gridTransform;
    public List<Transform> handTransforms = new List<Transform>();

    public SnapshotScreen snapshotPreview;
    public ToolShopScreen toolShopScreen;

    public int finishCount = 0;
    public GameObject winScreen;
    public TMP_Text winScreenScore;
    WinScreenDisplay winScreenDisplay;
    int scoreBeforeFinish;
    bool finishRoutineRunning;
    bool pendingFinishStart;
    public bool winScreenAccelerateRequested;
    public bool winScreenShowing;
    public float winScreenAnimSpeed = 1f;
    bool dismissingPopup;
    Vector3 cameraPanStart;
    float cameraPanElapsed = -1f;

    public bool diceMode = false;
    public bool useHaptics = false;
    public bool fastMode = false;

    public bool SkipDeathTilt => fastMode;
    public float PlacementSettleSeconds => fastMode ? 0.05f : 0.12f;
    public float LowerLiftDelay => fastMode ? 0.04f : 0.1f;
    public float UpgradeStartDelay => fastMode ? 0.04f : 0.1f;
    public float UpgradePresentationHold => fastMode ? 0.2f : 0.4f;
    public float DeathAnimationSpeed => fastMode ? 2f : 1f;

    public Transform bagButtonTransform;
    public bool newGame = false;
    public List<Token> dyingTokens = new List<Token>();

    public GameObject loadSnapshotButton;
    [SerializeField] GameObject debugNearEndGameButton;
    public List<Tile> tokensToDestroy = new List<Tile>();
    public PolaroidDisplay polaroidDisplay;

    // Start is called before the first frame update
    private List<Vector2Int> undidTiles = new List<Vector2Int>();
    private bool undidSlot = false;
    bool ignoreBagCloseInputThisFrame = false;
    void Awake()
    {
        InitializeServices();
#if UNITY_ANDROID
        UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        if (PlayerPrefs.HasKey("difficulty"))
        {
            if (PlayerPrefs.GetInt("difficulty") > 3)
            {
                PlayerPrefs.DeleteKey("difficulty");
                PlayerPrefs.DeleteKey("difficultyUnlock");
            }
        }
        if (PlayerPrefs.HasKey("difficultyUnlock"))
        {
            String unlock = PlayerPrefs.GetString("difficultyUnlock");
            for (int i = 0; i < unlock.Length; i++)
            {
                if (unlock[i] == '1')
                {
                    difficultyUnlocked[i] = true;
                }
            }
        }
        else
        {
            PlayerPrefs.SetString("difficultyUnlock", "1000");
            PlayerPrefs.Save();
        }
        if (PlayerPrefs.HasKey("musicVolume") == false)
        {
            PlayerPrefs.SetFloat("musicVolume", 1.0f);
            PlayerPrefs.Save();
        }
        if (PlayerPrefs.HasKey("soundVolume") == false)
        {
            PlayerPrefs.SetFloat("soundVolume", 1.0f);
            PlayerPrefs.Save();
        }
        if (PlayerPrefs.HasKey("diceMode") == false)
        {
            PlayerPrefs.SetInt("diceMode", diceMode ? 1 : 0);
            PlayerPrefs.Save();
        }
        else
        {
            diceMode = PlayerPrefs.GetInt("diceMode") == 1;
        }
        if (PlayerPrefs.HasKey("useHaptics") == false)
        {
            PlayerPrefs.SetInt("useHaptics", useHaptics ? 1 : 0);
            PlayerPrefs.Save();
        }
        else
        {
            useHaptics = PlayerPrefs.GetInt("useHaptics") == 1;
        }
        if (PlayerPrefs.HasKey("fastMode") == false)
        {
            PlayerPrefs.SetInt("fastMode", fastMode ? 1 : 0);
            PlayerPrefs.Save();
        }
        else
        {
            fastMode = PlayerPrefs.GetInt("fastMode") == 1;
        }
        ApplyAnimationSpeedSettings();
        Application.targetFrameRate = 60;
        winScreen.SetActive(false);
        EnsureWinScreenDisplay();
        switch (whichGame)
        {
            case GameType.Triple:
                game = new Logic.TripleGame();
                break;
            case GameType.Bubble:
                game = new Logic.BubbleGame();
                break;
        }
        if (PlayerPrefs.HasKey("difficulty") == false)
        {
            PlayerPrefs.SetInt("difficulty", difficulty);
            PlayerPrefs.Save();
        }
        else
        {
            difficulty = PlayerPrefs.GetInt("difficulty");
        }
        gameJson = difficulties[difficulty];
        Json.Root root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
        game.Initialize(root);

        foreach (flora.Screen screen in stateScreens)
        {
            screen.gameObject.SetActive(false);
        }
        switch (gameState)
        {
            case GameState.Start:
                GameStateStart();
                break;
            case GameState.Gameplay:
                GameStateGameplay();
                break;
            case GameState.Settings:
                GameStateSettings();
                break;
        }
        Vector3 cameraPos = new Vector3(0, 0, Camera.main.transform.position.z);
        switch (gameState)
        {
            case GameState.Gameplay:
                break;
            case GameState.Seeds:
            case GameState.Settings:
                cameraPos.x = 8;
                break;
            case GameState.Start:
                cameraPos.y = -15.33f;
                break;
        }
        /*
#if UNITY_EDITOR
        if (runTutorial)
        {
            PlayerPrefs.DeleteKey("tutorialComplete");
        }
#endif*/
        if (PlayerPrefs.HasKey("tutorialComplete"))
        {
            runTutorial = false;
        }
        //runTutorial = true;
        if (runTutorial)
        {
            difficulty = 0;
            gameJson = difficulties[difficulty];
            root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
            game.Initialize(root);
            game.StartTutorial();
            tutorialHandsDrawn = 1;
            game.skipAutoHandFillOnEmpty = true;
            pendingTutorialStart = true;
        }
        else
        {
            tutorial.ExitTutorial();
            game.skipAutoHandFillOnEmpty = false;
        }
        Camera.main.transform.position = cameraPos;

        //simInput = new TextInput.SimInput(game);
        flowers = new Dictionary<Tile, List<Flower>>();
        CreateGrid();
        hand = new List<Token>();
        newGame = false;
        if (IsTutorialSession)
        {
            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
            newGame = true;
        }

        deckDisplay.game = game;

        if (!IsTutorialSession)
        {
            if (SaveLoad.HasSave(0))
            {
                currentSave = SaveLoad.Load(0);
                if (currentSave != null)
                {
                    game.LoadTurn(currentSave);
                    score = game.score;
                    CreateHand();
                    ClearTokensFromGrid();
                    LoadTokensIntoGrid();
                }
                else
                {
                    newGame = true;
                }

            }
            else
            {
                newGame = true;
            }
        }
        if (!IsTutorialSession)
        {
            if (newGame == false)
            {
                CreateHand();
            }
            else
            {
                hand.Add(null);
                hand.Add(null);
                hand.Add(null);
                hand.Add(null);
            }
        }
        CameraViewportHandler.OnResolutionChanged += RefreshAnchorsOnResolutionChange;
        StartCoroutine(RefreshAllScreenAnchorsAfterStartup());
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (debugNearEndGameButton != null)
        {
            debugNearEndGameButton.SetActive(false);
        }
#endif
    }

    void Start()
    {
        if (pendingTutorialStart && gameState == GameState.Gameplay)
        {
            BeginTutorialSession();
        }
    }

    public void BeginTutorialSession()
    {
        if (!pendingTutorialStart)
        {
            return;
        }
        pendingTutorialStart = false;
        introTutorialInputEnabled = false;
        tutorial.StartTutorial();
        SyncTutorialHandFillMode();
        EnterInputState(InputState.Choose);
        StartCoroutine(EnableIntroTutorialInputNextFrame());
    }

    IEnumerator EnableIntroTutorialInputNextFrame()
    {
        yield return null;
        introTutorialInputEnabled = true;
    }

    IEnumerator TutorialCombineStageAdvanceRoutine(Token visualToken, Logic.Token newToken, Logic.Token toolToken, bool useHaptics, int consumedTileCount)
    {
        mergeUpgradeBlocking = true;
        yield return MergeUpgradeRoutine(visualToken, newToken, toolToken, useHaptics, consumedTileCount);
        mergeUpgradeBlocking = false;

        if (inTutorial && tutorial.stage == TutorialStage.Blue3 && tutorial.Blue3FormViewHold > 0f)
        {
            yield return new WaitForSeconds(tutorial.Blue3FormViewHold);
        }

        if (inTutorial && (tutorial.stage == TutorialStage.Placing
            || tutorial.stage == TutorialStage.Blue3
            || tutorial.stage == TutorialStage.Green2))
        {
            tutorial.RequestIncrementStage(IncrementTrigger.AfterUpgrade);
        }
    }

    bool AreSnapshotdMergeDeathsComplete(List<Token> consumedDeaths)
    {
        if (consumedDeaths == null || consumedDeaths.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < consumedDeaths.Count; i++)
        {
            Token t = consumedDeaths[i];
            if (t != null && !t.mergeDeathVisualComplete)
            {
                return false;
            }
        }

        return true;
    }

    List<Token> SnapshotRecentMergeDeaths(int count)
    {
        if (count <= 0 || dyingTokens.Count < count)
        {
            return null;
        }

        return dyingTokens.GetRange(dyingTokens.Count - count, count);
    }

    IEnumerator MergeUpgradeRoutine(
        Token visualToken,
        Logic.Token newToken,
        Logic.Token toolToken,
        bool useHaptics,
        int consumedTileCount,
        System.Action onPresentationComplete = null)
    {
        List<Token> consumedDeaths = null;
        if (consumedTileCount > 0)
        {
            while (dyingTokens.Count < consumedTileCount)
            {
                yield return null;
            }

            consumedDeaths = SnapshotRecentMergeDeaths(consumedTileCount);
        }

        if (visualToken != null)
        {
            yield return WaitForPlacementCompleteWithTimeout(visualToken, 3f);
        }

        if (consumedDeaths != null)
        {
            float deathWaitElapsed = 0f;
            const float deathWaitTimeout = 3f;
            while (!AreSnapshotdMergeDeathsComplete(consumedDeaths) && deathWaitElapsed < deathWaitTimeout)
            {
                deathWaitElapsed += Time.deltaTime;
                yield return null;
            }
        }

        bool upgradePresentationDone = false;
        if (visualToken != null)
        {
            visualToken.UpgradeToken(newToken, toolToken, useHaptics, () =>
            {
                upgradePresentationDone = true;
                onPresentationComplete?.Invoke();
            });
        }
        else
        {
            upgradePresentationDone = true;
            onPresentationComplete?.Invoke();
        }

        while (!upgradePresentationDone)
        {
            yield return null;
        }
    }

    IEnumerator RunBlockingMergeUpgrade(
        Token visualToken,
        Logic.Token newToken,
        Logic.Token toolToken,
        bool useHaptics,
        int consumedTileCount)
    {
        mergeUpgradeBlocking = true;
        yield return MergeUpgradeRoutine(visualToken, newToken, toolToken, useHaptics, consumedTileCount);
        mergeUpgradeBlocking = false;
    }

    void LateUpdate()
    {
        if (inTutorial && introTutorialInputEnabled && gameState == GameState.Gameplay)
        {
            tutorial.TryHandleIntroAdvanceInput();
        }
    }

    void OnDestroy()
    {
        CameraViewportHandler.OnResolutionChanged -= RefreshAnchorsOnResolutionChange;
    }

    void RefreshAnchorsOnResolutionChange()
    {
        RefreshAllScreenAnchors();
    }

    void RefreshAllScreenAnchors()
    {
        if (stateScreens == null)
            return;
        foreach (flora.Screen screen in stateScreens)
        {
            if (screen != null)
                screen.SetAnchor();
        }
    }

    IEnumerator RefreshAllScreenAnchorsAfterStartup()
    {
        int waitFrames = 0;
        while (CameraViewportHandler.Instance == null && waitFrames < 120)
        {
            waitFrames++;
            yield return null;
        }
        yield return null;
        RefreshAllScreenAnchors();
    }

    void InitializeServices()
    {
        Services.GameController = this;
        Services.Visuals = GameObject.FindObjectOfType<Visuals>();

        Services.AudioManager = GameObject.FindObjectOfType<AudioManager>();
        Services.AudioManager?.Initialize();

        Services.Gems = GameObject.FindObjectOfType<Gems>();

    }
    void CreateGrid()
    {
        foreach (Logic.Tile _tile in game.grid.tiles.Values)
        {
            Tile tile = GameObject.Instantiate(tilePrefab, gridTransform).GetComponent<Tile>();
            tile.tile = _tile;
            tiles.Add(tile.tile.pos, tile);
            tile.transform.position = firstGridPos + (_tile.pos * gridSeparation);

        }
        //free slot
        freeSlot = GameObject.Instantiate(tilePrefab, gridTransform).GetComponent<Tile>();
        freeSlot.freeSlot = true;
        freeSlot.transform.parent = freeSlotVisual.transform;
        LoadTokensIntoGrid();


    }
    public void StartNewRun()
    {
        if (Services.Gems.CanAfford("newGame"))
        {
            Services.Gems.SpendGems("newGame");
            GameStateGameplay();
        }
        else
        {
            Services.Gems.TooExpensive();
            GameLog.Log("can't afford a new game");
        }

    }

    /// <summary>
    /// Sets game state to gameplay, unless coming from settings or seeds, in which case it goes to select difficulty
    /// </summary>
    public void GameStateGameplay()
    {
        bool beginTutorialAfterTransition = pendingTutorialStart;
        if (gameState == GameState.Settings)
        {
            if (lastState == GameState.SelectDifficulty)
            {
                GameStateSelectDifficulty();
                return;
            }
        }
        if (gameState == GameState.Seeds)
        {
            if (lastState == GameState.SelectDifficulty)
            {
                GameStateSelectDifficulty();
                return;
            }
        }
        if (gameState == GameState.Start)
        {
            bagDisplay.gameObject.SetActive(true);

            if (newGame && !IsTutorialSession)
            {
                //if new game
                GameStateSelectDifficulty();
                return;
            }

        }
        if (gameState == GameState.SelectDifficulty)
        {
            if (Services.Gems.CanAfford("newGame"))
            {
                Services.Gems.SpendGems("newGame");
            }
            else
            {
                Services.Gems.TooExpensive();
                return;
            }
        }
        difficultyParent.SetActive(false);
        if (gameState == GameState.SelectDifficulty && !IsTutorialSession)
        {
            gameJson = difficulties[difficulty];
            Json.Root root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
            game.Initialize(root);
            CreateHand(true);
            Save();
        }
        /*if (inTutorial && tutorial.stage == TutorialStage.GreenNextBag)
        {
            tutorial.IncrementStage();
        }*/
        if (gameState == GameState.Gameplay) { return; }
        if (gameState == GameState.ToolShop)
        {
            var shop = toolShopScreen ?? stateScreens[(int)GameState.ToolShop].GetComponentInChildren<ToolShopScreen>();
            shop?.CloseScreen();
        }
        lastState = gameState;
        gameState = GameState.Gameplay;
        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
        if (beginTutorialAfterTransition)
        {
            BeginTutorialSession();
        }
    }
    public void GameStateSelectDifficulty()
    {
        lastState = gameState;
        gameState = GameState.SelectDifficulty;
        difficultyParent.SetActive(true);
        stateScreens[(int)GameState.Gameplay].gameObject.SetActive(true);
        stateScreens[(int)GameState.Gameplay].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateStart()
    {
        lastState = gameState;
        gameState = GameState.Start;
        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateSettings()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Settings;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateSnapshot()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Snapshot;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        snapshotPreview.openScreen();
        movingToScreen = true;
    }
    public void GameStateToolShop()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        if(gameState == GameState.ToolShop)
        {
            GameStateGameplay();
            return;
        }
        lastState = gameState;
        gameState = GameState.ToolShop;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        var shop = toolShopScreen ?? stateScreens[(int)gameState].GetComponentInChildren<ToolShopScreen>();
        shop?.OpenScreen();
        movingToScreen = true;
    }
    public void DeactivateToolShopScreen()
    {
        stateScreens[(int)GameState.ToolShop].gameObject.SetActive(false);
    }
    public void GameStateSeeds()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Seeds;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        //snapshotPreview.openScreen();
        movingToScreen = true;
    }
    public void GameStateBag()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        if (gameState != GameState.Gameplay) { return; }
        lastState = gameState;
        gameState = GameState.Bag;
        //stateScreens[(int)gameState].gameObject.SetActive(true);
        //stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
        ignoreBagCloseInputThisFrame = true;
    }
    public void GameStateCredits()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Credits;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateHighScore()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.HighScore;

        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.RefreshUI();
        }

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateHelp()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Help;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        //snapshotPreview.openScreen();
        movingToScreen = true;
    }
    public void ToggleDiceMode()
    {
        diceMode = !diceMode;
        PlayerPrefs.SetInt("diceMode", diceMode ? 1 : 0);
        PlayerPrefs.Save();
        foreach (Token t in hand)
        {
            if (t == null) { continue; }
            t.SetTokenData(t.token.data);
        }
        foreach (Tile t in tiles.Values)
        {
            if (t.token != null)
            {
                t.token.SetTokenData(t.token.token.data);
            }
        }
    }

    public void ToggleHaptics()
    {
        useHaptics = !useHaptics;
        PlayerPrefs.SetInt("useHaptics", useHaptics ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleFastMode()
    {
        fastMode = !fastMode;
        PlayerPrefs.SetInt("fastMode", fastMode ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAnimationSpeedSettings();
    }

    void ApplyAnimationSpeedSettings()
    {
        waitTime = fastMode ? fastWaitTime : normalWaitTime;
    }

    void ClearTokensFromGrid(bool undo = false)
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile.token)
            {
                if (undo)
                {
                    // animate away any tiles that no longer exist or differ from what should be there
                    if (tile.tile.token == null || tile.tile.token.data != tile.token.token.data)
                    {
                        tile.token.UndoDestroy();
                        tile.token = null;
                        undidTiles.Add(tile.tile.pos);
                    }
                    // otherwise, just instantly destroy the tokens like normal
                    else
                    {
                        GameObject.Destroy(tile.token.gameObject);
                        tile.token = null;
                    }
                }
                else
                {
                    GameObject.Destroy(tile.token.gameObject);
                    tile.token = null;
                }
            }
            else
            {
                // add empty tiles to undid tiles 
                if (undo)
                {
                    undidTiles.Add(tile.tile.pos);
                }
            }
        }

        if (freeSlot.token)
        {
            if (undo)
            {
                if (game.freeSlot == null || freeSlot.token.token.data != game.freeSlot.data)
                {
                    freeSlot.token.UndoDestroy();
                    freeSlot.token = null;
                    undidSlot = true;
                }
                else
                {
                    // otherwise, just instantly destroy the tokens like normal
                    GameObject.Destroy(freeSlot.token.gameObject);
                    freeSlot.token = null;
                }
            }
            else
            {
                GameObject.Destroy(freeSlot.token.gameObject);
                freeSlot.token = null;
            }
        }
        else
        {
            if (undo)
            {
                undidSlot = true;
            }
        }
    }
    public void ToggleBagDisplay()
    {
        deckDisplay.gameObject.SetActive(true);

        /*if (inTutorial && tutorial.stage == TutorialStage.BagIntro)
        {
            tutorial.IncrementStage();
        }*/

        if (movingToScreen) { return; }
        if (gameState == GameState.Bag)
        {
            deckDisplay.ClearBagAfterDelay();
            Services.AudioManager.PlayBagSound();
            GameStateGameplay();
        }
        else
        {
            /*if (inTutorial && tutorial.stage == TutorialStage.GreenStart)
            {
                tutorial.IncrementStage();
            }*/
            deckDisplay.MakeBag();
            GameStateBag();
        }

        //deckDisplay.gameObject.SetActive(!deckDisplay.gameObject.activeSelf);
        Services.AudioManager.PlayBagSound();
    }
    void LoadTokensIntoGrid(bool undo = false)
    {
        foreach (Vector2Int p in tiles.Keys)
        {
            Tile tile = tiles[p];
            Logic.Tile _tile = game.grid.tiles[p];
            tile.tile = _tile;
            if (tile.tile.token != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                bool animate = undo && undidTiles.Contains(p);
                token.transform.position = tile.transform.position;
                token.UpdateLayer("TokenPlaced");
                if (animate)
                {
                    token.UndoInit(tile.tile.token);
                }
                else
                {
                    token.Init(tile.tile.token);
                }
                tile.token = token;
            }
        }
        if (game.freeSlot != null)
        {
            Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
            bool animateSlot = undo && undidSlot;
            token.transform.position = freeSlot.transform.position;
            token.UpdateLayer("TokenPlaced");
            if (animateSlot)
            {
                token.UndoInit(game.freeSlot);
            }
            else
            {
                token.Init(game.freeSlot);
            }
            freeSlot.token = token;
        }
    }

    int tutorialHandsDrawn = 0;
    Coroutine tutorialHandRefreshCoroutine;

    public int CurrentTutorialScriptIndex => tutorialHandsDrawn - 1;

    public void ApplyTutorialHandLogic(int scriptIndex)
    {
        switch (scriptIndex)
        {
            case 0:
                game.StartTutorial();
                break;
            case 1:
                game.SecondTutorialHand();
                break;
            case 2:
                game.ThirdTutorialHand();
                break;
            case 3:
                game.FourthTutorialHand();
                break;
            case 4:
                game.FifthTutorialHand();
                break;
            default:
                GameLog.LogError($"Unknown tutorial hand script index: {scriptIndex}");
                return;
        }
    }

    public void DrawNextTutorialScriptHand(bool animateFromBag)
    {
        int scriptIndex = tutorialHandsDrawn;
        if (scriptIndex >= 5)
        {
            CreateHand(true);
            return;
        }
#if UNITY_EDITOR
        GameLog.Log($"Tutorial hand draw: scriptIndex={scriptIndex}, handsDrawn={tutorialHandsDrawn}, stage={tutorial.stage}");
#endif
        ApplyTutorialHandLogic(scriptIndex);
        tutorialHandsDrawn++;
        if (tutorialHandRefreshCoroutine != null)
        {
            StopCoroutine(tutorialHandRefreshCoroutine);
            tutorialHandRefreshCoroutine = null;
        }
        RefreshTutorialHandViews(animateFromBag);
    }

    bool IsHandDrawInProgress()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != null && hand[i].IsMoving)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator TutorialHandRefreshRoutine()
    {
        while (IsHandDrawInProgress())
        {
            yield return null;
        }
        RefreshTutorialHandViews(animateFromBag: true);
        tutorialHandRefreshCoroutine = null;
    }

    public void RefreshTutorialHandViews(bool animateFromBag = true)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            GameObject.Destroy(hand[i].gameObject);
            hand[i] = null;
        }
        tempDeckNumberForAnim = 0;
        CreateHand(animateFromBag);
    }

    public void RequestTutorialHandRefresh()
    {
        if (tutorialHandRefreshCoroutine != null)
        {
            return;
        }
        if (IsHandDrawInProgress())
        {
            tutorialHandRefreshCoroutine = StartCoroutine(TutorialHandRefreshRoutine());
            return;
        }
        RefreshTutorialHandViews(animateFromBag: true);
    }

    bool HandViewsMatchLogic()
    {
        if (game?.hand?.tokens == null)
        {
            return hand.Count == 0;
        }
        for (int i = 0; i < game.hand.tokens.Length; i++)
        {
            bool logicHasToken = game.hand.tokens[i] != null;
            bool viewHasToken = i < hand.Count && hand[i] != null;
            if (logicHasToken != viewHasToken)
            {
                return false;
            }
            if (logicHasToken && hand[i].token != game.hand.tokens[i])
            {
                return false;
            }
        }
        return true;
    }

    void SyncTutorialHandViewsIfNeeded()
    {
        if (IsTutorialHandLogicEmpty() || HandViewsMatchLogic())
        {
            return;
        }
        RefreshTutorialHandViews(animateFromBag: false);
    }

    public void PresentPreparedTutorialHand()
    {
        if (tutorialHandsDrawn < 1)
        {
            return;
        }
        if (!IsTutorialHandLogicEmpty() && HandViewsMatchLogic())
        {
            return;
        }
        if (tutorialHandRefreshCoroutine != null)
        {
            StopCoroutine(tutorialHandRefreshCoroutine);
            tutorialHandRefreshCoroutine = null;
        }
        RefreshTutorialHandViews(animateFromBag: true);
    }

    public void PresentTutorialHandAfterIntro()
    {
        if (!IsTutorialHandLogicEmpty())
        {
            if (!HandViewsMatchLogic())
            {
                PresentPreparedTutorialHand();
            }
            return;
        }

        DrawNextTutorialScriptHand(animateFromBag: true);
    }

    public void PresentBagHandForMulliganBeat()
    {
        if (game.hand.AllSlotsEmpty())
        {
            game.hand.FillHand(game.bag);
        }
        CreateHand(true);
    }

    public void OnTutorialEnded(bool refillHand = false)
    {
        game.skipAutoHandFillOnEmpty = false;
        game.tutorialNewHandGate = null;
        tutorialHandsDrawn = 0;
        if (tutorialHandRefreshCoroutine != null)
        {
            StopCoroutine(tutorialHandRefreshCoroutine);
            tutorialHandRefreshCoroutine = null;
        }
        if (!refillHand)
        {
            return;
        }
        if (game.hand.AllSlotsEmpty())
        {
            game.hand.FillHand(game.bag);
        }
        CreateHand(true);
    }

    int CountVisibleHandTokens()
    {
        int count = 0;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    void SyncTutorialHandFillMode()
    {
        game.skipAutoHandFillOnEmpty = inTutorial;
        game.tutorialNewHandGate = null;
    }

    bool IsTutorialHandLogicEmpty()
    {
        if (game?.hand?.tokens == null)
        {
            return true;
        }
        foreach (Logic.Token token in game.hand.tokens)
        {
            if (token != null)
            {
                return false;
            }
        }
        return true;
    }

    void HandleTutorialNewHand()
    {
        if (!IsTutorialHandLogicEmpty())
        {
            return;
        }

        if (tutorialHandsDrawn >= 5)
        {
            CreateHand(true);
            return;
        }

        DrawNextTutorialScriptHand(animateFromBag: true);
    }

    public void CreateHand(bool midgame = false, bool undo = false)
    {
        for (int i = 0; i < game.hand.tokens.Length; i++)
        {
            // If there isnt any token data for this index
            if (game.hand.tokens[i] == null)
            {
                // If there isn't a token view for this index, add a null entry to the hand list
                if (hand.Count < game.hand.tokens.Length)
                {
                    hand.Add(null);
                }
                else
                {
                    // Destroy the token views that are currently in the hand
                    if (hand[i] != null)
                    {
                        if (undo)
                        {
                            hand[i].UndoDestroy();
                            hand[i] = null;
                        }
                        else
                        {
                            // destroy the token if it exists
                            GameObject.Destroy(hand[i].gameObject);
                        }
                    }
                }
                continue;
            }

            bool tokenAlreadyExisted = false;

            // If there is a token view missing for this index, create a new one 
            if (hand.Count < game.hand.tokens.Length || hand[i] == null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, handTransforms[i]).GetComponent<Token>();
                token.token = game.hand.tokens[i];
                if (hand.Count < game.hand.tokens.Length)
                {
                    hand.Add(token);
                }
                else
                {
                    hand[i] = token;
                }
            }
            else
            {
                tokenAlreadyExisted = true;
                hand[i].token = game.hand.tokens[i];
            }

            if (undo && tokenAlreadyExisted == false)
            {
                hand[i].UndoInit(hand[i].token);
            }
            else
            {
                hand[i].Init(hand[i].token);
            }

            hand[i].UpdateLayer("TokenHand");
            if (tokenAlreadyExisted == false)
            {
                float angle = UnityEngine.Random.Range(2f, 5f);
                if (UnityEngine.Random.value < 0.5f)
                {
                    angle *= -1f;
                }
                hand[i].transform.localEulerAngles = new Vector3(0, 0, angle);
            }

            if (midgame)
            {
                if (tempDeckNumberForAnim < hand.Count)
                {
                    tempDeckNumberForAnim += 1;
                }

                hand[i].handPos = handTransforms[i].position;
                if (game.hand.handSize == 1)
                {
                    hand[i].handPos.x = 0;
                    hand[i].lifted = true;
                }
                hand[i].DrawFromBag(i);
            }
            else
            {
                if (game.hand.handSize == 1)
                {
                    hand[i].lifted = true;
                    chosenIndex = 0;
                    EnterInputState(InputState.Place);
                }
            }
            //hand[i].PlaceInHand(i);
        }

    }
    void EnterInputState(InputState newState)
    {
        switch (newState)
        {
            case InputState.Choose:
                draggingTile = false;
                holdingClick = false;
                if (inputState == InputState.Place)
                {
                    //returning
                    chosenToken.UpdateLayer("TokenHand");
                }
                break;
            case InputState.Place:
                chosenToken.UpdateLayer("TokenHeld");
                break;
            case InputState.Finish:
                lastTokenPlaced = null;
                scoreBeforeFinish = score;
                EnsureWinScreenDisplay();
                if (!finishRoutineRunning)
                {
                    finishRoutineRunning = true;
                    StartCoroutine(FinishBoardRoutine());
                }
                break;
            case InputState.Wait:
                draggingTile = false;
                holdingClick = false;
                // Flush before clear so re-entering Wait (place / popup dismiss)
                // cannot orphan floating death-score labels.
                FlushDyingTokens();
                break;
        }
        inputState = newState;

    }

    float PlacementSnapRadius(float snapRadiusScale) =>
        snapRadiusScale * Mathf.Min(Mathf.Abs(gridSeparation.x), Mathf.Abs(gridSeparation.y));

    bool ResolvePlacementTarget(Vector2 pointer, out Vector2Int pos, float snapRadiusScale)
    {
        pos = Vector2Int.one * -5;
        float bestDist = float.MaxValue;
        Vector2Int bestGridPos = pos;
        bool bestIsFreeSlot = false;

        foreach (Tile tile in tiles.Values)
        {
            float d = Vector2.Distance(pointer, tile.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestGridPos = tile.tile.pos;
                bestIsFreeSlot = false;
            }
        }

        if (freeSlot != null)
        {
            float freeDist = Vector2.Distance(pointer, freeSlot.transform.position);
            if (freeDist < bestDist)
            {
                bestDist = freeDist;
                bestIsFreeSlot = true;
            }
        }

        if (bestDist > PlacementSnapRadius(snapRadiusScale))
        {
            return false;
        }

        pos = bestIsFreeSlot ? freeSlotChoice : bestGridPos;
        return true;
    }

    float ActivePlacementSnapRadiusScale() =>
        draggingTile ? placementSnapRadiusScale : tapPlacementSnapRadiusScale;

    Vector2 GetDragDrawPosition()
    {
        if (!snapDragVisualToTile)
        {
            return InputHelper.GetPointerWorldPosition();
        }
        if (chosenPos == freeSlotChoice && freeSlot != null)
        {
            return freeSlot.transform.position;
        }
        if (tiles.ContainsKey(chosenPos))
        {
            return tiles[chosenPos].transform.position;
        }
        return InputHelper.GetPointerWorldPosition();
    }

    void DebugBagDisplay()
    {
        string s = "";
        Logic.Bag bag = game.bag;
        Dictionary<TokenData, int> numbers = new Dictionary<TokenData, int>();
        List<string> lines = new List<string>();
        foreach (TokenData token in bag.bagContents.Keys)
        {
            /*string line = token.ToString() + "<size=75%>x</size>" + bag.bagContents[token].ToString();
            int num = token.num + (10 * ((Logic.TripleGame)game).colorScoreMulti[token.color]);
            lines.Add(num.ToString()+"_"+line);*/
            numbers.Add(token, bag.bagContents[token]);
        }
        //temp
        foreach (TokenData token in bag.nextBagsTemporary)
        {
            if (numbers.ContainsKey(token))
            {
                numbers[token] += 1;
            }
            else
            {
                numbers.Add(token, 1);
            }
        }
        Dictionary<TokenData, int> currentbag = new Dictionary<TokenData, int>();
        foreach (TokenData token in bag.bag)
        {
            if (currentbag.ContainsKey(token))
            {
                currentbag[token] += 1;
            }
            else
            {
                currentbag.Add(token, 1);
            }
        }
        lines = new List<string>();
        foreach (TokenData token in currentbag.Keys)
        {
            string line = token.ToString() + "<size=75%>x</size>" + currentbag[token].ToString();
            int num = token.num + (10 * ((Logic.TripleGame)game).colorScoreMulti[token.color]);
            line = num.ToString() + "_" + line;

            if (numbers.ContainsKey(token))
            {
                line += "/";
                line += numbers[token].ToString();
            }
            lines.Add(line);
        }
        foreach (TokenData token in numbers.Keys)
        {
            if (currentbag.ContainsKey(token) == false)
            {
                string line = token.ToString() + "<size=75%>x</size>0";
                int num = token.num + (10 * ((Logic.TripleGame)game).colorScoreMulti[token.color]);
                line = num.ToString() + "_" + line;
                line += "/";
                line += numbers[token].ToString();
                lines.Add(line);
            }
        }
        lines.Sort();
        foreach (string line in lines)
        {
            s += line.Substring(3) + "\n";
        }
        debugBagDisplay.text = s;
    }
    void EnsureWinScreenDisplay()
    {
        if (winScreen == null)
            return;

        winScreenDisplay = winScreen.GetComponent<WinScreenDisplay>();
        if (winScreenDisplay == null)
            winScreenDisplay = winScreen.AddComponent<WinScreenDisplay>();
        winScreenDisplay.SetScoreText(winScreenScore);
    }

    string FormatWinScreenScore()
    {
        return "<size=35%>Your score:</size>\n" + score.ToString() + "\n<size=15%><line-height=100%>-Tap to restart-</size>";
    }

    float GetFinishStepDelay(Logic.TokenData data, int index, int total)
    {
        float delay = finishBaseDelay + finishDelayPerScore * ScoreToken(data);
        if (IsLastFinishTiles(index, total))
        {
            delay *= finishLastTileSlowdown;
        }
        return delay;
    }

    int GetFinishFlowerCount(Logic.TokenData data) => finishFlowerBase + finishFlowerPerNum * data.num;

    bool IsLastFinishTiles(int index, int total) => index >= total - finishLastTileCount;

    List<(Vector2Int pos, Tile tile)> BuildFinishTileOrder()
    {
        List<(Vector2Int, Tile)> ordered = new List<(Vector2Int, Tile)>();
        Vector2Int gridSize = game.grid.gridSize;
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (tiles.TryGetValue(pos, out Tile tile) && tile.token != null)
                {
                    ordered.Add((pos, tile));
                }
            }
        }
        return ordered;
    }

    IEnumerator FlushScoreDeltaImmediate()
    {
        if (scoreDelta > 0)
        {
            score += scoreDelta;
            scoreDelta = 0;
            FinishScoreRolling();
        }
        yield return null;
    }

    public void SpawnWinConfetti()
    {
        List<Tile> gridTiles = new List<Tile>(tiles.Values);
        if (gridTiles.Count == 0)
        {
            return;
        }

        int count = UnityEngine.Random.Range(15, 21);
        for (int i = 0; i < count; i++)
        {
            Tile tile = gridTiles[UnityEngine.Random.Range(0, gridTiles.Count)];
            TokenColor color = (TokenColor)UnityEngine.Random.Range(0, flowerPrefabs.Count);
            CreateFlower(tile, color, false, true);
        }
    }

    void PrepareAllTokensForFinish()
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile.token != null)
            {
                tile.token.PrepareForFinishParade();
            }
        }
        if (freeSlot != null && freeSlot.token != null)
        {
            freeSlot.token.PrepareForFinishParade();
        }
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != null)
            {
                hand[i].PrepareForFinishParade();
            }
        }
    }

    IEnumerator FinishBoardRoutine()
    {
        PrepareAllTokensForFinish();

        List<(Vector2Int pos, Tile tile)> finishTiles = BuildFinishTileOrder();
        int total = finishTiles.Count;

        for (int i = 0; i < total; i++)
        {
            Tile tile = finishTiles[i].tile;
            Token token = tile.token;
            if (token == null)
            {
                continue;
            }

            Logic.TokenData data = token.token.data;
            int points = ScoreToken(data);

            yield return new WaitForSeconds(GetFinishStepDelay(data, i, total));

            int flowerCount = GetFinishFlowerCount(data);
            for (int f = 0; f < flowerCount; f++)
            {
                CreateFlower(tile, data.color, false, true);
            }

            FinishTilePacing pacing = new FinishTilePacing
            {
                scoreHoldDuration = finishScoreHoldBase + finishDelayPerScore * points
            };
            bool isLastTile = i == total - 1;
            yield return token.PlayFinishTileRoutine(pacing, isLastTile);
            tile.token = null;
        }

        yield return FlushScoreDeltaImmediate();
        yield return ShowWinScreenSequence();
        finishRoutineRunning = false;
        EnterInputState(InputState.TapToRestart);
    }

    IEnumerator ShowWinScreenSequence()
    {
        winScreenAccelerateRequested = false;
        winScreenAnimSpeed = 1f;
        winScreenShowing = true;
        bool isNewHighScore = HighScoreManager.Instance != null && HighScoreManager.Instance.WouldBeNewBest(score, difficulty);
        SaveLoad.DeleteSave(0);
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.SubmitScore(score, difficulty);
        }

        SpawnWinConfetti();
        EnsureWinScreenDisplay();
        winScreen.SetActive(true);

        if (winScreenDisplay != null)
        {
            WinScreenPayload payload = new WinScreenPayload
            {
                fromScore = scoreBeforeFinish,
                toScore = score,
                isNewHighScore = isNewHighScore
            };
            yield return winScreenDisplay.ShowRoutine(payload);
        }
        else
        {
            winScreen.SetActive(true);
            winScreenScore.text = FormatWinScreenScore();
        }
        winScreenShowing = false;
        winScreenAccelerateRequested = false;
        winScreenAnimSpeed = 1f;
    }

    IEnumerator DismissUpgradePopupRoutine()
    {
        if (dismissingPopup || upgradePopup == null)
            yield break;

        dismissingPopup = true;
        upgradePopup.Close();
        while (upgradePopup.IsAnimatingClose)
            yield return null;

        popupopen = false;
        dismissingPopup = false;
        waiting = waitTime;
        EnterInputState(InputState.Wait);
    }

    bool AnyGridTokenStillPlacing(float snapDistance)
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile.token == null)
            {
                continue;
            }
            if (tile.token.IsPlacementAnimating)
            {
                return true;
            }
            if (Vector2.Distance(tile.token.transform.position, tile.transform.position) > snapDistance)
            {
                return true;
            }
        }
        return false;
    }

    bool AreMergeDeathsComplete()
    {
        if (dyingTokens.Count == 0)
        {
            return true;
        }

        foreach (Token t in dyingTokens)
        {
            if (t != null && !t.mergeDeathVisualComplete)
            {
                return false;
            }
        }

        return true;
    }

    void FlushDyingTokens()
    {
        for (int i = dyingTokens.Count - 1; i >= 0; i--)
        {
            Token t = dyingTokens[i];
            if (t != null)
            {
                t.StartKillNumber();
            }
        }
        dyingTokens.Clear();
    }

    bool TryFlushDyingTokens()
    {
        if (dyingTokens.Count == 0 || !AreMergeDeathsComplete())
        {
            return false;
        }

        FlushDyingTokens();
        return true;
    }

    void EnsureVisualGridMatchesLogic()
    {
        foreach (Vector2Int p in tiles.Keys)
        {
            Tile tile = tiles[p];
            Logic.Tile logicTile = game.grid.tiles[p];
            tile.tile = logicTile;
            if (logicTile.token != null && tile.token == null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                token.transform.position = tile.transform.position;
                token.UpdateLayer("TokenPlaced");
                token.Init(logicTile.token);
                tile.token = token;
            }
        }
    }

    void TryStartFinish()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inputState == InputState.Wait && game.gridUpdating) { return; }
        if (!game.isGameover()) { return; }
        if (finishRoutineRunning || pendingFinishStart) { return; }

        pendingFinishStart = true;
        StartCoroutine(WaitForBoardSettledThenFinish());
    }

    IEnumerator WaitForPlacementCompleteWithTimeout(Token token, float timeout)
    {
        float elapsed = 0f;
        while (token != null && token.IsPlacementAnimating && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (token == null || elapsed >= timeout)
        {
            yield break;
        }

        float settleElapsed = 0f;
        float settleSeconds = PlacementSettleSeconds;
        while (settleElapsed < settleSeconds)
        {
            settleElapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator WaitWithTimeout(System.Func<bool> condition, float timeout)
    {
        float elapsed = 0f;
        while (condition() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator WaitForBoardSettledThenFinish()
    {
        const float snapDistance = 0.02f;
        const float settlementTimeout = 3f;

        try
        {
            yield return WaitWithTimeout(() => game.gridUpdating, settlementTimeout);

            yield return WaitWithTimeout(() => game.status.events.Count > 0, settlementTimeout);

            yield return WaitWithTimeout(() => !AreMergeDeathsComplete(), settlementTimeout);

            float elapsed = 0f;
            while (AnyGridTokenStillPlacing(snapDistance))
            {
                if (elapsed >= settlementTimeout)
                {
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (lastTokenPlaced != null)
            {
                yield return WaitForPlacementCompleteWithTimeout(lastTokenPlaced, settlementTimeout);
            }

            if (AnyGridTokenStillPlacing(snapDistance)
                || (lastTokenPlaced != null && lastTokenPlaced.IsPlacementAnimating))
            {
                PrepareAllTokensForFinish();
            }

            EnsureVisualGridMatchesLogic();

            if (game.isGameover())
            {
                lastTokenPlaced = null;
                EnterInputState(InputState.Finish);
            }
        }
        finally
        {
            pendingFinishStart = false;
        }
    }

    void DeathCheck()
    {
        if (!game.isGameover()) { return; }
        TryStartFinish();
    }

    public void RequestWinScreenAccelerate()
    {
        if (winScreenAnimSpeed >= 2f)
        {
            return;
        }
        winScreenAccelerateRequested = true;
        winScreenAnimSpeed = 2f;
    }

    void PollWinScreenAccelerate()
    {
        if (!winScreenShowing)
        {
            return;
        }
        if (InputHelper.GetPrimaryPressBegan() || InputHelper.GetAnyPressBegan())
        {
            RequestWinScreenAccelerate();
        }
    }

    public void HigherDifficulty()
    {
        difficulty++;
        difficulty = Mathf.Clamp(difficulty, 0, difficulties.Count - 1);
        PlayerPrefs.SetInt("difficulty", difficulty);
        PlayerPrefs.Save();
    }
    public void LowerDifficulty()
    {
        difficulty--;
        difficulty = Mathf.Clamp(difficulty, 0, difficulties.Count - 1);
        PlayerPrefs.SetInt("difficulty", difficulty);
        PlayerPrefs.Save();
    }
    public void StartScoreRolling()
    {
        Services.AudioManager.PlayScoreLoop();
    }
    public void FinishScoreRolling()
    {
        Services.AudioManager.StopScoreLoop();
    }

    // Update is called once per frame
    void Update()
    {
        loadSnapshotButton.SetActive(SaveLoad.HasSave(1));
        difficultyName.text = "Difficulty\n" + difficultyNames[difficulty];
        /*if (difficulties[difficulty] != gameJson)
        {
            difficultyName.text += "<size=50%>\nRestart to change difficulty";
        }*/
        if (difficultyUnlocked[difficulty] == false)
        {
            difficultyName.text += "\nLocked! Earn " + scoreNeededToUnlock[difficulty].ToString() + " in ";
            string actualName = difficultyNames[difficulty - 1].Split('<')[0];
            difficultyName.text += actualName + " to unlock";
        }
        difficultyButtons[0].disabled = difficulty == 0;
        difficultyButtons[1].disabled = difficulty == difficulties.Count - 1;
        PollWinScreenAccelerate();
        DeathCheck();

        Services.AudioManager.SetVolume(0, PlayerPrefs.GetFloat("musicVolume"));
        Services.AudioManager.SetVolume(1, PlayerPrefs.GetFloat("soundVolume"));

        if (movingToScreen)
        {
            Vector3 cameraPos = new Vector3(0, 0, Camera.main.transform.position.z);
            switch (gameState)
            {
                case GameState.Gameplay:
                    break;
                case GameState.Seeds:
                case GameState.Settings:
                    cameraPos.x = 8;
                    break;
                case GameState.Help:
                    cameraPos.x = -8;
                    break;
                case GameState.Start:
                    cameraPos.y = -15.3f;
                    bagDisplay.gameObject.SetActive(false);
                    break;
                case GameState.Credits:
                case GameState.HighScore:
                    cameraPos.x = 8;
                    cameraPos.y = 12.33f;
                    break;
                case GameState.Bag:
                    cameraPos.y = -8;
                    break;
            }

            if (gameState != GameState.Start)
            {
                bagDisplay.gameObject.SetActive(true);
            }

            if (cameraPanElapsed < 0f)
            {
                cameraPanStart = Camera.main.transform.position;
                cameraPanElapsed = 0f;
            }

            cameraPanElapsed += Time.deltaTime;
            float panT = SlideHelper.ApplyEase(
                Mathf.Clamp01(cameraPanElapsed / cameraPanDuration),
                SlideHelper.EaseType.EaseInOutQuad);
            Camera.main.transform.position = Vector3.Lerp(cameraPanStart, cameraPos, panT);

            if (cameraPanElapsed >= cameraPanDuration || Vector2.Distance(Camera.main.transform.position, cameraPos) < 0.03f)
            {
                movingToScreen = false;
                cameraPanElapsed = -1f;
                Camera.main.transform.position = cameraPos;
                if (gameState != GameState.Bag && gameState != GameState.SelectDifficulty && gameState != GameState.ToolShop)
                {
                    for (int i = 0; i < stateScreens.Count; i++)
                    {
                        if ((int)gameState != i)
                        {
                            if (i == (int)GameState.ToolShop)
                            {
                                var shop = toolShopScreen ?? stateScreens[i].GetComponentInChildren<ToolShopScreen>();
                                if (shop != null && shop.IsAnimating)
                                    continue;
                            }
                            stateScreens[i].gameObject.SetActive(false);
                        }
                    }
                }

            }
        }


        if (game.isGameover())
        {
            //GameLog.Log("game over");
        }
        DebugBagDisplay();
        //save stuff

        //simInput.Update();
        //display.text = "<mspace=0.25\nem><line-height=75%>" + simInput.Draw();
        display.text = score.ToString();

        /*if(scoreDelta > 0)
        {
            display.text+= "<size=50%>+</size>"+scoreDelta.ToString();
        }*/
        if (scoreDelta > 0)
        {
            int tinyAmount = Mathf.Max(1, Mathf.CeilToInt(0.1f * scoreDelta));
            scoreDelta -= tinyAmount;
            score += tinyAmount;
            if (scoreDelta <= 0)
            {
                FinishScoreRolling();
            }
        }
        display.text += "\n<size=20%>-score-</size>";
        if (winScreenDisplay == null && winScreen.activeSelf)
            winScreenScore.text = FormatWinScreenScore();
        bagDisplay.text = " "; game.bag.ToString();
        InputHelper.BeginFrame();
        Vector2 mousePos = InputHelper.GetPointerWorldPosition();
        if (gameState == GameState.Bag)
        {
            if (InputHelper.GetPrimaryPressBegan() && !ignoreBagCloseInputThisFrame)
            {
                if (mousePos.y >= -13.0f)
                {
                    deckDisplay.ClearBagAfterDelay();
                    GameStateGameplay();
                }
            }
        }
        ignoreBagCloseInputThisFrame = false;
        if (inputState != InputState.Wait && inputState != InputState.Finish)
        {
            TryFlushDyingTokens();
        }
        //input
        switch (inputState)
        {
            case InputState.TapToRestart:
                if (InputHelper.GetAnyPressBegan())
                {
                    Services.AudioManager.StopMusic();
                    SceneManager.LoadScene(0);
                }
                break;
            case InputState.Choose:
                chosenIndex = -1;
                Logic.TokenColor color = Logic.TokenColor.Clipper;
                //hover
                for (int i = 0; i < game.hand.tokens.Length; i++)
                {
                    if (hand[i] == null) { continue; }
                    Vector2 pos = firstHandPos + (i * handSeparation);
                    if (game.hand.handSize == 1)
                    {
                        pos = handTransforms[i].position;
                        pos.x = 0;
                    }
                    float d = Vector2.Distance(mousePos, pos);
                    if (d < 0.5f)
                    {
                        chosenIndex = i;
                        color = hand[i].token.data.color;
                        break;
                    }
                }
                //freeslot
                if (chosenIndex == -1 && freeSlot.token)
                {
                    float _dist = Vector2.Distance(mousePos, freeSlot.transform.position);
                    if (_dist < 0.5f)
                    {
                        chosenIndex = game.hand.handSize + 2;
                        color = freeSlot.token.token.data.color;
                    }
                }
                if (InputHelper.GetPrimaryPressBegan())
                {
                    bool tutorialCanChoose = false;
                    if (inTutorial && !tutorial.CanAcceptTutorialInput)
                    {
                        tutorialCanChoose = false;
                    }
                    else if (inTutorial)
                    {
                        if (tutorial.choosingRule)
                        {
                            if (chosenIndex > -1 && color == tutorial.allowedColor)
                            {
                                tutorialCanChoose = true;
                            }
                        }
                        else
                        {
                            tutorialCanChoose = true;
                        }
                    }
                    else
                    {
                        tutorialCanChoose = true;
                    }
                    if (chosenIndex > -1 && tutorialCanChoose)
                    {
                        Services.AudioManager.PlayPickUpSound();
                        if (useHaptics)
                        {
                            Haptics.PlayTransient(.5f, .5f);
                        }
                        EnterInputState(InputState.Place);
                        clickHoldDuration = 0;
                        holdingClick = true;
                        break;
                    }
                }
                //undoing
                if (InputHelper.GetPrimaryPressBegan() && !game.isGameover())
                {
                    ResolvePlacementTarget(mousePos, out chosenPos, tapPlacementSnapRadiusScale);
                    if (tiles.ContainsKey(chosenPos) && tiles[chosenPos].token != null && tiles[chosenPos].token == lastTokenPlaced)
                    {
                        lastTokenPlaced = null;
                        if (useHaptics)
                        {
                            Haptics.PlayTransient(.5f, .5f);
                        }
                        Undo();
                    }
                }

                break;
            case InputState.Place:
            {
                bool dragDropThisFrame = false;
                if (holdingClick)
                {
                    clickHoldDuration += Time.deltaTime;
                    if (clickHoldDuration > 0.15f)
                    {
                        draggingTile = true;
                    }
                    if (InputHelper.GetPrimaryPressEnded())
                    {
                        holdingClick = false;
                    }
                }
                holdingClipper = chosenToken.token.data.color == Logic.TokenColor.Clipper;
                holdingSpade = chosenToken.token.data.color == Logic.TokenColor.Spade;
                holdingAdder = chosenToken.token.data.color == Logic.TokenColor.Adder;
                // Drop commits from thumb release with tap snap; in-drag highlight stays tighter.
                dragDropThisFrame = draggingTile && InputHelper.GetPrimaryPressEnded();
                if (dragDropThisFrame)
                {
                    ResolvePlacementTarget(mousePos, out chosenPos, tapPlacementSnapRadiusScale);
                }
                else
                {
                    ResolvePlacementTarget(mousePos, out chosenPos, ActivePlacementSnapRadiusScale());
                }
                //let go
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z))
                {
                    EnterInputState(InputState.Choose);
                }
                else if (InputHelper.GetPrimaryPressBegan() || dragDropThisFrame)
                {//clicking
                    if (inTutorial && !tutorial.CanAcceptTutorialInput)
                    {
                        break;
                    }
                    bool _tutorialGood = false;
                    if (inTutorial)
                    {
                        if (tutorial.stage == TutorialStage.FreeSlot || (tutorial.stage == TutorialStage.Blue3 && chosenToken.token.data.color == Logic.TokenColor.Red) || (tutorial.stage == TutorialStage.CleanUp && chosenToken.token.data.color == Logic.TokenColor.Red))
                        {
                            _tutorialGood = true;
                        }
                    }
                    else
                    {
                        _tutorialGood = true;
                    }
                    if (chosenPos == freeSlotChoice)
                    {
                        if (chosenIndex == game.hand.handSize + 2)
                        {//put it back!
                            Services.AudioManager.PlayLetGoSound();
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            EnterInputState(InputState.Choose);
                            break;
                        }
                        else if (game.IsFreeSlotFree() && _tutorialGood)
                        {
                            Services.AudioManager.PlayFreeSlotSound();
                            Token placedToken = hand[chosenIndex];
                            game.PlaceTokenInFreeSlot(chosenIndex);
                            freeSlot.token = placedToken;
                            placedToken.BeginPlacementAnimation();
                            hand[chosenIndex] = null;
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            if (inTutorial && (tutorial.stage == TutorialStage.FreeSlot || tutorial.stage == TutorialStage.CleanUp))
                            {
                                tutorial.RequestIncrementStageAfterPlacement(placedToken, IncrementTrigger.AfterPlace);
                            }
                            if (inTutorial && (tutorial.stage == TutorialStage.Blue3))
                            {
                                tutorial.RequestStageUpdateAfterPlacement(placedToken);
                            }
                            break;
                        }

                    }
                    if (holdingClipper == false && holdingSpade == false && holdingAdder == false)
                    {
                        bool tutorialGood = false;
                        if (inTutorial)
                        {
                            if (tutorial.placingRule == false)
                            {
                                tutorialGood = true;
                            }
                            else
                            {
                                if (tutorial.allowedPlaces.Contains(chosenPos))
                                {
                                    tutorialGood = true;
                                }
                                else
                                {
                                    tutorialGood = false;
                                }
                            }
                        }
                        else
                        {
                            tutorialGood = true;
                        }
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data) && tutorialGood)
                        {
                            Services.AudioManager.PlayPlaceSound();
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            game.PlaceTokenFromHand(chosenIndex, chosenPos);
                            Token placedToken;
                            if (chosenIndex >= game.hand.handSize)
                            {
                                //freeSlot
                                placedToken = freeSlot.token;
                                tiles[chosenPos].token = placedToken;
                                lastTokenPlaced = placedToken;
                                lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                placedToken.BeginPlacementAnimation();
                                freeSlot.token = null;

                            }
                            else
                            {
                                placedToken = hand[chosenIndex];
                                tiles[chosenPos].token = placedToken;
                                placedToken.BeginPlacementAnimation();
                                lastTokenPlaced = placedToken;
                                lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                hand[chosenIndex] = null;
                            }
                            if (inTutorial && (tutorial.stage == TutorialStage.Placing || tutorial.stage == TutorialStage.WeirdSet || tutorial.stage == TutorialStage.FirstRed || tutorial.stage == TutorialStage.Blue3 || tutorial.stage == TutorialStage.SecondRed || tutorial.stage == TutorialStage.LearnGreen || tutorial.stage == TutorialStage.CleanUp || tutorial.stage == TutorialStage.Purple))
                            {
                                tutorial.RequestStageUpdateAfterPlacement(placedToken);
                            }
                            if (inTutorial && (tutorial.stage == TutorialStage.Undo && tutorial.stagePhase == 1))
                            {
                                tutorial.RequestIncrementStageAfterPlacement(placedToken, IncrementTrigger.AfterPlace);
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                        else
                        {
                            if (chosenPos.x >= 0 && chosenPos.x <= 5)
                            {
                                chosenToken.StartInvalidAnim();
                            }

                        }
                    }
                    else if (holdingClipper || holdingAdder)
                    {
                        bool emptyTile = game.grid.HasTile(chosenPos) && game.grid.tiles[chosenPos].IsEmpty();
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data))
                        {
                            game.PlaceTokenFromHand(chosenIndex, chosenPos);
                            //Services.AudioManager.PlayPlaceSound();
                            if (holdingClipper)
                            {
                                Services.AudioManager.PlayShearsSound();
                                if (useHaptics)
                                {
                                    Haptics.PlayTransient(.5f, .5f);
                                }
                            }
                            if (chosenIndex >= game.hand.handSize)
                            {
                                if (holdingAdder && emptyTile)
                                {
                                    tiles[chosenPos].token = freeSlot.token;
                                    tiles[chosenPos].token.SetTokenData(tiles[chosenPos].token.token.data);
                                    lastTokenPlaced = freeSlot.token;
                                    lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                    freeSlot.token.BeginPlacementAnimation();
                                    freeSlot.token = null;
                                }
                                else
                                {
                                    //GameObject.Destroy(freeSlot.token.gameObject);
                                    tiles[chosenPos].token.ToolAnim(freeSlot.token, chosenIndex);
                                    lastTokenPlaced = tiles[chosenPos].token;
                                }
                                if (holdingClipper)
                                {
                                    //freeSlot.token = CreateClippingToken(game.freeSlot);//new tiletiles[chosenPos].token;
                                    //freeSlot.token.PlaceInTile(freeSlot);
                                    freeSlot.token = null;
                                }
                                else
                                {
                                    freeSlot.token = null;

                                }
                            }
                            else
                            {
                                if (holdingAdder && emptyTile)
                                {
                                    tiles[chosenPos].token = hand[chosenIndex];
                                    tiles[chosenPos].token.SetTokenData(tiles[chosenPos].token.token.data);
                                    hand[chosenIndex].BeginPlacementAnimation();
                                    lastTokenPlaced = hand[chosenIndex];
                                    lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                    hand[chosenIndex] = null;
                                }
                                else
                                {
                                    //GameObject.Destroy(hand[chosenIndex].gameObject);
                                    tiles[chosenPos].token.ToolAnim(hand[chosenIndex], chosenIndex);
                                    lastTokenPlaced = tiles[chosenPos].token;
                                }


                                if (holdingClipper)
                                {
                                    lastTokenPlaced = tiles[chosenPos].token;
                                    //hand[chosenIndex] = CreateClippingToken(game.hand.tokens[chosenIndex]);//new tiletiles[chosenPos].token;
                                    //hand[chosenIndex].PlaceInHand(chosenIndex);
                                    hand[chosenIndex] = null;
                                }
                                else
                                {
                                    hand[chosenIndex] = null;
                                }


                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                        else if (emptyTile)
                        {
                            chosenToken.StartInvalidAnim();
                            Services.AudioManager.PlayInvalidToolSound();
                        }
                        else
                        {
                            if (chosenPos.x >= 0 && chosenPos.x <= 5)
                            {
                                chosenToken.StartInvalidAnim();
                            }
                        }
                    }
                    else if (holdingSpade)
                    {
                        bool emptyTile = game.grid.HasTile(chosenPos) && game.grid.tiles[chosenPos].IsEmpty();
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data))
                        {
                            Services.AudioManager.PlayPlaceSound();
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            if (chosenIndex >= game.hand.handSize)
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                //GameObject.Destroy(freeSlot.token.gameObject);
                                tiles[chosenPos].token.ToolAnim(freeSlot.token, chosenIndex);
                                freeSlot.token = tiles[chosenPos].token;
                                lastTokenPlaced = null;
                                freeSlot.token.UpdateLayer("TokenHand");

                                tiles[chosenPos].token = null;
                            }
                            else
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                lastTokenPlaced = null;
                                //GameObject.Destroy(hand[chosenIndex].gameObject);
                                tiles[chosenPos].token.ToolAnim(hand[chosenIndex], chosenIndex);
                                hand[chosenIndex] = tiles[chosenPos].token;
                                hand[chosenIndex].UpdateLayer("TokenHand");
                                //hand[chosenIndex].SpadeAnim(chosenIndex);
                                tiles[chosenPos].token = null;
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                        else if (emptyTile)
                        {
                            chosenToken.StartInvalidAnim();
                            Services.AudioManager.PlayInvalidToolSound();
                        }
                    }


                }
                if (InputHelper.GetPrimaryPressBegan())
                {
                    GameLog.Log("up");
                    //are you clicking on another tile??
                    int new_index = -1;
                    for (int i = 0; i < game.hand.tokens.Length; i++)
                    {
                        if (hand[i] == null) { continue; }
                        Vector2 pos = firstHandPos + (i * handSeparation);
                        float d = Vector2.Distance(mousePos, pos);
                        if (d < 0.5f)
                        {
                            new_index = i;
                            break;
                        }
                    }
                    if (new_index != -1)
                    {
                        if (new_index == chosenIndex)
                        {
                            Services.AudioManager.PlayLetGoSound();
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            EnterInputState(InputState.Choose);
                        }
                        else
                        {
                            chosenToken.UpdateLayer("TokenHand");
                            chosenIndex = new_index;

                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            EnterInputState(InputState.Place);
                        }
                    }
                    else
                    {
                        if (freeSlot.token)
                        {
                            float _dist = Vector2.Distance(mousePos, freeSlot.transform.position);
                            if (_dist < 0.5f)
                            {
                                chosenToken.UpdateLayer("TokenHand");
                                chosenIndex = game.hand.handSize + 2;
                                EnterInputState(InputState.Place);
                            }
                        }
                    }
                }
                //undoing
                if (InputHelper.GetPrimaryPressBegan() && !game.isGameover())
                {
                    ResolvePlacementTarget(mousePos, out chosenPos, tapPlacementSnapRadiusScale);
                    if (tiles.ContainsKey(chosenPos) && tiles[chosenPos].token != null && tiles[chosenPos].token == lastTokenPlaced)
                    {
                        lastTokenPlaced = null;
                        if (useHaptics)
                        {
                            Haptics.PlayTransient(.5f, .5f);
                        }
                        Undo();
                        break;
                    }
                }
                // Only cancel on an explicit failed drag-drop release (not while sprite is catching up mid-drag).
                if (dragDropThisFrame && draggingTile)
                {
                    EnterInputState(InputState.Choose);
                }
                break;
            }
            case InputState.Wait:
                if (game.gridUpdating == false && !mergeUpgradeBlocking)
                {
                    waiting -= Time.deltaTime;

                    if (waiting <= 0f)
                    {
                        waiting = waitTime;
                        if (game.status.events.Count > 0)
                        {
                            Logic.StatusReport.Event _event = game.status.events[0];
                            game.status.events.RemoveAt(0);
                            Logic.Token token;
                            switch (_event.type)
                            {
                                case Logic.StatusReport.EventType.TokenWait:
                                    waiting *= tokenWaitMultiplier;
                                    break;
                                case Logic.StatusReport.EventType.TokenModelDestroyed:
                                    token = _event.tokens[0];
                                    Logic.Token toolTokenDestroyer = null;

                                    if (_event.tokens.Count >= 2)
                                    {
                                        toolTokenDestroyer = _event.tokens[1];
                                    }

                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                for (int i = 0; i < tile.token.token.data.num; i++)
                                                {
                                                    CreateFlower(tile, tile.token.token.data.color);
                                                }
                                                tile.token.Die(toolTokenDestroyer);
                                                tile.token = null;
                                                if (useHaptics)
                                                {
                                                    Haptics.PlayTransient(.5f, .5f);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.TokenChanged:
                                    //waiting = 0f;
                                    Logic.Token oldToken = _event.tokens[0];
                                    Logic.Token newToken = _event.tokens[1];
                                    Logic.Token toolTokenA = null;
                                    if (_event.tokens.Count >= 3)
                                    {
                                        toolTokenA = _event.tokens[2];
                                    }

                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token != null && tile.token.token == oldToken)
                                        {
                                            if (inTutorial && (tutorial.stage == TutorialStage.Placing
                                                || tutorial.stage == TutorialStage.Blue3
                                                || tutorial.stage == TutorialStage.Green2))
                                            {
                                                StartCoroutine(TutorialCombineStageAdvanceRoutine(
                                                    tile.token, newToken, toolTokenA, useHaptics, _event.num));
                                            }
                                            else
                                            {
                                                StartCoroutine(RunBlockingMergeUpgrade(
                                                    tile.token, newToken, toolTokenA, useHaptics, _event.num));
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.TokenAddedTo:
                                    //waiting = 0f;
                                    token = _event.tokens[0];
                                    Logic.Token toolToken = _event.tokens[2];
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                tile.token.UpgradeToken(_event.tokens[1], toolToken, useHaptics);
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.NewHand:
                                    if (game.isGameover()) { break; }
                                    if (inTutorial)
                                    {
                                        HandleTutorialNewHand();
                                    }
                                    else
                                    {
                                        CreateHand(true);
                                    }
                                    deckNumberDisplay.text = (game.bag.bag.Count + tempDeckNumberForAnim).ToString();
                                    break;
                                case Logic.StatusReport.EventType.BagRefill:
                                    tempDeckNumberForAnim = hand.Count;
                                    StartCoroutine(BagRefillAnim());
                                    deckNumberDisplay.text = (game.bag.bag.Count + tempDeckNumberForAnim).ToString();
                                    break;
                                case Logic.StatusReport.EventType.ScoreAdded:
                                    //scoreDelta += _event.num;
                                    waiting = waitTime * 0.1f;
                                    break;
                                case Logic.StatusReport.EventType.BagUpdated:
                                    deckDisplay.bagUpdated = true;
                                    if (inTutorial == false)
                                    {
                                        bool madePopup = upgradePopup.Create(_event.contents);
                                        if (madePopup)
                                        {
                                            popupopen = true;
                                            EnterInputState(InputState.Popup);
                                        }
                                    }

                                    waiting = waitTime * 0.01f;
                                    if (popupopen)
                                        break;
                                    break;
                            }
                        }
                        else
                        {
                            if (game.isGameover())
                            {
                                TryStartFinish();
                                break;
                            }

                            if (popupopen)
                            {
                                EnterInputState(InputState.Popup);
                            }
                            else if (game.hand.handSize == 1)
                            {
                                chosenIndex = 0;
                                EnterInputState(InputState.Place);
                            }
                            else
                            {
                                EnterInputState(InputState.Choose);
                            }

                            Save();
                            waiting = waitTime * 0.01f;
                            TryFlushDyingTokens();

                        }
                    }

                }
                break;
            case InputState.Popup:
                if (InputHelper.GetAnyPressBegan() && !dismissingPopup && upgradePopup != null && !upgradePopup.IsAnimatingClose)
                {
                    StartCoroutine(DismissUpgradePopupRoutine());
                }
                break;
            case InputState.Snapshot:
                if (InputHelper.GetAnyPressBegan())
                {
                    StartCoroutine(polaroidDisplay.HideSnapshotRoutine());
                    EnterInputState(InputState.Choose);
                }
                break;
        }
        //draw
        //draw grid
        foreach (Tile tile in tiles.Values)
        {
            if (inputState == InputState.Place)
            {
                tile.Draw(tile.tile.pos == chosenPos);
            }
            else
            {
                tile.Draw(false);
            }
            if (tile.token && inputState != InputState.Finish && inputState != InputState.TapToRestart)
            {
                tile.token.Draw(tile.transform.position);
            }
        }
        //free slot
        if (inputState == InputState.Place && chosenIndex < game.hand.handSize)
        {
            freeSlot.Draw(chosenPos == freeSlotChoice);
        }
        else
        {
            freeSlot.Draw(false);
        }
        if (freeSlot.token)
        {
            bool shouldHover = false;
            if (!freeSlot.token.IsPlacementAnimating)
            {
                freeSlot.token.lifted = false;
            }
            if (inputState == InputState.Place)
            {
                if (chosenIndex >= game.hand.handSize)
                {
                    shouldHover = true;
                    if (!draggingTile)
                    {
                        freeSlot.token.lifted = true;
                    }
                }
            }
            if (shouldHover)
            {
                if (draggingTile)
                {
                    freeSlot.token.Draw(GetDragDrawPosition(), true, Token.DragDrawFollowSpeed);
                }
                else
                {
                    freeSlot.token.Draw(freeSlot.transform.position + (shouldHover ? Vector3.up * 0.5f : Vector3.zero), shouldHover);
                }
            }
            else
            {
                freeSlot.token.Draw(freeSlot.transform.position + (shouldHover ? Vector3.up * 0.5f : Vector3.zero), shouldHover);
            }


        }
        //draw tokens
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            hand[i].lifted = false;
            Vector2 pos = handTransforms[i].position;// firstHandPos + (i * handSeparation);
            if (game.hand.handSize == 1)
            {
                pos.x = 0;
            }
            if (i == chosenIndex)
            {
                if (inputState == InputState.Choose)
                {
                    hand[i].Draw(pos, true);
                }
                else if (inputState == InputState.Place)
                {
                    hand[i].lifted = !draggingTile;
                    if (draggingTile)
                    {
                        hand[i].Draw(GetDragDrawPosition(), true, Token.DragDrawFollowSpeed);
                    }
                    else
                    {
                        hand[i].Draw(pos, true);
                    }

                }
            }
            else
            {
                hand[i].Draw(pos);
            }


        }
        bool updatedNumber = true;
        if (inputState == InputState.Wait)
        {
            foreach (Logic.StatusReport.Event _event in game.status.events)
            {
                if (_event.type == Logic.StatusReport.EventType.NewHand)
                {
                    updatedNumber = false;
                }
            }
        }
        if (updatedNumber)
        {
            deckNumberDisplay.text = (game.bag.bag.Count + tempDeckNumberForAnim).ToString();
        }

        switch (inputState)
        {
            case InputState.Choose:

                break;
        }
        InputHelper.EndFrame();
    }
    IEnumerator BagRefillAnim()
    {
        float originalScale = bagButtonTransform.localScale.x;
        float scale = originalScale;
        while (scale > originalScale * 0.8f)
        {
            scale += ((originalScale * 0.75f) - scale) * 0.2f;
            bagButtonTransform.localScale = Vector3.one * scale;
            yield return new WaitForEndOfFrame();
        }
        while (scale < originalScale * 0.95f)
        {
            scale += ((originalScale) - scale) * 0.2f;
            bagButtonTransform.localScale = Vector3.one * scale;
            yield return new WaitForEndOfFrame();
        }
        bagButtonTransform.localScale = Vector3.one * originalScale;
    }
    public int ScoreToken(Logic.TokenData _token)
    {
        int _score = _token.num;
        int mult = ((TripleGame)game).colorScoreMulti[_token.color];
        return _score * mult;
    }
    public Token CreateClippingToken(Logic.Token _token)
    {
        Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
        token.Init(_token);
        token.UpdateLayer("TokenHand");
        return token;

    }
    public void CreateFlower(Vector2 pos, TokenColor tokenColor, bool loaded = false)
    {
        CreateFlower(tiles[new Vector2Int((int)pos.x, (int)pos.y)], tokenColor, loaded);
    }
    public void CreateFlower(Tile tile, TokenColor tokenColor, bool loaded = false, bool finished = false)
    {
        bool deactivateScene = false;
        if (!stateScreens[(int)GameState.Gameplay].gameObject.activeSelf)
        {
            deactivateScene = true;
            stateScreens[(int)GameState.Gameplay].gameObject.SetActive(true);
        }

        Rect total = new Rect(-0.5f, -0.45f, 0.82f, 1.3f);
        total = new Rect(-gridSeparation.x * 0.5f, -gridSeparation.y * 0.5f, gridSeparation.x, gridSeparation.y);
        float x_dif = 0.1f;
        float y_dif = 0.1f;
        Rect notAllowed = new Rect(total.x + x_dif, total.y + y_dif, total.width - (x_dif), total.height - (y_dif * 2f));
        if (finished)
        {
            total = notAllowed;
            notAllowed = new Rect(0, 0, 0, 0);
        }
        total.y -= 0.2f;
        notAllowed.y -= 0.2f;
        Vector2 extents;
        int tries = 0;
        do
        {
            tries++;
            extents = new Vector2(UnityEngine.Random.Range(total.x, total.x + total.height), UnityEngine.Random.Range(total.y, total.y + total.height));
            if (tries > 20)
            {
                GameLog.Log("gave up");
                break;
            }
        } while (notAllowed.Contains(extents));
        Flower flower = GameObject.Instantiate(flowerPrefabs[(int)tokenColor], tile.transform.position + (Vector3)extents, Quaternion.identity, gridTransform).GetComponent<Flower>();
        flower.tokenColor = tokenColor;
        flower.x = tile.tile.pos.x;
        if (loaded)
        {
            flower.Finish();
        }
        flower.animSpeed *= UnityEngine.Random.Range(0.9f, 1.1f);
        if (finished)
        {
            flower.animSpeed *= 1.5f;
        }
        bool keepFlower = true;
        if (flowers.ContainsKey(tile) == false)
        {
            flowers.Add(tile, new List<Flower>());
        }
        else
        {
            bool farEnough = true;
            int count = 0;
            do
            {
                count++;
                if (count > 20)
                {
                    GameLog.Log("gave up for this reason");
                    //todo: boost the flower you hit last
                    keepFlower = false;
                    break;
                }
                foreach (Flower f in flowers[tile])
                {
                    if (Vector2.Distance(f.transform.position, flower.transform.position) < 0.02f)
                    {
                        farEnough = false;
                        break;
                    }
                }
                if (farEnough == false)
                {
                    tries = 0;
                    do
                    {
                        tries++;
                        extents = new Vector2(UnityEngine.Random.Range(total.x, total.x + total.height), UnityEngine.Random.Range(total.y, total.y + total.height));
                        if (tries > 20)
                        {
                            GameLog.Log("gave up inside");
                            break;
                        }
                    } while (notAllowed.Contains(extents));
                    flower.transform.position = tile.transform.position + (Vector3)extents;
                }

            } while (farEnough == false);
        }

        Vector3 flowerPos = flower.transform.position + (Vector3.forward * -10);
        RaycastHit hit;
        Physics.Raycast(flowerPos, Vector3.forward, out hit);
        flower.ChangeLayer(hit.collider != null);

        numFlowers++;
        if (keepFlower)
        {
            flowers[tile].Add(flower);

        }
        else
        {
            GameObject.Destroy(flower.gameObject);
        }

        if (deactivateScene)
        {
            stateScreens[(int)GameState.Gameplay].gameObject.SetActive(false);
        }
    }
    public void Snapshot()
    {
        if (Services.Gems.CanAfford("takeSnapshot") == false)
        {
            return;
        }
        Services.Gems.SpendGems("takeSnapshot");
        Services.AudioManager.PlaySnapshotSound();
        SaveLoad.Save(1, currentSave);
        snapshotSave = currentSave;
        StartCoroutine(polaroidDisplay.ShowSnapshotRoutine(game.grid.tiles));
        inputState = InputState.Snapshot;
        GameStateGameplay();
    }
    public void LoadSnapshot()
    {
        if (Services.Gems.CanAfford("newGame") == false)
        {
            Services.Gems.TooExpensive();
            return;
        }
        Services.Gems.SpendGems("newGame");
        Logic.History.Turn _save = null;
        if (SaveLoad.HasSave(1))
        {
            //gameState = GameState.Gameplay;
            _save = SaveLoad.Load(1);
            //gameState = GameState.Snapshot;
            snapshotSave = _save;
        }
        if (snapshotSave == null) { return; }
        game.LoadTurn(snapshotSave);
        score = game.score;
        scoreDelta = 0;
        dyingTokens.Clear();
        CreateHand();
        ClearTokensFromGrid();
        LoadTokensIntoGrid();
        GameStateGameplay();
    }
    public void Save()
    {
        if (IsTutorialSession)
        {
            return;
        }
        Logic.History.Turn save = new Logic.History.Turn(game);
        currentSave = save;
        SaveLoad.Save(0, currentSave);
        for (int i = 0; i < difficultyUnlocked.Count; i++)
        {
            if (difficultyUnlocked[i]) { continue; }
            if (difficulty != i - 1) { continue; }//only unlock on previous
            if (score >= scoreNeededToUnlock[i])
            {
                difficultyUnlocked[i] = true;
            }
        }
        String unlock = "";
        for (int i = 0; i < difficultyUnlocked.Count; i++)
        {
            if (difficultyUnlocked[i])
            {
                unlock = unlock + "1";
            }
            else
            {
                unlock = unlock + "0";
            }
        }
        PlayerPrefs.SetString("difficultyUnlock", unlock);
        PlayerPrefs.Save();
    }

    public void Mulligan()
    {
        if (inTutorial) { return; }
        //put back rest of hand and draw 4 more
        game.Mulligan();
        StartCoroutine(MulliganAnim());
    }

    public IEnumerator MulliganAnim()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            hand[i].PlaceInBag();
            yield return new WaitForSeconds(mulliganStaggerSeconds);
            hand[i] = null;
        }
        yield return new WaitForSeconds(mulliganPauseBeforeDraw);

        CreateHand(true);
        Services.AudioManager.PlayUndoSound();
        if (!inTutorial)
        {
            Save();
        }
    }


    public void Undo()
    {
        if (game.isGameover()) { return; }
        if (inTutorial && (tutorial.stage == TutorialStage.Undo))
        {
            tutorial.RequestStageUpdate();
        }
        if (inTutorial && tutorial.stage != TutorialStage.Undo)
        {
            return;
        }
        //if (inputState != InputState.Choose) { return; }
        if (inputState != InputState.Choose)
        {
            EnterInputState(InputState.Choose);
        }
        if (game.history.turns.Count > 1)
        {
            bool reserveTileFull = !game.IsFreeSlotFree();
            game.Undo();
            if (game.history.turns.Count > 2)
            {
                if (reserveTileFull && game.IsFreeSlotFree())
                {
                    game.Undo();
                }
            }
            score = game.score;
            scoreDelta = 0;
            dyingTokens.Clear();
            CreateHand(false, true);
            ClearTokensFromGrid(true);
            LoadTokensIntoGrid(true);
            undidTiles.Clear();
            undidSlot = false;
            Save();

        }
    }
    public void Restart()
    {
        SaveLoad.DeleteSave(0);
        Services.AudioManager.StopMusic();
        Services.AudioManager.StopScoreLoop();
        SceneManager.LoadScene(0);
    }
    public void RestartWithTutorial()
    {
        PlayerPrefs.DeleteKey("tutorialComplete");
        PlayerPrefs.DeleteKey("greenLearnt");
        PlayerPrefs.DeleteKey("purpleLearnt");
        Restart();
    }
    public void CompleteRestart()
    {
        PlayerPrefs.DeleteAll();
        Restart();
    }

    public void DebugNearEndGame()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsTutorialSession) { return; }
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }

        game.SetupNearEndGameDebug();
        score = game.score;
        scoreDelta = 0;
        dyingTokens.Clear();
        CreateHand();
        ClearTokensFromGrid();
        LoadTokensIntoGrid();
        EnterInputState(InputState.Choose);
        GameStateGameplay();
        Save();
        GameLog.Log("Debug: near end-game board loaded.");
#endif
    }

}
