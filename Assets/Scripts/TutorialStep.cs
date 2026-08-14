using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStageData : MonoBehaviour
{
    [Header("Presentation Timing")]
    public float dimTime = 0.5f;
    public float endFade = 0.73f;
    public Image dim;
    public List<StagePhase> stagePhase;

    public float PresentationDuration => dimTime + GetMaxArrowFadeTime();

    public bool HasPhase(int index) => stagePhase != null && index >= 0 && index < stagePhase.Count;

    public bool HasDim => dim != null;

    float GetMaxArrowFadeTime()
    {
        float max = 0f;
        foreach (StagePhase phase in stagePhase)
        {
            foreach (TutorialArrow arrow in phase.arrows)
            {
                if (arrow != null && arrow.fadeTime > max)
                {
                    max = arrow.fadeTime;
                }
            }
        }
        return max;
    }

    public void SnapDimVisible()
    {
        if (dim == null) { return; }
        dim.gameObject.SetActive(true);
        dim.DOKill();
        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, endFade);
    }

    public IEnumerator RunEnterPresentation(bool keepDim = false)
    {
        if (!HasPhase(0)) { yield break; }
        PrepStage(0);
        ActivateStage(0, true);
        yield return StepStartAnimation(keepDim);
    }

    public IEnumerator StepStartAnimation(bool keepDim = false)
    {
        Tween dimTween = null;
        if (dim != null)
        {
            dim.gameObject.SetActive(true);
            dim.DOKill();
            if (keepDim)
            {
                dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, endFade);
            }
            else
            {
                dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0);
                dimTween = dim.DOFade(endFade, dimTime);
            }
        }

        yield return AnimateInStageCoroutine(0);

        if (dimTween != null && dimTween.IsActive())
        {
            yield return dimTween.WaitForCompletion();
        }
    }

    public IEnumerator AnimateInStageCoroutine(int tuStageIndex)
    {
        if (!HasPhase(tuStageIndex)) { yield break; }
        StagePhase stage = stagePhase[tuStageIndex];
        foreach (RectTransform highlight in stage.highlights)
        {
            if (highlight != null)
            {
                highlight.gameObject.SetActive(true);
            }
        }

        foreach (Image border in stage.highlightBorder)
        {
            if (border == null) { continue; }
            border.gameObject.SetActive(true);
            border.color = new Color(border.color.r, border.color.g, border.color.b, 0);
            border.DOFade(1, dimTime);
        }

        float maxArrowTime = 0f;
        foreach (TutorialArrow arrow in stage.arrows)
        {
            if (arrow == null) { continue; }
            arrow.gameObject.SetActive(true);
            arrow.PrepArrow();
            StartCoroutine(arrow.FadeInArrow());
            if (arrow.fadeTime > maxArrowTime)
            {
                maxArrowTime = arrow.fadeTime;
            }
        }

        if (stage.highlightBorder.Count > 0)
        {
            yield return new WaitForSeconds(dimTime);
        }
        if (maxArrowTime > 0f)
        {
            yield return new WaitForSeconds(maxArrowTime);
        }
    }

    public IEnumerator AnimateOutStageCoroutine(int tuStageIndex, bool fadeDim = false)
    {
        if (!HasPhase(tuStageIndex)) { yield break; }
        StagePhase stage = stagePhase[tuStageIndex];
        float maxDuration = 0f;

        foreach (Image border in stage.highlightBorder)
        {
            if (border == null || !border.gameObject.activeInHierarchy) { continue; }
            border.DOFade(0, dimTime);
            maxDuration = Mathf.Max(maxDuration, dimTime);
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            if (arrow == null || !arrow.gameObject.activeInHierarchy) { continue; }
            StartCoroutine(arrow.FadeOutArrow());
            maxDuration = Mathf.Max(maxDuration, arrow.fadeTime);
        }

        if (fadeDim && dim != null && dim.gameObject.activeInHierarchy)
        {
            dim.DOKill();
            dim.DOFade(0f, dimTime);
            maxDuration = Mathf.Max(maxDuration, dimTime);
        }

        if (maxDuration > 0f)
        {
            yield return new WaitForSeconds(maxDuration);
        }

        if (fadeDim && dim != null)
        {
            dim.DOKill();
            dim.gameObject.SetActive(false);
        }

        ActivateStage(tuStageIndex, false);
    }

    public IEnumerator TransitionPhase(int fromIndex, int toIndex)
    {
        if (fromIndex >= 0 && fromIndex < stagePhase.Count)
        {
            yield return AnimateOutStageCoroutine(fromIndex);
        }

        PrepStage(toIndex);
        ActivateStage(toIndex, true);
        yield return AnimateInStageCoroutine(toIndex);
    }

    public void ActivateStage(int tuStageindex, bool activate)
    {
        if (!HasPhase(tuStageindex)) { return; }
        StagePhase stage = stagePhase[tuStageindex];
        foreach (RectTransform highlight in stage.highlights)
        {
            if (highlight != null)
            {
                highlight.gameObject.SetActive(activate);
            }
        }

        foreach (Image border in stage.highlightBorder)
        {
            if (border != null)
            {
                border.gameObject.SetActive(activate);
            }
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            if (arrow != null)
            {
                arrow.gameObject.SetActive(activate);
            }
        }
    }

    public void AnimateInStage(int tuStageIndex)
    {
        StartCoroutine(AnimateInStageCoroutine(tuStageIndex));
    }

    public void PrepStage(int tutorialStageIndex)
    {
        if (!HasPhase(tutorialStageIndex)) { return; }
        StagePhase stage = stagePhase[tutorialStageIndex];
        foreach (Image border in stage.highlightBorder)
        {
            if (border == null) { continue; }
            border.color = new Color(border.color.r, border.color.g, border.color.b, 0);
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            if (arrow != null)
            {
                arrow.PrepArrow();
            }
        }
    }
}

[Serializable]
public struct StagePhase
{
    public List<RectTransform> highlights;
    public List<TutorialArrow> arrows;
    public List<Image> highlightBorder;
}
