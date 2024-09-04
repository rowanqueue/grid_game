using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Logic.Tile tile;
    public bool freeSlot = false;

    public Token token;

    public void Draw(bool hover)
    {
        if (freeSlot)
        {
            transform.position = Services.GameController.firstGridPos + (Services.GameController.freeSlotGridPos * Services.GameController.gridSeparation);
        }
        else
        {
            transform.position = Services.GameController.firstGridPos + (tile.pos * Services.GameController.gridSeparation);
        }
        
        float size = 1f;
        if (hover)
        {
            size *= 0.75f;
        }
        float _real_size = (transform.localScale.x + (size - transform.localScale.x) * 0.25f);
        transform.localScale = Vector3.one* _real_size;
    }
}
