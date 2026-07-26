using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class WinScreenPayload
{
    public int fromScore;
    public int toScore;
    public bool isNewHighScore;
}

public class WinScreenDisplay : MonoBehaviour
{
    const float SkipSpeedMultiplier = 2f;

    [SerializeField] float animateInDuration = 0.4f;
    [SerializeField] float slideOffsetY = 0.5f;
    [SerializeField] float dimTargetAlpha = 1f;
    [SerializeField] float dimFadeDuration = 0.35f;
    [SerializeField] float titleStaggerDelay = 0.1f;
    [SerializeField] float scoreCountDuration = 0.8f;
    [SerializeField] float statsStaggerDelay = 0.15f;

    [Header("Action Buttons")]
    [SerializeField] GameObject actionButtonsRoot;
    [SerializeField] flora.Button highScoreButton;
    [SerializeField] flora.Button restartButton;

    TMP_Text scoreText;
    TMP_Text titleText;
    TMP_Text assignedScoreText;
    SpriteRenderer dimBackground;
    Vector2 scoreRestPosition;
    Vector2 titleRestPosition;
    float animSpeed = 1f;
    bool showingWinScreen;
    readonly List<Tween> activeTweens = new List<Tween>();

    void Awake()
    {
        CacheReferences();
        CaptureRestPosition();
        HideActionButtons();
    }

    void Update()
    {
        if (!showingWinScreen)
        {
            return;
        }
        if (InputHelper.GetPrimaryPressBegan() || InputHelper.GetAnyPressBegan())
        {
            if (Services.GameController != null)
            {
                Services.GameController.RequestWinScreenAccelerate();
            }
            else
            {
                ApplyAcceleration();
            }
        }
        SyncAnimationSpeed();
    }

    void SyncAnimationSpeed()
    {
        float targetSpeed = 1f;
        if (Services.GameController != null)
        {
            targetSpeed = Mathf.Max(targetSpeed, Services.GameController.winScreenAnimSpeed);
            if (Services.GameController.winScreenAccelerateRequested)
            {
                targetSpeed = Mathf.Max(targetSpeed, SkipSpeedMultiplier);
            }
        }
        if (targetSpeed > animSpeed)
        {
            ApplyAcceleration(targetSpeed);
        }
    }

    void ApplyAcceleration(float speed = SkipSpeedMultiplier)
    {
        if (animSpeed >= speed)
        {
            return;
        }
        animSpeed = speed;
        if (Services.GameController != null)
        {
            Services.GameController.winScreenAnimSpeed = speed;
            Services.GameController.winScreenAccelerateRequested = true;
        }
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = activeTweens[i];
            if (tween == null || !tween.IsActive())
            {
                activeTweens.RemoveAt(i);
                continue;
            }
            tween.timeScale = animSpeed;
        }
    }

    void CacheReferences()
    {
        titleText = null;
        scoreText = assignedScoreText;

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text tmp in tmps)
        {
            if (tmp.gameObject.name == "Title Text")
            {
                titleText = tmp;
            }
            else if (tmp.gameObject.name == "Desc Text")
            {
                scoreText = tmp;
            }
        }

        if (scoreText == null)
        {
            scoreText = assignedScoreText;
        }

        if (dimBackground == null)
        {
            Transform bg = transform.Find("Visual/Popup_Background");
            if (bg == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Popup_Background")
                    {
                        bg = child;
                        break;
                    }
                }
            }
            if (bg != null)
            {
                dimBackground = bg.GetComponent<SpriteRenderer>();
            }
        }

        if (actionButtonsRoot == null)
        {
            Transform found = transform.Find("ActionButtons");
            if (found != null)
            {
                actionButtonsRoot = found.gameObject;
            }
        }
        if (actionButtonsRoot != null)
        {
            if (highScoreButton == null)
            {
                Transform hs = actionButtonsRoot.transform.Find("HighScoresButton");
                if (hs != null)
                {
                    highScoreButton = hs.GetComponent<flora.Button>();
                }
            }
            if (restartButton == null)
            {
                Transform rs = actionButtonsRoot.transform.Find("RestartButton");
                if (rs != null)
                {
                    restartButton = rs.GetComponent<flora.Button>();
                }
            }
        }
    }

    public void SetScoreText(TMP_Text text)
    {
        assignedScoreText = text;
        CacheReferences();
        CaptureRestPosition();
    }

    void CaptureRestPosition()
    {
        if (titleText != null)
        {
            titleRestPosition = GetTextPosition(titleText);
        }
        if (scoreText != null)
        {
            scoreRestPosition = GetTextPosition(scoreText);
        }
    }

    static Vector2 GetTextPosition(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        if (rect != null)
        {
            return rect.anchoredPosition;
        }
        Vector3 local = text.transform.localPosition;
        return new Vector2(local.x, local.y);
    }

    static void SetTextPosition(TMP_Text text, Vector2 position)
    {
        RectTransform rect = text.rectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = position;
            return;
        }
        text.transform.localPosition = new Vector3(position.x, position.y, text.transform.localPosition.z);
    }

    Tweener CreateTextPositionTween(TMP_Text text, Vector2 target, float duration, Ease ease)
    {
        RectTransform rect = text.rectTransform;
        if (rect != null)
        {
            return rect.DOAnchorPos(target, duration).SetEase(ease);
        }
        return text.transform.DOLocalMove(
            new Vector3(target.x, target.y, text.transform.localPosition.z),
            duration).SetEase(ease);
    }

    void TrackTween(Tween tween)
    {
        if (tween == null)
        {
            return;
        }
        tween.timeScale = animSpeed;
        activeTweens.Add(tween);
    }

    void ResetAnimationSpeed()
    {
        animSpeed = 1f;
        activeTweens.Clear();
    }

    IEnumerator WaitForTween(Tween tween)
    {
        while (tween != null && tween.IsActive() && !tween.IsComplete())
        {
            SyncAnimationSpeed();
            if (animSpeed > 1f)
            {
                tween.fullPosition += Time.deltaTime * (animSpeed - 1f);
            }
            yield return null;
        }
    }

    IEnumerator WaitScaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SyncAnimationSpeed();
            elapsed += Time.deltaTime * animSpeed;
            yield return null;
        }
    }

    void LockScoreTextPosition()
    {
        if (scoreText == null)
        {
            return;
        }
        scoreText.DOKill();
        scoreText.transform.DOKill();
        if (scoreText.rectTransform != null)
        {
            scoreText.rectTransform.DOKill();
        }
        SetTextPosition(scoreText, scoreRestPosition);
    }

    void ApplyWinScreenSorting()
    {
        int layerId = SortingLayer.NameToID("UIToken");
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerID = layerId;
            sr.sortingOrder = 25;
        }
        foreach (TMP_Text tmp in GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp is TextMeshPro worldText)
            {
                worldText.sortingLayerID = layerId;
                worldText.sortingOrder = 26;
            }
        }
        if (actionButtonsRoot != null)
        {
            foreach (SpriteRenderer sr in actionButtonsRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerID = layerId;
                sr.sortingOrder = 27;
            }
            foreach (TextMeshPro tmp in actionButtonsRoot.GetComponentsInChildren<TextMeshPro>(true))
            {
                tmp.sortingLayerID = layerId;
                tmp.sortingOrder = 28;
            }
        }
    }

    public void ShowActionButtons()
    {
        CacheReferences();
        if (actionButtonsRoot != null)
        {
            actionButtonsRoot.SetActive(true);
            ApplyWinScreenSorting();
        }
    }

    public void HideActionButtons()
    {
        if (actionButtonsRoot != null)
        {
            actionButtonsRoot.SetActive(false);
        }
    }

    public IEnumerator ShowRoutine(WinScreenPayload payload)
    {
        showingWinScreen = true;
        ResetAnimationSpeed();
        HideActionButtons();
        if (Services.GameController != null)
        {
            Services.GameController.winScreenAnimSpeed = 1f;
            Services.GameController.winScreenAccelerateRequested = false;
        }
        CacheReferences();
        CaptureRestPosition();
        ApplyWinScreenSorting();
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }
        if (scoreText != null)
        {
            scoreText.alpha = 0f;
        }

        if (dimBackground != null)
        {
            Color c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
            Tween dimTween = dimBackground.DOFade(dimTargetAlpha, dimFadeDuration);
            TrackTween(dimTween);
            yield return WaitForTween(dimTween);
        }

        if (scoreText == null)
        {
            showingWinScreen = false;
            ShowActionButtons();
            ResetAnimationSpeed();
            yield break;
        }

        string headline = payload.isNewHighScore ? "New high score!" : "Congratulations!";
        scoreText.alpha = 0f;
        SetTextPosition(scoreText, scoreRestPosition + Vector2.down * slideOffsetY);

        int displayScore = payload.fromScore;
        scoreText.text = BuildScoreText(headline, displayScore);

        Tween scoreFadeTween = scoreText.DOFade(1f, animateInDuration * 0.5f).SetEase(Ease.OutQuad);
        Tween scoreMoveTween = CreateTextPositionTween(scoreText, scoreRestPosition, animateInDuration, Ease.OutQuad);
        Tween countTween = DOTween.To(() => displayScore, v =>
        {
            displayScore = v;
            scoreText.text = BuildScoreText(headline, displayScore);
        }, payload.toScore, scoreCountDuration).SetEase(Ease.OutQuad);
        Sequence scoreSeq = DOTween.Sequence();
        scoreSeq.Join(scoreFadeTween);
        scoreSeq.Join(scoreMoveTween);
        scoreSeq.Join(countTween);
        TrackTween(scoreFadeTween);
        TrackTween(scoreMoveTween);
        TrackTween(countTween);
        TrackTween(scoreSeq);
        yield return WaitForTween(scoreSeq);

        yield return WaitScaled(statsStaggerDelay);
        LockScoreTextPosition();
        scoreText.text = BuildScoreText(headline, payload.toScore);
        scoreText.alpha = 1f;
        LockScoreTextPosition();

        ShowActionButtons();
        showingWinScreen = false;
        ResetAnimationSpeed();
    }

    public IEnumerator ShowRoutine(string formattedScore)
    {
        showingWinScreen = true;
        ResetAnimationSpeed();
        HideActionButtons();
        CacheReferences();
        CaptureRestPosition();
        ApplyWinScreenSorting();
        if (scoreText == null)
        {
            gameObject.SetActive(true);
            ShowActionButtons();
            showingWinScreen = false;
            ResetAnimationSpeed();
            yield break;
        }

        scoreText.text = formattedScore;
        gameObject.SetActive(true);

        scoreText.alpha = 0f;
        SetTextPosition(scoreText, scoreRestPosition + Vector2.down * slideOffsetY);

        Tween fadeTween = scoreText.DOFade(1f, animateInDuration).SetEase(Ease.OutQuad);
        Tween moveTween = CreateTextPositionTween(scoreText, scoreRestPosition, animateInDuration, Ease.OutQuad);
        Sequence sequence = DOTween.Sequence();
        sequence.Join(fadeTween);
        sequence.Join(moveTween);
        TrackTween(fadeTween);
        TrackTween(moveTween);
        TrackTween(sequence);
        yield return WaitForTween(sequence);
        ShowActionButtons();
        showingWinScreen = false;
        ResetAnimationSpeed();
    }

    static string BuildScoreText(string headline, int scoreValue)
    {
        return "<size=35%>" + headline + "\nYour score:</size>\n" + scoreValue.ToString();
    }

    public void HideImmediate()
    {
        showingWinScreen = false;
        ResetAnimationSpeed();
        HideActionButtons();
        if (scoreText != null)
        {
            scoreText.DOKill();
            scoreText.transform.DOKill();
            if (scoreText.rectTransform != null)
            {
                scoreText.rectTransform.DOKill();
            }
            scoreText.alpha = 1f;
            SetTextPosition(scoreText, scoreRestPosition);
        }
        if (titleText != null)
        {
            titleText.DOKill();
            titleText.transform.DOKill();
            if (titleText.rectTransform != null)
            {
                titleText.rectTransform.DOKill();
            }
            titleText.alpha = 1f;
            SetTextPosition(titleText, titleRestPosition);
        }
        if (dimBackground != null)
        {
            dimBackground.DOKill();
        }
        gameObject.SetActive(false);
    }
}
