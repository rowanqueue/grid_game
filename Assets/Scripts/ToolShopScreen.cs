using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ToolShopScreen : MonoBehaviour
{
    List<Token> tokensToBuy = new List<Token>();
    public List<Transform> handTransforms = new List<Transform>(); 

    [Tooltip("Transform to slide in from the edge. If null, uses this object.")]
    public Transform slideRoot;
    [Tooltip("World units to offset the panel when off-screen (positive = from right).")]
    public float slideOffset = 8f;
    [Tooltip("Duration of the slide-in animation in seconds.")]
    public float slideInDuration = 0.35f;
    [Tooltip("Duration of the slide-out animation in seconds.")]
    public float slideOutDuration = 0.55f;
    [Tooltip("Collider covering the shop panel; clicks inside do not dismiss the shop.")]
    public Collider2D panelCollider;

    Vector3 restPosition;
    bool restPositionValid;
    Coroutine slideCoroutine;

    public bool IsAnimating => slideCoroutine != null;

    public bool IsPointerOverPanel()
    {
        if (panelCollider != null && InputHelper.IsPointerOverCollider(panelCollider))
        {
            return true;
        }

        Vector2 world = InputHelper.GetPointerWorldPosition();
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.enabled)
            {
                continue;
            }
            if (col.OverlapPoint(world))
            {
                return true;
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
            {
                continue;
            }
            Bounds bounds = r.bounds;
            Vector3 point = new Vector3(world.x, world.y, bounds.center.z);
            if (bounds.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    void Awake()
    {
        EnableShopButtons();
        BindUnboundBuyButtons();
    }

    void OnEnable()
    {
        EnableShopButtons();
    }

    void EnableShopButtons()
    {
        flora.Button[] buttons = GetComponentsInChildren<flora.Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].allowDuringToolShop = true;
            }
        }
    }

    void BindUnboundBuyButtons()
    {
        flora.Button[] buttons = GetComponentsInChildren<flora.Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            flora.Button button = buttons[i];
            if (button == null || button._event == null)
            {
                continue;
            }
            for (int c = 0; c < button._event.GetPersistentEventCount(); c++)
            {
                if (button._event.GetPersistentTarget(c) != null)
                {
                    continue;
                }
                switch (button._event.GetPersistentMethodName(c))
                {
                    case "BuySpade":
                        button._event.AddListener(BuySpade);
                        break;
                    case "BuyAdder":
                        button._event.AddListener(BuyAdder);
                        break;
                    case "BuyClipper":
                        button._event.AddListener(BuyClipper);
                        break;
                }
            }
        }
    }

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
            tokensToBuy[i].EnableDisplayShadow();
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
        slideCoroutine = StartCoroutine(SlideTo(restPosition, slideInDuration));
    }

    public void CloseScreen()
    {
        Transform root = slideRoot != null ? slideRoot : transform;
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        Vector3 offScreen = root.position + Vector3.right * slideOffset;
        slideCoroutine = StartCoroutine(SlideTo(offScreen, slideOutDuration, true));
    }

    IEnumerator SlideTo(Vector3 targetPos, float duration, bool closing = false)
    {
        Transform root = slideRoot != null ? slideRoot : transform;
        yield return SlideHelper.SlideWorldPosition(root, targetPos, duration);
        slideCoroutine = null;
        if (closing && Services.GameController.gameState != GameState.ToolShop)
        {
            Services.GameController.DeactivateToolShopScreen();
        }
    }
    void BuyToken(int shopIndex)
    {
        Logic.TokenData tokenData = tokensToBuy[shopIndex].token.data;
        Vector3 startPos = tokensToBuy[shopIndex].transform.position;
        GameController gc = Services.GameController;
        for (int i = 0; i < gc.hand.Count; i++)
        {
            if (gc.hand[i] == null)
            {
                gc.game.hand.AddTokenToHand(i, tokenData);
                gc.CreateHand(false, false);
                if (gc.hand[i] != null)
                {
                    Token shopTile = tokensToBuy[shopIndex];
                    shopTile.gameObject.SetActive(false);
                    gc.hand[i].BeginTravelFromShop(startPos, () => FadeShopTileIn(shopTile));
                }
                return;
            }
        }
        gc.game.bag.AddNewTokenToCurrentBag(tokenData);
        Token shopTileInBag = tokensToBuy[shopIndex];
        shopTileInBag.gameObject.SetActive(false);
        FlyToBag(tokenData, startPos, () => FadeShopTileIn(shopTileInBag));
    }

    void FadeShopTileIn(Token shopTile)
    {
        if (shopTile == null)
        {
            return;
        }

        shopTile.gameObject.SetActive(true);
        SpriteRenderer[] renderers = shopTile.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            float targetAlpha = renderer.color.a;
            Color color = renderer.color;
            renderer.color = new Color(color.r, color.g, color.b, 0f);
            renderer.DOFade(targetAlpha, 0.25f);
        }

        TextMeshPro[] texts = shopTile.GetComponentsInChildren<TextMeshPro>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshPro text = texts[i];
            float targetAlpha = text.alpha;
            text.alpha = 0f;
            DOTween.To(() => text.alpha, alpha => text.alpha = alpha, targetAlpha, 0.25f);
        }
    }

    void FlyToBag(Logic.TokenData tokenData, Vector3 startPos, System.Action onTravelComplete)
    {
        // Visual-only copy; the real token already lives in the bag data.
        GameController gc = Services.GameController;
        Token token = GameObject.Instantiate(gc.tokenPrefab).GetComponent<Token>();
        token.token = new Logic.Token(tokenData, true);
        token.Init(token.token);
        token.EnableDisplayShadow();
        Vector3 bagPosition = gc.bagButtonTransform != null
            ? gc.bagButtonTransform.position
            : new Vector3(-2.5f, -8f, 0f);
        token.BeginTravelFromShopToBag(startPos, bagPosition, onTravelComplete);
    }

    public void BuySpade()
    {
        if(Services.Gems.CanAfford("buySpade") == false)
        {
            Services.Gems.TooExpensive();
            return;
        }
        Services.Gems.SpendGems("buySpade");
        BuyToken(0);
    }
    public void BuyAdder()
    {
        if (Services.Gems.CanAfford("buyAdder") == false)
        {
            Services.Gems.TooExpensive();
            return;
        }
        Services.Gems.SpendGems("buyAdder");
        BuyToken(1);
    }
    public void BuyClipper()
    {
        if (Services.Gems.CanAfford("buyClipper") == false)
        {
            Services.Gems.TooExpensive();
            return;
        }
        Services.Gems.SpendGems("buyClipper");
        BuyToken(2);
    }
}
