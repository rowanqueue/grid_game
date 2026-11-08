using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialIntroClickForwarder : MonoBehaviour, IPointerDownHandler
{
    Tutorial tutorial;

    public void Bind(Tutorial owner)
    {
        tutorial = owner;
    }

    // No OnPointerClick: it fires on release (a later frame) and would advance a second slide.
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
