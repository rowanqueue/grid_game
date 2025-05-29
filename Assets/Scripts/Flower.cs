using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Flower : MonoBehaviour
{
    public Sprite[] stemSprites;
    public SpriteRenderer stem;
    public Sprite[] petalSprites;
    public SpriteRenderer petals;
    public float animSpeed = 0.1f;
    float animIndex = 0;
    public Logic.TokenColor tokenColor;
    float _angle;
    int wind = 0;
    public int x;
    // Start is called before the first frame update
    void Start()
    {
        bool flip = Random.value < 0.5f;
        //stem.flipX = flip;
        //petals.flipX = flip;
        float scale = Random.Range(0.375f, 0.525f);
        transform.localScale = Vector3.one * scale;
        float angle = Random.Range(-15f, 15f);
        transform.localEulerAngles = new Vector3(0f, 0f, angle);
        angle = Random.Range(-30f, 30f);
        _angle = angle;
        petals.transform.localEulerAngles = new Vector3(0f, 0f, angle);
        scale = Random.Range(0.8f, 1f);
        petals.transform.localScale = Vector3.one * scale;
        float[] values = new float[] {0.875f, 0.9f, 0.925f, 0.95f, 0.975f, 1.0f };
        float value = values[Random.Range(0,values.Length)];
        Color c = Color.HSVToRGB(0, 0, value);
        petals.color = c;
        if(tokenColor == Logic.TokenColor.Blue)
        {
            //petals.color = Color.blue;
        }else if(tokenColor== Logic.TokenColor.Green)
        {
            //petals.color = Color.green;
        }
    }
    public void Finish()
    {
        animIndex = 100;
    }

    // Update is called once per frame
    void Update()
    {
        animIndex += animSpeed;
        int index = Mathf.FloorToInt(animIndex);
        index = Mathf.Clamp(index,0,stemSprites.Length-1);
        stem.sprite = stemSprites[index];
        petals.sprite = petalSprites[index];
        if (Mathf.Round(Time.time*3f % 15) == x)
        {
            wind = 1;
        }
        float angle = _angle;
        float scale_factor = 0.25f * (1f / transform.localScale.x);
 ;       if (wind > 0)
        {
            if(wind == 1)
            {
                angle -= 7.5f*scale_factor;
            }
            if(wind == 2)
            {
                angle += 7.5f * scale_factor;
            }
            if(wind == 3)
            {
                angle -= 7.5f * 0.5f * scale_factor;
            }
            if (wind == 4)
            {
                angle += 7.5f * 0.5f * scale_factor;
            }
            if (wind == 5)
            {
                angle -= 7.5f * 0.25f * scale_factor;
            }
            if (wind == 6)
            {
                angle += 7.5f * 0.25f * scale_factor;
            }

            if (Mathf.Abs(Mathf.DeltaAngle(angle,transform.localEulerAngles.z)) < 0.1f)
            {
                wind += 1;
                if(wind > 6)
                {
                    wind = 0;
                }
            }
        }
        angle = Mathf.LerpAngle(transform.localEulerAngles.z, angle, 0.155f);
        transform.localEulerAngles = new Vector3(0f, 0f, angle);

    }
}
