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
        [Tooltip("If true, this button can be pressed while the tool shop is open.")]
        public bool allowDuringToolShop = false;
        [Tooltip("Extra objects to hide when this button is disabled (e.g. seed cost icons).")]
        public GameObject[] hideWhenDisabled;
        Vector3 baseScale;
        Coroutine pressRoutine;
        GemCostLabel[] costLabels;

        void Start()
        {
            baseScale = transform.localScale;
            if (type == ButtonType.StartGame)
            {
                costLabels = GetComponentsInChildren<GemCostLabel>(true);
            }
        }

        void Update()
        {
            switch (type)
            {
                case ButtonType.Mulligan:
                    disabled = Services.GameController.inTutorial
                        || Services.GameController.gameState != GameState.Gameplay
                        || Services.GameController.inputState != InputState.Choose
                        || Services.GameController.MulliganInProgress
                        || Services.GameController.game.mulliganUsesRemaining <= 0;
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
            if (type == ButtonType.Difficulty)
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
            else if (type == ButtonType.StartGame)
            {
                if (toggledDisplay != null)
                {
                    toggledDisplay.enabled = !disabled;
                }
                if (words != null)
                {
                    words.enabled = true;
                    words.text = disabled ? "Locked" : "Tap to Start";
                }
                bool showCost = !disabled;
                if (costLabels != null)
                {
                    for (int i = 0; i < costLabels.Length; i++)
                    {
                        if (costLabels[i] != null)
                        {
                            costLabels[i].gameObject.SetActive(showCost);
                        }
                    }
                }
                if (hideWhenDisabled != null)
                {
                    for (int i = 0; i < hideWhenDisabled.Length; i++)
                    {
                        if (hideWhenDisabled[i] != null)
                        {
                            hideWhenDisabled[i].SetActive(showCost);
                        }
                    }
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
            if (Services.GameController != null
                && Services.GameController.gameState == GameState.ToolShop
                && !allowDuringToolShop)
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
