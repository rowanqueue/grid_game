using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
public enum ButtonType
{
    None,
    Mulligan,
    BagButton,
    Haptics,
    DiceMode,
    Difficulty
}
public class Button : MonoBehaviour
{
    public ButtonType type;
    public UnityEvent _event;
    public SpriteRenderer display;
    public SpriteRenderer toggledDisplay;
    public TextMeshPro words;
    public Color hoverColor;
    bool hover = false;
    public bool disabled = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (type)
        {
            case ButtonType.Mulligan:
                if(Services.GameController.inTutorial == false)
                {
                    display.enabled = false;
                    return;
                    break;
                }
                break;
            case ButtonType.DiceMode:
                if(toggledDisplay != null)
                {
                    toggledDisplay.enabled = Services.GameController.diceMode;
                }
                break;
            case ButtonType.Haptics:
                if (toggledDisplay != null)
                {
                    toggledDisplay.enabled = Services.GameController.useHaptics;
                }
                break;
        }
        if(display != null)
        {
            display.color = (hover ? hoverColor : Color.white);
        }
        if (toggledDisplay != null)
        {
            toggledDisplay.enabled = !disabled;
        }
        
        if(hover && !disabled && Input.GetMouseButtonDown(0))
        {
            _event.Invoke();
        }
    }

    private void OnMouseEnter()
    {
        hover = true;
    }
    private void OnMouseExit()
    {
        hover = false;
    }
}
