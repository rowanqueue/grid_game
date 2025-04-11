using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    public Sprite[] stemSprites;
    public SpriteRenderer stem;
    public Sprite[] petalSprites;
    public SpriteRenderer petals;
    public float animSpeed = 0.1f;
    float animIndex = 0;
    public Logic.TokenColor tokenColor;
    // Start is called before the first frame update
    void Start()
    {
        bool flip = Random.value < 0.5f;
        stem.flipX = flip;
        petals.flipX = flip;
        float scale = Random.Range(0.375f, 0.525f);
        transform.localScale = Vector3.one * scale;
        float angle = Random.Range(-15f, 15f);
        transform.localEulerAngles = new Vector3(0f, 0f, angle);
        angle = Random.Range(-30f, 30f);
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
    }
}
