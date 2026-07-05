using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeTab : MonoBehaviour
{
    [SerializeField] float slideOffScreenX = -2f;
    [SerializeField] float slideDuration = 0.25f;
    [SerializeField] float autoDismissSeconds = 5f;

    public UpgradePopup popup;
    public bool active;
    public SpriteRenderer spriteRenderer;
    public List<MiniTile> miniTiles = new List<MiniTile>();

    Coroutine slideCoroutine;
    Coroutine dismissCoroutine;

    public void Activate()
    {
        active = true;
        spriteRenderer.enabled = true;
    }

    public void NewPosition(Vector3 pos)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        transform.localPosition = new Vector3(slideOffScreenX, pos.y);
        slideCoroutine = StartCoroutine(SlideIn(pos.x));
    }

    IEnumerator SlideIn(float x)
    {
        yield return SlideHelper.SlideLocalX(transform, x, slideDuration);
        slideCoroutine = null;
        if (dismissCoroutine != null)
            StopCoroutine(dismissCoroutine);
        dismissCoroutine = StartCoroutine(CloseTime());
    }

    public void Deactivate()
    {
        active = false;
        spriteRenderer.enabled = false;
        if (dismissCoroutine != null)
        {
            StopCoroutine(dismissCoroutine);
            dismissCoroutine = null;
        }
        foreach (MiniTile tile in miniTiles)
            tile.gameObject.SetActive(false);
    }

    public void SetHeight(float globalY)
    {
        if (active)
            StartCoroutine(FallDown(globalY));
        else
            transform.parent.localPosition = new Vector3(transform.parent.localPosition.x, globalY);
    }

    IEnumerator FallDown(float y)
    {
        yield return SlideHelper.SlideLocalY(transform.parent, y, slideDuration);
    }

    IEnumerator CloseTime()
    {
        yield return new WaitForSeconds(autoDismissSeconds);
        dismissCoroutine = null;
        StartCoroutine(Slideout());
    }

    IEnumerator Slideout()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        yield return SlideHelper.SlideLocalX(transform, slideOffScreenX, slideDuration);
        slideCoroutine = null;
        if (active)
        {
            Deactivate();
            popup.TabClosed(this);
        }
    }
}
