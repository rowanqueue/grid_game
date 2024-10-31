using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BagDisplay : MonoBehaviour
{
    public bool bagUpdated = true;
    public Logic.Game game;
    public Vector2 firstGridPos;
    public Vector2 gridSeparation;
    public int perRow;
    public Dictionary<Logic.TokenData,int> bagContents = new Dictionary<Logic.TokenData,int>();
    public List<Token> tokens = new List<Token>();
    public int numTokens = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(bagUpdated)
        {
            bagUpdated = false;
            MakeBag();
        }
        for(int i = 0; i < numTokens; i++)
        {
            Vector2 move = new Vector2(i%perRow*gridSeparation.x,i/perRow*gridSeparation.y);
            tokens[i].Draw(firstGridPos+move);
        }
    }
    public void MakeBag()
    {
        bagContents = game.bag.bagContents;
        int i = 0;
        foreach(Logic.TokenData tokenData in bagContents.Keys)
        {

            for (int j = 0; j < bagContents[tokenData]; j++)
            {
                Token token;
                if (i <= tokens.Count)
                {
                    tokens.Add(GameObject.Instantiate(Services.GameController.tokenPrefab, transform).GetComponent<Token>());
                }
                token = tokens[i];
                token.Init(new Logic.Token(tokenData, true));
                token.gameObject.SetActive(true);
                token.UpdateLayer("UIToken");
                i++;
                numTokens = i;
            }
            
        }
        for(int j = numTokens; j < tokens.Count; j++)
        {
            tokens[j].gameObject.SetActive(false);
        }
    }
}
