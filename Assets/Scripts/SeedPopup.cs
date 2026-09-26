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
    //[SerializeField] Collider2D seedsButtonCollider;
    [SerializeField] SpriteRenderer dimPanel;
    bool closing;
    bool ignoreDismissUntilRelease;
    SpriteRenderer[] fadeRenderers;
    CanvasGroup canvasGroup;
    TextMeshPro messageText;
    string defaultMessage;

    void Awake()
    {
        visual = transform.GetChild(0).gameObject;
        //Transform seedsButton = visual.transform.Find("Seeds");
        //if (seedsButton != null)
        //ConfigureSeedsButton(seedsButton);
        visual.SetActive(false);
        fadeRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        canvasGroup = visual.GetComponent<CanvasGroup>();
        messageText = visual.GetComponentInChildren<TextMeshPro>(true);

        if (messageText != null)
            defaultMessage = messageText.text;
        ApplyOverlaySorting();
    }

    void ConfigureSeedsButton(Transform seedsButton)
    {
        var anchor = seedsButton.GetComponent<AnchorGameObject>();
        if (anchor != null)
        {
            anchor.enabled = false;
            Destroy(anchor);
        }

        seedsButton.localPosition = new Vector3(0f, -0.35f, 0f);
        seedsButton.localRotation = Quaternion.identity;
        seedsButton.localScale = Vector3.one;

        for (int i = 0; i < seedsButton.childCount; i++)
        {
            Transform child = seedsButton.GetChild(i);
            if (child.name.StartsWith("9-Sliced"))
                child.gameObject.SetActive(false);
        }

        /*
        TextMeshPro label = seedsButton.GetComponentInChildren<TextMeshPro>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = "Get Seeds";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = 4f;
            if (label.rectTransform != null)
            {
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(4f, 1f);
            }
        }
        */

        var box = seedsButton.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.offset = Vector2.zero;
            box.size = new Vector2(3.5f, 1f);
        }
        //seedsButtonCollider = seedsButton.GetComponent<Collider2D>();
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

    /*
    void Update()
    {
        if (!open || closing)
            return;

        // Same click that opened us (button OnMouseDown → Open) must not dismiss.
        if (ignoreDismissUntilRelease)
        {
            if (!InputHelper.GetPrimaryPressHeld())
                ignoreDismissUntilRelease = false;
            return;
        }

        if (!InputHelper.GetPrimaryPressBegan())
            return;

        if (InputHelper.IsPointerOverCollider(seedsButtonCollider))
        {
            Close();
            return;
        }

        StartCoroutine(WaitToClose());
    }
    */

    public void Open(string message = null)
    {
        if (open)
            return;

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

        open = true;
        ignoreDismissUntilRelease = InputHelper.GetPrimaryPressBegan() || InputHelper.GetPrimaryPressHeld();
        //if (seedsButtonCollider != null)
        //    seedsButtonCollider.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        visual.SetActive(true);
        SetVisualAlpha(0f);
        StartCoroutine(FadeIn());
    }

    public void Close()
    {
        open = false;
        ignoreDismissUntilRelease = false;
        KillTweens();
        visual.SetActive(false);
        dimPanel.gameObject.SetActive(false);
        SetVisualAlpha(1f);
    }

    IEnumerator FadeIn()
    {
        yield return FadeTo(1f, fadeInDuration);
    }

    IEnumerator WaitToClose()
    {
        closing = true;
        yield return new WaitForSeconds(clickDebounce);
        yield return FadeTo(0f, fadeOutDuration);
        Close();
        closing = false;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
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
                color.a = Mathf.Clamp(alphaD,0f, 0.6f);
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
