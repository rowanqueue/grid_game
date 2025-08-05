using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedPopup : MonoBehaviour
{
    bool open;
    GameObject visual;
    bool closing;

    private void Awake()
    {
        visual = transform.GetChild(0).gameObject;
        visual.SetActive(false);
    }
    private void Update()
    {
        if (open && !closing)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartCoroutine(WaitToClose());
            }
        }
    }
    public void Open()
    {
        open = true;
        visual.SetActive(true);
    }
    public void Close()
    {
        open = false;
        visual.SetActive(false);
    }
    IEnumerator WaitToClose()
    {
        closing = true;
        yield return new WaitForSeconds(0.1f);
        Close();
        closing = false;
    }
}
