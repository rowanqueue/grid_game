using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //public GameObject audioPrefab;
    [HideInInspector]
    public AudioSource[] sounds;
    public AudioClip pop;
    public AudioClip select;
    public AudioClip crack;
    public FMODUnity.EventReference popSFX;
    public FMODUnity.EventReference selectSFX;
    public FMODUnity.EventReference crackSFX;
    [Header("Misc")]
    public float baseVolume;
    public float loudVolume;

    public SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    public bool muted;
    bool hovered;

    public void Initialize()
    {
        /*spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Services.Visuals.connectorColor;*/
        /*
        sounds = new AudioSource[32];
        muted = false;
        for (int i = 0; i < sounds.Length; i++)
        {
            GameObject obj = GameObject.Instantiate(audioPrefab, transform);
            sounds[i] = obj.GetComponent<AudioSource>();
            sounds[i].loop = false;
        }*/
        /*if(Services.GameController.grid.landscape == false){
            transform.position =  -Services.GameController.grid.menuVector+new Vector3(0,Services.GameController.grid.verticalDistance*1.75f);
            transform.position+=Services.GameController.grid.transform.position;
        }else{
            transform.position =  -Services.GameController.grid.menuVector+new Vector3((Services.GameController.grid.width-1)*Services.GameController.grid.horizontalDistance,Services.GameController.grid.verticalDistance*0.55f);
            transform.position+=Services.GameController.grid.transform.position;
        }*/
    }
    public void PlaySound(AudioClip clip, int depth = 0)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            AudioSource audio = sounds[i];
            if (audio.isPlaying)
            {
                continue;
            }
            audio.clip = clip;
            audio.pitch = 1 + ((float)depth * 0.2f);
            audio.Play();
            audio.volume = baseVolume;
            break;
        }
    }
    public void PlayPickUpSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(selectSFX);
        PlaySound(select);
    }
    public void PlayLetGoSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(selectSFX);
        PlaySound(select);
    }
    public void PlayPlaceSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(selectSFX);
        PlaySound(select);
    }
    public void PlayRemoveTileSound(int depth = 0)
    {
        FMODUnity.RuntimeManager.PlayOneShot(popSFX);
        PlaySound(pop,depth);
    }
    public void PlayUpgradeTileSound(int depth = 0)
    {
        FMODUnity.RuntimeManager.PlayOneShot(crackSFX);
        PlaySound(crack,depth);
    }

}
