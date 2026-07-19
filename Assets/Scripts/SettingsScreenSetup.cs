using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SettingsScreenSetup : MonoBehaviour
{
    [SerializeField] float fastModeRowY = -1.49f;

    void Start()
    {
        if (transform.Find("FastModeRow") != null)
        {
            return;
        }

        Transform templateRow = transform.Find("LHM");
        if (templateRow == null)
        {
            return;
        }

        Transform row = Instantiate(templateRow, templateRow.parent);
        row.name = "FastModeRow";

        RectTransform rect = row.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 pos = rect.anchoredPosition;
            pos.y = fastModeRowY;
            rect.anchoredPosition = pos;
        }

        TextMeshPro label = row.GetComponent<TextMeshPro>();
        if (label != null)
        {
            label.text = "Fast Mode";
        }

        flora.Button checkbox = row.GetComponentInChildren<flora.Button>();
        if (checkbox != null)
        {
            checkbox.type = flora.ButtonType.FastMode;
            checkbox._event = new UnityEvent();
            checkbox._event.AddListener(Services.GameController.ToggleFastMode);
        }
    }
}
