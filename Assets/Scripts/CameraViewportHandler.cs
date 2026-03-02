using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraViewportHandler : MonoBehaviour
{
    public enum Constraint { Landscape, Portrait }

    #region FIELDS
    public Color wireColor = Color.white;
    [Tooltip("Design height (Portrait) or width (Landscape) of the visible area in Unity units.")]
    public float UnitsSize = 1;
    public Constraint constraint = Constraint.Portrait;
    [Tooltip("When true, exposes Safe* anchor points inset from notches/home indicator via Screen.safeArea.")]
    public bool useSafeArea = true;
    [Tooltip("Manual safe area inset for Editor testing when Screen.safeArea is fullscreen (e.g. 0.05 = 5% inset).")]
    public Vector4 manualSafeAreaInset = Vector4.zero; // left, right, bottom, top (0-1 normalized)
    public static CameraViewportHandler Instance;
    public static System.Action OnResolutionChanged;
    public new Camera camera;

    public bool executeInUpdate;

    private float _width;
    private float _height;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    //*** bottom screen
    private Vector3 _bl;
    private Vector3 _bc;
    private Vector3 _br;
    //*** middle screen
    private Vector3 _ml;
    private Vector3 _mc;
    private Vector3 _mr;
    //*** top screen
    private Vector3 _tl;
    private Vector3 _tc;
    private Vector3 _tr;
    //*** safe area insets in world units
    private float _safeLeftX, _safeRightX, _safeBottomY, _safeTopY;
    #endregion

    #region PROPERTIES
    public float Width
    {
        get
        {
            return _width;
        }
    }
    public float Height
    {
        get
        {
            return _height;
        }
    }

    // helper points:
    public Vector3 BottomLeft
    {
        get
        {
            return _bl;
        }
    }
    public Vector3 BottomCenter
    {
        get
        {
            return _bc;
        }
    }
    public Vector3 BottomRight
    {
        get
        {
            return _br;
        }
    }
    public Vector3 MiddleLeft
    {
        get
        {
            return _ml;
        }
    }
    public Vector3 MiddleCenter
    {
        get
        {
            return _mc;
        }
    }
    public Vector3 MiddleRight
    {
        get
        {
            return _mr;
        }
    }
    public Vector3 TopLeft
    {
        get
        {
            return _tl;
        }
    }
    public Vector3 TopCenter
    {
        get
        {
            return _tc;
        }
    }
    public Vector3 TopRight
    {
        get
        {
            return _tr;
        }
    }

    public float SafeLeftX => _safeLeftX;
    public float SafeRightX => _safeRightX;
    public float SafeBottomY => _safeBottomY;
    public float SafeTopY => _safeTopY;
    public Vector3 SafeBottomLeft => new Vector3(_safeLeftX, _safeBottomY, 0);
    public Vector3 SafeBottomCenter => new Vector3((_safeLeftX + _safeRightX) * 0.5f, _safeBottomY, 0);
    public Vector3 SafeBottomRight => new Vector3(_safeRightX, _safeBottomY, 0);
    public Vector3 SafeTopLeft => new Vector3(_safeLeftX, _safeTopY, 0);
    public Vector3 SafeTopCenter => new Vector3((_safeLeftX + _safeRightX) * 0.5f, _safeTopY, 0);
    public Vector3 SafeTopRight => new Vector3(_safeRightX, _safeTopY, 0);
    #endregion

    #region METHODS
    private void Awake()
    {
        camera = GetComponent<Camera>();
        Instance = this;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        ComputeResolution();
    }

    private void OnEnable()
    {
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    private void ComputeResolution()
    {
        float leftX, rightX, topY, bottomY;

        if (constraint == Constraint.Landscape)
        {
            camera.orthographicSize = 1f / camera.aspect * UnitsSize / 2f;
        }
        else
        {
            camera.orthographicSize = UnitsSize / 2f;
        }

        _height = 2f * camera.orthographicSize;
        _width = _height * camera.aspect;

        float cameraX, cameraY;
        cameraX = camera.transform.position.x;
        cameraY = camera.transform.position.y;
        cameraX = 0;
        cameraY = 0;

        leftX = cameraX - _width / 2;
        rightX = cameraX + _width / 2;
        topY = cameraY + _height / 2;
        bottomY = cameraY - _height / 2;

        //*** safe area
        float safeLeftNorm = 0, safeRightNorm = 0, safeBottomNorm = 0, safeTopNorm = 0;
        if (useSafeArea)
        {
            Rect safe = Screen.safeArea;
            float pw = Screen.width;
            float ph = Screen.height;
            if (pw > 0 && ph > 0 && (safe.x > 0 || safe.y > 0 || safe.width < pw || safe.height < ph))
            {
                safeLeftNorm = safe.x / pw;
                safeRightNorm = 1f - (safe.x + safe.width) / pw;
                safeBottomNorm = safe.y / ph;
                safeTopNorm = 1f - (safe.y + safe.height) / ph;
            }
            if (manualSafeAreaInset.sqrMagnitude > 0.0001f)
            {
                safeLeftNorm = Mathf.Max(safeLeftNorm, manualSafeAreaInset.x);
                safeRightNorm = Mathf.Max(safeRightNorm, manualSafeAreaInset.y);
                safeBottomNorm = Mathf.Max(safeBottomNorm, manualSafeAreaInset.z);
                safeTopNorm = Mathf.Max(safeTopNorm, manualSafeAreaInset.w);
            }
        }
        _safeLeftX = leftX + _width * safeLeftNorm;
        _safeRightX = rightX - _width * safeRightNorm;
        _safeBottomY = bottomY + _height * safeBottomNorm;
        _safeTopY = topY - _height * safeTopNorm;

        //*** bottom
        _bl = new Vector3(leftX, bottomY, 0);
        _bc = new Vector3(cameraX, bottomY, 0);
        _br = new Vector3(rightX, bottomY, 0);
        //*** middle
        _ml = new Vector3(leftX, cameraY, 0);
        _mc = new Vector3(cameraX, cameraY, 0);
        _mr = new Vector3(rightX, cameraY, 0);
        //*** top
        _tl = new Vector3(leftX, topY, 0);
        _tc = new Vector3(cameraX, topY, 0);
        _tr = new Vector3(rightX, topY, 0);
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            ComputeResolution();
            OnResolutionChanged?.Invoke();
        }
#if UNITY_EDITOR
        else if (executeInUpdate)
        {
            ComputeResolution();
        }
#endif
    }

    void OnDrawGizmos()
    {
        Gizmos.color = wireColor;

        Matrix4x4 temp = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        if (camera.orthographic)
        {
            float spread = camera.farClipPlane - camera.nearClipPlane;
            float center = (camera.farClipPlane + camera.nearClipPlane) * 0.5f;
            Gizmos.DrawWireCube(new Vector3(0, 0, center), new Vector3(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2, spread));
        }
        else
        {
            Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, camera.aspect);
        }
        Gizmos.matrix = temp;
    }
    #endregion

} // class