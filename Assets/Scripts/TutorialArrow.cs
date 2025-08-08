using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TutorialArrow : MonoBehaviour
{
    public float fadeTime = 0.5f;
    public SpriteRenderer arrowEnd;
    public List<SpriteRenderer> arrowLines;

    public void PrepArrow()
    {
        arrowEnd.color = new Color(arrowEnd.color.r, arrowEnd.color.g, arrowEnd.color.b, 0);
        foreach (SpriteRenderer line in arrowLines)
        {
            line.color = new Color(line.color.r, line.color.g, line.color.b, 0);
        }
    }
    public IEnumerator FadeInArrow()
    {
        foreach (SpriteRenderer line in arrowLines)
        {
            line.DOFade(1, fadeTime).Play();
        }
        yield return arrowEnd.DOFade(1, fadeTime).WaitForCompletion();
    }
    
    public IEnumerator FadeOutArrow()
    {
        foreach (SpriteRenderer line in arrowLines)
        {
            line.DOFade(0,fadeTime).Play();
        }
        yield return arrowEnd.DOFade(0,fadeTime).WaitForCompletion();
    }
}
