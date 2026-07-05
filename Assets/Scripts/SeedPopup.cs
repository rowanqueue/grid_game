using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SeedPopup : MonoBehaviour
{
    [SerializeField] float fadeInDuration = 0.15f;
    [SerializeField] float fadeOutDuration = 0.15f;
    [SerializeField] float clickDebounce = 0.1f;

    bool open;
    GameObject visual;
    bool closing;
    SpriteRenderer[] fadeRenderers;
    CanvasGroup canvasGroup;
    TextMeshPro messageText;
    string defaultMessage;

    void Awake()
    {
        visual = transform.GetChild(0).gameObject;
        visual.SetActive(false);
        fadeRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        canvasGroup = visual.GetComponent<CanvasGroup>();
        messageText = visual.GetComponentInChildren<TextMeshPro>(true);
        if (messageText != null)
            defaultMessage = messageText.text;
    }

    void Update()
    {
        if (open && !closing && InputHelper.GetPrimaryPressBegan())
            StartCoroutine(WaitToClose());
    }

    public void Open(string message = null)
    {
        if (open)
            return;

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

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
