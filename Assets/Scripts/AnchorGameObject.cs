using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AnchorGameObject : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        TopLeft,
        TopCenter,
        TopRight,
        SafeBottomLeft,
        SafeBottomCenter,
        SafeBottomRight,
        SafeTopLeft,
        SafeTopCenter,
        SafeTopRight,
    };

    public bool executeInUpdate;
    // Has the anchor moved yet?
    public bool moved = false;
    public AnchorType anchorType;
    public Vector3 anchorOffset;

    IEnumerator updateAnchorRoutine; //Coroutine handle so we don't start it if it's already running

    public void OnEnable()
    {
        if (moved == false)
        {
            SetAnchor();
        }
    }
    /// <summary>Update anchor position. Pass screenAnchorOffset from flora.Screen for per-screen offset.</summary>
    public void SetAnchor(Vector3 screenOffset = default)
    {
        if (gameObject.activeInHierarchy == false || gameObject.activeSelf == false)
        {
            return;
        }
        moved = true;
        updateAnchorRoutine = UpdateAnchorAsync(screenOffset);
        StartCoroutine(updateAnchorRoutine);
    }

    /// <summary>
    /// Coroutine to update the anchor only once CameraViewportHandler.Instance is not null.
    /// </summary>
    IEnumerator UpdateAnchorAsync(Vector3 screenOffset)
    {

        uint cameraWaitCycles = 0;

        while (CameraViewportHandler.Instance == null)
        {
            ++cameraWaitCycles;
            yield return new WaitForEndOfFrame();
        }

        if (cameraWaitCycles > 0)
        {
            print(string.Format("CameraAnchor found CameraFit instance after waiting {0} frame(s). " +
                "You might want to check that CameraFit has an earlie execution order.", cameraWaitCycles));
        }

        UpdateAnchor(screenOffset);
        updateAnchorRoutine = null;

    }

    void UpdateAnchor(Vector3 screenOffset)
    {
        switch (anchorType)
        {
            case AnchorType.BottomLeft:
                ApplyAnchor(CameraViewportHandler.Instance.BottomLeft, screenOffset);
                break;
            case AnchorType.BottomCenter:
                ApplyAnchor(CameraViewportHandler.Instance.BottomCenter, screenOffset);
                break;
            case AnchorType.BottomRight:
                ApplyAnchor(CameraViewportHandler.Instance.BottomRight, screenOffset);
                break;
            case AnchorType.MiddleLeft:
                ApplyAnchor(CameraViewportHandler.Instance.MiddleLeft, screenOffset);
                break;
            case AnchorType.MiddleCenter:
                ApplyAnchor(CameraViewportHandler.Instance.MiddleCenter, screenOffset);
                break;
            case AnchorType.MiddleRight:
                ApplyAnchor(CameraViewportHandler.Instance.MiddleRight, screenOffset);
                break;
            case AnchorType.TopLeft:
                ApplyAnchor(CameraViewportHandler.Instance.TopLeft, screenOffset);
                break;
            case AnchorType.TopCenter:
                ApplyAnchor(CameraViewportHandler.Instance.TopCenter, screenOffset);
                break;
            case AnchorType.TopRight:
                ApplyAnchor(CameraViewportHandler.Instance.TopRight, screenOffset);
                break;
            case AnchorType.SafeBottomLeft:
                ApplyAnchor(CameraViewportHandler.Instance.SafeBottomLeft, screenOffset);
                break;
            case AnchorType.SafeBottomCenter:
                ApplyAnchor(CameraViewportHandler.Instance.SafeBottomCenter, screenOffset);
                break;
            case AnchorType.SafeBottomRight:
                ApplyAnchor(CameraViewportHandler.Instance.SafeBottomRight, screenOffset);
                break;
            case AnchorType.SafeTopLeft:
                ApplyAnchor(CameraViewportHandler.Instance.SafeTopLeft, screenOffset);
                break;
            case AnchorType.SafeTopCenter:
                ApplyAnchor(CameraViewportHandler.Instance.SafeTopCenter, screenOffset);
                break;
            case AnchorType.SafeTopRight:
                ApplyAnchor(CameraViewportHandler.Instance.SafeTopRight, screenOffset);
                break;
        }
    }

    void ApplyAnchor(Vector3 anchor, Vector3 screenOffset)
    {
        Vector3 newPos = anchor + anchorOffset + screenOffset;
        if (!transform.localPosition.Equals(newPos))
        {
            transform.localPosition = newPos;
        }
    }

#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (updateAnchorRoutine == null && executeInUpdate)
        {
            updateAnchorRoutine = UpdateAnchorAsync(Vector3.zero);
            StartCoroutine(updateAnchorRoutine);
        }
    }
#endif
}