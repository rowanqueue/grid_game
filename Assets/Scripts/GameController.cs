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
    Bag,
    SelectDifficulty,
    Snapshot
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
    public float waiting = 0f;
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

    public int finishCount = 0;
    Vector2Int finishTokenPos;
    public GameObject winScreen;
    public TextMeshPro winScreenScore;

    public bool diceMode = false;
    public bool useHaptics = false;

    public Transform bagButtonTransform;
    public bool newGame = false;
    public List<Token> dyingTokens = new List<Token>();

    public GameObject loadSnapshotButton;
    public List<Tile> tokensToDestroy = new List<Tile>();


    // Start is called before the first frame update
    private List<Vector2Int> undidTiles = new List<Vector2Int>();
    private bool undidSlot = false;
    void Awake()
    {
        InitializeServices();
#if UNITY_ANDROID
        Handheld.Vibrate();
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
        Application.targetFrameRate = 60;
        winScreen.SetActive(false);
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
                cameraPos.y = 12.33f;
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
            tutorial.StartTutorial();
        }
        else
        {
            tutorial.ExitTutorial();
        }
        Camera.main.transform.position = cameraPos;

        //simInput = new TextInput.SimInput(game);
        flowers = new Dictionary<Tile, List<Flower>>();
        CreateGrid();
        hand = new List<Token>();
        newGame = false;
        if (inTutorial)
        {

            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
        }

        deckDisplay.game = game;

        if (inTutorial == false)
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
        if (newGame == false)
        {
            CreateHand();
        }
        else if (inTutorial == false)
        {
            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
            hand.Add(null);
        }
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
            Debug.Log("can't afford a new game");
        }

    }
    public void GameStateGameplay()
    {
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
            if (newGame)
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
        if (gameState == GameState.SelectDifficulty)
        {
            gameJson = difficulties[difficulty];
            Json.Root root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
            game.Initialize(root);
            CreateHand(true);
        }
        /*if (inTutorial && tutorial.stage == TutorialStage.GreenNextBag)
        {
            tutorial.IncrementStage();
        }*/
        if (gameState == GameState.Gameplay) { return; }
        lastState = gameState;
        gameState = GameState.Gameplay;
        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
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
        lastState = gameState;
        gameState = GameState.ToolShop;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        //todo: make toolshop open
        movingToScreen = true;
    }
    public void GameStateSeeds()
    {
        if (inputState == InputState.Finish || inputState == InputState.TapToRestart) { return; }
        if (inTutorial) { return; }
        lastState = gameState;
        gameState = GameState.Seeds;

        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        snapshotPreview.openScreen();
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
            if (undo)
            {
                bool animate = undidTiles.Contains(p);
                print("ANIMATE" + animate);
                tile.tile = _tile;
                if (tile.tile.token != null)
                {
                    Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                    token.token = tile.tile.token;
                    tile.token = token;
                    token.PlaceInTile(tile, animate);
                }
            }
            else
            {
                tile.tile = _tile;
                if (tile.tile.token != null)
                {
                    Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                    token.token = tile.tile.token;
                    tile.token = token;
                    token.PlaceInTile(tile);
                }
            }
        }
        if (undo)
        {
            if (game.freeSlot != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                token.token = game.freeSlot;
                freeSlot.token = token;
                token.PlaceInTile(freeSlot, undidSlot);
            }
        }
        else
        {
            if (game.freeSlot != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                token.token = game.freeSlot;
                freeSlot.token = token;
                token.PlaceInTile(freeSlot);
            }
        }
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
                finishCount = 0;
                finishTokenPos = new Vector2Int(0, 4);
                break;
            case InputState.Wait:
                dyingTokens.Clear();
                break;
        }
        inputState = newState;

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
    void DeathCheck()
    {
        bool emptyTile = false;
        if (inputState == InputState.Choose)
        {
            foreach (Tile tile in tiles.Values)
            {
                if (tile.token == null)
                {
                    emptyTile = true;
                    break;
                }
            }
            if (emptyTile == false)
            {
                EnterInputState(InputState.Finish);
            }
        }
    }
    void WaitToFinish()
    {
        StartCoroutine(Finishing());

    }
    IEnumerator Finishing()
    {
        for (int i = dyingTokens.Count - 1; i >= 0; i--)
        {
            dyingTokens[i].StartKillNumber();
        }
        dyingTokens.Clear();
        yield return new WaitForSeconds(1.2f);
        SaveLoad.DeleteSave(0);
        winScreen.SetActive(true);
        EnterInputState(InputState.TapToRestart);


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
            difficultyName.text += "\nLocked! Earn " + scoreNeededToUnlock[difficulty].ToString() + " to unlock";
        }
        difficultyButtons[0].disabled = difficulty == 0;
        difficultyButtons[1].disabled = difficulty == difficulties.Count - 1;
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
                case GameState.Start:
                    cameraPos.y = 12.33f;
                    break;
                case GameState.ToolShop:
                    cameraPos.x = -8;
                    break;
                case GameState.Bag:
                    cameraPos.y = -8;
                    break;
            }
            float cameraSpeed = 0.125f;
            if (gameState == GameState.Bag)
            {
                cameraSpeed *= 1.5f;
            }

            Camera.main.transform.position += (cameraPos - Camera.main.transform.position) * cameraSpeed; ;
            if (Vector2.Distance(Camera.main.transform.position, cameraPos) < 0.03f)
            {
                movingToScreen = false;
                Camera.main.transform.position = cameraPos;
                if (gameState != GameState.Bag && gameState != GameState.SelectDifficulty)
                {
                    for (int i = 0; i < stateScreens.Count; i++)
                    {
                        if ((int)gameState != i)
                        {
                            stateScreens[i].gameObject.SetActive(false);
                        }
                    }
                }

            }
        }


        if (game.isGameover())
        {
            //Debug.Log("game over");
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
            if(scoreDelta <= 0)
            {
                FinishScoreRolling();
            }
        }
        display.text += "\n<size=20%>-score-</size>";
        winScreenScore.text = "<size=35%>Your score:</size>\n" + score.ToString() + "\n<size=15%><line-height=100%>-Tap to restart-</size>"; ;
        bagDisplay.text = game.bag.ToString();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (gameState == GameState.Bag)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (mousePos.y >= -13.0f)
                {
                    deckDisplay.ClearBagAfterDelay();
                    GameStateGameplay();
                }
            }
        }
        if (inputState != InputState.Wait && inputState != InputState.Finish)
        {
            if (dyingTokens.Count > 0)
            {
                bool readyToTrigger = true;
                foreach (Token t in dyingTokens)
                {
                    if (t.waitingToDie == false)
                    {
                        readyToTrigger = false;
                    }
                }
                if (readyToTrigger)
                {
                    for (int i = dyingTokens.Count - 1; i >= 0; i--)
                    {
                        dyingTokens[i].StartKillNumber();
                    }
                    dyingTokens.Clear();
                }
            }
        }
        //input
        switch (inputState)
        {
            case InputState.TapToRestart:
                if (Input.anyKeyDown)
                {
                    Services.AudioManager.StopMusic();
                    SceneManager.LoadScene(0);
                }
                break;
            case InputState.Finish:
                waiting -= Time.deltaTime;
                if (waiting <= 0f)
                {
                    waiting = waitTime;
                    finishCount++;
                    if (tiles[finishTokenPos].token != null)
                    {
                        //next pos
                        for (int i = 0; i < tiles[finishTokenPos].token.token.data.num * 10; i++)
                        {
                            CreateFlower(tiles[finishTokenPos], tiles[finishTokenPos].token.token.data.color, false, true);
                        }
                        //score += tiles[finishTokenPos].token.token.data.num * ((TripleGame)game).colorScoreMulti[tiles[finishTokenPos].token.token.data.color];

                        tiles[finishTokenPos].token.Die();
                        tiles[finishTokenPos].token = null;
                    }
                    finishTokenPos.x += 1;
                    if (finishTokenPos.x > 4)
                    {
                        finishTokenPos.x = 0;
                        finishTokenPos.y -= 1;
                    }
                    if (finishTokenPos.y < 0)
                    {
                        WaitToFinish();
                        EnterInputState(InputState.Choose);
                    }
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
                if (Input.GetMouseButtonDown(0))
                {
                    bool tutorialCanChoose = false;
                    if (inTutorial)
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
                if (Input.GetMouseButtonDown(0))
                {
                    chosenPos = Vector2Int.one * -5;
                    foreach (Tile tile in tiles.Values)
                    {
                        float d = Vector2.Distance(mousePos, tile.transform.position);
                        if (d < 0.5f)
                        {
                            chosenPos = tile.tile.pos;
                            break;
                        }
                    }
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
                if (holdingClick)
                {
                    clickHoldDuration += Time.deltaTime;
                    if (clickHoldDuration > 0.15f)
                    {
                        draggingTile = true;
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        holdingClick = false;
                    }
                }
                holdingClipper = chosenToken.token.data.color == Logic.TokenColor.Clipper;
                holdingSpade = chosenToken.token.data.color == Logic.TokenColor.Spade;
                holdingAdder = chosenToken.token.data.color == Logic.TokenColor.Adder;
                chosenPos = Vector2Int.one * -5;
                foreach (Tile tile in tiles.Values)
                {
                    float d = Vector2.Distance(mousePos, tile.transform.position);
                    if (d < 0.5f)
                    {
                        chosenPos = tile.tile.pos;

                        break;
                    }
                }
                float dist = Vector2.Distance(mousePos, freeSlot.transform.position);
                if (dist < 0.5f)
                {
                    chosenPos = freeSlotChoice;
                }
                //let go
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z))
                {
                    EnterInputState(InputState.Choose);
                }
                else if (Input.GetMouseButtonDown(0) || (draggingTile && Input.GetMouseButtonUp(0)))
                {//clicking
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
                            game.PlaceTokenInFreeSlot(chosenIndex);
                            freeSlot.token = hand[chosenIndex];
                            hand[chosenIndex].UpdateLayer("TokenMoving");
                            hand[chosenIndex] = null;
                            if (useHaptics)
                            {
                                Haptics.PlayTransient(.5f, .5f);
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            if (inTutorial && (tutorial.stage == TutorialStage.FreeSlot || tutorial.stage == TutorialStage.CleanUp))
                            {
                                tutorial.IncrementStage();
                            }
                            if (inTutorial && (tutorial.stage == TutorialStage.Blue3))
                            {
                                tutorial.StageUpdate();
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
                            if (chosenIndex >= game.hand.handSize)
                            {
                                //freeSlot
                                tiles[chosenPos].token = freeSlot.token;
                                lastTokenPlaced = freeSlot.token;
                                lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                freeSlot.token.UpdateLayer("TokenMoving");
                                freeSlot.token = null;

                            }
                            else
                            {
                                tiles[chosenPos].token = hand[chosenIndex];
                                hand[chosenIndex].UpdateLayer("TokenMoving");
                                lastTokenPlaced = hand[chosenIndex];
                                lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                hand[chosenIndex] = null;
                            }
                            if (inTutorial && (tutorial.stage == TutorialStage.Placing || tutorial.stage == TutorialStage.WeirdSet || tutorial.stage == TutorialStage.FirstRed || tutorial.stage == TutorialStage.Blue3 || tutorial.stage == TutorialStage.SecondRed || tutorial.stage == TutorialStage.LearnGreen || tutorial.stage == TutorialStage.CleanUp || tutorial.stage == TutorialStage.Purple))
                            {
                                tutorial.StageUpdate();
                            }
                            if(inTutorial && (tutorial.stage == TutorialStage.Undo && tutorial.stagePhase == 1))
                            {
                                tutorial.IncrementStage();
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
                                    freeSlot.token.UpdateLayer("TokenMoving");
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
                                    hand[chosenIndex].UpdateLayer("TokenMoving");
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
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("up");
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
                if (Input.GetMouseButtonDown(0))
                {
                    chosenPos = Vector2Int.one * -5;
                    foreach (Tile tile in tiles.Values)
                    {
                        float d = Vector2.Distance(mousePos, tile.transform.position);
                        if (d < 0.5f)
                        {
                            chosenPos = tile.tile.pos;
                            break;
                        }
                    }
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
                if (holdingClick == false)
                {
                    if (draggingTile)
                    {
                        EnterInputState(InputState.Choose);
                    }
                    draggingTile = false;
                }
                break;
            case InputState.Wait:
                if (game.gridUpdating == false)
                {
                    waiting -= Time.deltaTime;
                    
                    if (inTutorial)
                    {
                        bool waitingStage = false;
                        switch (tutorial.stage)
                        {
                            case TutorialStage.Blue2:
                                waitingStage = true;
                                break;
                        }
                        if (waitingStage)
                        {
                            waiting += Time.deltaTime;
                        }
                        
                    }
                    if (popupopen)
                    {
                        waiting += Time.deltaTime;
                        if (Input.anyKeyDown)
                        {
                            upgradePopup.Close();
                            popupopen = false;
                        }
                    }
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
                                    waiting *= 1.5f;
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
                                    token = _event.tokens[0];
                                    Logic.Token toolTokenA = null;
                                    if (_event.tokens.Count >= 3)
                                    {
                                        toolTokenA = _event.tokens[2];
                                    }

                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                tile.token.UpgradeToken(_event.tokens[1], toolTokenA, useHaptics);
                                            }
                                        }
                                    }
                                    if (inTutorial && (tutorial.stage == TutorialStage.Placing || tutorial.stage == TutorialStage.Green2))
                                    {
                                        tutorial.IncrementStage();
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
                                    if (inTutorial)
                                    {
                                        if (tutorial.stage == TutorialStage.HandRefill || tutorial.stage == TutorialStage.FirstRed)
                                        {
                                            game.SecondTutorialHand();
                                        }
                                        if(tutorial.stage == TutorialStage.Blue3)
                                        {
                                            game.ThirdTutorialHand();
                                        }
                                        if(tutorial.stage == TutorialStage.LearnGreen || tutorial.stage == TutorialStage.Green2)
                                        {
                                            game.FourthTutorialHand();
                                        }
                                        if(tutorial.stage == TutorialStage.CleanUp || tutorial.stage == TutorialStage.Purple)
                                        {
                                            game.FifthTutorialHand();
                                        }
                                        /*
                                        if (tutorial.stage == TutorialStage.BagIntro || tutorial.stage == TutorialStage.Red2)
                                        {
                                            game.SecondTutorialHand();
                                        }
                                        if (tutorial.stage == TutorialStage.TeachMulligan)
                                        {
                                            game.FourthTutorialHand();
                                        }*/
                                    }

                                    CreateHand(true);
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
                                        }
                                    }

                                    waiting = waitTime * 0.01f;
                                    break;
                            }
                        }
                        else
                        {
                            if (game.hand.handSize == 1)
                            {
                                chosenIndex = 0;
                                EnterInputState(InputState.Place);
                            }
                            else
                            {
                                EnterInputState(InputState.Choose);
                            }

                            //save
                            Save();
                            waiting = waitTime * 0.01f;
                            /*score += scoreDelta;
                            scoreDelta = 0;*/
                            /*if (popupopen)
                            {
                                EnterInputState(InputState.Popup);
                            }
                            else
                            {
                                EnterInputState(InputState.Choose);
                            }*/
                            if (dyingTokens.Count == 0)
                            {

                            }
                            else
                            {
                                bool readyToTrigger = true;
                                foreach (Token t in dyingTokens)
                                {
                                    if (t.waitingToDie == false)
                                    {
                                        readyToTrigger = false;
                                    }
                                }
                                if (readyToTrigger)
                                {
                                    for (int i = dyingTokens.Count - 1; i >= 0; i--)
                                    {
                                        dyingTokens[i].StartKillNumber();
                                    }
                                    dyingTokens.Clear();

                                }
                            }

                        }
                    }

                }
                break;
            case InputState.Popup:
                if (Input.anyKeyDown)
                {
                    upgradePopup.Close();
                    popupopen = false;
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
            if (tile.token)
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
            freeSlot.token.lifted = false;
            if (inputState == InputState.Place)
            {
                if (chosenIndex >= game.hand.handSize)
                {
                    shouldHover = true;
                    freeSlot.token.lifted = true;
                }
            }
            if (shouldHover)
            {
                if (draggingTile)
                {
                    freeSlot.token.Draw(mousePos + Vector2.up * 0.5f, true);
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
                    hand[i].lifted = true;
                    if (draggingTile)
                    {
                        hand[i].Draw(mousePos, true);
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
                Debug.Log("gave up");
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
                    Debug.Log("gave up for this reason");
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
                            Debug.Log("gave up inside");
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
        Services.AudioManager.PlaySnapshotSound();
        SaveLoad.Save(1, currentSave);
        snapshotSave = currentSave;
        GameStateGameplay();
    }
    public void LoadSnapshot()
    {
        if (Services.Gems.CanAfford("snapshot") == false)
        {
            Services.Gems.TooExpensive();
            return;
        }
        Services.Gems.SpendGems("snapshot");
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
        Logic.History.Turn save = new Logic.History.Turn(game);
        currentSave = save;
        SaveLoad.Save(0, currentSave);
        for (int i = 0; i < difficultyUnlocked.Count; i++)
        {
            if (difficultyUnlocked[i]) { continue; }
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
        //put back rest of hand and draw 4 more
        if (!inTutorial)
        {
            game.Mulligan();
        }

        StartCoroutine(MulliganAnim());
    }

    public IEnumerator MulliganAnim()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            hand[i].PlaceInBag();
            yield return new WaitForSeconds(0.1f);
            hand[i] = null;
        }
        yield return new WaitForSeconds(0.5f);

        CreateHand(true);
        Services.AudioManager.PlayUndoSound();
    }


    public void Undo()
    {
        if (inTutorial && (tutorial.stage == TutorialStage.Undo))
        {
            tutorial.StageUpdate();
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

}
