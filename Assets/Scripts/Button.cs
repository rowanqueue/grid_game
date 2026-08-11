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
        StartGame,
        FastMode
    }
    public class Button : MonoBehaviour
    {
        public ButtonType type;
        public UnityEvent _event;
        public SpriteRenderer display;
        public SpriteRenderer toggledDisplay;
        public TextMeshPro words;
        public Color hoverColor;
        public Color standardColor = Color.white; 
        [SerializeField] float pressScale = 0.95f;
        [SerializeField] float pressDuration = 0.08f;
        bool hover = false;
        public bool disabled = false;
        Vector3 baseScale;
        Coroutine pressRoutine;

        void Start()
        {
            baseScale = transform.localScale;
        }

        void Update()
        {
            switch (type)
            {
                case ButtonType.Mulligan:
                    if (Services.GameController.inTutorial)
                    {
                        disabled = true;
                    }
                    else
                    {
                        disabled = Services.GameController.game.mulliganUsesRemaining <= 0;
                    }
                    break;
                case ButtonType.DiceMode:
                    if (toggledDisplay != null)
                    {
                        toggledDisplay.gameObject.SetActive(Services.GameController.diceMode);
                    }
                    break;
                case ButtonType.Haptics:
                    if (toggledDisplay != null)
                    {
                        toggledDisplay.gameObject.SetActive(Services.GameController.useHaptics);
                    }
                    break;
                case ButtonType.FastMode:
                    if (toggledDisplay != null)
                    {
                        toggledDisplay.gameObject.SetActive(Services.GameController.fastMode);
                    }
                    break;
                case ButtonType.StartGame:
                    disabled = !Services.GameController.difficultyUnlocked[Services.GameController.difficulty];
                    break;
            }
            if (display != null && disabled == false)
            {
                display.color = (hover ? hoverColor : standardColor);
            }
            if (type == ButtonType.Difficulty || type == ButtonType.StartGame)
            {
                if (toggledDisplay != null)
                {
                    toggledDisplay.gameObject.SetActive(!disabled);
                }
                if (words != null)
                {
                    words.enabled = !disabled;
                }
            }
        }

        void OnMouseDown()
        {
            TryActivate();
        }

        void TryActivate()
        {
            if (disabled)
            {
                return;
            }

            PlayPressFeedback();
            _event.Invoke();
        }

        void OnMouseEnter()
        {
            hover = true;
        }

        void OnMouseExit()
        {
            hover = false;
        }

        void PlayPressFeedback()
        {
            if (pressRoutine != null)
                StopCoroutine(pressRoutine);
            pressRoutine = StartCoroutine(PressFeedbackRoutine());
        }

        IEnumerator PressFeedbackRoutine()
        {
            transform.localScale = baseScale * pressScale;
            yield return new WaitForSeconds(pressDuration);
            transform.localScale = baseScale;
            pressRoutine = null;
        }
    }
}
