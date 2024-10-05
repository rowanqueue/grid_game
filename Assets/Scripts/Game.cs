using Json;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure;

namespace Logic
{
    public class TripleGame : Game
    {
        public new string name = "3tile";
        public new int version = 0;
        public int groupCollapseNum = 3;
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
            {"purple",TokenColor.Purple }
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
            base.Initialize(root);
        }
        protected override void GridChanged(Token tokenChanged)
        {
            //recursively make groups based on neighbors
            List<Token> tokenGroup = new List<Token>();
            if(tokenChanged.data.color == TokenColor.Purple)
            {
                tokenChanged.tile.CheckNeighbors(Check.Equals, tokenGroup);
            }
            else
            {
                tokenChanged.tile.CheckNeighbors(Check.Equals, tokenGroup);
            }
            
            if (tokenGroup.Count >= groupCollapseNum)
            {
                //remove everything except the one you placed and change it to the next num
                for (int i = tokenGroup.Count - 1; i >= 0; i--)
                {
                    Token token = tokenGroup[i];
                    if (token != tokenChanged)
                    {
                        
                        token.Destroy();
                        status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenDestroyed, token));
                        EarnPoints(token.data.num * colorScoreMulti[token.data.color]);
                    }
                }
                Token newToken = new Token(tokenChanged.data, false);
                newToken.data.num += 1;
                
                Tile tile = tokenChanged.tile;
                tokenChanged.Destroy();
                grid.PlaceToken(tile.pos,newToken);
                Dictionary<TokenData, int> updatedContents = progress.CheckProgress(newToken);
                if (updatedContents.Count > 0)
                {
                    bag.AddContents(updatedContents);
                }
                status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenChanged, new List<Token>() { tokenChanged,newToken }));
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
                tile.CheckNeighbors(Check.Color, tokenGroup);
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
                        status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenDestroyed, _token));
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
                            status.events.Add(new StatusReport.Event(StatusReport.EventType.TokenDestroyed, neighbor.token));
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
            TokenDestroyed,
            TokenChanged,//tokens[0] becomes tokens[1]
            ScoreAdded
        }
        public class Event
        {
            public EventType type;
            public List<Token> tokens;
            public int num;
            public Event(EventType type, List<Token> tokens)
            {
                this.type = type;
                this.tokens = tokens;
            }
            public Event(EventType type, Token[] _tokens)
            {
                this.type = type;
                this.tokens = new List<Token>();
                foreach(Token t in _tokens)
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

        public virtual void Initialize(Json.Root root)
        {
            Debug.Log(root.name);
            //gridSize
            gridSize = new Vector2Int(root.gridSize.x, root.gridSize.y);
            //handSize
            handSize = root.handSize;
            //bagContents
            bagContents = new Dictionary<TokenData, int>();
            foreach(StartingBag tokenSet in root.startingBag)
            {
                TokenData _token = ConvertJsonToken(tokenSet.token);
                bagContents.Add(_token, tokenSet.count);
            }
            //progress
            Dictionary<List<TokenData>, Dictionary<TokenData, int>> tokenUnlocks = new Dictionary<List<TokenData>, Dictionary<TokenData, int>>();
            Dictionary<List<TokenData>, bool> prototypical = new Dictionary<List<TokenData>, bool>();
            foreach (Json.Event _event in root.progress.events)
            {
                List<TokenData> triggers = new List<TokenData>();
                foreach(Json.Trigger trigger in _event.trigger)
                {
                    triggers.Add(ConvertJsonToken(trigger.token));
                }
                Dictionary<TokenData, int> rewards = new Dictionary<TokenData, int>();
                foreach (Json.Reward reward in _event.reward)
                {
                    rewards.Add(ConvertJsonToken(reward.token), reward.count);
                    if(reward.replacesToken != null)
                    {
                        TokenData replacedToken = ConvertJsonToken(reward.replacesToken.color, reward.replacesToken.number);
                        rewards.Add(replacedToken, -1);
                    }
                }
                tokenUnlocks.Add(triggers, rewards);
                prototypical.Add(triggers, _event.prototypical);
                if (_event.prototypical)
                {
                    for(int i = 1; i < 10; i++)
                    {
                        List<TokenData> moreTriggers = new List<TokenData>();
                        foreach(TokenData tokenData in triggers)
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
                                moreRewards.Add(new TokenData(reward.color,reward.num+i), count);
                                continue;
                            }
                            moreRewards.Add(new TokenData(reward.color,reward.num + i), rewards[reward]);
                        }
                        tokenUnlocks.Add(moreTriggers, moreRewards);
                        prototypical.Add(moreTriggers, true);
                    }
                }
            }
            progress = new ProgressToken(this, tokenUnlocks,prototypical);
            /*new ProgressToken(this, new Dictionary<List<TokenData>, Dictionary<TokenData, int>>()
        {
            {new List<TokenData>(){new TokenData(TokenColor.Blue,4)}, new Dictionary<TokenData, int>()
                {
                    {new TokenData(TokenColor.Blue,1),-1 },
                    {new TokenData(TokenColor.Blue,2),1 }
                }
            },*/
            grid = new Grid(gridSize);

            bag = new Bag(bagContents);
            hand = new Hand(handSize, handChoices);
            hand.FillHand(bag);
            history = new History(this);
            history.turns.Add(new History.Turn(this));
            status = new StatusReport();
        }
        public TokenData ConvertJsonToken(Json.Token token)
        {
            Dictionary<string, TokenColor> colors = new Dictionary<string, TokenColor>()
            {
                {"red",TokenColor.Red },
                {"blue",TokenColor.Blue },
                {"green",TokenColor.Green },
                {"purple",TokenColor.Purple },
            };
            return new TokenData(colors[token.color], token.number);
        }
        public TokenData ConvertJsonToken(string color, int number)
        {
            Dictionary<string, TokenColor> colors = new Dictionary<string, TokenColor>()
            {
                {"red",TokenColor.Red },
                {"blue",TokenColor.Blue },
                {"green",TokenColor.Green },
                {"purple",TokenColor.Purple },
            };
            return new TokenData(colors[color], number);
        }
        public void EarnPoints(int pts)
        {
            score += pts;
            //todo: score event :(((
            status.events.Add(new StatusReport.Event(StatusReport.EventType.ScoreAdded, pts));
        }
        public bool CanPlaceHere(Vector2Int p)
        {
            if (grid.tiles.ContainsKey(p) && grid.tiles[p].IsEmpty())
            {
                return true;
            }
            return false;
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
            }
            
            gridUpdating = true;
            grid.PlaceToken(gridPos, token);
            GridChanged(token);
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
    }
    public class Grid
    {
        public Vector2Int gridSize;
        public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        public List<Vector2Int> dirs = new List<Vector2Int>()
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };
        public Grid(Vector2Int size)
        {
            this.gridSize = size;
            PopulateGrid();
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
        public void PlaceToken(Vector2Int p, Token token)
        {
            Tile tile = tiles[p];
            tile.token = token;
            token.tile = tile;
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
        public void CheckNeighbors(Check check, List<Token> tokenGroup,Token tokenToCheck = null)
        {
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
                            neighbor.CheckNeighbors(check, tokenGroup,tokenToCheck);
                        }
                        else
                        {
                            neighbor.CheckNeighbors(check, tokenGroup);
                        }
                        
                    }
                }
            }
        }
    }
    public class Bag
    {
        public Dictionary<TokenData, int> bagContents; //prototypical bag
        public List<TokenData> bag = new List<TokenData>();

        public Bag(Dictionary<TokenData, int> bagContents)
        {
            this.bagContents = bagContents;
            RefillBag();

        }
        public void AddContents(Dictionary<TokenData, int> newContents)
        {
            foreach (TokenData tokenData in newContents.Keys)
            {
                if (bagContents.ContainsKey(tokenData))
                {
                    bagContents[tokenData] += newContents[tokenData];
                    if (bagContents[tokenData] <= 0)
                    {
                        bagContents.Remove(tokenData);
                    }
                }
                else
                {
                    bagContents.Add(tokenData, newContents[tokenData]);
                }
            }
        }
        void RefillBag()
        {
            bag.Clear();
            foreach (var tokenData in bagContents.Keys)
            {
                for (int i = 0; i < bagContents[tokenData]; i++)
                {
                    bag.Add(tokenData);
                }
            }
            Shuffle();
        }
        public TokenData DrawToken()
        {
            TokenData tokenData = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            if (bag.Count <= 0)
            {
                RefillBag();
            }
            return tokenData;
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var tokenData in bagContents.Keys)
            {
                for (int i = 0; i < bagContents[tokenData]; i++)
                {
                    s += tokenData.ToString() + ", ";
                }

            }
            /*for(int i = 0; i < bag.Count; i++)
            {
                if (s != string.Empty)
                {
                    s += "-";
                }
                s += bag[i].ToString();
            }*/
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
        Purple
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
        public TokenData(TokenColor color, int num)
        {
            this.color = color;
            this.num = num;
        }
        public override string ToString()
        {
            string s = string.Empty;
            s += "<color=\"" + color.ToString().ToLower() + "\">";
            s += num.ToString();
            s += "</color>";
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
                if (tokensTaken == handSize)
                {
                    return true;
                }
                else
                {
                    return false;
                }
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
        public virtual Dictionary<TokenData, int> CheckProgress(Token token) { return new Dictionary<TokenData, int>(); }
        public virtual List<bool> GetUnlockedByIndex() { return new List<bool>(); }
        public virtual Dictionary<TokenData, int> LoadFromIndexList(List<bool> completed) { return new Dictionary<TokenData, int>(); }
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

        public override Dictionary<TokenData, int> CheckProgress(Token token)
        {
            Dictionary<TokenData, int> newContents = new Dictionary<TokenData, int>();

            foreach (List<TokenData> tokens in tokenUnlocks.Keys)
            {
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
                
            }
            string s = "";
            if(newContents.Count > 0)
            {
                foreach(TokenData tokenData in newContents.Keys)
                {
                    s += tokenData.ToString() + ":" + newContents[tokenData] + ", ";
                    if (newContents[tokenData] < 0)
                    {
                        if(game.bag.bagContents.ContainsKey(tokenData) == false)
                        {
                            //you don't have that token to upgrade :(
                            return new Dictionary<TokenData, int>();
                        }
                    }
                }
                //Debug.Log(s);
            }
           

            return newContents;
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
            //state of progress
            public List<bool> unlocked;

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
                unlocked = game.progress.GetUnlockedByIndex();
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
                //bag
                game.bag.bag = currentBag;
                Dictionary<TokenData, int> updatedContents = game.progress.LoadFromIndexList(unlocked);
                if (updatedContents.Count > 0)
                {
                    game.bag.AddContents(updatedContents);
                }
            }
        }
    }
}