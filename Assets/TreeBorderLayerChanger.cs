using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeBorderLayerChanger : MonoBehaviour
{
    SpriteRenderer[] treeRenderers;
    [SerializeField] string standardSortingLayerString= "Default";
    [SerializeField] string tutorialSortingLayerString = "Tutorial";

    int standardSortingLayerID;
    int tutorialSortingLayerID;

    // Start is called before the first frame update
    void Awake()
    {
        treeRenderers = GetComponentsInChildren<SpriteRenderer>();
        standardSortingLayerID = SortingLayer.NameToID(standardSortingLayerString);
        tutorialSortingLayerID = SortingLayer.NameToID(tutorialSortingLayerString);
    }

    void Update()
    {
        if (Services.GameController.inTutorial)
        {
            foreach (var renderer in treeRenderers)
            {
                renderer.sortingLayerID = tutorialSortingLayerID;
            }
        }
        else
        {
            foreach (var renderer in treeRenderers)
            {
                renderer.sortingLayerID = standardSortingLayerID;
            }   
        }
    }
}
