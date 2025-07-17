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
                        if (Services.Gems.numGems >= 2)
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


            if (hover && !disabled && Input.GetMouseButtonDown(0))
            {
                _event.Invoke();
                if (type == ButtonType.Mulligan && Services.GameController.inTutorial == false)
                {
                    Services.Gems.SpendGems(2);
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

