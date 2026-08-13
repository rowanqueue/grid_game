using UnityEngine;
using TMPro;

/// <summary>
/// Sets this GameObject's TextMeshPro to the gem cost for a given cost key (e.g. "buySpade", "buyAdder").
/// Add this to the same GameObject as the TextMeshPro; set costKey in the Inspector to match a key in Gems.seedCosts.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class GemCostLabel : MonoBehaviour
{
    [Tooltip("Key in Gems.seedCosts (e.g. buySpade, buyAdder, buyClipper, newGame, mulligan).")]
    public string costKey = "buySpade";

    TextMeshPro label;

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

    public void Refresh()
    {
        if (label == null) label = GetComponent<TextMeshPro>();
        if (label == null || Services.Gems == null) return;

        int cost = Services.Gems.GetCost(costKey);
        label.text = $" {cost}";
    }
}
