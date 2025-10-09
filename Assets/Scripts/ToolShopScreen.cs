using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolShopScreen : MonoBehaviour
{
    List<Token> tokensToBuy = new List<Token>();
    public List<Transform> handTransforms = new List<Transform>();

    private void Start()
    {
        for(int i = 0; i < 3; i++)
        {
            Token token = GameObject.Instantiate(Services.GameController.tokenPrefab, handTransforms[i]).GetComponent<Token>();
            Logic.TokenData tokenData = new Logic.TokenData(Logic.TokenColor.Red,1);
            switch (i)
            {
                case 0:
                    tokenData = new Logic.TokenData(Logic.TokenColor.Spade, 0);
                    break;
                case 1:
                    tokenData = new Logic.TokenData(Logic.TokenColor.Adder, 0);
                    break;
                case 2:
                    tokenData = new Logic.TokenData(Logic.TokenColor.Clipper, -1);
                    break;
            }
            tokenData.temporary = true;
            token.token = new Logic.Token(tokenData,true);
            tokensToBuy.Add(token);
            tokensToBuy[i].Init(token.token);
            tokensToBuy[i].UpdateLayer("UIToken");
            tokensToBuy[i].transform.position = handTransforms[i].position;
        }
    }
    public void OpenScreen()
    {
        
    }
    public void CloseScreen()
    {

    }
    public bool FreeSpaceInHands()
    {
        for (int i = 0; i < Services.GameController.hand.Count; i++)
        {
            if (Services.GameController.hand[i] == null)
            {
                return true;
            }
        }
        return false;
    }
    void BuyToken(Logic.TokenData tokenData)
    {
        for (int i = 0; i < Services.GameController.hand.Count; i++)
        {
            if (Services.GameController.hand[i] == null)
            {
                Services.GameController.game.hand.AddTokenToHand(i, tokenData);
                Services.GameController.CreateHand(false, false);
                break;
            }
        }
    }
    public void BuySpade()
    {
        if(FreeSpaceInHands() == false) { return; }
        if(Services.Gems.CanAfford("buySpade") == false)
        {
            return;
        }
        Services.Gems.SpendGems("buySpade");
        BuyToken(tokensToBuy[0].token.data);
        //Services.GameController.game.bag.nextBagsTemporary.Add(tokensToBuy[0].token.data);
    }
    public void BuyAdder()
    {
        if (FreeSpaceInHands() == false) { return; }
        if (Services.Gems.CanAfford("buyAdder") == false)
        {
            return;
        }
        Services.Gems.SpendGems("buyAdder");
        BuyToken(tokensToBuy[1].token.data);
    }
    public void BuyClipper()
    {
        if (FreeSpaceInHands() == false) { return; }
        if (Services.Gems.CanAfford("buyClipper") == false)
        {
            return;
        }
        Services.Gems.SpendGems("buyClipper");
        BuyToken(tokensToBuy[2].token.data);
    }
}
