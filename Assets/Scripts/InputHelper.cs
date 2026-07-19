using UnityEngine;

public static class InputHelper
{
    static Vector3 lastPointerScreenPosition;
    static bool hasLastPointer;
    static bool primaryHeldLastFrame;

    /// <summary>
    /// Call once at the start of the frame before reading press edges / pointer.
    /// </summary>
    public static void BeginFrame()
    {
        if (Input.touchCount > 0)
        {
            lastPointerScreenPosition = Input.GetTouch(0).position;
            hasLastPointer = true;
        }
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || Input.mousePresent)
        {
            lastPointerScreenPosition = Input.mousePosition;
            hasLastPointer = true;
        }
    }

    /// <summary>
    /// Call once at the end of input handling so Ended pulses only one frame.
    /// </summary>
    public static void EndFrame()
    {
        primaryHeldLastFrame = IsPrimaryHeldNow();
    }

    static bool IsPrimaryHeldNow()
    {
        if (Input.GetMouseButton(0))
            return true;
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Began
                || phase == TouchPhase.Moved
                || phase == TouchPhase.Stationary;
        }
        return false;
    }

    public static Vector3 GetPointerScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            lastPointerScreenPosition = Input.GetTouch(0).position;
            hasLastPointer = true;
            return lastPointerScreenPosition;
        }
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            lastPointerScreenPosition = Input.mousePosition;
            hasLastPointer = true;
            return lastPointerScreenPosition;
        }
        if (hasLastPointer)
            return lastPointerScreenPosition;
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
        return IsPrimaryHeldNow();
    }

    public static bool GetPrimaryPressEnded()
    {
        if (Input.GetMouseButtonUp(0))
        {
            lastPointerScreenPosition = Input.mousePosition;
            hasLastPointer = true;
            return true;
        }
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                lastPointerScreenPosition = Input.GetTouch(0).position;
                hasLastPointer = true;
                return true;
            }
            return false;
        }
        // Touch vanished without an Ended sample — still treat as release once.
        return primaryHeldLastFrame && !Input.GetMouseButton(0);
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
