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

    void Awake()
    {
        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0).gameObject;

        Transform title = visual.transform.Find("Title Text");
        messageText = title != null
            ? title.GetComponent<TextMeshPro>()
            : visual.GetComponentInChildren<TextMeshPro>(true);

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

    void ApplyOverlaySorting()
    {
        int layerId = SortingLayer.NameToID("UIToken");
        TextMeshPro[] texts = visual.GetComponentsInChildren<TextMeshPro>(true);

        int minOrder = int.MaxValue;
        foreach (SpriteRenderer sr in fadeRenderers)
            minOrder = Mathf.Min(minOrder, sr.sortingOrder);
        foreach (TextMeshPro tmp in texts)
            minOrder = Mathf.Min(minOrder, tmp.sortingOrder);
        if (minOrder == int.MaxValue)
            return;

        int shift = OverlayBaseOrder - minOrder;
        foreach (SpriteRenderer sr in fadeRenderers)
        {
            sr.sortingLayerID = layerId;
            sr.sortingOrder += shift;
        }
        foreach (TextMeshPro tmp in texts)
        {
            tmp.sortingLayerID = layerId;
            tmp.sortingOrder += shift;
        }
    }

    public void OpenWatchAdPrompt(int seedAmount)
    {
        Open();
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

        if (messageText != null && !string.IsNullOrEmpty(message))
        {
            messageText.text = message;
            messageText.ForceMeshUpdate();
        }

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
