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

    private void Start()
    {
        start = transform.position;
        childStart = child.localPosition;
        seed = Random.Range(0f, 50f);
    }
    private void Update()
    {;
        transform.position += ((start + Vector3.right * Mathf.Sin(seed + Time.time * (Mathf.PerlinNoise1D(Time.time * 0.5f + seed) * valA)) * valB) - (new Vector3(transform.position.x / 2, transform.position.y, transform.position.z))) * 0.1f;
        child.localPosition += ((childStart + Vector3.right * Mathf.Sin(seed+Time.time * (Mathf.PerlinNoise1D(Time.time * 0.5f + seed) * valChildA)) * valChildB) - ((new Vector3(child.localPosition.x / 2, child.localPosition.y, child.localPosition.z)))) * 0.1f;
    }
}
