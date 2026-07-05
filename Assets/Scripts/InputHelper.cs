using UnityEngine;

public static class InputHelper
{
    public static Vector3 GetPointerScreenPosition()
    {
        return Input.mousePosition;
    }

    public static Vector2 GetPointerWorldPosition(Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;
        if (camera == null)
            return Vector2.zero;
        return camera.ScreenToWorldPoint(GetPointerScreenPosition());
    }

    public static bool GetPrimaryPressBegan()
    {
        if (Input.GetMouseButtonDown(0))
            return true;
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    public static bool GetPrimaryPressHeld()
    {
        if (Input.GetMouseButton(0))
            return true;
        return Input.touchCount > 0
            && (Input.GetTouch(0).phase == TouchPhase.Moved
                || Input.GetTouch(0).phase == TouchPhase.Stationary
                || Input.GetTouch(0).phase == TouchPhase.Began);
    }

    public static bool GetPrimaryPressEnded()
    {
        if (Input.GetMouseButtonUp(0))
            return true;
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }
        return false;
    }

    public static bool GetAnyPressBegan()
    {
        if (Input.anyKeyDown || GetPrimaryPressBegan())
            return true;
        return false;
    }

    public static bool IsPointerOverCollider(Collider2D collider, Camera camera = null)
    {
        if (collider == null)
            return false;
        if (camera == null)
            camera = Camera.main;
        if (camera == null)
            return false;
        Vector2 world = GetPointerWorldPosition(camera);
        return collider.OverlapPoint(world);
    }

    public static bool IsPointerOverCollider(Collider collider, Camera camera = null)
    {
        if (collider == null)
            return false;
        if (camera == null)
            camera = Camera.main;
        if (camera == null)
            return false;
        Ray ray = camera.ScreenPointToRay(GetPointerScreenPosition());
        return collider.Raycast(ray, out _, Mathf.Infinity);
    }
}
