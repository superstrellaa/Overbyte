using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("General Settings")]
    public Transform cameraTransform;
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.5f;
    public float rotationSmoothTime = 0.1f;
    public float gravity = -9.81f;

    [Header("Jump Settings")]
    public float jumpHeight = 1.5f;
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.1f;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    [Header("Weapon Settings")]
    public Transform weaponHolder;
    public List<GameObject> weaponPrefabs;
    public List<WeaponConfigSO> weaponConfigs;
    private WeaponConfigSO currentWeaponConfig;

    private static readonly string[] weaponNames = {
        "Nothing",
        "HandGun",
        "Stinger",
        "Claw",
        "HandVulcan",
        "Ironfang",
        "Predator",
        "Executioner",
        "Bombard",
        "Vulcan"
    };

    private int currentWeaponIndex = 0;
    private GameObject currentWeaponInstance;

    [Header("Network Settings")]
    public bool simulateNetwork = false;
    public float sendInterval = 0.05f;
    [Range(0f, 1f)] public float packetLossChance = 0.0f;
    public float minLatency = 0.05f;
    public float maxLatency = 0.2f;

    public enum WeaponSlot { Nothing, LittleGun, BigGun }
    private WeaponSlot currentSlot = WeaponSlot.Nothing;
    private Dictionary<WeaponSlot, string> defaultWeapons = new Dictionary<WeaponSlot, string>
    {
        { WeaponSlot.Nothing, "Nothing" },
        { WeaponSlot.LittleGun, "HandGun" },
        { WeaponSlot.BigGun, "Predator" }
    };
    private int littleGunIndex;
    private int bigGunIndex;
    private WeaponConfigSO littleGunConfig;
    private WeaponConfigSO bigGunConfig;
    private int[] currentAmmo = new int[2];
    private bool isReloading = false;
    private float reloadDuration = 2f;
    private float reloadTimer = 0f;
    private Quaternion weaponOriginalRotation;
    private float reloadAnimSpeed = 8f;
    private bool isReloadingAnim = false;
    private float reloadAnimProgress = 0f;
    private float reloadSendInterval = 0.05f;
    private float lastReloadSendTime = 0f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private float rotationVelocity;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction aimAction;
    private InputAction shootAction;
    private InputAction nextGunGamepad;
    private InputAction prevGunGamepad;
    private InputAction changeGunMouse;
    private InputAction oneKeyboard;
    private InputAction twoKeyboard;
    private InputAction threeKeyboard;
    private InputAction reload;
    private Vector2 movementInput;
    private bool sprintInput;
    private bool isAiming;
    private float lastSentPitch = 0f;
    private const float pitchThreshold = 0.05f;
    private float lastAimingSendTime = 0f;
    public float aimingSendInterval = 0.05f;
    private bool isShooting = false;
    private float shootTimer = 0f;

    private Vector3 lastSentPosition;
    private float lastSentRotationY;
    private Vector3 lastSentVelocity;
    private float sendTimer = 0f;
    private bool wasIdle = true;
    private bool canShoot = true;

    private class PendingPacket
    {
        public float sendTime;
        public Vector3 position;
        public float rotationY;
        public Vector3 velocity;
    }
    private List<PendingPacket> pendingPackets = new List<PendingPacket>();

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        aimAction = playerInput.actions["Aim"];
        shootAction = playerInput.actions["Shoot"];
        nextGunGamepad = playerInput.actions["NextGunGamepad"];
        prevGunGamepad = playerInput.actions["PrevGunGamepad"];
        changeGunMouse = playerInput.actions["ChangeGunMouse"];
        oneKeyboard = playerInput.actions["OneKeyboard"];
        twoKeyboard = playerInput.actions["TwoKeyboard"];
        threeKeyboard = playerInput.actions["ThreeKeyboard"];
        reload = playerInput.actions["Reload"];

        moveAction.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => movementInput = Vector2.zero;

        jumpAction.performed += ctx => jumpBufferCounter = jumpBufferTime;

        sprintAction.performed += ctx => sprintInput = true;
        sprintAction.canceled += ctx => sprintInput = false;

        ConfigureAimInput();

        nextGunGamepad.performed += ctx => NextWeapon();
        prevGunGamepad.performed += ctx => PrevWeapon();

        shootAction.started += ctx => StartShooting();
        shootAction.canceled += ctx => StopShooting();

        oneKeyboard.performed += ctx => EquipSlot(WeaponSlot.Nothing);
        twoKeyboard.performed += ctx => EquipSlot(WeaponSlot.LittleGun);
        threeKeyboard.performed += ctx => EquipSlot(WeaponSlot.BigGun);

        reload.performed += ctx => TryReload();
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        aimAction.Enable();
        shootAction.Enable();
        nextGunGamepad.Enable();
        prevGunGamepad.Enable();
        changeGunMouse.Enable();
        oneKeyboard.Enable();
        twoKeyboard.Enable();
        threeKeyboard.Enable();
        reload.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        aimAction.Disable();
        shootAction.Disable();
        nextGunGamepad.Disable();
        prevGunGamepad.Disable();
        changeGunMouse.Disable();
        oneKeyboard.Disable();
        twoKeyboard.Disable();
        threeKeyboard.Disable();
        reload.Disable();
    }

    void Start()
    {
        lastSentPosition = transform.position;
        lastSentRotationY = transform.eulerAngles.y;
        lastSentVelocity = Vector3.zero;

        littleGunIndex = weaponNames.ToList().IndexOf(defaultWeapons[WeaponSlot.LittleGun]);
        bigGunIndex = weaponNames.ToList().IndexOf(defaultWeapons[WeaponSlot.BigGun]);

        littleGunConfig = weaponConfigs[littleGunIndex - 1];
        bigGunConfig = weaponConfigs[bigGunIndex - 1];

        EquipSlot(WeaponSlot.Nothing);
    }

    void Update()
    {
        if (GUIManager.Instance != null && GUIManager.Instance.IsGUIOpen)
            return;

        Vector2 mouseScroll = changeGunMouse.ReadValue<Vector2>();
        if (mouseScroll.y > 0.1f)
        {
            NextWeapon();
        }
        else if (mouseScroll.y < -0.1f)
        {
            PrevWeapon();
        }

        if (currentWeaponInstance != null)
        {
            if (isAiming && cameraTransform != null)
            {
                float targetPitch = cameraTransform.eulerAngles.x;
                if (targetPitch > 180f) targetPitch -= 360f;
                targetPitch = Mathf.Clamp(targetPitch, -45f, 45f);

                Quaternion currentRot = currentWeaponInstance.transform.localRotation;
                Quaternion targetRot = Quaternion.Euler(-targetPitch, 0f, 0f);
                currentWeaponInstance.transform.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * 10f);

                if (Mathf.Abs(targetPitch - lastSentPitch) > pitchThreshold &&
                    Time.time - lastAimingSendTime >= aimingSendInterval)
                {
                    string pitchString = targetPitch.ToString(CultureInfo.InvariantCulture);
                    NetworkManager.Instance.Send($"{{\"type\":\"aiming\",\"pitch\":{pitchString}}}");
                    lastSentPitch = targetPitch;
                    lastAimingSendTime = Time.time;
                }
            }
            else
            {
                Quaternion currentRot = currentWeaponInstance.transform.localRotation;
                Quaternion targetRot = Quaternion.identity;

                currentWeaponInstance.transform.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * 10f);

                if (lastSentPitch != 0f)
                {
                    NetworkManager.Instance.Send("{\"type\":\"aiming\",\"pitch\":0}");
                    lastSentPitch = 0f;
                }
            }
        }

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y).normalized;

        if ((GUIManager.Instance == null || !GUIManager.Instance.freezeMovement) && inputDir.magnitude >= 0.1f)
        {
            if (isAiming)
            {
                float cameraYaw = cameraTransform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

                moveDirection = (Quaternion.Euler(0f, cameraYaw, 0f) *
                                new Vector3(movementInput.x, 0f, movementInput.y)).normalized;
            }
            else if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }
        else moveDirection = Vector3.zero;

        float currentSpeed = moveSpeed * (sprintInput ? sprintMultiplier : 1f);
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        velocity.y += gravity * Time.deltaTime;
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

        if (cameraTransform != null)
        {
            ThirdPersonCamera cam = cameraTransform.GetComponent<ThirdPersonCamera>();
            if (cam != null)
            {
                if (ConfigManager.Instance.Data.dynamicFOV)
                    cam.SetTargetSpeed(horizontalVelocity.magnitude);
                else
                    cam.SetTargetSpeed(0);
                cam.SetAiming(isAiming);

                if (isAiming && currentWeaponConfig != null && currentWeaponConfig.name == "Predator")
                    HUDManager.Instance.ShowPredatorCrosshair(true);
                else
                    HUDManager.Instance.ShowPredatorCrosshair(false);

            }
        }

        if (inputDir.magnitude < 0.1f && isAiming)
        {
            float targetAngle = cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        if ((GUIManager.Instance == null || !GUIManager.Instance.freezeMovement) &&
        jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0;
        }

        if (PlayerManager.Instance.CurrentState == PlayerState.Playing)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= sendInterval)
            {
                Vector3 posDelta = transform.position - lastSentPosition;
                float rotDelta = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, lastSentRotationY));
                bool isIdle = moveDirection.sqrMagnitude < 0.01f;
                bool hasMoved = posDelta.sqrMagnitude > 0.0001f || rotDelta > 0.1f;

                if (hasMoved || isIdle != wasIdle)
                {
                    Vector3 sendVelocity = isIdle ? Vector3.zero : controller.velocity;

                    if (simulateNetwork)
                    {
                        if (Random.value > packetLossChance)
                        {
                            float delay = Random.Range(minLatency, maxLatency);
                            pendingPackets.Add(new PendingPacket
                            {
                                sendTime = Time.time + delay,
                                position = transform.position,
                                rotationY = transform.eulerAngles.y,
                                velocity = sendVelocity
                            });
                        }
                    }
                    else
                    {
                        MessageHandler.Move(transform.position, transform.eulerAngles.y, sendVelocity);
                    }

                    lastSentPosition = transform.position;
                    lastSentRotationY = transform.eulerAngles.y;
                    lastSentVelocity = sendVelocity;
                    wasIdle = isIdle;
                }

                sendTimer = 0f;
            }
        }

        if (simulateNetwork && pendingPackets.Count > 0)
        {
            for (int i = pendingPackets.Count - 1; i >= 0; i--)
            {
                if (Time.time >= pendingPackets[i].sendTime)
                {
                    MessageHandler.Move(pendingPackets[i].position, pendingPackets[i].rotationY, pendingPackets[i].velocity);
                    pendingPackets.RemoveAt(i);
                }
            }
        }

        if (currentWeaponConfig != null)
        {
            float fireInterval = 1f / currentWeaponConfig.fireRate;

            if (currentWeaponConfig.isAutomatic)
            {
                if (isShooting)
                {
                    shootTimer += Time.deltaTime;
                    if (shootTimer >= fireInterval)
                    {
                        Shoot();
                        shootTimer = 0f;
                    }
                }
                else
                {
                    shootTimer = fireInterval;
                }
            }
            else
            {
                if (isShooting && canShoot)
                {
                    Shoot();
                    canShoot = false;
                }

                if (!isShooting)
                {
                    canShoot = true;
                }
            }
        }

        if (isReloading && currentWeaponInstance != null)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadDuration)
            {
                isReloading = false;

                int slotIndex = currentSlot == WeaponSlot.LittleGun ? 0 : 1;
                currentAmmo[slotIndex] = currentWeaponConfig.magazineSize;

                HUDManager.Instance.UpdateAmmoCount(currentAmmo[slotIndex]);
            }

            if (isReloadingAnim)
            {
                reloadAnimProgress += Time.deltaTime * reloadAnimSpeed;
                float animT = Mathf.PingPong(reloadAnimProgress, 1f);
                 
                float pitch = (animT * 90f) - 45f;
                currentWeaponInstance.transform.localRotation = weaponOriginalRotation * Quaternion.Euler(pitch, 0f, 0f);

                if (Time.time - lastReloadSendTime >= reloadSendInterval)
                {
                    string pitchString = pitch.ToString(CultureInfo.InvariantCulture);
                    NetworkManager.Instance.Send($"{{\"type\":\"aiming\",\"pitch\":{pitchString}}}");
                    lastReloadSendTime = Time.time;
                }

                if (reloadTimer >= reloadDuration)
                {
                    isReloadingAnim = false;
                    currentWeaponInstance.transform.localRotation = weaponOriginalRotation;
                    NetworkManager.Instance.Send("{\"type\":\"aiming\",\"pitch\":0}");
                }
            }
        }
    }

    public void ChangeSlotWeapon(WeaponSlot slot, string newWeaponName)
    {
        if (slot == WeaponSlot.Nothing)
        {
            LogManager.Log("You cannot change weapon for the 'Nothing' slot.", LogType.Warning);
            return;
        }

        int newWeaponIndex = weaponNames.ToList().IndexOf(newWeaponName);
        if (newWeaponIndex < 0)
        {
            LogManager.Log($"Weapon '{newWeaponName}' not found in weapon names list.", LogType.Error);
            return;
        }

        WeaponConfigSO newConfig = weaponConfigs[newWeaponIndex - 1];

        switch (slot)
        {
            case WeaponSlot.LittleGun:
                littleGunIndex = newWeaponIndex;
                littleGunConfig = newConfig;
                break;

            case WeaponSlot.BigGun:
                bigGunIndex = newWeaponIndex;
                bigGunConfig = newConfig;
                break;
        }

        if (currentSlot == slot)
        {
            currentWeaponIndex = newWeaponIndex;
            currentWeaponConfig = newConfig;

            if (currentWeaponInstance != null)
                Destroy(currentWeaponInstance);
            currentWeaponInstance = Instantiate(weaponPrefabs[newWeaponIndex - 1], weaponHolder);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            HUDManager.Instance.UpdateGunName(newWeaponName);
            int slotIndex = slot == WeaponSlot.LittleGun ? 0 : 1;
            if (currentAmmo[slotIndex] == 0)
                currentAmmo[slotIndex] = currentWeaponConfig.magazineSize;
            HUDManager.Instance.UpdateAmmoCount(currentAmmo[slotIndex]);
        }

        LogManager.LogDebugOnly($"Changed weapon in slot {slot} to {newWeaponName}", LogType.Gameplay);
    }

    public void ConfigureAimInput()
    {
        aimAction.started -= OnAimStarted;
        aimAction.canceled -= OnAimCanceled;
        aimAction.performed -= OnAimPerformed; 

        if (ConfigManager.Instance.Data.toggleAim)
        {
            aimAction.started += OnAimToggle;
        }
        else
        {
            aimAction.started += OnAimStarted;
            aimAction.canceled += OnAimCanceled;
        }
    }

    private void OnAimToggle(InputAction.CallbackContext ctx)
    {
        isAiming = !isAiming;
    }

    private void OnAimStarted(InputAction.CallbackContext ctx)
    {
        isAiming = true;
    }

    private void OnAimCanceled(InputAction.CallbackContext ctx)
    {
        isAiming = false;
    }

    private void OnAimPerformed(InputAction.CallbackContext ctx) { }

    private void StartShooting() => isShooting = true;
    private void StopShooting() => isShooting = false;

    private void TryReload()
    {
        if (currentWeaponConfig == null || isReloading || currentSlot == WeaponSlot.Nothing || GUIManager.Instance.IsGUIOpen || GUIManager.Instance.freezeMovement)
            return;

        int slotIndex = currentSlot == WeaponSlot.LittleGun ? 0 : 1;

        if (currentAmmo[slotIndex] >= currentWeaponConfig.magazineSize)
            return;

        isReloading = true;
        reloadTimer = 0f;

        if (currentWeaponInstance != null)
        {
            weaponOriginalRotation = currentWeaponInstance.transform.localRotation;
            isReloadingAnim = true;
            reloadAnimProgress = 0f;
        }
    }

    private void EquipSlot(WeaponSlot slot)
    {
        currentSlot = slot;

        if (currentWeaponInstance != null)
            Destroy(currentWeaponInstance);

        if (slot == WeaponSlot.Nothing)
        {
            currentWeaponConfig = null;
            currentWeaponIndex = 0;

            HUDManager.Instance.UpdateGunName(LocalitzationManager.Instance.GetKey("hud_hand"));
            HUDManager.Instance.UpdateSelectedGun(0);
            HUDManager.Instance.UpdateAmmoCount(0);

            if (NetworkManager.Instance.IsConnected)
                NetworkManager.Instance.Send("{\"type\":\"changeGun\",\"gun\":\"Nothing\"}");
            return;
        }

        int weaponIndex = slot == WeaponSlot.LittleGun ? littleGunIndex : bigGunIndex;
        currentWeaponConfig = slot == WeaponSlot.LittleGun ? littleGunConfig : bigGunConfig;

        if (weaponIndex <= 0 || currentWeaponConfig == null)
        {
            Debug.LogError($"EquipSlot: configuración inválida para {slot}");
            return;
        }

        currentWeaponIndex = weaponIndex;
        string weaponName = weaponNames[weaponIndex];
        currentWeaponInstance = Instantiate(weaponPrefabs[weaponIndex - 1], weaponHolder);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;

        HUDManager.Instance.UpdateGunName(weaponName);
        int hudSlot = slot == WeaponSlot.LittleGun ? 1 : 2;
        HUDManager.Instance.UpdateSelectedGun(hudSlot);

        int slotIndex = slot == WeaponSlot.LittleGun ? 0 : 1;
        if (currentAmmo[slotIndex] == 0)
            currentAmmo[slotIndex] = currentWeaponConfig.magazineSize;

        HUDManager.Instance.UpdateAmmoCount(currentAmmo[slotIndex]);

        if (NetworkManager.Instance.IsConnected)
            NetworkManager.Instance.Send($"{{\"type\":\"changeGun\",\"gun\":\"{weaponName}\"}}");
    }

    public void NextWeapon()
    {
        if (GUIManager.Instance != null && (GUIManager.Instance.IsGUIOpen || GUIManager.Instance.freezeMovement))
            return;

        switch (currentSlot)
        {
            case WeaponSlot.Nothing: EquipSlot(WeaponSlot.LittleGun); break;
            case WeaponSlot.LittleGun: EquipSlot(WeaponSlot.BigGun); break;
            case WeaponSlot.BigGun: EquipSlot(WeaponSlot.Nothing); break;
        }
    }

    public void PrevWeapon()
    {
        if (GUIManager.Instance != null && (GUIManager.Instance.IsGUIOpen || GUIManager.Instance.freezeMovement))
            return;

        switch (currentSlot)
        {
            case WeaponSlot.Nothing: EquipSlot(WeaponSlot.BigGun); break;
            case WeaponSlot.LittleGun: EquipSlot(WeaponSlot.Nothing); break;
            case WeaponSlot.BigGun: EquipSlot(WeaponSlot.LittleGun); break;
        }
    }

    private void Shoot()
    {
        if (currentWeaponIndex == 0 || currentWeaponConfig == null) return;
        if (cameraTransform == null) return;
        if (GUIManager.Instance.freezeMovement) return;

        string gunName = currentWeaponConfig.weaponName;

        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;

        RaycastHit hit;
        bool hasHit = Physics.Raycast(origin, direction, out hit, 100f, ~0, QueryTriggerInteraction.Collide);

        int slotIndex = currentSlot == WeaponSlot.LittleGun ? 0 : 1;

        if (currentAmmo[slotIndex] <= 0)
        {
            TryReload();
            return;
        }

        currentAmmo[slotIndex]--;

        if (currentAmmo[slotIndex] == 0 && ConfigManager.Instance.Data.autoReload)
            TryReload();

        HUDManager.Instance.UpdateAmmoCount(currentAmmo[slotIndex]);

        if (currentWeaponInstance != null && currentWeaponConfig.muzzlePrefab != null)
        {
            Transform particleContainer = currentWeaponInstance.transform.Find("ParticleContainer");
            if (particleContainer != null)
            {
                var parent = gunName == "Bombard" ? null : particleContainer;

                var muzzleInstance = Instantiate(currentWeaponConfig.muzzlePrefab, particleContainer.position, Quaternion.identity, parent);

                if (gunName == "Bombard")
                {
                    muzzleInstance.transform.SetParent(null);

                    if (hasHit)
                    {
                        Vector3 aimDir = (hit.point - muzzleInstance.transform.position).normalized;
                        muzzleInstance.transform.rotation = Quaternion.LookRotation(aimDir);
                    }
                    else
                    {
                        muzzleInstance.transform.rotation = particleContainer.rotation;
                    }
                }
                else
                {
                    muzzleInstance.transform.localPosition = Vector3.zero;
                    muzzleInstance.transform.localRotation = Quaternion.identity;
                }

                Destroy(muzzleInstance, 2f);
            }
        }

        if (cameraTransform != null)
        {
            ThirdPersonCamera cam = cameraTransform.GetComponent<ThirdPersonCamera>();
            if (cam != null)
                cam.AddRecoil(currentWeaponConfig.recoilUp, currentWeaponConfig.recoilSide, currentWeaponConfig.recoilBack);
        }

        string message;

        if (hasHit)
        {
            string hitTag = hit.collider.tag;

            if (hitTag == "Server/RemoteOtherPlayer")
            {
                string hitUuid = hit.collider.GetComponentInParent<PlayerNetworkId>()?.Uuid ?? "unknown";
                message = string.Format(CultureInfo.InvariantCulture,
                    "{{\"type\":\"shoot\",\"gun\":\"{10}\",\"origin\":{{\"x\":{0},\"y\":{1},\"z\":{2}}},\"hit\":\"player\",\"hitUuid\":\"{3}\",\"hitPoint\":{{\"x\":{4},\"y\":{5},\"z\":{6}}},\"hitNormal\":{{\"x\":{7},\"y\":{8},\"z\":{9}}}}}",
                    origin.x, origin.y, origin.z,
                    hitUuid,
                    hit.point.x, hit.point.y, hit.point.z,
                    hit.normal.x, hit.normal.y, hit.normal.z,
                    gunName);
            }
            else
            {
                if (currentWeaponConfig.bulletHolePrefab != null)
                {
                    StartCoroutine(SpawnBulletHole(hit.point, hit.normal, currentWeaponConfig.bulletHoleDelay));
                }

                message = string.Format(CultureInfo.InvariantCulture,
                    "{{\"type\":\"shoot\",\"gun\":\"{9}\",\"origin\":{{\"x\":{0},\"y\":{1},\"z\":{2}}},\"hit\":\"wall\",\"hitPoint\":{{\"x\":{3},\"y\":{4},\"z\":{5}}},\"hitNormal\":{{\"x\":{6},\"y\":{7},\"z\":{8}}}}}",
                    origin.x, origin.y, origin.z,
                    hit.point.x, hit.point.y, hit.point.z,
                    hit.normal.x, hit.normal.y, hit.normal.z,
                    gunName);
            }
        }
        else
        {
            message = string.Format(CultureInfo.InvariantCulture,
                "{{\"type\":\"shoot\",\"gun\":\"{3}\",\"origin\":{{\"x\":{0},\"y\":{1},\"z\":{2}}},\"hit\":\"none\"}}",
                origin.x, origin.y, origin.z,
                gunName);
        }

        NetworkManager.Instance.Send(message);
    }

    private IEnumerator SpawnBulletHole(Vector3 position, Vector3 normal, float delay)
    {
        yield return new WaitForSeconds(delay);
        Quaternion rotation = Quaternion.LookRotation(normal);
        Instantiate(currentWeaponConfig.bulletHolePrefab, position + normal * 0.001f, rotation);
    }
}
