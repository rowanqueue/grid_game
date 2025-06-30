using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerBurstParticleController : MonoBehaviour
{
    public ParticleSystem blueFlowerBurst;
    public ParticleSystem redFlowerBurst;
    public ParticleSystem purpleFlowerBurst;
    public ParticleSystem greenFlowerBurst;
    public ParticleSystem goldFlowerBurst;

    public void PlayFlowerBurst(Logic.TokenColor tokenColor)
    {
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                Debug.Log("Playing blue flower burst");
                blueFlowerBurst.Play();
                break;
            case Logic.TokenColor.Red:
                Debug.Log("Playing red flower burst"); 
                redFlowerBurst.Play();
                break;
            case Logic.TokenColor.Purple:
                Debug.Log("Playing purple flower burst");   
                purpleFlowerBurst.Play();
                break;
            case Logic.TokenColor.Green:
                greenFlowerBurst.Play();
                break;
            case Logic.TokenColor.Gold:
                goldFlowerBurst.Play();
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
    }
}
