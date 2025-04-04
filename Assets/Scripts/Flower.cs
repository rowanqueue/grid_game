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
        float scale = Random.Range(0.4f, 0.8f);
        transform.localScale = Vector3.one * scale;
        if(tokenColor == Logic.TokenColor.Blue)
        {
            //petals.color = Color.blue;
        }else if(tokenColor== Logic.TokenColor.Green)
        {
            //petals.color = Color.green;
        }
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
