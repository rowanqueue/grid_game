using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialIntroClickForwarder : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    Tutorial tutorial;

    public void Bind(Tutorial owner)
    {
        tutorial = owner;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Advance();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Advance();
    }

    void OnMouseDown()
    {
        Advance();
    }

    void Advance()
    {
        if (tutorial != null)
        {
            tutorial.AdvanceIntroSlideFromPointer();
        }
    }
}
