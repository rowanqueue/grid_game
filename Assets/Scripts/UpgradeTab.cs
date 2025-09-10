using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeTab : MonoBehaviour
{
    public UpgradePopup popup;
    public bool active;
    public SpriteRenderer spriteRenderer;
    public List<MiniTile> miniTiles = new List<MiniTile>();
    public void Activate()
    {
        active = true;
        spriteRenderer.enabled = true;
    }
    public void NewPosition(Vector3 pos)
    {
        transform.localPosition = new Vector3(-2f, pos.y);
        StartCoroutine(SlideIn(pos.x));
    }
    IEnumerator SlideIn(float x)
    {
        while(transform.localPosition.x < x)
        {
            transform.localPosition += Vector3.right * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        transform.localPosition = new Vector3(x, transform.localPosition.y);
        StartCoroutine(CloseTime());
        

    }
    public void Deactivate()
    {
        
        active = false;
        spriteRenderer.enabled = false;
        foreach (MiniTile tile in miniTiles)
        {
            tile.gameObject.SetActive(false);
        }
        
    }
    public void SetHeight(float globalY)
    {
        if (active)
        {
            StartCoroutine(FallDown(globalY));
        }
        else
        {
            transform.parent.localPosition = new Vector3(transform.parent.localPosition.x, globalY);
        }
        

    }
    IEnumerator FallDown(float y)
    {
        while (Mathf.Abs(y - transform.parent.localPosition.y) > 0.1f)
        {
            transform.parent.localPosition += (new Vector3(0,y > transform.parent.localPosition.y ? 1f : -1f)) * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        transform.parent.localPosition = new Vector3(transform.parent.localPosition.x, y);


    }
    IEnumerator CloseTime()
    {
        yield return new WaitForSeconds(5f);
        StartCoroutine(Slideout());
        
    }
    IEnumerator Slideout()
    {
        while (transform.localPosition.x > -2f)
        {
            transform.localPosition += Vector3.left * 0.1f;
            yield return new WaitForEndOfFrame();
        }
        if (active)
        {
            Deactivate();
            popup.TabClosed(this);
        }
    }
}
