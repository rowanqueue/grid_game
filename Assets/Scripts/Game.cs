using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;

namespace Logic
{
    public class TripleGame : Game
    {
        public new string name = "3tile";
        public new int version = 0;
        public int groupCollapseNum = 3;
        public int maxTileNum = 8;
        public Dictionary<TokenColor, int> colorScoreMulti = new Dictionary<TokenColor, int>()
    {
        {TokenColor.Blue,1 },
        {TokenColor.Red,2 }
    };
        public Dictionary<string, TokenColor> colorNames = new Dictionary<string, TokenColor>()
        {
            {"blue",TokenColor.Blue},
            {"red",TokenColor.Red },
            {"green",TokenColor.Green },
            {"purple",TokenColor.Purple },
            {"clipper",TokenColor.Clipper },
            {"gold",TokenColor.Gold },
            {"spade",TokenColor.Spade },
            {"adder",TokenColor.Adder },
            {"gnome",TokenColor.Gnome }
        };
        public override void Initialize(Json.Root root)
        {
            groupCollapseNum = root.gameVariables.groupCollapseNum;
            colorScoreMulti = new Dictionary<TokenColor, int>();
            //code this better later lol
            colorScoreMulti.Add(TokenColor.Blue, root.scoreVariables.colorScoreMultiplier.blue);
            colorScoreMulti.Add(TokenColor.Red, root.scoreVariables.colorScoreMultiplier.red);
            colorScoreMulti.Add(TokenColor.Green, root.scoreVariables.colorScoreMultiplier.green);
            colorScoreMulti.Add(TokenColor.Purple, root.scoreVariables.colorScoreMultiplier.purple);
            colorScoreMulti.Add(TokenColor.Gold, root.scoreVariables.colorScoreMultiplier.gold);
            colorScoreMulti.Add(TokenColor.Spade, 6);
            colorScoreMulti.Add(TokenColor.Adder, 7);
            colorScoreMulti.Add(TokenColor.Clipper, 8);
            colorScoreMulti.Add(TokenColor.Gnome, 100);
            maxTileNum = root.gameVariables.maxTileNum;
            base.Initialize(root);
        }
        protected override void GridChanged(Token tokenChanged)
        {
            //recursively make groups based on neighbors
            List<Token> tokenGroup = new List<Token>();
            if(tokenChanged.data.color != TokenColor.Gnome)
            {
                if (tokenChanged.data.num < maxTileNum)
                {
                    if (tokenChanged.data.color == TokenColor.Purple)
                    {
                        tokenChanged.tile.CheckNeighbors(Check.Equals, tokenGroup, 0, tokenChanged);
                    }
                    else
                    {
                        tokenChanged.tile.CheckNeighbors(Check.Equals, tokenGroup, 0, tokenChanged);
                    }
                }
            }
            
            
            
            if (tokenGroup.Count >= groupCollapseNum)
            {
                //remove everything except the one you placed and change it to the next num
                status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenWait,1));
                for (int i = tokenGroup.Count - 1; i >= 0; i--)
                {
                    Token token = tokenGroup[i];
                    if (token != tokenChanged)
                    {
                        
                        token.Destroy();
                        status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed, token));
                        EarnPoints(token.data.num * colorScoreMulti[token.data.color]);
                    }
                }
                Token newToken = new Token(tokenChanged.data, false);
                newToken.data.num += 1;
                
                Tile tile = tokenChanged.tile;
                tokenChanged.Destroy();
                grid.PlaceToken(tile.pos,newToken);
                status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenChanged, new List<Token>() { tokenChanged, newToken }));
                List<Dictionary<TokenData, int>> updatedContents = progress.CheckProgress(newToken);
                for (int i = 0; i < updatedContents.Count; i++)
                {
                    status.events.Add(new StatusReport.Event(StatusReport.EventType.BagUpdated, updatedContents[i]));
                    bag.AddContents(updatedContents[i]);
                }
                
                GridChanged(newToken);
            }
            else
            {
                base.GridChanged(tokenChanged);
            }

        }
    }
    public class BubbleGame : Game
    {
        public new string name = "bubble9";
        public new int version = 0;
        public int depth = 1;
        public override void Initialize(Json.Root root)
        {
            gridSize = new Vector2Int(3, 3);
            handSize = 3;
            handChoices = 2;
            bagContents = new Dictionary<TokenData, int>()
        {
            {new TokenData(TokenColor.Red,1),2 },
            {new TokenData(TokenColor.Green,1),2 },
            {new TokenData(TokenColor.Red,2),2 },
            {new TokenData(TokenColor.Green,2),2 },
            {new TokenData(TokenColor.Red,3),2 },
            {new TokenData(TokenColor.Green,3),2 },
            {new TokenData(TokenColor.Red,4),2 },
            {new TokenData(TokenColor.Green,4),2 },
        };
            progress = new ProgressScore(this, new Dictionary<int, Dictionary<TokenData, int>>()
        {
            {100, new Dictionary<TokenData, int>()
                {
                    {new TokenData(TokenColor.Red,5),2 },
                    {new TokenData(TokenColor.Blue,5),2 }
                }
            }
        });
            base.Initialize(root);
        }
        protected override void GridChanged(Token token)
        {
            //check whole grid (who cares about token)
            List<List<Token>> tokenGroups = new List<List<Token>>();
            foreach (Tile tile in grid.tiles.Values)
            {
                if (tile.IsEmpty()) { continue; }
                //make sure this token isn't in another group already
                bool alreadyGrouped = false;
                foreach (List<Token> _tokens in tokenGroups)
                {
                    if (_tokens.Contains(tile.token))
                    {
                        alreadyGrouped = true;
                    }
                }
                if (alreadyGrouped) { continue; }
                List<Token> tokenGroup = new List<Token>();
                tile.CheckNeighbors(Check.Color, tokenGroup,0);
                tokenGroups.Add(tokenGroup);
            }
            if (depth > 1 && tokenGroups.Count == 0)
            {
                //you cleared the board
                score += (depth - 1) * 9;
                status.events.Add(new StatusReport.Event(StatusReport.EventType.ScoreAdded, (depth - 1) * 9));
            }
            bool change = false;
            Dictionary<Tile, TokenColor> shotsFired = new Dictionary<Tile, TokenColor>();
            foreach (List<Token> tokenGroup in tokenGroups)
            {
                foreach (Token _token in tokenGroup)
                {
                    if (_token.data.num == tokenGroup.Count)
                    {
                        //pop
                        score += depth;
                        shotsFired.Add(_token.tile, _token.data.color);
                        _token.Destroy();
                        status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed, _token));
                        status.events.Add(new StatusReport.Event(StatusReport.EventType.ScoreAdded, depth));
                        change = true;
                    }
                }
            }
            foreach (Tile shotTile in shotsFired.Keys)
            {
                foreach (Tile neighbor in shotTile.neighbors)
                {
                    if (neighbor.IsEmpty()) { continue; }
                    if (neighbor.token.data.color != shotsFired[shotTile])
                    {
                        neighbor.token.data.num -= 1;
                        if (neighbor.token.data.num <= 0)
                        {
                            neighbor.token.Destroy();
                            status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed, neighbor.token));
                        }
                    }
                }
            }
            string s = "";
            foreach (List<Token> _tokens in tokenGroups)
            {
                if (s != "")
                {
                    s += "|";
                }
                foreach (Token t in _tokens)
                {
                    s += t.ToString();
                }
            }
            Debug.Log(s);
            if (change)
            {
                depth += 1;
                GridChanged(token);
            }
            else
            {
                base.GridChanged(token);
            }

        }
        protected override void GridFinishedChanging()
        {
            depth = 1;
            base.GridFinishedChanging();
        }

    }
    public class StatusReport
    {
        public enum EventType
        {
            NewHand,
            TokenCreated,//unimplemented
            TokenModelDestroyed,
            TokenViewAnimateDestroy,
            TokenChanged,//tokens[0] becomes tokens[1]
            ScoreAdded,
            BagUpdated,
            BagRefill,
            TokenWait,
            TokenAddedTo
        }
        public class Event
        {
            public EventType type;
            public List<Token> tokens;
            public int num;
            public Dictionary<TokenData, int> contents;
            public Event(EventType type, List<Token> tokens)
            {
                this.type = type;
                this.tokens = tokens;
            }
            public Event(EventType type, Token[] _tokens)
            {
                this.type = type;
                this.tokens = new List<Token>();
                foreach (Token t in _tokens)
                {
                    this.tokens.Add(t);
                }
            }
            public Event(EventType type, Token token)
            {
                this.type = type;
                this.tokens = new List<Token>();
                this.tokens.Add(token);
            }
            public Event(EventType type, int num)
            {
                this.type = type;
                this.tokens = new List<Token>();
                this.num = num;
            }
            public Event(EventType type, Dictionary<TokenData, int> contents)
            {
                this.type = type;
                this.tokens = new List<Token>();
                this.contents = contents;
            }
        }
        public List<Event> events = new List<Event>();
        
    }
    public class Game
    {
        public Json.Root root;
        public string name;
        public int version;
        public bool gridUpdating = false;
        public Grid grid;
        public Bag bag;
        public Hand hand;
        public Token freeSlot;
        public int score = 0;
        uint turn = 0;//counts placed tokens
        public Progress progress;
        public History history;
        public StatusReport status;

        //custom variables
        protected Vector2Int gridSize;
        protected int handSize;
        protected int handChoices = -1;
        protected Dictionary<TokenData, int> bagContents;
        public Dictionary<int, TokenColor> clippingColors = new Dictionary<int, TokenColor>()
        {
            {1,TokenColor.Blue },
            {2,TokenColor.Red },
            {3,TokenColor.Green },
            {4,TokenColor.Purple },
            {5,TokenColor.Gold }
        };
        public Dictionary<TokenColor, int> clippingNumbers = new Dictionary<TokenColor, int>()
        {
            {TokenColor.Blue,1},
            {TokenColor.Red,2 },
            {TokenColor.Green,3 },
            {TokenColor.Purple,4 },
            {TokenColor.Gold,5 }
        };

        public virtual void Initialize(Json.Root root)
        {
            Debug.Log(root.name);
            //gridSize
            gridSize = new Vector2Int(root.gridSize.x, root.gridSize.y);
            //handSize
            handSize = root.handSize;
            //bagContents
            bagContents = new Dictionary<TokenData, int>();
            if(root.name == "tripleTileSage")
            {
                int seed = DateTime.Today.Second;
                UnityEngine.Random.InitState(seed);
                for (int i = 0; i < 19; i++)
                {
                    int color = Mathf.FloorToInt(UnityEngine.Random.value * 4f);
                    TokenData blueToken = new TokenData((TokenColor)color, Mathf.FloorToInt(UnityEngine.Random.value * 8f));
                    if (bagContents.ContainsKey(blueToken))
                    {
                        bagContents[blueToken] += 1;
                    }
                    else
                    {
                        bagContents.Add(blueToken, 1);
                    }
                    
                }
                float val = UnityEngine.Random.value;
                if(val < 0.33f)
                {
                    bagContents.Add(new TokenData(TokenColor.Clipper, 0), 1);
                }else if (val < 0.66f)
                {
                    bagContents.Add(new TokenData(TokenColor.Adder, 0), 1);
                }
                else
                {
                    bagContents.Add(new TokenData(TokenColor.Spade, 0), 1);
                }

                
            }
            else
            {
                foreach (StartingBag tokenSet in root.startingBag)
                {
                    TokenData _token = ConvertJsonToken(tokenSet.token);
                    bagContents.Add(_token, tokenSet.count);

                }
            }
            
            //progress
            List<Unlock> unlocks = new List<Unlock>();
            foreach(Json.Event _event in root.progress.events)
            {
                List<TokenData> triggers = new List<TokenData>();
                foreach(Json.Trigger trigger in _event.trigger)
                {
                    triggers.Add(ConvertJsonToken(trigger.token));
                }
                Dictionary<TokenData,int> rewards = new Dictionary<TokenData,int>();
                foreach(Json.Reward reward in _event.reward)
                {
                    rewards.Add(ConvertJsonToken(reward.token), (reward.count == 0 ? 1 : reward.count));
                    if(reward.replacesToken != null)
                    {
                        TokenData replacedToken = ConvertJsonToken(reward.replacesToken.color, reward.replacesToken.number);
                        rewards.Add(replacedToken, -1);
                    }
                }
                bool repeatable = _event.prototypical || _event.repeatable;
                unlocks.Add(new Unlock(triggers, rewards, repeatable));
                if (_event.prototypical)
                {
                    for (int i = 1; i < 10; i++)
                    {
                        List<TokenData> moreTriggers = new List<TokenData>();
                        foreach (TokenData tokenData in triggers)
                        {
                            moreTriggers.Add(new TokenData(tokenData.color, tokenData.num + i));
                        }
                        Dictionary<TokenData, int> moreRewards = new Dictionary<TokenData, int>();
                        foreach (TokenData reward in rewards.Keys)
                        {
                            int count = rewards[reward];
                            if (rewards[reward] < 0)
                            {
                                //jsut removing a 1
                                moreRewards.Add(new TokenData(reward.color, reward.num + i), count);
                                continue;
                            }
                            moreRewards.Add(new TokenData(reward.color, reward.num + i), rewards[reward]);
                        }
                        unlocks.Add(new Unlock(moreTriggers, moreRewards, true));
                    }
                }
            }
            progress = new ProgressNew(this, unlocks);
            grid = new Grid(gridSize,this);
            status = new StatusReport();
            bag = new Bag(this,bagContents);
            hand = new Hand(handSize, handChoices);
            hand.FillHand(bag);
            history = new History(this);
            history.turns.Add(new History.Turn(this));
            
        }
        public void StartTutorial()
        {
            hand.ReturnHand(bag);
            hand.TutorialHand(0,bag);
        }
        public void SecondTutorialHand()
        {
            hand.ReturnHand(bag);
            hand.TutorialHand(1,bag);
        }
        public void ThirdTutorialHand()
        {
            hand.ReturnHand(bag);
            hand.TutorialHand(2, bag);
        }
        public void FourthTutorialHand()
        {
            hand.ReturnHand(bag);
            hand.TutorialHand(3, bag);
        }
        public void FifthTutorialHand()
        {
            hand.ReturnHand(bag);
            hand.TutorialHand(4, bag);
        }
        public TokenData ConvertJsonToken(Json.Token token)
        {
            Dictionary<string, TokenColor> colors = new Dictionary<string, TokenColor>()
            {
                {"red",TokenColor.Red },
                {"blue",TokenColor.Blue },
                {"green",TokenColor.Green },
                {"purple",TokenColor.Purple },
                {"clipper",TokenColor.Clipper },
                {"gold",TokenColor.Gold },
                {"spade",TokenColor.Spade },
                {"adder",TokenColor.Adder },
                {"gnome",TokenColor.Gnome }
            };
            return new TokenData(colors[token.color], token.number,token.temporary);
        }
        public TokenData ConvertJsonToken(string color, int number)
        {
            Dictionary<string, TokenColor> colors = new Dictionary<string, TokenColor>()
            {
                {"red",TokenColor.Red },
                {"blue",TokenColor.Blue },
                {"green",TokenColor.Green },
                {"purple",TokenColor.Purple },
                {"clipper",TokenColor.Clipper },
                {"gold",TokenColor.Gold },
                {"spade",TokenColor.Spade },
                {"adder",TokenColor.Adder},
                {"gnome",TokenColor.Gnome }
            };
            return new TokenData(colors[color], number);
        }
        public bool isGameover()
        {
            return grid.isFull();
        }
        public void EarnPoints(int pts)
        {
            score += pts;
            //todo: score event :(((
            status.events.Add(new StatusReport.Event(StatusReport.EventType.ScoreAdded, pts));
        }
        public bool CanPlaceHere(Vector2Int p,TokenData heldToken )
        {
            if (grid.tiles.ContainsKey(p) == false)
            {
                return false;
            }
            if (grid.tiles[p].IsEmpty())
            {
                if (heldToken.color == TokenColor.Clipper || heldToken.color == TokenColor.Spade)
                {
                    return false;
                }
                else if (heldToken.color == TokenColor.Adder)
                {
                    if (heldToken.num == 0) { return false; }
                    if (heldToken.num > 0) { return true; }
                }
                else
                {
                    return true;
                }
            }
            //non empty
            if(heldToken.color == TokenColor.Clipper || heldToken.color == TokenColor.Spade)
            {
                if (heldToken.color == TokenColor.Clipper && grid.tiles[p].token.data.color == TokenColor.Gnome)
                {
                    return false;
                }
                return true;
            }
            if(heldToken.color != TokenColor.Adder)
            {
                return false;
            }
            //adder
            TokenData gridToken = grid.tiles[p].token.data;
            if(gridToken.num >= 8) { return false; }
            if(gridToken.color == TokenColor.Gnome) { return false; }
            if (clippingColors.ContainsKey(heldToken.num) ==false) { return true; }
            //clipping tile
            int num = clippingNumbers[gridToken.color];
            if (num == heldToken.num) { return true; }
            
            return false;
        }
        public void Mulligan()
        {
            hand.ReturnHand(bag);
            hand.FillHand(bag);
        }
        public void PlaceTokenBackInHand(int handIndex, Vector2Int gridPos)
        {
            history.turns.Add(new History.Turn(this));
            Token token = grid.tiles[gridPos].token;
            TripleGame _game = this as TripleGame;
            _game.EarnPoints(token.data.num * _game.colorScoreMulti[token.data.color]);
            if (handIndex >= hand.handSize)
            {
                freeSlot = token;
            }
            else
            {
                if (hand.tokens[handIndex].data.temporary)
                {
                    bag.RemoveToken(hand.tokens[handIndex]);
                    status.events.Add(new StatusReport.Event(StatusReport.EventType.BagUpdated, new Dictionary<TokenData, int>() { { hand.tokens[handIndex].data, -1 } }));
                }
                hand.tokens[handIndex] = token;
            }
            grid.tiles[gridPos].token = null;
            hand.tokensTaken--;
        }
        public void FakeTurn()
        {
            GridFinishedChanging();
        }
        public void PlaceTokenFromHand(int handIndex, Vector2Int gridPos)
        {
            //new turn stuff
            history.turns.Add(new History.Turn(this));
            status = new StatusReport();
            //end new turn stuff
            Token token;
            if(handIndex >= hand.handSize)
            {
                token = freeSlot;
                freeSlot = null;
            }
            else
            {
                token = hand.TakeToken(handIndex);
                if (token.data.temporary)
                {
                    bag.PlayedTempToken(token);
                    bag.RemoveToken(token);
                    status.events.Add(new StatusReport.Event(StatusReport.EventType.BagUpdated, new Dictionary<TokenData, int>() { { token.data, -1 } }));
                }
            }
            
            gridUpdating = true;
            if(token.data.color == TokenColor.Clipper)
            {
                /*Token clippedToken = grid.tiles[gridPos].token;
                Token newToken = new Token(new TokenData(TokenColor.Adder, clippingNumbers[clippedToken.data.color]), true);
                if(handIndex >= hand.handSize)
                {
                    freeSlot = newToken;
                }
                else
                {
                    hand.tokens[handIndex] = newToken;
                }*/
                
            }
            bool placingAdder = token.data.color == TokenColor.Adder;
            token = grid.PlaceToken(gridPos, token);
            if (placingAdder)
            {
                List<Dictionary<TokenData, int>> updatedContents = progress.CheckProgress(token);
                for(int i = 0; i < updatedContents.Count; i++)
                {
                    status.events.Add(new StatusReport.Event(StatusReport.EventType.BagUpdated, updatedContents[i]));
                    bag.AddContents(updatedContents[i]);
                }
            }
            if (token != null)
            {
                GridChanged(token);
            }
            else
            {
                GridFinishedChanging();
            }
            
        }
        public bool IsFreeSlotFree()
        {
            if(freeSlot != null)
            {
                return false;
            }
            return true;
        }
        public void PlaceTokenInFreeSlot(int handIndex)
        {
            if(IsFreeSlotFree() == false) { return; }
            history.turns.Add(new History.Turn(this));
            status = new StatusReport();
            //end new turn stuff
            Token token = hand.TakeToken(handIndex);
            if (token.data.temporary)
            {
                status.events.Add(new StatusReport.Event(StatusReport.EventType.BagUpdated, new Dictionary<TokenData, int>() { { token.data, -1 } }));
                bag.PlayedTempToken(token);
                bag.RemoveToken(token);
            }
            gridUpdating = true;
            freeSlot = token;
            GridFinishedChanging();
        }
        protected virtual void GridChanged(Token token)
        {
            //custom code goes here :)
            GridFinishedChanging();
        }
        protected virtual void GridFinishedChanging()
        {
            //this has to be called
            
            if (hand.IsHandEmpty())
            {
                hand.EmptyHand();
                hand.FillHand(bag);
                status.events.Add(new StatusReport.Event(StatusReport.EventType.NewHand,hand.tokens));
            }
            gridUpdating = false;
        }
        public void Undo()
        {
            History.Turn turn = history.turns[history.turns.Count - 1];
            if (history.turns.Count > 1)
            {
                history.turns.Remove(turn);
            }
            turn.Load(this);
        }
        public void LoadTurn(History.Turn turn)
        {
            history.turns.Clear();
            turn.Load(this);
            history.turns.Add(new History.Turn(this));
        }
    }
    public class Grid
    {
        Game game;
        public Vector2Int gridSize;
        public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        public List<Vector2Int> dirs = new List<Vector2Int>()
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };
        public Grid(Vector2Int size, Game game)
        {
            this.gridSize = size;
            PopulateGrid();
            this.game = game;   
        }
        public override string ToString()
        {
            string s = string.Empty;
            for(int y = gridSize.y - 1; y >= 0; y--)
            {
                for(int x = 0; x < gridSize.x; x++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if(tiles.ContainsKey(p) == false)
                    {
                        s += "_";
                        continue;
                    }
                    Tile tile = tiles[p];
                    if (tile.IsEmpty())
                    {
                        s += "_";
                        continue;
                    }
                    s += tile.token.ToString();
                }
                s += "\n";
            }
            return s;
        }
        public bool HasTile(Vector2Int pos)
        {
            return tiles.ContainsKey(pos);
        }
        public bool isFull()
        {
            foreach(Tile tile in tiles.Values)
            {
                if (tile.IsEmpty()) {  return false; }
            }
            return true;
        }
        public void Clear()
        {
            foreach (Tile tile in tiles.Values)
            {
                if (tile.IsEmpty()) { continue; }
                tile.token.Destroy();
            }
        }

        void PopulateGrid()
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    tiles.Add(p, new Tile(p));
                }
            }
            foreach (Tile tile in tiles.Values)
            {
                foreach (Vector2Int dir in dirs)
                {
                    if (tiles.ContainsKey(tile.pos + dir))
                    {
                        tile.neighbors.Add(tiles[tile.pos + dir]);
                    }
                }
            }
        }
        public Token PlaceToken(Vector2Int p, Token token)
        {
            
            Tile tile = tiles[p];
            if (token.data.color == TokenColor.Clipper)
            {
                game.bag.AddContents(new Dictionary<TokenData, int>() {{ new TokenData(TokenColor.Adder, game.clippingNumbers[tile.token.data.color],true),1}});
                game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed, new List<Token>() { token }));
                int num = tile.token.data.num - 1;
                if(num > 0)
                {
                    Token newToken = new Token(tile.token.data, false);

                    newToken.data.num = num;
                    game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenChanged, new List<Token>() { tile.token, newToken, token }));
                    tile.token.Destroy();
                    PlaceToken(p, newToken);
                    
                    return newToken;
                }
                else
                {
                    game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed,new List<Token>() { tile.token, token }));
                    tile.token.Destroy();
                    return null;
                }

            }else if(token.data.color == TokenColor.Adder)
            {
                if (tile.IsEmpty())
                {
                    token.data.color = game.clippingColors[token.data.num];
                    token.data.num = 1;
                    //game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenChanged, new List<Token>() { token, token }));

                    tile.token = token;
                    token.tile = tile;
                    return token;
                }
                game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenModelDestroyed, new List<Token>() { token }));
                int num = tile.token.data.num + 1;
                Token newToken = new Token(tile.token.data, false);

                newToken.data.num = num;
                game.status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenAddedTo, new List<Token>() { tile.token, newToken, token }));
                tile.token.Destroy();
                PlaceToken(p, newToken);

                return newToken;

            }
            else
            {
                tile.token = token;
                token.tile = tile;
                return token;
            }
            
        }
        public bool Contains(TokenData tokenData)
        {
            foreach (Tile tile in tiles.Values)
            {
                if (tile.IsEmpty()) { continue; }
                if (tile.token.data == tokenData)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public enum Check
    {
        Equals,
        Color,
        Num
    }
    public class Tile
    {
        public Vector2Int pos { get; private set; }
        public Token token;
        public List<Tile> neighbors = new List<Tile>();
        public Tile(Vector2Int pos)
        {
            this.pos = pos;
        }
        public bool IsEmpty()
        {
            if (ReferenceEquals(token, null))
            {
                return true;
            }
            return false;
        }
        public void CheckNeighbors(Check check, List<Token> tokenGroup,int depth,Token tokenToCheck = null)
        {
            if (depth >= 3) { return; }
            if (IsEmpty()) { return; }
            tokenGroup.Add(token);
            bool hadAToken = tokenToCheck != null;
            if(tokenToCheck == null)
            {
                tokenToCheck = token;
            }
            foreach (Tile neighbor in neighbors)
            {
                if (neighbor.IsEmpty() == false && tokenGroup.Contains(neighbor.token) == false)
                {
                    bool shouldContinue = false;
                    switch (check)
                    {
                        case Check.Equals:
                            if (neighbor.token.data == tokenToCheck.data)
                            {
                                shouldContinue = true;
                            }
                            else
                            {
                                //not the same
                                if(neighbor.token.data.num == tokenToCheck.data.num)
                                {
                                    //same number
                                    if(neighbor.token.data.color == TokenColor.Purple)
                                    {
                                        if(tokenToCheck.data.color == TokenColor.Blue || tokenToCheck.data.color == TokenColor.Red)
                                        {
                                            shouldContinue = true;
                                        }
                                    }
                                    if(tokenToCheck.data.color == TokenColor.Purple)
                                    {
                                        if (neighbor.token.data.color == TokenColor.Blue || neighbor.token.data.color == TokenColor.Red)
                                        {
                                            //tokenGroup.Add(neighbor.token);
                                            shouldContinue = true;
                                        }
                                    }
                                }
                            }
                            break;
                        case Check.Color:
                            if (neighbor.token.data.color == tokenToCheck.data.color)
                            {
                                shouldContinue = true;
                            }
                            break;
                        case Check.Num:
                            if (neighbor.token.data.num == tokenToCheck.data.num)
                            {
                                shouldContinue = true;
                            }
                            break;
                    }
                    if (shouldContinue)
                    {
                        if (hadAToken)
                        {
                            neighbor.CheckNeighbors(check, tokenGroup,depth+1,tokenToCheck);
                        }
                        else
                        {
                            neighbor.CheckNeighbors(check, tokenGroup,depth+1);
                        }
                        
                    }
                }
            }
        }
    }
    public class Bag
    {
        public Game game;
        public Dictionary<TokenData, int> startingBagContents;
        public Dictionary<TokenData, int> bagContents; //prototypical bag
        public List<TokenData> bag = new List<TokenData>();
        public List<TokenData> nextBagsTemporary = new List<TokenData>();
        public List<TokenData> playedTempTiles = new List<TokenData>();
        public List<TokenData> tilesDrawnThisBag = new List<TokenData>();

        public Bag(Game game,Dictionary<TokenData, int> _bagContents)
        {
            this.game = game;
            this.bagContents = new Dictionary<TokenData, int>();
            startingBagContents = new Dictionary<TokenData, int>();
            foreach(TokenData token in _bagContents.Keys)
            {
                if (token.temporary)
                {
                    for (int i = 0; i < _bagContents[token]; i++)
                    {
                        nextBagsTemporary.Add(token);
                    }
                    continue;
                }
                bagContents.Add(token, _bagContents[token]);
                startingBagContents.Add(token, _bagContents[token]);
            }
            RefillBag();

        }
        public Dictionary<TokenData,Vector2Int> GetCurrentBag()
        {
            //vector2: x is how many you currently have, y is how many you're supposed to have
            Dictionary<TokenData, Vector2Int> contents = new Dictionary<TokenData, Vector2Int>();
            /*foreach(TokenData token in bagContents.Keys)
            {
                Vector2Int num = new Vector2Int(0, bagContents[token]);
                contents.Add(token,num);
            }*/
            foreach(TokenData token in bag)
            {
                
                if (contents.ContainsKey(token))
                {
                    contents[token] += new Vector2Int(1, 1);
                }
                else
                {
                    contents.Add(token, new Vector2Int(1, 1));
                }
            }
            foreach(TokenData token in tilesDrawnThisBag)
            {
                if (contents.ContainsKey(token))
                {
                    contents[token] += new Vector2Int(0, 1);
                }
                else
                {
                    contents.Add(token, new Vector2Int(0, 1));
                }
            }
            return contents;
        }
        public Dictionary<TokenData,Vector2Int> GetNextBag()
        {
            Dictionary<TokenData, Vector2Int> contents = new Dictionary<TokenData, Vector2Int>();
            //todo: implement new and changed tags by comparing bagContents with bag and aaah
            foreach (TokenData token in bagContents.Keys)
            {
                Vector2Int num = new Vector2Int(bagContents[token], bagContents[token]);
                contents.Add(token, num);
            }
            foreach(TokenData token in nextBagsTemporary)
            {
                if (contents.ContainsKey(token))
                {
                    contents[token] += Vector2Int.right;
                }
                else
                {
                    contents.Add(token, new Vector2Int(1, 1));
                }
            }
            return contents;
        }
        public void ResetBag()
        {
            tilesDrawnThisBag.Clear();
            bagContents.Clear();
            foreach (TokenData token in startingBagContents.Keys)
            {
                bagContents.Add(token, startingBagContents[token]);
            }
        }
        public void AddContents(Dictionary<TokenData, int> newContents,bool loading = false)
        {
            foreach (TokenData tokenData in newContents.Keys)
            {
                if (newContents[tokenData] == 100) { continue; }
                if (tokenData.temporary)
                {
                    if(loading == false)
                    {
                        for (int i = 0; i < newContents[tokenData]; i++)
                        {
                            nextBagsTemporary.Add(tokenData);
                        }
                        
                    }
                    continue;

                }
                if (bagContents.ContainsKey(tokenData))
                {
                    bagContents[tokenData] += newContents[tokenData];
                    if (bagContents[tokenData] <= 0 && loading == false)
                    {
                        bagContents.Remove(tokenData);
                    }
                }
                else
                {
                    bagContents.Add(tokenData, newContents[tokenData]);
                }
            }
            if (loading)
            {
                List<TokenData> removed = new List<TokenData>();
                foreach(TokenData tokenData in bagContents.Keys)
                {
                    if (bagContents[tokenData] <= 0)
                    {
                        removed.Add(tokenData);
                    }
                }
                foreach(TokenData tokenData in removed)
                {
                    bagContents.Remove(tokenData);
                }
            }
        }
        void RefillBag()
        {
            tilesDrawnThisBag.Clear();
            game.status.events.Add(new StatusReport.Event(StatusReport.EventType.BagRefill,0));
            bag.Clear();
            foreach (var tokenData in bagContents.Keys)
            {
                for (int i = 0; i < bagContents[tokenData]; i++)
                {
                    bag.Add(tokenData);
                }
            }
            foreach(TokenData tokenData in nextBagsTemporary)
            {
                bag.Add(tokenData);
            }
            nextBagsTemporary.Clear();
            playedTempTiles.Clear();
            Shuffle();
        }
        public TokenData DrawToken()
        {
            //Shuffle();
            TokenData tokenData = bag[bag.Count - 1];
            tilesDrawnThisBag.Add(tokenData);
            bag.RemoveAt(bag.Count - 1);
            if (bag.Count <= 0)
            {
                RefillBag();
            }
            return tokenData;
        }
        public void AddTokenBack(Token token)
        {
            
            TokenData tokenData = token.data;
            if (tilesDrawnThisBag.Contains(tokenData))
            {
                tilesDrawnThisBag.Remove(tokenData);
            }
            bag.Add(tokenData);
            Shuffle();
        }
        public void PlayedTempToken(Token token)
        {
            playedTempTiles.Add(token.data);
        }
        public void RemoveToken(Token token)
        {
            TokenData tokenData = token.data;
            if (bagContents.ContainsKey(tokenData))
            {
                bagContents[tokenData]--;
                if (bagContents[tokenData] <= 0)
                {
                    bagContents.Remove(tokenData);
                }
            }
        }

        public override string ToString()
        {
            string s = string.Empty;
            s += "Next Bag\n";
            foreach (var tokenData in bagContents.Keys)
            {
                for (int i = 0; i < bagContents[tokenData]; i++)
                {
                    s += tokenData.ToString() + ", ";
                }

            }
            s += "|";
            for (int i = 0; i < nextBagsTemporary.Count; i++)
            {
                if (s != string.Empty)
                {
                    s += ",";
                }
                s += nextBagsTemporary[i].ToString();
            }
            s += "\nThis Bag\n";
            for(int i = 0; i < bag.Count; i++)
            {
                if (s != string.Empty)
                {
                    s += ",";
                }
                s += bag[i].ToString();
            }
            return s;
        }

        void Shuffle()
        {
            var count = bag.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = bag[i];
                bag[i] = bag[r];
                bag[r] = tmp;
            }
        }

    }
    [System.Serializable]
    public enum TokenColor
    {
        Red,
        Green,
        Blue,
        Purple,
        Clipper,
        Gold,
        Spade,
        Adder,
        Gnome
    }
    public class Token
    {
        public TokenData data;
        public Tile tile;
        public Vector2Int pos => tile.pos;
        bool inHand = false;
        public uint turnPlaced = 0;

        public Token(TokenData data, bool inHand)
        {
            this.data = data;
            this.inHand = inHand;
        }

        public override string ToString()
        {
            string s = string.Empty;
            s += data.ToString();
            return s;
        }
        public void Destroy()
        {
            if (ReferenceEquals(tile, null) == false)
            {
                tile.token = null;
            }
        }
    }
    [System.Serializable]
    public struct TokenData
    {
        public TokenColor color;
        public int num;
        public bool temporary;
        public TokenData(TokenColor color, int num)
        {
            this.color = color;
            this.num = num;
            this.temporary = false;
        }
        public TokenData(TokenColor color, int num, bool temporary)
        {
            this.color = color;
            this.num = num;
            this.temporary = temporary;
        }
        public override string ToString()
        {
            string s = string.Empty;
            if(color == TokenColor.Clipper || color == TokenColor.Adder || color == TokenColor.Spade || color == TokenColor.Gold)
            {
                s += color.ToString().Substring(0, 1) + num.ToString();
            }
            else
            {
                s += "<color=\"" + color.ToString().ToLower() + "\">";
                s += num.ToString();
                s += "</color>";
            }
            
            return s;
        }
        public string ShortString()
        {
            string s = string.Empty;
            s += color.ToString().ToLower().Substring(0, 1);
            if(color == TokenColor.Gnome)
            {
                s += "n";
            }
            s += num.ToString();
            return s;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            TokenData _data = (TokenData)obj;
            if (_data.color == this.color && _data.num == this.num)
            {
                return true;
            }
            return false;
        }
        public static bool operator ==(TokenData c1, TokenData c2)
        {
            if (c1 == null && c2 == null)
            {
                return true;
            }
            if (c1 == null || c2 == null) { return false; }
            return (c1.color == c2.color && c1.num == c2.num);
        }
        public static bool operator !=(TokenData c1, TokenData c2)
        {
            return !(c1.color == c2.color && c1.num == c2.num);
        }

        public int CompareTo(TokenData other)
        {
            int a = (int)color;
            //this is a stupid fix to reorder red,green,blue into blue,red,green in the bag display
            switch (a)
            {
                case 0:
                    a = 1;
                    break;
                case 1:
                    a = 2;
                    break;
                case 2:
                    a = 0;
                    break;
            }
            int b = (int)other.color;
            switch (b)
            {
                case 0:
                    b = 1;
                    break;
                case 1:
                    b = 2;
                    break;
                case 2:
                    b = 0;
                    break;
            }
            if (a < b)
            {
                return -1;
            }
            if (a > b)
            {
                return 1;
            }
            //same color
            if(num < other.num)
            {
                return -1;
            }
            if(num > other.num)
            {
                return 1;
            }
            return 0;

        }
    }
    public class Hand
    {
        public int handSize;
        public int handChoices;//how many can you choose
        public Token[] tokens;

        public int tokensTaken = 0;
        public Hand(int handSize, int handChoices)
        {
            this.handSize = handSize;
            this.handChoices = handChoices;
            tokens = new Token[handSize];
        }
        public Hand(int handSize)
        {
            this.handSize = handSize;
            this.handChoices = -1;
            tokens = new Token[handSize];
        }
        public void FillHand(Bag bag)
        {
            tokensTaken = 0;
            for (int i = 0; i < handSize; i++)
            {
                TokenData nextData = bag.DrawToken();
                tokens[i] = new Token(nextData, true);
            }
        }
        public void TutorialHand(int num,Bag bag)
        {
            if(num == 0)
            {
                for (int i = 0; i < handSize; i++)
                {
                    tokens[i] = new Token(new TokenData(TokenColor.Blue, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
            }
            if(num == 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    tokens[i] = new Token(new TokenData(TokenColor.Blue, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 2; i++)
                {
                    tokens[i+2] = new Token(new TokenData(TokenColor.Red, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
            }
            if(num == 2)
            {
                for (int i = 0; i < 1; i++)
                {
                    tokens[i] = new Token(new TokenData(TokenColor.Blue, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 1; i++)
                {
                    tokens[i + 1] = new Token(new TokenData(TokenColor.Red, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 2; i++)
                {
                    tokens[i+2] = new Token(new TokenData(TokenColor.Green, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Green, 1));
                }
            }
            if(num == 3)
            {
                //blue,red,green,red
                for (int i = 0; i < 1; i++)
                {
                    tokens[i] = new Token(new TokenData(TokenColor.Blue, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 2; i++)
                {
                    tokens[i + 1] = new Token(new TokenData(TokenColor.Red, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 1; i++)
                {
                    tokens[i + 3] = new Token(new TokenData(TokenColor.Green, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Green, 1));
                }
            }
            if (num == 4)
            {
                for (int i = 0; i < 1; i++)
                {
                    tokens[i] = new Token(new TokenData(TokenColor.Blue, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 1; i++)
                {
                    tokens[i + 1] = new Token(new TokenData(TokenColor.Red, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Blue, 1));
                }
                for (int i = 0; i < 1; i++)
                {
                    tokens[i +2] = new Token(new TokenData(TokenColor.Green, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Green, 1));
                }
                for (int i = 0; i < 1; i++)
                {
                    tokens[i + 3] = new Token(new TokenData(TokenColor.Purple, 1), true);
                    bag.bag.Remove(new TokenData(TokenColor.Green, 1));
                }
            }
            
        }
        public void ReturnHand(Bag bag)
        {
            for(int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == null) { continue; }
                Token token = tokens[i];
                bag.AddTokenBack(token);

            }
            EmptyHand();
        }
        public void EmptyHand()
        {
            tokens = new Token[handSize];
        }
        public Token TakeToken(int _index)
        {
            Token token = tokens[_index];
            tokensTaken++;
            tokens[_index] = null;
            return token;
        }
        public bool IsHandEmpty()
        {
            if (handChoices == -1)
            {
                foreach(Token token in tokens)
                {
                    if(token != null)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                if (tokensTaken >= handChoices)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
    public enum ProgressType
    {
        Score,
        Token
    }
    public abstract class Progress
    {
        public Game game;
        public ProgressType type;
        public virtual List<Dictionary<TokenData, int>> CheckProgress(Token token) { return new List<Dictionary<TokenData, int>>(); }
        public virtual List<bool> GetUnlockedByIndex() { return new List<bool>(); }
        public virtual Dictionary<TokenData, int> LoadFromIndexList(List<bool> completed) { return new Dictionary<TokenData, int>(); }
    }
    public class ProgressNew : Progress
    {
        public List<Unlock> unlocks = new List<Unlock>();
        Dictionary<string, Unlock> name2Unlock = new Dictionary<string, Unlock>();
        public ProgressNew(Game game,List<Unlock> tokenUnlocks)
        {
            this.game = game;
            type = ProgressType.Token;
            unlocks = tokenUnlocks;
            foreach(Unlock unlock in unlocks)
            {
                name2Unlock.Add(unlock.ID(), unlock);
            }
        }
        public override List<Dictionary<TokenData, int>> CheckProgress(Token token)
        {
            List<Dictionary<TokenData,int>> newerContents = new List<Dictionary<TokenData,int>>();
            
            foreach(Unlock unlock in unlocks)
            {
                Dictionary<TokenData, int> newContents = new Dictionary<TokenData, int>();
                if (unlock.repeatable == false && unlock.unlocked > 0) { continue; }
                bool did_i_unlock = true;
                bool newTokenIsNeeded = false;
                foreach (TokenData tokenData in unlock.triggers)
                {
                    newContents.Add(tokenData, 100);
                    if (game.grid.Contains(tokenData) == false)
                    {
                        did_i_unlock = false;
                    }
                    if (token.data == tokenData)
                    {
                        newTokenIsNeeded = true;
                    }
                }
                if(did_i_unlock && newTokenIsNeeded)
                {
                    bool hasenough = true;
                    if (unlock.replacing)
                    {
                        foreach(TokenData tokenData in unlock.rewards.Keys)
                        {
                            if (unlock.rewards[tokenData] < 0)
                            {
                                if (game.bag.bagContents.ContainsKey(tokenData) == false)
                                {
                                    hasenough = false;
                                }
                            }
                            
                        }
                    }
                    if (hasenough)
                    {
                        unlock.ActuallyUnlock(newContents);
                        newerContents.Add(newContents);
                    }
                    
                    
                }
            }


            return newerContents;
        }
        public string SaveUnlocks()
        {
            string s = string.Empty;
            foreach(Unlock unlock in unlocks)
            {
                if(s != string.Empty)
                {
                    s += "_";
                }
                s += unlock.ID() + "|" + unlock.unlocked.ToString();
            }
            return s;
        }
        public Dictionary<TokenData,int> LoadUnlocks(string s)
        {
            string[] split = s.Split('_');
            foreach(string info in split)
            {
                string[] info_split = info.Split('|');
                string id = info_split[0];
                int num = int.Parse(info_split[1]);
                if(name2Unlock.ContainsKey(id) != false)
                {
                    name2Unlock[id].unlocked = num;
                }
            }
            Dictionary<TokenData, int> newContents = new Dictionary<TokenData, int>();
            foreach(Unlock unlock in unlocks)
            {
                int unlock_num = unlock.unlocked;
                unlock.unlocked = 0;
                for(int i = 0; i < unlock_num; i++)
                {
                    unlock.ActuallyUnlock(newContents);
                }
            }
            return newContents;
        }

    }
    public class Unlock
    {
        public List<TokenData> triggers;
        public Dictionary<TokenData, int> rewards;
        public bool repeatable = false;
        public bool replacing = false;

        public int unlocked = 0;
        public Unlock(List<TokenData> triggers, Dictionary<TokenData, int> rewards, bool repeatable)
        {
            this.triggers = triggers;
            this.rewards = rewards;
            this.repeatable = repeatable;
            foreach(TokenData reward in rewards.Keys)
            {
                if (rewards[reward] < 0)
                {
                    replacing = true;
                }
            }
        }
        public void ActuallyUnlock(Dictionary<TokenData,int> newContents)
        {
            unlocked++;
            foreach (TokenData tokenData in rewards.Keys)
            {
                if (newContents.ContainsKey(tokenData))
                {
                    newContents[tokenData] += rewards[tokenData];
                }
                else
                {
                    newContents.Add(tokenData, rewards[tokenData]);
                }
                
            }
        }
        public string ID()
        {
            string s = "";
            foreach(TokenData t in triggers)
            {
                s += t.ShortString();
            }
            s += ":";
            foreach(TokenData t in rewards.Keys)
            {
                s += t.ShortString();
            }

            return s;
        }
        public bool isMyId(string s)
        {
            if(s == ID())
            {
                return true;
            }
            return false;
        }
    }
    public class ProgressToken : Progress
    {
        public Dictionary<List<TokenData>, Dictionary<TokenData, int>> tokenUnlocks;
        public Dictionary<List<TokenData>, bool> unlocked;
        public Dictionary<List<TokenData>, bool> prototypical;
        public Dictionary<List<TokenData>, int> prototypicalCount;

        public ProgressToken(Game game, Dictionary<List<TokenData>, Dictionary<TokenData, int>> tokenUnlocks, Dictionary<List<TokenData>,bool> _proto)
        {
            this.game = game;
            type = ProgressType.Token;
            this.tokenUnlocks = tokenUnlocks;
            unlocked = new Dictionary<List<TokenData>, bool>();
            prototypical = _proto;
            prototypicalCount = new Dictionary<List<TokenData>, int>();
            foreach (List<TokenData> tokens in tokenUnlocks.Keys)
            {
                unlocked.Add(tokens, false);
                prototypicalCount.Add(tokens, 0);
            }
        }

        public override List<Dictionary<TokenData, int>> CheckProgress(Token token)
        {
            List<Dictionary<TokenData,int>> newerContents = new List<Dictionary<TokenData,int>>();
            

            foreach (List<TokenData> tokens in tokenUnlocks.Keys)
            {
                Dictionary<TokenData, int> newContents = new Dictionary<TokenData, int>();
                if (unlocked[tokens] && prototypical[tokens] == false) { continue; }
                //does this tokenData exist on the board?
                bool _unlock = true;
                bool newTokenIsNeeded = false;
                foreach(TokenData tokenData in tokens)
                {
                    if (game.grid.Contains(tokenData) == false)
                    {
                        _unlock = false;
                    }
                    if(token.data == tokenData)
                    {
                        newTokenIsNeeded = true;
                    }
                }
                if (_unlock && newTokenIsNeeded)
                {
                    unlocked[tokens] = true;
                    if (prototypical[tokens])
                    {
                        prototypicalCount[tokens]++;
                    }
                    foreach(TokenData tokenData in tokenUnlocks[tokens].Keys)
                    {
                        newContents.Add(tokenData, tokenUnlocks[tokens][tokenData]);
                    }
                }
                if(newContents.Count == 0) { continue; }
                foreach (TokenData tokenData in newContents.Keys)
                {
                    if (newContents[tokenData] < 0)
                    {
                        if (game.bag.bagContents.ContainsKey(tokenData) == false)
                        {
                            //you don't have that token to upgrade :(
                            continue;
                        }
                    }
                    
                }
                newerContents.Add(newContents);

            }
     
           

            return newerContents;
        }
        public override List<bool> GetUnlockedByIndex()
        {
            List<bool> completed = new List<bool>();
            foreach(List<TokenData> tokens in tokenUnlocks.Keys)
            {
                completed.Add(unlocked[tokens]);
            }
            return completed;
        }
        public override Dictionary<TokenData, int> LoadFromIndexList(List<bool> completed)
        {
            Dictionary<TokenData, int> newContents = new Dictionary<TokenData, int>();
            int i = 0;
            foreach(List<TokenData> tokens in tokenUnlocks.Keys)
            {
                if (completed[i] && unlocked[tokens] == false)
                {
                    unlocked[tokens] = true;
                    foreach (TokenData unlockedToken in tokenUnlocks[tokens].Keys)
                    {
                        newContents.Add(unlockedToken, tokenUnlocks[tokens][unlockedToken]);
                    }
                }
                i++;
            }
            return newContents;
        }

    }
    
    public class ProgressScore : Progress
    {
        public Dictionary<int, Dictionary<TokenData, int>> scoreUnlocks;
        public Dictionary<int, bool> unlocked;
        public ProgressScore(Game game, Dictionary<int, Dictionary<TokenData, int>> scoreUnlocks)
        {
            this.game = game;
            type = ProgressType.Score;
            this.scoreUnlocks = scoreUnlocks;

            unlocked = new Dictionary<int, bool>();
            foreach (int score in scoreUnlocks.Keys)
            {
                unlocked.Add(score, false);
            }
        }
    }
    public class History
    {
        public Game game;
        public List<Turn> turns;
        public static TokenData nullToken = new TokenData(TokenColor.Red, -1);

        public History(Game game)
        {
            this.game = game;
            this.turns = new List<Turn>();

            
        }
        [System.Serializable]
        public class Turn
        {
            //score/progress
            public int score;
            //todo: how to save progress hmm
            //state of grid
            public List<TokenData> grid;//null for empty, and %width to get actual position
                                        //state of hand
            public List<TokenData> hand;
            public int tokensTaken;
            public TokenData freeSlot;
            //state of bag
            public List<TokenData> currentBag;
            public List<TokenData> nextBag;
            public List<TokenData> playedTokens;
            //state of progress
            public string unlocked;

            public Turn(Game game)
            {
                score = game.score;
                grid = new List<TokenData>();
                for (int y = 0; y < game.grid.gridSize.y; y++)
                {
                    for (int x = 0; x < game.grid.gridSize.x; x++)
                    {
                        Vector2Int p = new Vector2Int(x, y);
                        if (game.grid.tiles[p].IsEmpty())
                        {
                            grid.Add(History.nullToken);
                        }
                        else
                        {
                            grid.Add(game.grid.tiles[p].token.data);
                        }
                    }
                }
                hand = new List<TokenData>();
                tokensTaken = game.hand.tokensTaken;
                foreach (Token token in game.hand.tokens)
                {
                    if (token != null)
                    {
                        hand.Add(token.data);
                    }
                    else
                    {
                        hand.Add(History.nullToken);
                    }

                }
                if(game.freeSlot != null)
                {
                    freeSlot = game.freeSlot.data;
                }
                else
                {
                    freeSlot = nullToken;
                }
                currentBag = new List<TokenData>(game.bag.bag);
                nextBag = new List<TokenData>(game.bag.nextBagsTemporary);
                playedTokens = new List<TokenData>(game.bag.tilesDrawnThisBag);
                unlocked = ((ProgressNew)game.progress).SaveUnlocks();
            }
            public void Load(Game game)
            {
                game.score = score;
                //make sure the grid is clear before this
                game.grid.Clear();
                //grid
                for (int i = 0; i < grid.Count; i++)
                {
                    if (grid[i] == History.nullToken) { continue; }
                    Vector2Int p = new Vector2Int(i % game.grid.gridSize.x, i / game.grid.gridSize.y);
                    game.grid.PlaceToken(p, new Token(grid[i], false));
                }
                //hand
                game.hand.tokensTaken = tokensTaken;
                for (int i = 0; i < hand.Count; i++)
                {
                    if (hand[i] == History.nullToken)
                    {
                        game.hand.tokens[i] = null;
                    }
                    else
                    {
                        game.hand.tokens[i] = new Token(hand[i], true);
                    }
                }
                if(freeSlot != nullToken)
                {
                    game.freeSlot = new Token(freeSlot, true);
                }
                else
                {
                    game.freeSlot = null;
                }
                //bag
                game.bag.bag = currentBag;
                game.bag.nextBagsTemporary = nextBag;
                game.bag.ResetBag();
                game.bag.tilesDrawnThisBag = playedTokens;
                
                Dictionary<TokenData, int> updatedContents = ((ProgressNew)game.progress).LoadUnlocks(unlocked);
                if (updatedContents.Count > 0)
                {
                    game.bag.AddContents(updatedContents,true);
                }
            }
        }
    }
}