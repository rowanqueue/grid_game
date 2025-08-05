using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeShadow : MonoBehaviour
{
    Vector3 start;
    public Transform child;
    Vector3 childStart;
    float seed;
    public float valA = 0.2f;
    public float valB = 0.75f;
    public float valChildA = 0.3f;
    public float valChildB = 0.3f;

    public float noise;
    public float input;
    public float output;

    private void Start()
    {
        start = transform.position;
        childStart = child.localPosition;
        seed = 22 + UnityEngine.Random.Range(1,5);
    }
    private void Update()
    {
        noise = Mathf.Clamp(Mathf.PerlinNoise1D(Time.time * 0.1f + seed), 0f, 0.7f);
        input = seed + Time.time * valA;
        output = Mathf.Sin(input) * valB * noise;
        float offsetX = output;
        float offsetY = Mathf.Sin(seed/2 + Time.time * (noise * valA)) * valB;
        float childOffsetX = Mathf.Sin(seed +Time.time * (noise * valChildA)) * valChildB;
        float childOffsetY = Mathf.Sin(seed/2 + Time.time * (noise * valChildA)) * valChildB;
        transform.position += ((start + (Vector3.right * offsetX) + Vector3.down * offsetY) - (new Vector3(transform.position.x / 2, transform.position.y, transform.position.z))) * 0.1f;
        child.localPosition += ((childStart + Vector3.right * childOffsetX + Vector3.down * childOffsetY) - ((new Vector3(child.localPosition.x / 2, child.localPosition.y, child.localPosition.z)))) * 0.1f;
    }
}
