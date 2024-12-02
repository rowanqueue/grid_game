using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public enum GameType
{
    Triple,
    Bubble
}
public enum InputState
{
    Choose,//waiting to pick up a token
    Place,//waiting to place a token
    Wait// waiting for game to update
}
public class GameController : MonoBehaviour
{
    public bool load;
    public TextAsset gameJson;
    public GameType whichGame;
    public Logic.Game game;
    //public TextInput.SimInput simInput;
    public TextMeshPro display;
    public TextMeshPro bagDisplay;

    public GameObject tokenPrefab;
    public GameObject tilePrefab;
    //bad
    public Vector2 firstHandPos;
    public Vector2 handSeparation;

    public Vector2 firstGridPos;
    public Vector2 gridSeparation;

    public Vector2 freeSlotPos;
    Vector2Int freeSlotChoice = new Vector2Int(-20, -20);

    public BagDisplay deckDisplay;
    public TextMeshPro deckNumberDisplay;

    //gameplay
    public InputState inputState = InputState.Choose;
    public List<Token> hand = new List<Token>();
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

    // Start is called before the first frame update
    void Awake()
    {
        InitializeServices();
        Application.targetFrameRate = 60;
        switch (whichGame)
        {
            case GameType.Triple:
                game = new Logic.TripleGame();
                break;
            case GameType.Bubble:
                game = new Logic.BubbleGame();
                break;
        }
        Json.Root root = JsonConvert.DeserializeObject<Json.Root>(gameJson.text);
        game.Initialize(root);
        if (load && PlayerPrefs.HasKey("save"))
        {
            string save = PlayerPrefs.GetString("save");
            Debug.Log(save);
            Logic.History.Turn turn = JsonUtility.FromJson<Logic.History.Turn>(save);
            game.history.turns.Clear();
            game.history.turns.Add(turn);
            turn.Load(game);
            currentSave = turn;
            score = turn.score;
        }
        //simInput = new TextInput.SimInput(game);

        CreateGrid();
        hand = new List<Token>();
        CreateHand();
        deckDisplay.game = game;
    }
    void InitializeServices()
    {
        Services.GameController = this;
        Services.Visuals = GameObject.FindObjectOfType<Visuals>();

        Services.AudioManager = GameObject.FindObjectOfType<AudioManager>();
        Services.AudioManager?.Initialize();

    }
    void CreateGrid()
    {
        foreach(Logic.Tile _tile in game.grid.tiles.Values)
        {
            Tile tile = GameObject.Instantiate(tilePrefab, transform).GetComponent<Tile>();
            tile.tile = _tile;
            tiles.Add(tile.tile.pos, tile);
            tile.transform.position = firstGridPos + (_tile.pos * gridSeparation);
            
        }
        //free slot
        freeSlot = GameObject.Instantiate(tilePrefab, transform).GetComponent<Tile>();
        freeSlot.freeSlot = true;
        freeSlot.transform.parent = freeSlotVisual.transform;
        LoadTokensIntoGrid();
        

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
            Debug.Log("remove free slot");
            GameObject.Destroy(freeSlot.token.gameObject);
            freeSlot.token = null;
        }
    }
    public void ToggleBagDisplay()
    {
        deckDisplay.gameObject.SetActive(!deckDisplay.gameObject.activeSelf);
        Services.AudioManager.PlayBagSound();
    }
    void LoadTokensIntoGrid()
    {
        foreach(Tile tile in tiles.Values)
        {
            if(tile.tile.token != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
                token.token = tile.tile.token;
                tile.token = token;
                token.PlaceInTile(tile);
            }
        }
        if (game.freeSlot != null)
        {
            Debug.Log("load in freeslot");
            Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
            token.token = game.freeSlot;
            freeSlot.token = token;
            token.PlaceInTile(freeSlot);
        }
    }
    void CreateHand()
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
                Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
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
            hand[i].PlaceInHand(i);

        }
        
    }
    void EnterInputState(InputState newState)
    {
        switch (newState)
        {
            case InputState.Choose:
                if(inputState == InputState.Place)
                {
                    //returning
                    chosenToken.UpdateLayer("TokenHand");
                }
                break;
            case InputState.Place:
                chosenToken.UpdateLayer("TokenHeld");
                break;
        }
        inputState = newState;
        
    }

    // Update is called once per frame
    void Update()
    {
        //save stuff

        //simInput.Update();
        //display.text = "<mspace=0.25\nem><line-height=75%>" + simInput.Draw();
        display.text = "<size=50%>SCORE</size>\n" + score.ToString();
        if(scoreDelta > 0)
        {
            display.text+= "+"+scoreDelta.ToString();
        }
        bagDisplay.text = game.bag.ToString();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //input
        switch (inputState)
        {
            case InputState.Choose:
                chosenIndex = -1;
                //hover
                for (int i = 0; i < game.hand.tokens.Length; i++)
                {
                    if (hand[i] == null) { continue; }
                    Vector2 pos = firstHandPos + (i * handSeparation);
                    float d = Vector2.Distance(mousePos, pos);
                    if (d < 0.5f)
                    {
                        chosenIndex = i;
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
                    }
                }
                if (Input.GetMouseButtonDown(0))
                {
                    if(chosenIndex > -1)
                    {
                        
                        Services.AudioManager.PlayPickUpSound();
                        EnterInputState(InputState.Place);
                        break;
                    }
                }
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
                        Undo();
                    }
                }
                
                break;
            case InputState.Place:
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
                }else if (Input.GetMouseButtonDown(0))
                {//clicking
                    if(chosenPos == freeSlotChoice)
                    {
                        if (chosenIndex == game.hand.handSize + 2)
                        {//put it back!
                            Services.AudioManager.PlayLetGoSound();
                            EnterInputState(InputState.Choose);
                            break;
                        }else if (game.IsFreeSlotFree())
                        {
                            Services.AudioManager.PlayFreeSlotSound();
                            game.PlaceTokenInFreeSlot(chosenIndex);
                            freeSlot.token = hand[chosenIndex];
                            hand[chosenIndex].UpdateLayer("TokenMoving");
                            hand[chosenIndex] = null;
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }

                    }
                    if(holdingClipper == false && holdingSpade == false && holdingAdder == false)
                    {
                        if (game.CanPlaceHere(chosenPos, false))
                        {
                            Services.AudioManager.PlayPlaceSound();
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
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                    }else if (holdingClipper || holdingAdder)
                    {
                        if (game.CanPlaceHere(chosenPos, holdingClipper || holdingAdder))
                        {
                            game.PlaceTokenFromHand(chosenIndex, chosenPos);
                            Services.AudioManager.PlayPlaceSound();
                            if (chosenIndex >= game.hand.handSize)
                            {
                                GameObject.Destroy(freeSlot.token.gameObject);
                                
                                freeSlot.token = null;
                            }
                            else
                            {
                                GameObject.Destroy(hand[chosenIndex].gameObject);
                                Services.AudioManager.PlayShearsSound();
                                hand[chosenIndex] = null;
                            }
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                    }else if (holdingSpade)
                    {
                        if (game.CanPlaceHere(chosenPos, holdingSpade))
                        {
                            Services.AudioManager.PlayPlaceSound();
                            if (chosenIndex >= game.hand.handSize)
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                GameObject.Destroy(freeSlot.token.gameObject);

                                freeSlot.token = tiles[chosenPos].token;
                                freeSlot.token.UpdateLayer("TokenHand");
                                freeSlot.token.PlaceInHand(chosenIndex);
                                tiles[chosenPos].token = null;
                            }
                            else
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                GameObject.Destroy(hand[chosenIndex].gameObject);

                                hand[chosenIndex] = tiles[chosenPos].token;
                                hand[chosenIndex].UpdateLayer("TokenHand");
                                hand[chosenIndex].PlaceInHand(chosenIndex);
                                tiles[chosenPos].token = null;
                            }
                            game.FakeTurn();
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                            break;
                        }
                    }
                    Vector2 pos = firstHandPos + (chosenIndex * handSeparation);
                    float d = Vector2.Distance(mousePos, pos);
                    if (d < 0.5f)
                    {
                        Services.AudioManager.PlayLetGoSound();
                        EnterInputState(InputState.Choose);
                    }
                }
                break;
            case InputState.Wait:
                if(game.gridUpdating == false)
                {
                    waiting -= Time.deltaTime;
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
                                case Logic.StatusReport.EventType.TokenDestroyed:
                                    Debug.Log("destr'yed");
                                    token = _event.tokens[0];
                                    Debug.Log(token);
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                GameObject.Destroy(tile.token.gameObject);
                                                tile.token = null;
                                                Services.AudioManager.PlayRemoveTileSound(1);
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.TokenChanged:
                                    token = _event.tokens[0];
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                tile.token.token = _event.tokens[1];
                                                Services.AudioManager.PlayUpgradeTileSound();
                                            }
                                        }
                                    }
                                    break;
                                case Logic.StatusReport.EventType.NewHand:
                                    CreateHand();
                                    break;
                                case Logic.StatusReport.EventType.ScoreAdded:
                                    scoreDelta += _event.num;
                                    waiting = waitTime * 0.1f;
                                    break;
                                case Logic.StatusReport.EventType.BagUpdated:
                                    deckDisplay.bagUpdated = true;
                                    waiting = waitTime * 0.01f;
                                    break;
                            }
                        }
                        else
                        {
                            deckDisplay.MakeBag();
                            score += scoreDelta;
                            scoreDelta = 0;
                            EnterInputState(InputState.Choose);
                            //save
                            Save();
                        }
                    }
                    
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
            freeSlot.token.Draw(freeSlot.transform.position+(shouldHover ? Vector3.up*0.5f : Vector3.zero),shouldHover);
        }
        //draw tokens
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            Vector2 pos = firstHandPos + (i * handSeparation);
            if(i == chosenIndex)
            {
                if(inputState == InputState.Choose)
                {
                    hand[i].Draw(pos,true);
                }else if(inputState == InputState.Place)
                {
                    hand[i].Draw(pos+Vector2.up*0.5f,true);
                }
            }
            else
            {
                hand[i].Draw(pos);
            }
            
            
        }
        deckNumberDisplay.text = game.bag.bag.Count.ToString();
        switch (inputState)
        {
            case InputState.Choose:
                
                break;
        }
    }
    public void Save()
    {
        Logic.History.Turn save = new Logic.History.Turn(game);
        currentSave = save;
        PlayerPrefs.SetString("save", JsonUtility.ToJson(save));
    }
    public void Mulligan()
    {
        //put back rest of hand and draw 4 more
        game.Mulligan();
        CreateHand();
        Services.AudioManager.PlayUndoSound();
    }
    public void Undo()
    {
        if (inputState != InputState.Choose) { return; }
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
        }
    }
    public void Restart()
    {
        PlayerPrefs.DeleteKey("save");
        Services.AudioManager.StopMusic();
        SceneManager.LoadScene(0);
    }

}
/*namespace TextInput
{

    public enum InputState
    {
        Choose,
        Place,
        Wait
    }
    public class SimInput
    {
        public Logic.Game game;
        public InputState state = InputState.Choose;

        //choose
        public int chooseIndex = 0;

        //place
        public Vector2Int placePos = Vector2Int.zero;
        public SimInput(Logic.Game game)
        {
            this.game = game;
        }
        void AcceptableHandIndex(bool up)
        {
            chooseIndex = (chooseIndex + game.hand.handSize) % game.hand.handSize;
            while (game.hand.tokens[chooseIndex] == null)
            {
                chooseIndex += (up ? 1 : -1);
                chooseIndex = (chooseIndex + game.hand.handSize) % game.hand.handSize;
            }
        }
        public void Update()
        {
            switch (state)
            {
                case InputState.Choose:
                    if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        chooseIndex++;
                        AcceptableHandIndex(true);
                    }
                    else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        chooseIndex--;
                        AcceptableHandIndex(false);
                    }
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        EnterInputState(InputState.Place);
                    }
                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        game.Undo();
                    }
                    break;
                case InputState.Place:
                    if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        placePos.x += 1;
                        placePos.x %= game.grid.gridSize.x;
                    }
                    else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        placePos.x -= 1;
                        placePos.x = (placePos.x + game.grid.gridSize.x) % game.grid.gridSize.x;
                    }
                    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        placePos.y += 1;
                        placePos.y %= game.grid.gridSize.y;
                    }
                    else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        placePos.y -= 1;
                        placePos.y = (placePos.y + game.grid.gridSize.y) % game.grid.gridSize.y;
                    }
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        if (game.CanPlaceHere(placePos))
                        {
                            game.PlaceTokenFromHand(chooseIndex, placePos);
                            EnterInputState(InputState.Wait);
                        }

                    }
                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        EnterInputState(InputState.Choose);
                    }
                    break;
                case InputState.Wait:
                    if (game.gridUpdating == false)
                    {
                        EnterInputState(InputState.Choose);
                    }
                    break;
            }
        }
        public string Draw()
        {
            string s = string.Empty;
            s += "<size=50%>" + game.score.ToString() + "</size>\n";
            s += "<size=75%>";
            s += DrawGrid();
            s += "\n" + DrawHand();
            if (game.hand.handChoices > -1)
            {
                s += (game.hand.handChoices - game.hand.tokensTaken).ToString() + "/" + game.hand.handChoices.ToString();
            }
            s += " " + state.ToString();
            return s;
        }
        string DrawGrid()
        {
            string s = string.Empty;
            for (int y = game.grid.gridSize.y - 1; y >= 0; y--)
            {
                if (y != game.grid.gridSize.y - 1)
                {
                    s += "\n";
                }
                for (int x = 0; x < game.grid.gridSize.y; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    string p = "";
                    if (game.grid.tiles[pos].IsEmpty())
                    {
                        p = " ";
                    }
                    else
                    {
                        p = "<size=50%>" + game.grid.tiles[pos].token.ToString() + "</size>";
                    }
                    p = "[" + p + "]";
                    if (state == InputState.Place)
                    {
                        if (placePos == pos)
                        {
                            p = "<s>" + p + "</s>";
                        }
                    }
                    s += p;
                }

            }
            return s;
        }
        string DrawHand()
        {
            string s = string.Empty;
            s += "<size=50%>";
            for (int i = 0; i < game.hand.handSize; i++)
            {
                string p = "";
                p += " ";
                if (game.hand.tokens[i] == null)
                {
                    p += " ";
                }
                else
                {
                    p += game.hand.tokens[i].ToString();
                }
                p += " ";
                if (i == chooseIndex)
                {
                    if (state == InputState.Choose)
                    {
                        p = "<u>" + p + "</u>";
                    }
                    else if (state == InputState.Place)
                    {
                        p = "<u>" + p + "</u>";
                    }

                }

                s += p;
            }
            return s;
        }
        void EnterInputState(InputState _state)
        {
            if (state == _state) { return; }
            ExitInputState();
            switch (_state)
            {
                case InputState.Choose:
                    AcceptableHandIndex(true);
                    break;
                case InputState.Place:
                    break;
                case InputState.Wait:
                    break;
            }
            state = _state;
        }
        void ExitInputState()
        {

        }
    }
}
*/