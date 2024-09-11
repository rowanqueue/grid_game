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

    public Vector2Int freeSlotGridPos;

    //gameplay
    public InputState inputState = InputState.Choose;
    public List<Token> hand = new List<Token>();
    public int chosenIndex = -1;
    public Dictionary<Vector2Int,Tile> tiles = new Dictionary<Vector2Int,Tile>();
    public Tile freeSlot;
    public Vector2Int chosenPos = Vector2Int.left;
    public float waitTime = 0.1f;
    float waiting = 0f;

    //data
    public int score = 0;
    public int scoreDelta = 0;

    public Logic.History.Turn currentSave;
    // Start is called before the first frame update
    void Start()
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
    }
    void InitializeServices()
    {
        Services.GameController = this;
        Services.Visuals = GameObject.FindObjectOfType<Visuals>();

        Services.AudioManager = GameObject.FindObjectOfType<AudioManager>();
        Services.AudioManager.Initialize();

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
            GameObject.Destroy(freeSlot.token.gameObject);
            freeSlot.token = null;
        }
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
            Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
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
                        Services.AudioManager.PlaySound(Services.AudioManager.select);
                        EnterInputState(InputState.Place);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    Undo();
                    
                }
                break;
            case InputState.Place:
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
                    chosenPos = freeSlotGridPos;
                }
                //let go
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z))
                {
                    EnterInputState(InputState.Choose);
                }else if (Input.GetMouseButtonDown(0))
                {
                    if(chosenPos == freeSlotGridPos)
                    {
                        if (game.IsFreeSlotFree())
                        {
                            Services.AudioManager.PlaySound(Services.AudioManager.select, 1);
                            game.PlaceTokenInFreeSlot(chosenIndex);
                            freeSlot.token = hand[chosenIndex];
                            hand[chosenIndex].UpdateLayer("TokenMoving");
                            hand[chosenIndex] = null;
                            EnterInputState(InputState.Wait);
                            waiting = 0f;
                        }

                    }else if (game.CanPlaceHere(chosenPos))
                    {
                        Services.AudioManager.PlaySound(Services.AudioManager.select,1);
                        game.PlaceTokenFromHand(chosenIndex, chosenPos);
                        if(chosenIndex >= game.hand.handSize)
                        {
                            //freeSlot
                            tiles[chosenPos].token = freeSlot.token;
                            freeSlot.token.UpdateLayer("TokenMoving");
                            freeSlot.token = null;
                        }
                        else
                        {
                            tiles[chosenPos].token = hand[chosenIndex];
                            hand[chosenIndex].UpdateLayer("TokenMoving");
                            hand[chosenIndex] = null;
                        }
                        
                        EnterInputState(InputState.Wait);
                        waiting = 0f;
                    }
                    else
                    {
                        Vector2 pos = firstHandPos + (chosenIndex * handSeparation);
                        float d = Vector2.Distance(mousePos, pos);
                        if (d < 0.5f)
                        {
                            EnterInputState(InputState.Choose);
                        }
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
                                    token = _event.tokens[0];
                                    foreach (Tile tile in tiles.Values)
                                    {
                                        if (tile.token)
                                        {
                                            if (tile.token.token == token)
                                            {
                                                GameObject.Destroy(tile.token.gameObject);
                                                tile.token = null;
                                                Services.AudioManager.PlaySound(Services.AudioManager.pop);
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
                                                Services.AudioManager.PlaySound(Services.AudioManager.crack);
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
                            }
                        }
                        else
                        {
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
            freeSlot.Draw(chosenPos == freeSlotGridPos);
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