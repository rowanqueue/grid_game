using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace flora
{
    public enum ButtonType
    {
        None,
        Mulligan,
        BagButton,
        Haptics,
        DiceMode,
        Difficulty,
        StartGame
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
                    if (Services.GameController.inTutorial == false)
                    {
                        if (Services.Gems.CanAfford("mulligan"))
                        {
                            disabled = false;
                        }
                        else
                        {
                            disabled = true;
                        }
                        break;
                    }
                    break;
                case ButtonType.DiceMode:
                    if (toggledDisplay != null)
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
                case ButtonType.StartGame:
                    disabled = !Services.GameController.difficultyUnlocked[Services.GameController.difficulty];
                    break;
            }
            if (display != null && disabled == false)
            {
                display.color = (hover ? hoverColor : Color.white);
            }
            if (type == ButtonType.Difficulty || type == ButtonType.StartGame)
            {
                if (toggledDisplay != null)
                {
                    toggledDisplay.enabled = !disabled;
                }
                if (words != null)
                {
                    words.enabled = !disabled;
                }
            }


            if (hover && Input.GetMouseButtonDown(0))
            {
                if (disabled)
                {
                    if (type == ButtonType.Mulligan && Services.GameController.inTutorial == false)
                    {
                        Services.Gems.TooExpensive();
                    }
                }
                else
                {
                    _event.Invoke();
                    if (type == ButtonType.Mulligan && Services.GameController.inTutorial == false)
                    {
                        Services.Gems.SpendGems("mulligan");
                    }
                }
                
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
}

