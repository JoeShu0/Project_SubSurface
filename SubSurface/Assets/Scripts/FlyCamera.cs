using System;
using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    private const float MaxEffectiveTPF = 1.0f;
    // 是否在缩放
    private bool bPinch = false;
    // 是否在移动
    private bool bMove = false;
    // 是否在旋转
    private bool bRotate = false;

    protected enum FlyCameraAction
    {
        //Rotation
        Left,
        Right,
        Up,
        Down,
        Drag,

        //Translation
        Forward,
        Backword,
        Leftword,
        Rightword,
        Rise,
        Lower,

        //Zoom
        ZoomIn,
        ZoomOut,

        Count
    }

    //public float minX = -412.5f;
    //public float maxX = 712.5f;
    //public float minY = 150;
    //public float maxY = 450;
    //public float minZ = -412.5f;
    //public float maxZ = 712.5f;

    public float MoveSpeed = 15;
    public float RotationSpeed = 1;
    public float ZoomSpeed = 1;

    private Vector3 _upVector;
    private float[] _actionStatus;

    private int _oldMouseX;
    private int _oldMouseY;
    private bool _rightMouseDown = false;
    private bool _leftMouseDown = false;
    private Vector2 _dragDir;

    private static Transform _target = null;


    void Start()
    {
        //EasyTouch.On_PinchEnd += On_PinchEnd;
        //EasyTouch.On_Pinch += On_Pinch;
        _upVector = Vector3.up;
        _actionStatus = new float[(int)FlyCameraAction.Count];
    }

    // Update is called once per frame
    void Update()
    {
        _keyPressed(this);
        _keyReleased(this);
        _mouseMoved(this, (int)Input.mousePosition.x, (int)Input.mousePosition.y);
        _wheelScrooled(this);

        float effectiveTPF = Math.Min(Time.deltaTime, MaxEffectiveTPF);

        if (_actionStatus[(int)FlyCameraAction.Left] != 0)
        {
            _rotate_camera(transform, -_actionStatus[(int)FlyCameraAction.Left] * RotationSpeed, Vector3.up,
                _upVector);
            _actionStatus[(int)FlyCameraAction.Left] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.Right] != 0)
        {
            _rotate_camera(transform, _actionStatus[(int)FlyCameraAction.Right] * RotationSpeed, Vector3.up,
                _upVector);
            _actionStatus[(int)FlyCameraAction.Right] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.Up] != 0)
        {
            Vector3 camDir = transform.forward;
            float dotvy = Vector3.Dot(Vector3.up, camDir);
            Vector3 axis = Vector3.Cross(camDir, _upVector).normalized;
            if (dotvy > -0.99f)
            {
                _rotate_camera(transform, -_actionStatus[(int)FlyCameraAction.Up] * RotationSpeed, axis, _upVector);
            }
            _actionStatus[(int)FlyCameraAction.Up] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.Down] != 0)
        {
            Vector3 camDir = transform.forward;
            float dotvy = Vector3.Dot(Vector3.up, camDir);
            Vector3 axis = Vector3.Cross(camDir, _upVector).normalized;
            if (dotvy < 0.99f)
            {
                _rotate_camera(transform, _actionStatus[(int)FlyCameraAction.Down] * RotationSpeed, axis, _upVector);
            }
            _actionStatus[(int)FlyCameraAction.Down] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.Drag] != 0)
        {
            Vector3 direction = new Vector3(_dragDir.x, 0, _dragDir.y);
            _move_camera(transform, _actionStatus[(int)FlyCameraAction.Drag] * MoveSpeed * effectiveTPF, direction);

            _actionStatus[(int)FlyCameraAction.Drag] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.Forward] != 0)
        {
            Vector3 direction = transform.forward;
            _move_camera(transform, _actionStatus[(int)FlyCameraAction.Forward] * MoveSpeed * effectiveTPF, direction);
        }
        if (_actionStatus[(int)FlyCameraAction.Backword] != 0)
        {
            Vector3 direction = transform.forward;
            _move_camera(transform, -_actionStatus[(int)FlyCameraAction.Backword] * MoveSpeed * effectiveTPF, direction);
        }
        if (_actionStatus[(int)FlyCameraAction.Leftword] != 0)
        {
            Vector3 direction = transform.forward;
            Vector3 up = transform.up;
            Vector3 left = Vector3.Cross(up, direction);
            _move_camera(transform, -_actionStatus[(int)FlyCameraAction.Leftword] * MoveSpeed * effectiveTPF, left);
        }
        if (_actionStatus[(int)FlyCameraAction.Rightword] != 0)
        {
            Vector3 direction = transform.forward;
            Vector3 up = transform.up;
            Vector3 left = Vector3.Cross(up, direction);
            _move_camera(transform, _actionStatus[(int)FlyCameraAction.Rightword] * MoveSpeed * effectiveTPF, left);
        }
        if (_actionStatus[(int)FlyCameraAction.Rise] != 0)
        {
            Vector3 up = Vector3.up;
            _move_camera(transform, _actionStatus[(int)FlyCameraAction.Rise] * MoveSpeed * effectiveTPF, up);
        }
        if (_actionStatus[(int)FlyCameraAction.Lower] != 0)
        {
            Vector3 up = Vector3.up;
            _move_camera(transform, -_actionStatus[(int)FlyCameraAction.Lower] * MoveSpeed * effectiveTPF, up);
        }
        if (_actionStatus[(int)FlyCameraAction.ZoomIn] != 0)
        {
            _zoom_camera(transform, -_actionStatus[(int)FlyCameraAction.ZoomIn] * ZoomSpeed);
            _actionStatus[(int)FlyCameraAction.ZoomIn] = 0;
        }
        if (_actionStatus[(int)FlyCameraAction.ZoomOut] != 0)
        {
            _zoom_camera(transform, _actionStatus[(int)FlyCameraAction.ZoomOut] * ZoomSpeed);
            _actionStatus[(int)FlyCameraAction.ZoomOut] = 0;
        }
        //transform.position = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), Mathf.Clamp(transform.position.y, minY, maxY), Mathf.Clamp(transform.position.z, minZ, maxZ));
    }

    static void _rotate_camera(Transform cam, float value, Vector3 axis, Vector3 up)
    {
        Quaternion quatSrc = cam.rotation;
        Quaternion quat = Quaternion.AngleAxis(value, axis);
        quat = quat * quatSrc;
        cam.rotation = quat;
        Vector3 direction = cam.forward;
        Vector3 left = Vector3.Cross(up, direction).normalized;
        Vector3 fixedUp = Vector3.Cross(direction, left);
        cam.up = fixedUp;
        cam.forward = direction;
    }

    static void _move_camera(Transform cam, float value, Vector3 axis)
    {
        Vector3 vec;
        Vector3 vecSrc = cam.position;
        vec = axis * value;

        if (_target != null)
        {
            Vector3 pos = _target.position;
            pos.x += vec.x;
            pos.z += vec.z;
            _target.position = pos;

        }
        vec += vecSrc;
        cam.position = vec;
    }

    //void On_Pinch(Gesture gesture)
    //{
    //    if (bRotate || bMove)
    //    {
    //        return;
    //    }
    //    bPinch = true;
    //    _zoom_camera(transform, gesture.deltaPinch * Time.deltaTime * ZoomSpeed * 0.01f);
    //}

    //void On_PinchEnd(Gesture gesture)
    //{
    //    bPinch = false;
    //}

    /// <summary>
    /// 摇杆函数
    /// </summary>
    /// <param name="direction"></param>
    public void JoyStickControlMove(Vector2 direction)
    {
        if (bPinch)
        {
            return;
        }
        // 横向移动
        Vector3 v3direction = transform.forward;
        Vector3 v3up = transform.up;
        Vector3 v3left = Vector3.Cross(v3up, v3direction);
        _move_camera(transform, direction.x * MoveSpeed * 0.04f, v3left);

        // 前进后退
        _move_camera(transform, direction.y * MoveSpeed * 0.04f, transform.forward);
        bMove = true;
    }

    public void On_MoveEnd()
    {
        bMove = false;
    }

    /// <summary>
    /// 摇杆函数
    /// </summary>
    /// <param name="direction"></param>
    public void JoyStickControlRotate(Vector2 direction)
    {
        if (bPinch)
        {
            return;
        }
        // 左右旋转
        _rotate_camera(transform, direction.x * RotationSpeed * 0.5f, Vector3.up, _upVector);

        // 上下旋转
        Vector3 camDir = transform.forward;
        float dotvy = Vector3.Dot(Vector3.up, camDir);
        Vector3 axis = Vector3.Cross(camDir, _upVector).normalized;
        if (direction.y > 0)
        {
            if (dotvy < 0.99f)
            {
                _rotate_camera(transform, direction.y * RotationSpeed * 0.5f, axis, _upVector);
            }
        }
        else
        {
            if (dotvy > -0.99f)
            {
                _rotate_camera(transform, direction.y * RotationSpeed, axis, _upVector);
            }
        }
        bRotate = true;
    }

    public void On_RotateEnd()
    {
        bRotate = false;
    }

    static void _zoom_camera(Transform cam, float value)
    {
        Camera c = cam.GetComponent<Camera>();
        if (!c)
        {
            return;
        }

        float zoom = 1.0f + 1.0f / 5.0f * value;
        if (c.orthographic)
        {
            c.orthographicSize = c.orthographicSize * zoom;
        }
        else
        {
            c.transform.position += -c.transform.forward * value * c.farClipPlane * 0.1f;
            //c.fieldOfView = c.fieldOfView * zoom;
        }
    }

    static void _flyCameraAction(FlyCamera flyCam, FlyCameraAction action, float value)
    {
        flyCam._actionStatus[(int)action] = value;
    }

    private void _keyPressed(FlyCamera flyCam)
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Forward, 1);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Backword, 1);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Leftword, 1);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Rightword, 1);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Lower, 1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Rise, 1);
        }
    }

    private void _keyReleased(FlyCamera flyCam)
    {
        if (Input.GetKeyUp(KeyCode.W))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Forward, 0);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Backword, 0);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Leftword, 0);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Rightword, 0);
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Lower, 0);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            _flyCameraAction(flyCam, FlyCameraAction.Rise, 0);
        }
    }

    static void _mouseMoved(FlyCamera flyCam, int x, int y)
    {
        if (Input.touchCount >= 1)
        {
            if (Input.touches[0].phase == TouchPhase.Began)
            {
                flyCam._leftMouseDown = true;
                x = (int)Input.touches[0].position.x;
                y = (int)Input.touches[0].position.y;

            }
            if (Input.touches[0].phase == TouchPhase.Ended)
            {
                flyCam._leftMouseDown = false;
            }
            if (Input.touches[0].phase == TouchPhase.Moved)
            {
                x = (int)Input.touches[0].position.x;
                y = (int)Input.touches[0].position.y;
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        //if (Input.GetMouseButtonDown(0))
        {
            flyCam._rightMouseDown = true;
            flyCam._oldMouseX = x;
            flyCam._oldMouseY = y;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        // if (Input.GetMouseButtonUp(0))
        {
            flyCam._rightMouseDown = false;
        }
        if (Input.GetKeyDown(KeyCode.Mouse0))
        //if (Input.GetMouseButtonDown(0))
        {
            flyCam._leftMouseDown = true;
            flyCam._oldMouseX = x;
            flyCam._oldMouseY = y;
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        // if (Input.GetMouseButtonUp(0))
        {
            flyCam._leftMouseDown = false;
        }
        float aspect = 1; //flyCam._camera.aspect;
        if (flyCam._rightMouseDown && (flyCam._oldMouseX != x || flyCam._oldMouseY != y))
        {
            int offsetX = x - flyCam._oldMouseX;
            int offsetY = y - flyCam._oldMouseY;
            if (offsetX > 0)
            {
                _flyCameraAction(flyCam, FlyCameraAction.Right, offsetX / 10.0f);
            }
            else
            {
                _flyCameraAction(flyCam, FlyCameraAction.Left, -offsetX / 10.0f);
            }
            if (offsetY > 0)
            {
                _flyCameraAction(flyCam, FlyCameraAction.Down, offsetY / 10.0f / aspect);
            }
            else
            {
                _flyCameraAction(flyCam, FlyCameraAction.Up, -offsetY / 10.0f / aspect);
            }
        }

        if (flyCam._leftMouseDown && (flyCam._oldMouseX != x || flyCam._oldMouseY != y))
        {
            int offsetX = x - flyCam._oldMouseX;
            int offsetY = y - flyCam._oldMouseY;
            Vector3 dir = new Vector3(offsetX, 0, offsetY);
            float len = dir.magnitude;
            dir.Normalize();

            Vector3 horizontalCamDir =
                new Vector3(flyCam.transform.forward.x, 0, flyCam.transform.forward.z).normalized;
            Vector3 originalDir = new Vector3(0, 0, 1);
            Quaternion q = Quaternion.FromToRotation(originalDir, horizontalCamDir);
            Vector3 tmp = -(q * dir);
            flyCam._dragDir = new Vector2(tmp.x, tmp.z);

            _flyCameraAction(flyCam, FlyCameraAction.Drag, len * 0.2f);
        }
        flyCam._oldMouseX = x;
        flyCam._oldMouseY = y;
    }

    static void _wheelScrooled(FlyCamera flyCam)
    {
        float amount = Input.GetAxis("Mouse ScrollWheel");

        if (amount > 0)
        {
            _flyCameraAction(flyCam, FlyCameraAction.ZoomIn, (float)amount);
        }
        else if (amount < 0)
        {
            _flyCameraAction(flyCam, FlyCameraAction.ZoomOut, -(float)amount);
        }
    }
}
