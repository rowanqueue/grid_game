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
    public FMODUnity.EventReference placeSFX;
    public FMODUnity.EventReference pickUpSFX;
    public FMODUnity.EventReference putDownSFX;
    public FMODUnity.EventReference crackSFX;
    public FMODUnity.EventReference musicRef;
    public FMODUnity.EventReference ambienceRef;
    public FMODUnity.EventReference freeSlotSFX;
    public FMODUnity.EventReference bagSFX;
    public FMODUnity.EventReference undoSFX;
    public FMODUnity.EventReference shearsSFX;
    public FMODUnity.EventReference bagRustleSFX;
    public FMODUnity.EventReference newTileSFX;
    public FMODUnity.EventReference buttonPressSFX;
    public FMODUnity.EventReference menuTransitionSFX;
    public FMODUnity.EventReference startButtonSFX;
    public FMODUnity.EventReference tutorialNotificationSFX;
    public FMODUnity.EventReference invalidToolSFX;
    FMOD.Studio.PARAMETER_ID popDepth;
    [Header("Misc")]
    public float baseVolume;
    public float loudVolume;

    public SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    public bool muted;
    bool hovered;

    FMOD.Studio.EventInstance music;
    FMOD.Studio.EventInstance ambience;

    FMOD.Studio.Bus musicBus;
    FMOD.Studio.Bus soundBus;

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
        FMOD.Studio.EventDescription popEventDescription = FMODUnity.RuntimeManager.GetEventDescription(popSFX);
        FMOD.Studio.PARAMETER_DESCRIPTION popDepthParameterDescription;
        popEventDescription.getParameterDescriptionByName("TilePopDepth", out popDepthParameterDescription);
        popDepth = popDepthParameterDescription.id;

        musicBus = FMODUnity.RuntimeManager.GetBus("bus:/Music");
        soundBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");

        music = FMODUnity.RuntimeManager.CreateInstance(musicRef);
        ambience = FMODUnity.RuntimeManager.CreateInstance(ambienceRef);
        //music.start();
        ambience.start();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Services.AudioManager.PlayButtonPressSound();
        }
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
        FMODUnity.RuntimeManager.PlayOneShot(pickUpSFX);
        //PlaySound(select);
    }
    public void PlayLetGoSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(putDownSFX);
        //PlaySound(select);
    }
    public void PlayPlaceSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(placeSFX);
        //PlaySound(select);
    }
    public void PlayRemoveTileSound(int depth = 0)
    {
        FMOD.Studio.EventInstance popEvent = FMODUnity.RuntimeManager.CreateInstance(popSFX);
        popEvent.setParameterByID(popDepth, depth);
        popEvent.start();
        popEvent.release();
        //FMODUnity.RuntimeManager.PlayOneShot(popSFX);
        //PlaySound(pop,depth);
    }
    public void PlayUpgradeTileSound(int depth = 0)
    {
        FMOD.Studio.EventInstance popEvent = FMODUnity.RuntimeManager.CreateInstance(popSFX);
        popEvent.setParameterByID(popDepth, depth);
        popEvent.start();
        popEvent.release();
        //FMODUnity.RuntimeManager.PlayOneShot(crackSFX);
        //PlaySound(crack,depth);
    }

    public void PlayFreeSlotSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(freeSlotSFX);
    }

    public void PlayBagSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(bagSFX);
    }

    public void PlayInvalidToolSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(invalidToolSFX);
    }

    public void PlayUndoSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(undoSFX);
    }

    public void PlayShearsSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(shearsSFX);
    }

    public void PlayBagRustleSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(bagRustleSFX);
    }

    public void PlayNewTileSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(newTileSFX);
    }

    public void PlayButtonPressSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(buttonPressSFX);
    }

    public void PlayMenuTransitionSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(menuTransitionSFX);
    }

    public void PlayStartTransitionSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(startButtonSFX);
    }

    public void PlayTutorialNotificationSound()
    {
        FMODUnity.RuntimeManager.PlayOneShot(tutorialNotificationSFX);
    }

    public void StartMusic()
    {
        music.start();
    }

    public void StopMusic()
    {
        music.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        music.release();
    }

    public void SetVolume(int busNumber, float volume)
    {
        if (busNumber == 0)
        {
            musicBus.setVolume(volume);
        }
        else if (busNumber == 1)
        {
            soundBus.setVolume(volume);
        }
    }

}
