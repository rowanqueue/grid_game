using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class WinScreenDisplay : MonoBehaviour
{
    [SerializeField] float animateInDuration = 0.4f;
    [SerializeField] float slideOffsetY = 0.5f;

    TextMeshPro scoreText;
    Vector3 scoreRestLocalPosition;
    bool restPositionCaptured;

    void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshPro>(true);
        CaptureRestPosition();
    }

    public void SetScoreText(TextMeshPro text)
    {
        scoreText = text;
        CaptureRestPosition();
    }

    void CaptureRestPosition()
    {
        if (scoreText == null || restPositionCaptured)
            return;
        scoreRestLocalPosition = scoreText.transform.localPosition;
        restPositionCaptured = true;
    }

    public IEnumerator ShowRoutine(string formattedScore)
    {
        if (scoreText == null)
        {
            gameObject.SetActive(true);
            yield break;
        }

        scoreText.text = formattedScore;
        gameObject.SetActive(true);

        scoreText.alpha = 0f;
        Vector3 startPos = scoreRestLocalPosition + Vector3.down * slideOffsetY;
        scoreText.transform.localPosition = startPos;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(scoreText.DOFade(1f, animateInDuration).SetEase(Ease.OutQuad));
        sequence.Join(scoreText.transform.DOLocalMove(scoreRestLocalPosition, animateInDuration).SetEase(Ease.OutQuad));
        yield return sequence.WaitForCompletion();
    }

    public void HideImmediate()
    {
        if (scoreText != null)
        {
            scoreText.DOKill();
            scoreText.transform.DOKill();
            scoreText.alpha = 1f;
            scoreText.transform.localPosition = scoreRestLocalPosition;
        }
        gameObject.SetActive(false);
    }
}
