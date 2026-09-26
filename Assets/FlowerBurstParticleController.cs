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

    public IEnumerator PlayFlowerBurstCoroutine(float delay, Logic.TokenColor tokenColor)
    {
        yield return new WaitForSeconds(delay);
        PlayFlowerBurst(tokenColor);
    }

    void EnsureBurstPlayable(ParticleSystem ps)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        // Parent may be Token/Text deactivated by HideScoreLabel — walk up and wake.
        Transform t = transform.parent;
        while (t != null && !t.gameObject.activeSelf)
        {
            t.gameObject.SetActive(true);
            t = t.parent;
        }
        if (ps != null && !ps.gameObject.activeSelf)
        {
            ps.gameObject.SetActive(true);
        }
    }

    private void PlayFlowerBurst(Logic.TokenColor tokenColor)
    {
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(blueFlowerBurst);
                blueFlowerBurst.Play();
                break;
            case Logic.TokenColor.Red:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(redFlowerBurst);
                redFlowerBurst.Play();
                break;
            case Logic.TokenColor.Purple:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(purpleFlowerBurst);
                purpleFlowerBurst.Play();
                break;
            case Logic.TokenColor.Green:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(greenFlowerBurst);
                greenFlowerBurst.Play();
                break;
            case Logic.TokenColor.Gold:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(goldFlowerBurst);
                goldFlowerBurst.Play();
                break;
            case Logic.TokenColor.Spade:
                EnsureBurstPlayable(spadeDirtBurst);
                spadeDirtBurst.Play();
                break;
            case Logic.TokenColor.Adder:
                EnsureBurstPlayable(adderBurst);
                adderBurst.Play();
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
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
