using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Logic;
using Save;

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
    public TextMeshPro debugBagDisplay;

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

    public Logic.History.Turn snapshotSave;

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

    // Update is called once per frame
    void Update()
    {
        if (game.isGameover())
        {
            Debug.Log("game over");
        }
        DebugBagDisplay();
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
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data))
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
                        bool emptyTile = game.grid.HasTile(chosenPos) && game.grid.tiles[chosenPos].IsEmpty();
                        if (game.CanPlaceHere(chosenPos, chosenToken.token.data))
                        {
                            game.PlaceTokenFromHand(chosenIndex, chosenPos);
                            //Services.AudioManager.PlayPlaceSound();
                            if (holdingClipper)
                            {
                                Services.AudioManager.PlayShearsSound();
                            }
                            if (chosenIndex >= game.hand.handSize)
                            {
                                if (holdingAdder && emptyTile)
                                {
                                    tiles[chosenPos].token = freeSlot.token;
                                    lastTokenPlaced = freeSlot.token;
                                    lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                    freeSlot.token.UpdateLayer("TokenMoving");
                                    freeSlot.token = null;
                                }
                                else
                                {
                                    GameObject.Destroy(freeSlot.token.gameObject);
                                    lastTokenPlaced = null;
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
                                    hand[chosenIndex].UpdateLayer("TokenMoving");
                                    lastTokenPlaced = hand[chosenIndex];
                                    lastTokenPlaced.transform.localEulerAngles = Vector3.zero;
                                    hand[chosenIndex] = null;
                                }
                                else
                                {
                                    GameObject.Destroy(hand[chosenIndex].gameObject);
                                    lastTokenPlaced = null;
                                }
                                
                                
                                if (holdingClipper)
                                {
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
                            if (chosenIndex >= game.hand.handSize)
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                GameObject.Destroy(freeSlot.token.gameObject);

                                freeSlot.token = tiles[chosenPos].token;
                                lastTokenPlaced = null;
                                freeSlot.token.UpdateLayer("TokenHand");
                                freeSlot.token.PlaceInHand(chosenIndex);
                                tiles[chosenPos].token = null;
                            }
                            else
                            {
                                game.PlaceTokenBackInHand(chosenIndex, chosenPos);
                                lastTokenPlaced = null;
                                GameObject.Destroy(hand[chosenIndex].gameObject);

                                hand[chosenIndex] = tiles[chosenPos].token;
                                hand[chosenIndex].UpdateLayer("TokenHand");
                                hand[chosenIndex].PlaceInHand(chosenIndex);
                                tiles[chosenPos].token = null;
                            }
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
                                    token = _event.tokens[0];
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
    public Token CreateClippingToken(Logic.Token _token)
    {
        Token token = GameObject.Instantiate(tokenPrefab, transform).GetComponent<Token>();
        token.Init(_token);
        token.UpdateLayer("TokenHand");
        return token;
        
    }
    public void Snapshot()
    {
        SaveLoad.Save(currentSave);
        snapshotSave = currentSave;
    }
    public void LoadSnapshot()
    {
        Logic.History.Turn _save = null;
        if (SaveLoad.HasSave())
        {
            _save = SaveLoad.Load();
            snapshotSave = _save;
        }
        if(snapshotSave == null) { return; }
        game.LoadTurn(snapshotSave);
        score = game.score;
        CreateHand();
        ClearTokensFromGrid();
        LoadTokensIntoGrid();
    }
    public void Save()
    {
        Logic.History.Turn save = new Logic.History.Turn(game);
        currentSave = save;
        SaveLoad.Save(currentSave);
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
