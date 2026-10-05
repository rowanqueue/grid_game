using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SeedPopup : MonoBehaviour
{
    const int OverlayBaseOrder = 100;

    [SerializeField] float fadeInDuration = 0.15f;
    [SerializeField] float fadeOutDuration = 0.15f;
    [SerializeField] float clickDebounce = 0.1f;

    bool open;
    [SerializeField] GameObject visual;
    [SerializeField] SpriteRenderer dimPanel;
    bool closing;
    SpriteRenderer[] fadeRenderers;
    CanvasGroup canvasGroup;
    TextMeshPro messageText;
    string defaultMessage;

    void Awake()
    {
        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0).gameObject;

        Transform title = visual.transform.Find("Title Text");
        messageText = title != null
            ? title.GetComponent<TextMeshPro>()
            : visual.GetComponentInChildren<TextMeshPro>(true);
        if (messageText != null)
            defaultMessage = messageText.text;

        HideNamedChildren(visual.transform, "Seeds");
        RefreshEarnLabel();

        visual.SetActive(false);
        fadeRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
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
        SetVisualAlpha(0f);
        StartCoroutine(FadeIn());
    }

    public void Close()
    {
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

        if (fadeRenderers == null || fadeRenderers.Length == 0)
            yield break;

        float startAlpha = fadeRenderers[0].color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetVisualAlpha(alpha);
            if (dimPanel != null)
            {
                float alphaD = Mathf.Lerp(startAlpha, targetAlpha / 0.6f, elapsed / duration);
                Color color = dimPanel.color;
                color.a = Mathf.Clamp(alphaD, 0f, 0.6f);
                dimPanel.color = color;
            }
            yield return null;
        }

        SetVisualAlpha(targetAlpha);
    }

    void SetVisualAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (fadeRenderers == null)
            return;

        foreach (SpriteRenderer renderer in fadeRenderers)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    void KillTweens()
    {
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}
