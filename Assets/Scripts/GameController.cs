using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Logic;
using Save;
using MoreMountains.NiceVibrations;

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
    Snapshot,
    Bag,
    SelectDifficulty
}
public class GameController : MonoBehaviour
{
    public TextAsset gameJson;
    public int difficulty = 0;
    public List<TextAsset> difficulties = new List<TextAsset>();
    public List<string> difficultyNames = new List<string>();
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
    public List<Screen> stateScreens = new List<Screen>();
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
    public Dictionary<Vector2Int,Tile> tiles = new Dictionary<Vector2Int,Tile>();
    public Tile freeSlot;
    public GameObject freeSlotVisual;
    public Vector2Int chosenPos = Vector2Int.left;
    public float waitTime = 0.1f;
    float waiting = 0f;
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
    // Start is called before the first frame update
    void Awake()
    {
        InitializeServices();
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
        if(PlayerPrefs.HasKey("diceMode") == false)
        {
            PlayerPrefs.SetInt("diceMode",diceMode ? 1 : 0);
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
        if(PlayerPrefs.HasKey("difficulty") == false)
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

        foreach (Screen screen in stateScreens)
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
        
        if(inTutorial == false)
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
        if (newGame ==false)
        {
            CreateHand();
        }
        else if(inTutorial ==false)
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
        foreach(Logic.Tile _tile in game.grid.tiles.Values)
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
    public void GameStateGameplay()
    {
        if(gameState == GameState.Start)
        {
            if (newGame)
            {
                //if new game
                GameStateSelectDifficulty();
                return;
            }
            
        }
        difficultyParent.SetActive(false);
        if(gameState == GameState.SelectDifficulty)
        {
            gameJson = difficulties[difficulty];
            Json.Root root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
            game.Initialize(root);
            CreateHand(true);
        }
        if (inTutorial && tutorial.stage == TutorialStage.GreenNextBag)
        {
            tutorial.IncrementStage();
        }
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
        lastState = gameState;
        gameState = GameState.Settings;
        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void GameStateSnapshot()
    {
        lastState = gameState;
        gameState = GameState.Snapshot;
        
        stateScreens[(int)gameState].gameObject.SetActive(true);
        stateScreens[(int)gameState].SetAnchor();
        snapshotPreview.openScreen();
        movingToScreen = true;
    }
    public void GameStateBag()
    {
        if(gameState != GameState.Gameplay) { return; } 
        lastState = gameState;
        gameState = GameState.Bag;
        //stateScreens[(int)gameState].gameObject.SetActive(true);
        //stateScreens[(int)gameState].SetAnchor();
        movingToScreen = true;
    }
    public void ToggleDiceMode()
    {
        diceMode = !diceMode;
        PlayerPrefs.SetInt("diceMode",diceMode ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void ToggleHaptics()
    {
        useHaptics = !useHaptics;
        PlayerPrefs.SetInt("useHaptics", useHaptics ? 1 : 0);
        PlayerPrefs.Save();
    }
    void ClearTokensFromGrid()
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile.token)
            {
                GameObject.Destroy(tile.token.gameObject);
                tile.token = null;
            }
        }
        if (freeSlot.token)
        {
            GameObject.Destroy(freeSlot.token.gameObject);
            freeSlot.token = null;
        }
    }
    public void ToggleBagDisplay()
    {
        if(inTutorial && tutorial.stage == TutorialStage.BagIntro)
        {
            tutorial.IncrementStage();
        }
        
        if (movingToScreen) { return; }
        if(gameState == GameState.Bag)
        {
            deckDisplay.ClearBagAfterDelay();
            Services.AudioManager.PlayBagSound();
            GameStateGameplay();
        }
        else
        {
            if (inTutorial && tutorial.stage == TutorialStage.GreenStart)
            {
                tutorial.IncrementStage();
            }
            deckDisplay.MakeBag();
            GameStateBag();
        }
        
        //deckDisplay.gameObject.SetActive(!deckDisplay.gameObject.activeSelf);
        Services.AudioManager.PlayBagSound();
    }
    void LoadTokensIntoGrid()
    {
        foreach(Tile tile in tiles.Values)
        {
            if(tile.tile.token != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                token.token = tile.tile.token;
                tile.token = token;
                token.PlaceInTile(tile);
            }
        }
        if (game.freeSlot != null)
        {
            Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
            token.token = game.freeSlot;
            freeSlot.token = token;
            token.PlaceInTile(freeSlot);
        }
    }
    public void CreateHand(bool midgame = false)
    {
        for(int i = 0; i < game.hand.tokens.Length; i++)
        {
            if (game.hand.tokens[i] == null) {
                if(hand.Count < game.hand.tokens.Length)
                {
                    hand.Add(null);
                }
                else
                {
                    if (hand[i] != null)
                    {
                        GameObject.Destroy(hand[i].gameObject);
                    }
                }
                continue;
            }
            if(hand.Count < game.hand.tokens.Length || hand[i] == null)
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
                hand[i].token = game.hand.tokens[i];
            }
            hand[i].Init(hand[i].token);
            hand[i].UpdateLayer("TokenHand");
            float angle = Random.Range(2f, 5f);
            if(Random.value < 0.5f)
            {
                angle *= -1f;
            }
            hand[i].transform.localEulerAngles = new Vector3(0, 0, angle);
            if (midgame)
            {
                if(tempDeckNumberForAnim < hand.Count)
                {
                    tempDeckNumberForAnim += 1;
                }
                
                hand[i].handPos = handTransforms[i].position;
                hand[i].DrawFromBag(i);
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
                if(inputState == InputState.Place)
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
        }
        inputState = newState;

    }
    void DebugBagDisplay()
    {
        string s = "";
        Logic.Bag bag = game.bag;
        Dictionary<TokenData, int> numbers = new Dictionary<TokenData, int>();
        List<string> lines = new List<string>();
        foreach(TokenData token in bag.bagContents.Keys)
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
        foreach(TokenData token in numbers.Keys)
        {
            if(currentbag.ContainsKey(token) == false)
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
        if(inputState == InputState.Choose)
        {
            foreach(Tile tile in tiles.Values)
            {
                if(tile.token == null)
                {
                    emptyTile = true;
                    break;
                }
            }
            if(emptyTile == false)
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
        SaveLoad.DeleteSave(0);
        yield return new WaitForSeconds(0.75f);
        winScreen.SetActive(true);
        EnterInputState(InputState.TapToRestart);
        
        
    }
    public void HigherDifficulty()
    {
        difficulty++;
        difficulty = Mathf.Clamp(difficulty, 0, difficulties.Count-1);
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

    // Update is called once per frame
    void Update()
    {
        difficultyName.text = "Difficulty\n" + difficultyNames[difficulty];
        /*if (difficulties[difficulty] != gameJson)
        {
            difficultyName.text += "<size=50%>\nRestart to change difficulty";
        }*/
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
                case GameState.Settings:
                    cameraPos.x = 8;
                    break;
                case GameState.Start:
                    cameraPos.y = 12.33f;
                    break;
                case GameState.Snapshot:
                    cameraPos.x = -8;
                    break;
                case GameState.Bag:
                    cameraPos.y = -8;
                    break;
            }
            float cameraSpeed = 0.125f;
            if(gameState == GameState.Bag)
            {
                cameraSpeed *= 1.5f;
            }
            
            Camera.main.transform.position += (cameraPos - Camera.main.transform.position) * cameraSpeed; ;
            if (Vector2.Distance(Camera.main.transform.position,cameraPos) < 0.03f)
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
        display.text += "\n<size=20%>-score-</size>";
        winScreenScore.text = "<size=35%>Your score:</size>\n"+score.ToString()+"\n<size=15%>-Tap to restart-</size>"; ;
        bagDisplay.text = game.bag.ToString();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (gameState == GameState.Bag)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if(mousePos.y >= -13.0f)
                {
                    deckDisplay.ClearBagAfterDelay();
                    GameStateGameplay();
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
                        for (int i = 0; i < tiles[finishTokenPos].token.token.data.num*10; i++)
                        {
                            CreateFlower(tiles[finishTokenPos], tiles[finishTokenPos].token.token.data.color,false,true);
                        }
                        score += tiles[finishTokenPos].token.token.data.num * ((TripleGame)game).colorScoreMulti[tiles[finishTokenPos].token.token.data.color];

                        tiles[finishTokenPos].token.Die();
                        tiles[finishTokenPos].token = null;
                    }
                    finishTokenPos.x += 1;
                    if(finishTokenPos.x > 4)
                    {
                        finishTokenPos.x = 0;
                        finishTokenPos.y -= 1;
                    }
                    if(finishTokenPos.y < 0)
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
                    float d = Vector2.Distance(mousePos, pos);
                    if (d < 0.5f)
                    {
                        chosenIndex = i;
                        color = hand[i].token.data.color;
                        break;
                    }
                }
                //freeslot
                if(chosenIndex == -1 && freeSlot.token)
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
                    if(chosenIndex > -1 && tutorialCanChoose)
                    {
                        Services.AudioManager.PlayPickUpSound();
                        if (useHaptics)
                        {
                            MMVibrationManager.Haptic(HapticTypes.MediumImpact);
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
                            MMVibrationManager.Haptic(HapticTypes.MediumImpact);
                        }
                        Undo();
                    }
                }

                break;
            case InputState.Place:
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
                            MMVibrationManager.Haptic(HapticTypes.MediumImpact);
                        }
                        Undo();
                        break;
                    }
                }
                if (holdingClick)
                {
                    clickHoldDuration += Time.deltaTime;
                    if(clickHoldDuration > 0.15f)
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
                foreach(Tile tile in tiles.Values)
                {
                    float d = Vector2.Distance(mousePos, tile.transform.position);
                    if (d < 0.5f)
                    {
                        chosenPos = tile.tile.pos;
                        break;
                    }
                }
                float dist = Vector2.Distance(mousePos, freeSlot.transform.position);
                if(dist < 0.5f)
                {
                    chosenPos = freeSlotChoice;
                }
                //let go
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z))
                {
                    EnterInputState(InputState.Choose);
                }else if (Input.GetMouseButtonDown(0) || (draggingTile && Input.GetMouseButtonUp(0)))
                {//clicking
                    bool _tutorialGood = false;
                    if (inTutorial)
                    {
                        if((tutorial.stage == TutorialStage.FreeSlot || tutorial.stage == TutorialStage.EmptyHand))
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
                                MMVibrationManager.Haptic(HapticTypes.MediumImpact);
                            }
                            EnterInputState(InputState.Choose);
                            break;
                        }else if (game.IsFreeSlotFree() && _tutorialGood)
                        {
                            Services.AudioManager.PlayFreeSlotSound();
                            game.PlaceTokenInFreeSlot(chosenIndex);
                            freeSlot.token = hand[chosenIndex];
                            hand[chosenIndex].UpdateLayer("TokenMoving");
                            hand[chosenIndex] = null;
                            if (useHaptics)
                            {
                                MMVibrationManager.Haptic(HapticTypes.MediumImpact);
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            if(inTutorial && (tutorial.stage == TutorialStage.FreeSlot || tutorial.stage == TutorialStage.EmptyHand))
                            {
                                tutorial.IncrementStage();
                            }
                            break;
                        }

                    }
                    if(holdingClipper == false && holdingSpade == false && holdingAdder == false)
                    {
                        bool tutorialGood = false;
                        if (inTutorial)
                        {
                            if(tutorial.placingRule == false)
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
                                MMVibrationManager.Haptic(HapticTypes.MediumImpact);
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
                            if(inTutorial && (tutorial.stage == TutorialStage.Placing || tutorial.stage == TutorialStage.WeirdSet || tutorial.stage == TutorialStage.FirstRed || tutorial.stage == TutorialStage.EmptyHand))
                            {
                                tutorial.StageUpdate();
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                    }else if (holdingClipper || holdingAdder)
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
                                    MMVibrationManager.Haptic(HapticTypes.MediumImpact);
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
                                if(holdingAdder && emptyTile)
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
                    }else if (holdingSpade)
                    {
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data))
                        {
                            Services.AudioManager.PlayPlaceSound();
                            if (useHaptics)
                            {
                                MMVibrationManager.Haptic(HapticTypes.MediumImpact);
                            }
                            if (chosenIndex >= game.hand.handSize)
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                //GameObject.Destroy(freeSlot.token.gameObject);
                                tiles[chosenPos].token.ToolAnim(freeSlot.token,chosenIndex);
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
                    }
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
                    if(new_index != -1)
                    {
                        if (new_index == chosenIndex)
                        {
                            Services.AudioManager.PlayLetGoSound();
                            EnterInputState(InputState.Choose);
                        }
                        else
                        {
                            chosenIndex = new_index;
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
                                chosenIndex = game.hand.handSize + 2;
                                EnterInputState(InputState.Place);
                            }
                        }
                    }
                    
                }
                if(holdingClick == false)
                {
                    if (draggingTile)
                    {
                        EnterInputState(InputState.Choose);
                    }
                    draggingTile = false;
                }
                break;
            case InputState.Wait:
                if(game.gridUpdating == false)
                {
                    waiting -= Time.deltaTime;
                    if(inTutorial && tutorial.stage == TutorialStage.Blue3Appears)
                    {
                        waiting += Time.deltaTime;
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
                    if(waiting <= 0f)
                    {
                        waiting = waitTime;
                        if(game.status.events.Count > 0)
                        {
                            Logic.StatusReport.Event _event = game.status.events[0];
                            game.status.events.RemoveAt(0);
                            Logic.Token token;
                            switch (_event.type)
                            {
                                case Logic.StatusReport.EventType.TokenWait:
                                    waiting *= 1.5f;
                                    break;
                                case Logic.StatusReport.EventType.TokenDestroyed:
                                    token = _event.tokens[0];
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                for(int i = 0; i < token.data.num; i++)
                                                {
                                                    CreateFlower(tile, tile.token.token.data.color);
                                                }
                                                tile.token.Die();
                                                tile.token = null;
                                                if (useHaptics)
                                                {
                                                    MMVibrationManager.Haptic(HapticTypes.LightImpact);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.TokenChanged:
                                    //waiting = 0f;
                                    token = _event.tokens[0];
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                tile.token.UpgradeToken(_event.tokens[1]);
                                                Services.AudioManager.PlayUpgradeTileSound();
                                                if (useHaptics)
                                                {
                                                    MMVibrationManager.Haptic(HapticTypes.HeavyImpact);
                                                }
                                            }
                                        }
                                    }
                                    if (inTutorial && (tutorial.stage == TutorialStage.Placing || tutorial.stage == TutorialStage.WeirdSet || tutorial.stage == TutorialStage.Red2 || tutorial.stage == TutorialStage.ThirdBlue2))
                                    {
                                        tutorial.IncrementStage();
                                    }
                                    break;
                                case Logic.StatusReport.EventType.NewHand:
                                    if (inTutorial)
                                    {
                                        if(tutorial.stage == TutorialStage.HandRefill)
                                        {
                                            game.SecondTutorialHand();
                                        }
                                        if (tutorial.stage == TutorialStage.BagIntro || tutorial.stage == TutorialStage.Red2)
                                        {
                                            game.SecondTutorialHand();
                                        }
                                        if(tutorial.stage == TutorialStage.TeachMulligan)
                                        {
                                            game.FourthTutorialHand();
                                        }
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
                                    scoreDelta += _event.num;
                                    waiting = waitTime * 0.1f;
                                    break;
                                case Logic.StatusReport.EventType.BagUpdated:
                                    deckDisplay.bagUpdated = true;
                                    if(inTutorial == false)
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
                            EnterInputState(InputState.Choose);
                            //save
                            Save();
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
        foreach(Tile tile in tiles.Values)
        {
            if(inputState == InputState.Place)
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
        if(inputState == InputState.Place && chosenIndex < game.hand.handSize)
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
            if(inputState == InputState.Place)
            {
                if(chosenIndex >= game.hand.handSize)
                {
                    shouldHover = true;
                }
            }
            if (draggingTile)
            {
                freeSlot.token.Draw(mousePos + Vector2.up * 0.5f, true);
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
            if(i == chosenIndex)
            {
                if(inputState == InputState.Choose)
                {
                    hand[i].Draw(pos,true);
                }else if(inputState == InputState.Place)
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
        if(inputState == InputState.Wait)
        {
            foreach(Logic.StatusReport.Event _event in game.status.events)
            {
                if(_event.type == Logic.StatusReport.EventType.NewHand)
                {
                    updatedNumber = false;
                }
            }
        }
        if(updatedNumber)
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
        while(scale > originalScale * 0.8f)
        {
            scale += ((originalScale * 0.75f) - scale) * 0.2f;
            bagButtonTransform.localScale = Vector3.one * scale;
            yield return new WaitForEndOfFrame();
        }
        while (scale < originalScale*0.95f)
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
        return _score*mult;
    }
    public Token CreateClippingToken(Logic.Token _token)
    {
        Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
        token.Init(_token);
        token.UpdateLayer("TokenHand");
        return token;
        
    }
    public void CreateFlower(Vector2 pos,TokenColor tokenColor,bool loaded = false)
    {
        CreateFlower(tiles[new Vector2Int((int)pos.x,(int)pos.y)], tokenColor,loaded);
    }
    public void CreateFlower(Tile tile,TokenColor tokenColor, bool loaded = false,bool finished = false)
    {
        Rect total = new Rect(-0.5f, -0.45f, 0.82f, 1.3f);
        total = new Rect(-gridSeparation.x * 0.5f, -gridSeparation.y * 0.5f, gridSeparation.x, gridSeparation.y);
        float x_dif = 0.1f;
        float y_dif = 0.1f;
        Rect notAllowed = new Rect(total.x+x_dif, total.y+y_dif, total.width-(x_dif), total.height-(y_dif*2f));
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
            extents = new Vector2(Random.Range(total.x, total.x + total.height), Random.Range(total.y, total.y + total.height));
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
        flower.animSpeed *= Random.Range(0.9f, 1.1f);
        if (finished)
        {
            flower.animSpeed *= 1.5f;
        }
        bool keepFlower = true;
        if (flowers.ContainsKey(tile) == false)
        {
            flowers.Add(tile,new List<Flower>());
        }
        else
        {
            bool farEnough = true;
            int count = 0;
            do
            {
                count++;
                if(count > 20)
                {
                    Debug.Log("gave up for this reason");
                    //todo: boost the flower you hit last
                    keepFlower = false;
                    break;
                }
                foreach(Flower f in flowers[tile])
                {
                    if(Vector2.Distance(f.transform.position,flower.transform.position) < 0.02f)
                    {
                        farEnough = false;
                        break;
                    }
                }
                if(farEnough == false)
                {
                    tries = 0;
                    do
                    {
                        tries++;
                        extents = new Vector2(Random.Range(total.x, total.x + total.height), Random.Range(total.y, total.y + total.height));
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
        numFlowers++;
        if (keepFlower)
        {
            flowers[tile].Add(flower);
            
        }
        else
        {
            GameObject.Destroy(flower.gameObject);
        }
        
        
    }
    public void Snapshot()
    {
        SaveLoad.Save(1,currentSave);
        snapshotSave = currentSave;
        GameStateGameplay();
    }
    public void LoadSnapshot()
    {
        Logic.History.Turn _save = null;
        if (SaveLoad.HasSave(1))
        {
            gameState = GameState.Gameplay;
            _save = SaveLoad.Load(1);
            gameState = GameState.Snapshot;
            snapshotSave = _save;
        }
        if(snapshotSave == null) { return; }
        game.LoadTurn(snapshotSave);
        score = game.score;
        CreateHand();
        ClearTokensFromGrid();
        LoadTokensIntoGrid();
        GameStateGameplay();
    }
    public void Save()
    {
        Logic.History.Turn save = new Logic.History.Turn(game);
        currentSave = save;
        SaveLoad.Save(0,currentSave);
    }
    public void Mulligan()
    {
        //put back rest of hand and draw 4 more
        if(inTutorial && tutorial.stage == TutorialStage.TeachMulligan)
        {
            game.FifthTutorialHand();
            tutorial.IncrementStage();
        }
        else
        {
            if (inTutorial)
            {

            }
            else
            {
                game.Mulligan();
            }
            
        }
        
        CreateHand();
        Services.AudioManager.PlayUndoSound();
    }
    public void Undo()
    {
        //if (inputState != InputState.Choose) { return; }
        if (game.history.turns.Count > 1)
        {
            game.Undo();
            score = game.score;
            CreateHand();
            ClearTokensFromGrid();
            LoadTokensIntoGrid();
            /*foreach (Tile tile in tiles.Values)
            {
                if (tile.token && tile.tile.token == null)
                {
                    //delete your token
                    GameObject.Destroy(tile.token.gameObject);
                    tile.token = null;
                }else if (tile.token == null && tile.tile.token != null)
                {
                    //add the token
                    Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
                    token.token = tile.tile.token;
                    tile.token = token;
                    token.PlaceInTile(tile);
                }
            }*/
            Save();
            if(inputState != InputState.Choose)
            {
                EnterInputState(InputState.Choose);
            }
        }
    }
    public void Restart()
    {
        SaveLoad.DeleteSave(0);
        Services.AudioManager.StopMusic();
        SceneManager.LoadScene(0);
    }
    public void RestartWithTutorial()
    {
        PlayerPrefs.DeleteKey("tutorialComplete");
        PlayerPrefs.DeleteKey("greenLearnt");
        PlayerPrefs.DeleteKey("purpleLearnt");
        Restart();
    }

}
