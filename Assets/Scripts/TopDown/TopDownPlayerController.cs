using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerController : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    float moveSpeed = 6f;

    [SerializeField, Min(1f)]
    float sprintMultiplier = 4.5f;

    [SerializeField]
    Camera gameplayCamera;

    [SerializeField, Min(0.001f)]
    float aimDeadZone = 0.001f;

    [SerializeField, Min(0.25f)]
    float facingIndicatorLength = 1.5f;

    [SerializeField, Range(0.01f, 0.2f)]
    float facingIndicatorWidth = 0.05f;

    [SerializeField, Range(0f, 10f)]
    float aimRotationDeadZoneDegrees = 2f;

    [SerializeField]
    Color facingIndicatorColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    Rigidbody body;
    LineRenderer facingIndicator;
    Vector2 moveInput;
    Vector3 aimDirection = Vector3.forward;
    bool isSprinting;

    public Vector3 AimDirection => aimDirection;

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        facingIndicator = CreateFacingIndicator();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    void Update()
    {
        moveInput = ReadMovementInput();
        aimDirection = ReadAimDirection();
        isSprinting = ReadSprintInput();
    }

    void FixedUpdate()
    {
        Vector3 movement = GetCameraRelativeMovement();
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float currentMoveSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 velocity = movement * currentMoveSpeed;
        velocity.y = body.linearVelocity.y;
        body.linearVelocity = velocity;

        if (aimDirection.sqrMagnitude > aimDeadZone)
        {
            float rotationDelta = Vector3.Angle(transform.forward, aimDirection);
            if (rotationDelta > aimRotationDeadZoneDegrees)
            {
                body.MoveRotation(Quaternion.LookRotation(aimDirection, Vector3.up));
            }
        }
    }

    Vector3 GetCameraRelativeMovement()
    {
        Camera activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (activeCamera != null)
        {
            cameraForward = Vector3.ProjectOnPlane(activeCamera.transform.forward, Vector3.up);
            cameraRight = Vector3.ProjectOnPlane(activeCamera.transform.right, Vector3.up);

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                cameraForward.Normalize();
            }
            else
            {
                cameraForward = Vector3.forward;
            }

            if (cameraRight.sqrMagnitude > 0.001f)
            {
                cameraRight.Normalize();
            }
            else
            {
                cameraRight = Vector3.right;
            }
        }

        return cameraRight * moveInput.x + cameraForward * moveInput.y;
    }

    LineRenderer CreateFacingIndicator()
    {
        Transform indicatorRoot = transform.Find("FacingIndicator");
        GameObject indicatorObject;

        if (indicatorRoot == null)
        {
            indicatorObject = new GameObject("FacingIndicator");
            indicatorObject.transform.SetParent(transform, false);
            indicatorObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            indicatorObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            indicatorObject = indicatorRoot.gameObject;
        }

        LineRenderer indicator = indicatorObject.GetComponent<LineRenderer>();
        if (indicator == null)
        {
            indicator = indicatorObject.AddComponent<LineRenderer>();
        }

        indicator.useWorldSpace = false;
        indicator.positionCount = 2;
        indicator.startWidth = facingIndicatorWidth;
        indicator.endWidth = facingIndicatorWidth * 0.8f;
        indicator.startColor = facingIndicatorColor;
        indicator.endColor = facingIndicatorColor;
        indicator.alignment = LineAlignment.View;
        indicator.numCapVertices = 4;
        indicator.numCornerVertices = 4;
        indicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicator.receiveShadows = false;
        indicator.sharedMaterial = CreateLineMaterial();
        indicator.SetPosition(0, Vector3.zero);
        indicator.SetPosition(1, Vector3.forward * facingIndicatorLength);

        return indicator;
    }

    static Material lineMaterial;

    static Material CreateLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        Shader shader = Shader.Find("TopDown/UnlitVertexColor");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return lineMaterial;
    }

    Vector2 ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    static bool ReadSprintInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }

    Vector3 ReadAimDirection()
    {
        Camera activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        Mouse mouse = Mouse.current;
        if (activeCamera == null || mouse == null)
        {
            return transform.forward;
        }

        Ray ray = activeCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (!aimPlane.Raycast(ray, out float enter))
        {
            return transform.forward;
        }

        Vector3 aimPoint = ray.GetPoint(enter);
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > aimDeadZone ? direction.normalized : transform.forward;
    }
}
