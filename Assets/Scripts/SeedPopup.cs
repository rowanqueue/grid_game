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
    bool offerAd;
    [SerializeField] GameObject visual;
    [SerializeField] SpriteRenderer dimPanel;
    Transform yesButton;
    Transform noButton;
    Collider2D yesCollider;
    Collider2D noCollider;
    bool closing;
    bool ignoreDismissUntilRelease;
    SpriteRenderer[] fadeRenderers;
    CanvasGroup canvasGroup;
    TextMeshPro messageText;
    string defaultMessage;

    void Awake()
    {
        if (visual == null)
            visual = transform.GetChild(0).gameObject;
        Transform title = visual.transform.Find("Title Text");
        messageText = title != null
            ? title.GetComponent<TextMeshPro>()
            : visual.GetComponentInChildren<TextMeshPro>(true);
        if (messageText != null)
            defaultMessage = messageText.text;

        Transform desc = visual.transform.Find("Desc Text");
        if (desc != null)
            desc.gameObject.SetActive(false);

        Transform seedsButton = visual.transform.Find("Seeds");
        if (seedsButton != null)
            SetupChoiceButtons(seedsButton);

        visual.SetActive(false);
        fadeRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        canvasGroup = visual.GetComponent<CanvasGroup>();
        ApplyOverlaySorting();
    }

    void SetupChoiceButtons(Transform seedsButton)
    {
        yesButton = seedsButton;
        ConfigureChoiceButton(yesButton, "Yes", new Vector3(-1.15f, -0.35f, 0f));
        yesCollider = yesButton.GetComponent<Collider2D>();

        GameObject noObject = Instantiate(yesButton.gameObject, yesButton.parent);
        noObject.name = "No";
        noButton = noObject.transform;
        ConfigureChoiceButton(noButton, "No", new Vector3(1.15f, -0.35f, 0f));
        noCollider = noButton.GetComponent<Collider2D>();
    }

    void StripAnchor(Transform button)
    {
        var anchor = button.GetComponent<AnchorGameObject>();
        if (anchor == null)
            return;
        anchor.enabled = false;
        DestroyImmediate(anchor);
    }

    void ConfigureChoiceButton(Transform button, string labelText, Vector3 localPos)
    {
        StripAnchor(button);

        var floraButton = button.GetComponent<flora.Button>();
        if (floraButton != null)
            floraButton.enabled = false;

        button.gameObject.SetActive(true);
        button.localPosition = localPos;
        button.localRotation = Quaternion.identity;
        button.localScale = Vector3.one;

        for (int i = 0; i < button.childCount; i++)
        {
            Transform child = button.GetChild(i);
            if (child.name.StartsWith("9-Sliced"))
                child.gameObject.SetActive(false);
        }

        TextMeshPro label = button.GetComponentInChildren<TextMeshPro>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.enabled = true;
            label.text = labelText;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = 4f;
            if (label.rectTransform != null)
            {
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(2.2f, 1f);
            }
            label.ForceMeshUpdate();
        }

        var box = button.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.offset = Vector2.zero;
            box.size = new Vector2(2.2f, 1f);
        }
    }

    void SetChoiceVisible(Transform button, Collider2D collider, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        if (collider != null)
            collider.enabled = visible;

        Renderer[] renderers = button.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;

        TextMeshPro[] labels = button.GetComponentsInChildren<TextMeshPro>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].enabled = visible;
            if (visible)
                labels[i].ForceMeshUpdate();
        }
    }

    void ApplyButtonLayout()
    {
        if (yesButton != null)
        {
            yesButton.localPosition = new Vector3(-1.15f, -0.35f, 0f);
            SetChoiceVisible(yesButton, yesCollider, offerAd);
        }
        if (noButton != null)
        {
            noButton.localPosition = offerAd
                ? new Vector3(1.15f, -0.35f, 0f)
                : new Vector3(0f, -0.35f, 0f);
            SetChoiceVisible(noButton, noCollider, true);
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

        if (offerAd && InputHelper.IsPointerOverCollider(yesCollider))
        {
            Close();
            if (Services.Gems != null)
                Services.Gems.WatchAd();
            return;
        }

        if (InputHelper.IsPointerOverCollider(noCollider))
        {
            Close();
            return;
        }

        StartCoroutine(WaitToClose());
    }

    public void OpenWatchAdPrompt(int seedAmount)
    {
        offerAd = true;
        OpenInternal($"Watch an ad to earn {seedAmount} seeds");
    }

    public void Open(string message = null)
    {
        offerAd = false;
        OpenInternal(string.IsNullOrEmpty(message) ? defaultMessage : message);
    }

    void OpenInternal(string message)
    {
        if (closing)
        {
            StopAllCoroutines();
            closing = false;
            KillTweens();
        }

        if (messageText != null)
        {
            messageText.text = message;
            if (messageText.rectTransform != null)
                messageText.rectTransform.sizeDelta = new Vector2(4.5f, 2f);
            messageText.ForceMeshUpdate();
        }

        ignoreDismissUntilRelease = InputHelper.GetPrimaryPressBegan() || InputHelper.GetPrimaryPressHeld();
        ApplyButtonLayout();

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
        ignoreDismissUntilRelease = false;
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
