using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AnchorGameObject : MonoBehaviour
{
    const int MaxCameraWaitFrames = 120;
    const float PositionEpsilonSq = 1e-6f;

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
    public bool moved = false;
    public AnchorType anchorType;
    public Vector3 anchorOffset;

    Vector3 _lastScreenOffset;
    Coroutine _anchorRoutine;

    void Awake()
    {
        if (Application.isPlaying)
            moved = false;
    }

    public void OnEnable()
    {
        if (Application.isPlaying)
            SetAnchor(_lastScreenOffset);
        else if (moved == false)
            SetAnchor();
    }

    /// <summary>Update anchor position. Pass screenAnchorOffset from flora.Screen for per-screen offset.</summary>
    public void SetAnchor(Vector3 screenOffset = default)
    {
        SetAnchor(screenOffset, forceInactive: false);
    }

    /// <summary>
    /// When forceInactive is true, positions anchors even if this object is under an inactive screen root
    /// (used by flora.Screen to pre-layout menus before first open).
    /// </summary>
    public void SetAnchor(Vector3 screenOffset, bool forceInactive)
    {
        if (!forceInactive && (gameObject.activeInHierarchy == false || gameObject.activeSelf == false))
            return;

        _lastScreenOffset = screenOffset;
        moved = true;

        // Inactive GameObjects cannot StartCoroutine; pre-layout uses sync path after startup refresh.
        if (forceInactive)
        {
            CancelAnchorRoutineIfRunning();
            if (CameraViewportHandler.Instance != null)
                UpdateAnchor(screenOffset);
            return;
        }

        CancelAnchorRoutineIfRunning();
        if (!isActiveAndEnabled)
            return;
        _anchorRoutine = StartCoroutine(UpdateAnchorAsync(screenOffset));
    }

    void CancelAnchorRoutineIfRunning()
    {
        if (_anchorRoutine == null)
            return;
        if (isActiveAndEnabled)
            StopCoroutine(_anchorRoutine);
        _anchorRoutine = null;
    }

    IEnumerator UpdateAnchorAsync(Vector3 screenOffset)
    {
        int cameraWaitCycles = 0;

        while (CameraViewportHandler.Instance == null)
        {
            if (++cameraWaitCycles > MaxCameraWaitFrames)
            {
                Debug.LogError(
                    $"[AnchorGameObject] CameraViewportHandler.Instance is null after {MaxCameraWaitFrames} frames on '{name}'. " +
                    "Ensure Main Camera has CameraViewportHandler.",
                    this);
                _anchorRoutine = null;
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        if (cameraWaitCycles > 0)
        {
            Debug.LogWarning(
                $"[AnchorGameObject] '{name}' found CameraViewportHandler after waiting {cameraWaitCycles} frame(s). " +
                "Check CameraViewportHandler DefaultExecutionOrder (-100).",
                this);
        }

        if (gameObject.activeInHierarchy == false || gameObject.activeSelf == false)
        {
            _anchorRoutine = null;
            yield break;
        }

        UpdateAnchor(screenOffset);
        _anchorRoutine = null;
    }

    void UpdateAnchor(Vector3 screenOffset)
    {
        var cvh = CameraViewportHandler.Instance;
        if (cvh == null)
            return;

        switch (anchorType)
        {
            case AnchorType.BottomLeft:
                ApplyAnchor(cvh.BottomLeft, screenOffset);
                break;
            case AnchorType.BottomCenter:
                ApplyAnchor(cvh.BottomCenter, screenOffset);
                break;
            case AnchorType.BottomRight:
                ApplyAnchor(cvh.BottomRight, screenOffset);
                break;
            case AnchorType.MiddleLeft:
                ApplyAnchor(cvh.MiddleLeft, screenOffset);
                break;
            case AnchorType.MiddleCenter:
                ApplyAnchor(cvh.MiddleCenter, screenOffset);
                break;
            case AnchorType.MiddleRight:
                ApplyAnchor(cvh.MiddleRight, screenOffset);
                break;
            case AnchorType.TopLeft:
                ApplyAnchor(cvh.TopLeft, screenOffset);
                break;
            case AnchorType.TopCenter:
                ApplyAnchor(cvh.TopCenter, screenOffset);
                break;
            case AnchorType.TopRight:
                ApplyAnchor(cvh.TopRight, screenOffset);
                break;
            case AnchorType.SafeBottomLeft:
                ApplyAnchor(cvh.SafeBottomLeft, screenOffset);
                break;
            case AnchorType.SafeBottomCenter:
                ApplyAnchor(cvh.SafeBottomCenter, screenOffset);
                break;
            case AnchorType.SafeBottomRight:
                ApplyAnchor(cvh.SafeBottomRight, screenOffset);
                break;
            case AnchorType.SafeTopLeft:
                ApplyAnchor(cvh.SafeTopLeft, screenOffset);
                break;
            case AnchorType.SafeTopCenter:
                ApplyAnchor(cvh.SafeTopCenter, screenOffset);
                break;
            case AnchorType.SafeTopRight:
                ApplyAnchor(cvh.SafeTopRight, screenOffset);
                break;
        }
    }

    void ApplyAnchor(Vector3 anchor, Vector3 screenOffset)
    {
        Vector3 newPos = anchor + anchorOffset + screenOffset;
        if ((transform.localPosition - newPos).sqrMagnitude > PositionEpsilonSq)
            transform.localPosition = newPos;
    }

#if UNITY_EDITOR
    void Update()
    {
        if (_anchorRoutine == null && executeInUpdate)
            _anchorRoutine = StartCoroutine(UpdateAnchorAsync(Vector3.zero));
    }
#endif
}
