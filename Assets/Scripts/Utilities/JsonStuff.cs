// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;
namespace Json
{
    [System.Serializable]
    public class ColorScoreMultiplier
    {
        public int blue { get; set; }
        public int red { get; set; }
        public int green { get; set; }
        public int purple { get; set; }
        public int gold { get; set; }
    }
    [System.Serializable]
    public class Event
    {
        public bool prototypical { get; set; }
        public bool repeatable { get; set; }
        public string __helper { get; set; }
        public List<Trigger> trigger { get; set; }
        public List<Reward> reward { get; set; }
    }
    [System.Serializable]
    public class GameVariables
    {
        public int groupCollapseNum { get; set; }
        public int maxTileNum { get; set; }
    }
    [System.Serializable]
    public class GridSize
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    [System.Serializable]
    public class Progress
    {
        public List<Event> events { get; set; }
    }
    [System.Serializable]
    public class ReplacesToken
    {
        public string color { get; set; }
        public int number { get; set; }
    }
    [System.Serializable]
    public class Reward
    {
        public Token token { get; set; }
        public ReplacesToken replacesToken { get; set; }
        public int count { get; set; }
    }
    [System.Serializable]
    public class Root
    {
        public string name { get; set; }
        public int version { get; set; }
        public GridSize gridSize { get; set; }
        public int handSize { get; set; }
        public GameVariables gameVariables { get; set; }
        public TokenVariables tokenVariables { get; set; }
        public ScoreVariables scoreVariables { get; set; }
        public List<StartingBag> startingBag { get; set; }
        public Progress progress { get; set; }
    }
    [System.Serializable]
    public class ScoreVariables
    {
        public ColorScoreMultiplier colorScoreMultiplier { get; set; }
    }
    [System.Serializable]
    public class StartingBag
    {
        public Token token { get; set; }
        public int count { get; set; }
    }
    [System.Serializable]
    public class Token
    {
        public string color { get; set; }
        public int number { get; set; }
        public bool temporary { get; set; }
    }
    [System.Serializable]
    public class TokenVariables
    {
        public List<string> color { get; set; }
        public List<int> number { get; set; }
        public List<string> special { get; set; }
        public bool temporary { get; set; }
    }
    [System.Serializable]
    public class Trigger
    {
        public Token token { get; set; }
    }
}


