using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeShadow : MonoBehaviour
{
    Vector3 start;
    public Transform child;
    Vector3 childStart;
    float seed;
    private void Start()
    {
        start = transform.position;
        childStart = child.localPosition;
        seed = Random.Range(0f, 50f);
    }
    private void Update()
    {
        transform.position += ((start + Vector3.right* Mathf.Sin(seed+ Time.time*0.2f)*0.75f)-transform.position) *0.1f;
        child.localPosition += ((childStart + Vector3.right * Mathf.Sin(seed+Time.time * 0.33f) * 0.3f) - child.localPosition) * 0.1f;
    }
}
