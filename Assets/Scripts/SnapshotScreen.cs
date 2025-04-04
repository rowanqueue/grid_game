using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Save;
using Newtonsoft.Json;

public class SnapshotScreen : MonoBehaviour
{
    Logic.Game game;
    GameObject tilePrefab => Services.GameController.tilePrefab;
    GameObject tokenPrefab => Services.GameController.tokenPrefab;
    public Transform gridTransform;
    public Tile freeSlot;
    public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
    Vector2 firstHandPos => Services.GameController.firstHandPos;
    Vector2 handSeparation => Services.GameController.handSeparation;

    Vector2 firstGridPos => Services.GameController.firstGridPos;
    Vector2 gridSeparation => Services.GameController.gridSeparation;

    Vector2 freeSlotPos => Services.GameController.freeSlotPos;
    public GameObject freeSlotVisual;
    public List<Transform> handTransforms = new List<Transform>();
    public List<Token> hand = new List<Token>();
    public void openScreen()
    {
        if(game == null)
        {
            game = new Logic.TripleGame();
            Json.Root root = JsonConvert.DeserializeObject<Json.Root>(Services.GameController.gameJson.text);
            game.Initialize(root);
            CreateGrid();
        }
        
        
        LoadSnapshot();
    }
    public void CreateGrid()
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
    void LoadTokensIntoGrid()
    {
        foreach (Tile tile in tiles.Values)
        {
            if (tile.tile.token != null)
            {
                Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
                token.token = tile.tile.token;
                tile.token = token;
                token.PlaceInTile(tile);
            }
        }
        if (game.freeSlot != null)
        {
            Debug.Log("load in freeslot");
            Token token = GameObject.Instantiate(tokenPrefab, gridTransform).GetComponent<Token>();
            token.token = game.freeSlot;
            freeSlot.token = token;
            token.PlaceInTile(freeSlot);
        }
    }
    void CreateHand()
    {
        for (int i = 0; i < game.hand.tokens.Length; i++)
        {
            if (game.hand.tokens[i] == null)
            {
                if (hand.Count < game.hand.tokens.Length)
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
                hand[i].token = game.hand.tokens[i];
            }
            hand[i].Init(hand[i].token);
            hand[i].UpdateLayer("TokenHand");
            hand[i].PlaceInHand(i);
            hand[i].transform.position = handTransforms[i].position;

        }

    }
    public void LoadSnapshot()
    {
        Logic.History.Turn _save = null;
        if (SaveLoad.HasSave(1))
        {
            _save = SaveLoad.Load(1);
        }
        if (_save == null) { return; }
        game.LoadTurn(_save);
        Update();
        CreateHand();
        ClearTokensFromGrid();
        LoadTokensIntoGrid();
    }
    private void Update()
    {
        //draw
        //draw grid
        foreach (Tile tile in tiles.Values)
        {
            tile.Draw(false);
            if (tile.token)
            {
                tile.token.Draw(tile.transform.position);
            }
        }
        //freeSlot.Draw(false);
        if (freeSlot.token)
        {
            freeSlot.token.Draw(freeSlot.transform.position);
        }
        //draw tokens
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null) { continue; }
            Vector2 pos = handTransforms[i].position;// firstHandPos + (i * handSeparation);
            hand[i].Draw(pos);


        }
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

}
