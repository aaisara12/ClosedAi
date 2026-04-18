using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _gamepadSensitivity = 120f;
    [SerializeField] private float _verticalClamp = 80f;
    [SerializeField] private bool _invertY = false;
    [SerializeField] private bool _smoothing = false;
    [SerializeField] private float _smoothTime = 0.05f;
    [SerializeField] private float _fieldOfView = 90f;
    [SerializeField] private float _crouchCameraLerpSpeed = 10f;

    private ClosedAi _input;
    private Vector3 _baseCameraLocalPos;
    private float _verticalAngle;
    private float _targetVertical;
    private float _horizontalAngle;
    private float _targetHorizontal;
    private float _verticalVelocity;
    private float _horizontalVelocity;

    public float CameraRoll { get; set; }
    public float CameraHeightOffset { get; set; }
    public float BaseFOV => _fieldOfView;

    public void SetFOV(float fov)
    {
        if (_cameraTransform == null) return;
        Camera cam = _cameraTransform.GetComponentInChildren<Camera>();
        if (cam != null) cam.fieldOfView = fov;
    }

    private void Awake()
    {
        _input = new ClosedAi();
        _horizontalAngle = transform.eulerAngles.y;
    }

    private void Start()
    {
        _baseCameraLocalPos = _cameraTransform != null ? _cameraTransform.localPosition : Vector3.zero;
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_cameraTransform != null)
        {
            Camera cam = _cameraTransform.GetComponentInChildren<Camera>();
            if (cam != null)
                cam.fieldOfView = _fieldOfView;
        }
    }

    private void OnDisable()
    {
        _input.Player.Disable();
    }

    private void Update()
    {
        HandleCursorLock();
        ApplyLook();
        ApplyCameraHeight();
    }

    private void HandleCursorLock()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ApplyLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        var lookAction = _input.Player.Look;
        Vector2 rawDelta = lookAction.ReadValue<Vector2>();

        bool isMouse = lookAction.activeControl?.device is Mouse;
        Vector2 scaledDelta = isMouse
            ? rawDelta * _mouseSensitivity
            : rawDelta * (_gamepadSensitivity * Time.deltaTime);

        if (_invertY)
            scaledDelta.y = -scaledDelta.y;

        _targetHorizontal += scaledDelta.x;
        _targetVertical = Mathf.Clamp(_targetVertical - scaledDelta.y, -_verticalClamp, _verticalClamp);

        if (_smoothing)
        {
            _horizontalAngle = Mathf.SmoothDamp(_horizontalAngle, _targetHorizontal, ref _horizontalVelocity, _smoothTime);
            _verticalAngle = Mathf.SmoothDamp(_verticalAngle, _targetVertical, ref _verticalVelocity, _smoothTime);
        }
        else
        {
            _horizontalAngle = _targetHorizontal;
            _verticalAngle = _targetVertical;
        }

        transform.rotation = Quaternion.Euler(0f, _horizontalAngle, 0f);

        if (_cameraTransform != null)
            _cameraTransform.localRotation = Quaternion.Euler(_verticalAngle, 0f, CameraRoll);
    }

    private void ApplyCameraHeight()
    {
        if (_cameraTransform == null) return;
        Vector3 target = _baseCameraLocalPos + Vector3.up * CameraHeightOffset;
        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, target, _crouchCameraLerpSpeed * Time.deltaTime);
    }
}
