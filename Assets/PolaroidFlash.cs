using System.Collections;
using System.Collections.Generic;
using Logic;
using UnityEngine;
using DG.Tweening;

public class PolaroidFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private float flashDuration = 0.2f;

    public IEnumerator FlashRoutine()
    {
        ResetFlash();
        flash.gameObject.SetActive(true);
        yield return flash.transform.DOScale(200f, flashDuration).SetEase(Ease.InQuad).WaitForCompletion();
        yield return flash.DOFade(0, flashDuration).SetEase(Ease.InQuad).WaitForCompletion();
        flash.gameObject.SetActive(false);
    }

    private void ResetFlash()
    {
        flash.transform.localScale = Vector3.zero;
        flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1f);
    }
}
