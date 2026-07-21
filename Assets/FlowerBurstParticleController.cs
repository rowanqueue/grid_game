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

    static void LogBurst(string action, string which, ParticleSystem ps)
    {
        if (ps == null)
        {
            Debug.Log($"[ParticleLife] flowerBurst {action} {which} ps=NULL t={Time.time:F3}");
            return;
        }
        var main = ps.main;
        Debug.Log(
            $"[ParticleLife] flowerBurst {action} {which} " +
            $"name={ps.name} playing={ps.isPlaying} paused={ps.isPaused} count={ps.particleCount} " +
            $"duration={main.duration:F2} startLife={main.startLifetime.constantMax:F2} " +
            $"active={ps.gameObject.activeInHierarchy} emission={ps.emission.enabled} " +
            $"parent={(ps.transform.parent != null ? ps.transform.parent.name : "null")} " +
            $"worldPos={ps.transform.position} t={Time.time:F3}");
        if (action == "AfterPlay" && !ps.isPlaying && ps.particleCount == 0)
        {
            Debug.LogWarning($"[ParticleLife] flowerBurst NEVER_STARTED {which} after Play() t={Time.time:F3}");
        }
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
        Debug.Log(
            $"[ParticleLife] flowerBurst EnsureBurstPlayable " +
            $"controllerActive={gameObject.activeInHierarchy} " +
            $"psActive={(ps != null && ps.gameObject.activeInHierarchy)} t={Time.time:F3}");
    }

    private void PlayFlowerBurst(Logic.TokenColor tokenColor)
    {
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(blueFlowerBurst);
                LogBurst("Play", "Blue", blueFlowerBurst);
                blueFlowerBurst.Play();
                LogBurst("AfterPlay", "Blue", blueFlowerBurst);
                StartCoroutine(WatchBurst("Blue", blueFlowerBurst));
                break;
            case Logic.TokenColor.Red:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(redFlowerBurst);
                LogBurst("Play", "Red", redFlowerBurst);
                redFlowerBurst.Play();
                LogBurst("AfterPlay", "Red", redFlowerBurst);
                StartCoroutine(WatchBurst("Red", redFlowerBurst));
                break;
            case Logic.TokenColor.Purple:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(purpleFlowerBurst);
                LogBurst("Play", "Purple", purpleFlowerBurst);
                purpleFlowerBurst.Play();
                LogBurst("AfterPlay", "Purple", purpleFlowerBurst);
                StartCoroutine(WatchBurst("Purple", purpleFlowerBurst));
                break;
            case Logic.TokenColor.Green:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(greenFlowerBurst);
                LogBurst("Play", "Green", greenFlowerBurst);
                greenFlowerBurst.Play();
                LogBurst("AfterPlay", "Green", greenFlowerBurst);
                StartCoroutine(WatchBurst("Green", greenFlowerBurst));
                break;
            case Logic.TokenColor.Gold:
                Services.AudioManager.PlayFlowerBurstSound();
                EnsureBurstPlayable(goldFlowerBurst);
                LogBurst("Play", "Gold", goldFlowerBurst);
                goldFlowerBurst.Play();
                LogBurst("AfterPlay", "Gold", goldFlowerBurst);
                StartCoroutine(WatchBurst("Gold", goldFlowerBurst));
                break;
            case Logic.TokenColor.Spade:
                EnsureBurstPlayable(spadeDirtBurst);
                LogBurst("Play", "Spade", spadeDirtBurst);
                spadeDirtBurst.Play();
                LogBurst("AfterPlay", "Spade", spadeDirtBurst);
                StartCoroutine(WatchBurst("Spade", spadeDirtBurst));
                break;
            case Logic.TokenColor.Adder:
                EnsureBurstPlayable(adderBurst);
                LogBurst("Play", "Adder", adderBurst);
                adderBurst.Play();
                LogBurst("AfterPlay", "Adder", adderBurst);
                StartCoroutine(WatchBurst("Adder", adderBurst));
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
    }

    IEnumerator WatchBurst(string which, ParticleSystem ps)
    {
        float start = Time.time;
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForSeconds(0.1f);
            if (ps == null)
            {
                Debug.Log($"[ParticleLife] flowerBurst DESTROYED/NULL {which} after={Time.time - start:F2}s t={Time.time:F3}");
                yield break;
            }
            LogBurst($"tick{i}", which, ps);
            if (!ps.isPlaying && ps.particleCount == 0 && i > 2)
            {
                Debug.Log($"[ParticleLife] flowerBurst IDLE_END {which} after={Time.time - start:F2}s t={Time.time:F3}");
                yield break;
            }
        }
    }

    public void StopFlowerBurst(Logic.TokenColor tokenColor)
    {
        Debug.Log($"[ParticleLife] flowerBurst StopFlowerBurst color={tokenColor} t={Time.time:F3}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
        switch (tokenColor)
        {
            case Logic.TokenColor.Blue:
                LogBurst("Stop", "Blue", blueFlowerBurst);
                blueFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Red:
                LogBurst("Stop", "Red", redFlowerBurst);
                redFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Purple:
                LogBurst("Stop", "Purple", purpleFlowerBurst);
                purpleFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Green:
                LogBurst("Stop", "Green", greenFlowerBurst);
                greenFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Gold:
                LogBurst("Stop", "Gold", goldFlowerBurst);
                goldFlowerBurst.Stop();
                break;
            case Logic.TokenColor.Spade:
                LogBurst("Stop", "Spade", spadeDirtBurst);
                spadeDirtBurst.Stop();
                break;
            case Logic.TokenColor.Adder:
                LogBurst("Stop", "Adder", adderBurst);
                adderBurst.Stop();
                break;
            default:
                Debug.LogWarning("Unknown token color: " + tokenColor);
                break;
        }
    }
}
