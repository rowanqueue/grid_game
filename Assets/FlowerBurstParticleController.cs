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
    public ParticleSystem spadeDirtBurst;
    public ParticleSystem adderBurst;

    private void PlayFlowerBurst(Logic.TokenColor tokenColor)
    {
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                Debug.Log("Playing blue flower burst");
                Services.AudioManager.PlayFlowerBurstSound();
                blueFlowerBurst.Play();
                break;
            case Logic.TokenColor.Red:
                Debug.Log("Playing red flower burst");
                Services.AudioManager.PlayFlowerBurstSound();
                redFlowerBurst.Play();
                break;
            case Logic.TokenColor.Purple:
                Debug.Log("Playing purple flower burst");
                Services.AudioManager.PlayFlowerBurstSound();
                purpleFlowerBurst.Play();
                break;
            case Logic.TokenColor.Green:
                Services.AudioManager.PlayFlowerBurstSound();
                greenFlowerBurst.Play();
                break;
            case Logic.TokenColor.Gold:
                Services.AudioManager.PlayFlowerBurstSound();
                goldFlowerBurst.Play();
                break;
            case Logic.TokenColor.Spade:
                Services.AudioManager.PlaySpadeSound();
                spadeDirtBurst.Play();
                break;
            case Logic.TokenColor.Adder:
                Debug.Log("Playing adder burst");
                adderBurst.Play();
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
    }

    public IEnumerator PlayFlowerBurstCoroutine(float delay, Logic.TokenColor tokenColor)
    {
        yield return new WaitForSeconds(delay);
        PlayFlowerBurst(tokenColor);
    }

    public void StopFlowerBurst(Logic.TokenColor tokenColor)
    {
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                blueFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Red:
                redFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Purple:
                purpleFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Green:
                greenFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Gold:
                goldFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Spade:
                spadeDirtBurst.Stop();
                break;
            case Logic.TokenColor.Adder:
                adderBurst.Stop();
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
    }
}
