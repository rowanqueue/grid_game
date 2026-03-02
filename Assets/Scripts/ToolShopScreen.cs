using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolShopScreen : MonoBehaviour
{
    List<Token> tokensToBuy = new List<Token>();
    public List<Transform> handTransforms = new List<Transform>();

    [Tooltip("Transform to slide in from the edge. If null, uses this object.")]
    public Transform slideRoot;
    [Tooltip("World units to offset the panel when off-screen (positive = from right).")]
    public float slideOffset = 8f;
    [Tooltip("Duration of the slide-in/out animation in seconds.")]
    public float slideDuration = 0.35f;

    Vector3 restPosition;
    bool restPositionValid;
    Coroutine slideCoroutine;

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
        Transform root = slideRoot != null ? slideRoot : transform;
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        if (!restPositionValid)
        {
            restPosition = root.position;
            restPositionValid = true;
        }
        root.position = restPosition + Vector3.right * slideOffset;
        slideCoroutine = StartCoroutine(SlideTo(restPosition));
    }

    public void CloseScreen()
    {
        Transform root = slideRoot != null ? slideRoot : transform;
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        Vector3 start = root.position;
        Vector3 offScreen = start + Vector3.right * slideOffset;
        slideCoroutine = StartCoroutine(SlideTo(offScreen));
    }

    IEnumerator SlideTo(Vector3 targetPos)
    {
        Transform root = slideRoot != null ? slideRoot : transform;
        Vector3 start = root.position;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = 1f - (1f - t) * (1f - t); // ease-out quadratic
            root.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        root.position = targetPos;
        slideCoroutine = null;
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
