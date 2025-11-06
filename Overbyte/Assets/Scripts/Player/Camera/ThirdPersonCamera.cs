using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerInput playerInput;
    private InputAction lookAction;

    [Header("Base camera")]
    [Tooltip("X no se usa normalmente (lateral base), Y = altura del pivot")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 3f);
    [SerializeField] private float distance = 6.5f;

    [Header("Aim offset (local camera space)")]
    [Tooltip("X = lateral (derecha positivo), Y = altura extra, Z = hacia atrás (neg = acercar)")]
    [SerializeField] private Vector3 aimOffset = new Vector3(1.2f, 0.5f, -2.5f);

    [Header("Transition / feel")]
    [SerializeField][Range(1f, 30f)] private float aimTransitionSpeed = 12f; 
    [SerializeField] public float mouseRotationSpeed = 0.2f;
    [SerializeField] public float gamepadRotationSpeed = 280f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] public bool invertY = false;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cameraCollisionRadius = 0.25f;
    [SerializeField] private float collisionPushback = 0.12f;

    [Header("FOV")]
    [SerializeField] private Camera cam;
    [SerializeField] private float minFOV = 60f; 
    [SerializeField] private float maxFOV = 80f;  
    [SerializeField] private float speedForMaxFOV = 20f;
    [SerializeField] private float fovSmooth = 8f;

    private float yaw;
    private float pitch;
    private float targetSpeed;

    private bool isAiming;
    private float aimBlend = 0f;
    private float aimBlendTarget = 0f;

    private float recoilUpTarget = 0f;
    private float recoilYawTarget = 0f;
    private Vector3 recoilOffsetTarget = Vector3.zero;
    private float recoilPitch = 0f; 
    private float recoilYaw = 0f; 
    private Vector3 recoilOffset = Vector3.zero;

    [Header("Recoil smoothing")]
    [SerializeField] private float recoilSmoothIn = 20f;
    [SerializeField] private float recoilSmoothOut = 6f;

    private void Awake()
    {
        if (playerInput != null)
            lookAction = playerInput.actions["Look"];

        if (cam == null) cam = Camera.main;

        aimBlend = isAiming ? 1f : 0f; 
        aimBlendTarget = aimBlend;
    }

    private void LateUpdate()
    {
        if (GUIManager.Instance != null && (GUIManager.Instance.IsGUIOpen || GUIManager.Instance.freezeMovement))
            return;

        if (target == null)
            return;

        Vector2 lookInput = Vector2.zero;
        if (lookAction != null)
            lookInput = lookAction.ReadValue<Vector2>();

        bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero;

        if (usingMouse)
        {
            yaw += lookInput.x * mouseRotationSpeed;

            float yInput = invertY ? lookInput.y : -lookInput.y;
            pitch += yInput * mouseRotationSpeed;
        }
        else
        {
            yaw += lookInput.x * gamepadRotationSpeed * Time.deltaTime;

            float yInput = invertY ? lookInput.y : -lookInput.y;
            pitch += yInput * gamepadRotationSpeed * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        recoilPitch = Mathf.Lerp(recoilPitch, recoilUpTarget, Time.deltaTime * recoilSmoothIn);
        recoilYaw = Mathf.Lerp(recoilYaw, recoilYawTarget, Time.deltaTime * recoilSmoothIn);
        recoilOffset = Vector3.Lerp(recoilOffset, recoilOffsetTarget, Time.deltaTime * recoilSmoothIn);

        recoilUpTarget = Mathf.Lerp(recoilUpTarget, 0f, Time.deltaTime * recoilSmoothOut);
        recoilYawTarget = Mathf.Lerp(recoilYawTarget, 0f, Time.deltaTime * recoilSmoothOut);
        recoilOffsetTarget = Vector3.Lerp(recoilOffsetTarget, Vector3.zero, Time.deltaTime * recoilSmoothOut);

        float finalPitch = pitch - recoilPitch;
        float finalYaw = yaw + recoilYaw;

        finalPitch = Mathf.Clamp(finalPitch, minPitch, maxPitch);

        Vector3 pivot = target.position + Vector3.up * offset.y;
        Quaternion rotation = Quaternion.Euler(finalPitch, finalYaw, 0f);

        Vector3 normalPos = pivot - (rotation * Vector3.forward) * distance;
        Vector3 aimLocal = new Vector3(aimOffset.x, aimOffset.y, aimOffset.z);
        Vector3 aimPos = pivot + rotation * aimLocal;

        aimBlendTarget = isAiming ? 1f : 0f;
        aimBlend = Mathf.MoveTowards(aimBlend, aimBlendTarget, Time.deltaTime * aimTransitionSpeed);

        Vector3 desiredPos = Vector3.Lerp(
            pivot - (rotation * Vector3.forward) * distance,
            pivot + rotation * aimOffset,
            aimBlend
        ) + rotation * recoilOffset;

        Vector3 dir = desiredPos - pivot;
        float dist = dir.magnitude;
        if (dist > Mathf.Epsilon)
        {
            if (Physics.SphereCast(pivot, cameraCollisionRadius, dir.normalized, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivot + dir.normalized * Mathf.Max(0.1f, hit.distance - collisionPushback);
            }
        }

        transform.position = desiredPos;
        transform.rotation = rotation;

        if (cam != null)
        {
            float baseFOV = Mathf.Lerp(minFOV, maxFOV, Mathf.Clamp01(targetSpeed / speedForMaxFOV));
            float targetFOV = Mathf.Lerp(baseFOV, minFOV, aimBlend);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
        }
    }

    public void SetAiming(bool aiming) => isAiming = aiming;

    public void SetTargetSpeed(float speed) => targetSpeed = Mathf.Max(0f, speed);

    public void AddRecoil(float up, float sideways = 0f, float back = 0f)
    {
        recoilUpTarget += up;
        recoilYawTarget += Random.Range(-sideways, sideways);
        recoilOffsetTarget -= new Vector3(0f, 0f, back);
    }
}
