using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SeedPopup : MonoBehaviour
{
    const int OverlayBaseOrder = 100;
    const float DimMaxAlpha = 0.6f;

    [SerializeField] float fadeInDuration = 0.15f;
    [SerializeField] float popInDuration = 0.22f;
    [SerializeField] float popInStartScale = 0.85f;
    Vector3 visualBaseScale = Vector3.one;
    [SerializeField] float fadeOutDuration = 0.15f;
    [SerializeField] float clickDebounce = 0.1f;

    bool open;
    [SerializeField] GameObject visual;
    [SerializeField] SpriteRenderer dimPanel;
    bool closing;
    int closedFrame = -1;
    SpriteRenderer[] fadeRenderers;

    // Also true on the frame it closed, so the click that hit Back doesn't fall through (e.g. closing the tool shop).
    public bool IsBlockingInput => open || Time.frameCount == closedFrame;
    float[] fadeRendererBaseAlphas;
    TextMeshPro[] fadeTexts;
    float[] fadeTextBaseAlphas;
    float currentAlpha;
    CanvasGroup canvasGroup;
    TextMeshPro messageText;
    string defaultMessage;

    void Awake()
    {
        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0).gameObject;
        visualBaseScale = visual.transform.localScale;

        Transform title = visual.transform.Find("Title Text");
        messageText = title != null
            ? title.GetComponent<TextMeshPro>()
            : visual.GetComponentInChildren<TextMeshPro>(true);
        if (messageText != null)
            defaultMessage = messageText.text;

        // Popup can open over the tool shop, which otherwise blocks button input.
        foreach (flora.Button button in visual.GetComponentsInChildren<flora.Button>(true))
            button.allowDuringToolShop = true;

        HideNamedChildren(visual.transform, "Seeds");
        RefreshEarnLabel();

        visual.SetActive(false);
        // Cache authored alphas so the fade scales them (shadows are ~9-45% alpha, not opaque).
        fadeRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        fadeRendererBaseAlphas = new float[fadeRenderers.Length];
        for (int i = 0; i < fadeRenderers.Length; i++)
            fadeRendererBaseAlphas[i] = fadeRenderers[i].color.a;
        fadeTexts = visual.GetComponentsInChildren<TextMeshPro>(true);
        fadeTextBaseAlphas = new float[fadeTexts.Length];
        for (int i = 0; i < fadeTexts.Length; i++)
            fadeTextBaseAlphas[i] = fadeTexts[i].alpha;
        canvasGroup = visual.GetComponent<CanvasGroup>();
        ApplyOverlaySorting();
    }

    Transform FindNamedChild(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                return children[i];
        }
        return null;
    }

    void RefreshEarnLabel()
    {
        if (visual == null)
            return;
        Transform seedPrice = FindNamedChild(visual.transform, "SeedPrice");
        if (seedPrice == null)
            return;
        seedPrice.gameObject.SetActive(true);
        TextMeshPro earnLabel = seedPrice.GetComponentInChildren<TextMeshPro>(true);
        if (earnLabel != null && Services.Gems != null)
        {
            earnLabel.text = "+" + Services.Gems.GetCost("earn");
            earnLabel.ForceMeshUpdate();
        }
    }

    void HideNamedChildren(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                children[i].gameObject.SetActive(false);
        }
    }

    bool overlaySortingApplied;

    void ApplyOverlaySorting()
    {
        // Difficulty UI on UIToken tops out at the seed counter (order 38).
        // Dim sits just under the popup art, and both sit above that counter.
        int layerId = SortingLayer.NameToID("UIToken");
        int contentFloor = OverlayBaseOrder + 1;

        if (dimPanel != null)
        {
            dimPanel.sortingLayerID = layerId;
            dimPanel.sortingOrder = OverlayBaseOrder;
        }

        if (visual == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        if (!overlaySortingApplied)
        {
            int minOrder = int.MaxValue;
            foreach (Renderer renderer in renderers)
                minOrder = Mathf.Min(minOrder, renderer.sortingOrder);

            int shift = contentFloor - minOrder;
            foreach (Renderer renderer in renderers)
            {
                renderer.sortingLayerID = layerId;
                renderer.sortingOrder += shift;
            }
            overlaySortingApplied = true;
        }
        else
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.sortingLayerID = layerId;
                if (renderer.sortingOrder < contentFloor)
                    renderer.sortingOrder = contentFloor;
            }
        }

        // Flowers hanging off the popup stay above the difficulty UI but under the dim.
        ParkDecorationBehindDim(layerId);
    }

    void ParkDecorationBehindDim(int layerId)
    {
        Transform decoration = FindNamedChild(visual.transform, "Decoration");
        if (decoration == null)
            return;

        Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = layerId;
            renderers[i].sortingOrder = OverlayBaseOrder - 1;
        }
    }

    public void OpenWatchAdPrompt(int seedAmount)
    {
        Open("Watch Ads for seeds!");
    }

    public void Open(string message = null)
    {
        if (closing)
        {
            StopAllCoroutines();
            closing = false;
            KillTweens();
        }

        RefreshEarnLabel();

        if (messageText != null)
        {
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
            messageText.ForceMeshUpdate();
        }

        ApplyOverlaySorting();

        if (open && visual.activeSelf)
            return;

        open = true;
        visual.SetActive(true);
        // Close() turns the dim off; FadeTo used to turn it back on, but Open no longer fades.
        if (dimPanel != null)
            dimPanel.gameObject.SetActive(true);
        // Pop in at full opacity: alpha-fading the stacked sprites (9-slice, borders, shadows)
        // makes every layer see-through mid-fade and the panel looks ghostly.
        SetVisualAlpha(1f);
        Transform t = visual.transform;
        t.DOKill();
        t.localScale = visualBaseScale * popInStartScale;
        t.DOScale(visualBaseScale, popInDuration).SetEase(Ease.OutBack);
    }

    public void Close()
    {
        if (open)
            closedFrame = Time.frameCount;
        open = false;
        KillTweens();
        visual.SetActive(false);
        if (dimPanel != null)
            dimPanel.gameObject.SetActive(false);
        SetVisualAlpha(1f);
    }

    IEnumerator FadeIn()
    {
        yield return FadeTo(1f, fadeInDuration);
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (dimPanel != null)
            dimPanel.gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            yield return canvasGroup.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad).WaitForCompletion();
            yield break;
        }

        float startAlpha = currentAlpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetVisualAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetVisualAlpha(targetAlpha);
    }

    void SetVisualAlpha(float alpha)
    {
        currentAlpha = alpha;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (fadeRenderers != null)
        {
            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                if (fadeRenderers[i] == null) continue;
                Color color = fadeRenderers[i].color;
                color.a = fadeRendererBaseAlphas[i] * alpha;
                fadeRenderers[i].color = color;
            }
        }

        if (fadeTexts != null)
        {
            for (int i = 0; i < fadeTexts.Length; i++)
            {
                if (fadeTexts[i] == null) continue;
                fadeTexts[i].alpha = fadeTextBaseAlphas[i] * alpha;
            }
        }

        if (dimPanel != null)
        {
            Color color = dimPanel.color;
            color.a = DimMaxAlpha * alpha;
            dimPanel.color = color;
        }
    }

    void KillTweens()
    {
        if (visual != null)
        {
            visual.transform.DOKill();
            visual.transform.localScale = visualBaseScale;
        }
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}
