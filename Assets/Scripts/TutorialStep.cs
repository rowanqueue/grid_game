using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStageData : MonoBehaviour
{
    public float dimTime = 0.5f;
    public float endFade = 0.73f;
    public Image dim;
    public List<StagePhase> stagePhase;

    public IEnumerator StepStartAnimation()
    {
        AnimateInStage(0);
        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0);
        yield return dim.DOFade(endFade, dimTime).WaitForCompletion();
    }

    public void ActivateStage(int tuStageindex, bool activate)
    {
        StagePhase stage = stagePhase[tuStageindex];
        foreach (RectTransform highlight in stage.highlights)
        {
            highlight.gameObject.SetActive(activate); 
        }
        
        foreach (Image border in stage.highlightBorder)
        {
            border.gameObject.SetActive(activate); 
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            arrow.gameObject.SetActive(activate);
        }
    }

    public void AnimateInStage(int tuStageIndex)
    {
        StagePhase stage = stagePhase[tuStageIndex];
        foreach (Image border in stage.highlightBorder)
        {
            border.color = new Color(border.color.r, border.color.g, border.color.b, 0);
            border.DOFade(1, dimTime);
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            StartCoroutine(arrow.FadeInArrow());
        }
    }

    public void PrepStage(int tutorialStageIndex)
    {
        StagePhase stage = stagePhase[tutorialStageIndex];
        foreach (Image border in stage.highlightBorder)
        {
            border.color = new Color(border.color.r, border.color.g, border.color.b, 0);
        }

        foreach (TutorialArrow arrow in stage.arrows)
        {
            arrow.PrepArrow();
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
