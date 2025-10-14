using System.Collections;
using System.Collections.Generic;
using Logic;
using UnityEngine;
using DG.Tweening;

public class PolaroidFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flash;

    public IEnumerator FlashRoutine()
    {
        ResetFlash();
        flash.gameObject.SetActive(true);
        yield return flash.transform.DOScale(200f, 0.3f).SetEase(Ease.OutQuad).WaitForCompletion();
        yield return flash.DOFade(0, 0.3f).SetEase(Ease.OutQuad).WaitForCompletion();
        flash.gameObject.SetActive(false);
    }

    private void ResetFlash()
    {
        flash.transform.localScale = Vector3.zero;
        flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1f);
    }
}
