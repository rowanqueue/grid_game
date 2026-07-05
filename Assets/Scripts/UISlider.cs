using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISlider : MonoBehaviour
{
    public bool music;
    public Vector2 startPoint;
    public Vector2 endPoint;
    bool held = false;

    void Start()
    {
        string setting = (music ? "musicVolume" : "soundVolume");
        if (PlayerPrefs.HasKey(setting))
        {
            float percent = PlayerPrefs.GetFloat(setting);
            float x = Mathf.Lerp(startPoint.x, endPoint.x, percent);
            transform.localPosition = new Vector2(x, startPoint.y);
        }
    }

    void SetVolume(float x)
    {
        x -= startPoint.x;
        x /= (endPoint.x - startPoint.x);
        string setting = (music ? "musicVolume" : "soundVolume");
        PlayerPrefs.SetFloat(setting, x);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (held)
        {
            float x = InputHelper.GetPointerWorldPosition().x;
            x -= transform.parent.position.x;
            x = Mathf.Clamp(x, startPoint.x, endPoint.x);
            transform.localPosition = new Vector2(x, startPoint.y);
            if (InputHelper.GetPrimaryPressEnded())
            {
                held = false;
                SetVolume(x);
            }
        }
    }

    void OnMouseDown()
    {
        held = true;
        float x = InputHelper.GetPointerWorldPosition().x;
        x -= transform.parent.position.x;
        x = Mathf.Clamp(x, startPoint.x, endPoint.x);
        transform.localPosition = new Vector2(x, startPoint.y);
    }
}
