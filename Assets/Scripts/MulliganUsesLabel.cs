using TMPro;
using UnityEngine;

/// <summary>
/// Sets this GameObject's TextMeshPro to the remaining mulligan uses for the current game.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class MulliganUsesLabel : MonoBehaviour
{
    TextMeshPro label;
    int lastDisplayed = -1;

    void Awake()
    {
        label = GetComponent<TextMeshPro>();
    }

    void Start()
    {
        Refresh();
    }

    void OnEnable()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) label = GetComponent<TextMeshPro>();
        if (label == null || Services.GameController == null || Services.GameController.game == null) return;

        int uses = Services.GameController.game.mulliganUsesRemaining;
        if (uses == lastDisplayed) return;

        lastDisplayed = uses;
        label.text = uses.ToString();
    }
}
