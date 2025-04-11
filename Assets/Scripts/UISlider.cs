using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISlider : MonoBehaviour
{
    public bool music;
    public Vector2 startPoint;
    public Vector2 endPoint;
    bool hover = false;
    bool held = false;

    // Start is called before the first frame update
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
        Debug.Log(x);
        string setting = (music ? "musicVolume" : "soundVolume");
        PlayerPrefs.SetFloat(setting, x);
        PlayerPrefs.Save();
    }

    // Update is called once per frame
    void Update()
    {
        if (held)
        {
            float x = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            x -= transform.parent.position.x;
            x = Mathf.Clamp(x, startPoint.x, endPoint.x);
            transform.localPosition = new Vector2(x, startPoint.y);
            if(Input.GetMouseButtonUp(0))
            {
                held = false;
                SetVolume(x);
            }
        }
        else
        {
            if (hover)
            {
                if(Input.GetMouseButtonDown(0))
                {
                    held = true;
                }
            }
        }
    }
    private void OnMouseEnter()
    {
        hover = true;
    }
    private void OnMouseExit()
    {
        hover = false;
    }
}
