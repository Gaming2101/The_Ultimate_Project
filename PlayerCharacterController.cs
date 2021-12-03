using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Windows.Input;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
public class PlayerCharacterController : MonoBehaviour
{
    public static PlayerCharacterController Me;
    public Animator Ani;
    public CharacterStatus MyEffects;
    public PlayerAbilityBar DashAbility;
    public bool stopRotate;
    public GrapplingGun m_grapplingGun;
    public GameObject TeleportingVFX;
    public Transform TeleportingPos;
    public Transform Teleporter;
    public TeleportationBeacon TeleportingBeaconPref;
    public int teleportationRange = 50;
    public int maxBeaconCount;
    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    public Camera playerCamera;
    [Tooltip("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;
    public GameObject UI;
    public bool isLookingToTeleport;
    public bool isLookingToTeleportBeacon;
    public List<TeleportationBeacon> TeleportingBeacons = new List<TeleportationBeacon>();

    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    public float gravityDownForce = 20f;
    [Tooltip("Physic layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
    public float groundCheckDistance = 0.05f;

    [Header("Movement")]
    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;
    [Tooltip("Max movement speed when crouching")]
    [Range(0,1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    public float sprintSpeedModifiere = 2f;
    [Tooltip("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    public float rotationSpeed = 200f;
    [Range(0.1f, 1f)]
    [Tooltip("Rotation speed multiplier when aiming")]
    public float aimingRotationMultiplier = 0.4f;

    [Header("Jump")]

    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    public float cameraHeightRatio = 0.9f;
    [Tooltip("Height of character when standing")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip("Height of character when crouching")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip("Speed of crouching transitions")]
    public float crouchingSharpness = 10f;

    [Header("Audio")]
    [Tooltip("Amount of footstep sounds played when moving one meter")]
    public float footstepSFXFrequency = 1f;
    [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
    public float footstepSFXFrequencyWhileSprinting = 1f;
    [Tooltip("Sound played for footsteps")]
    public AudioClip footstepSFX;
    [Tooltip("Sound played when jumping")]
    public AudioClip jumpSFX;
    [Tooltip("Sound played when landing")]
    public AudioClip landSFX;
    [Tooltip("Sound played when taking damage froma fall")]
    public AudioClip fallDamageSFX;
    public AudioClip dashSFX;

    [Header("Fall Damage")]
    [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
    public bool recievesFallDamage;
    [Tooltip("Minimun fall speed for recieving fall damage")]
    public float minSpeedForFallDamage = 10f;
    [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
    public float maxSpeedForFallDamage = 30f;
    [Tooltip("Damage recieved when falling at the mimimum speed")]
    public float fallDamageAtMinSpeed = 10f;
    [Tooltip("Damage recieved when falling at the maximum speed")]
    public float fallDamageAtMaxSpeed = 50f;

    public UnityAction<bool> onStanceChanged;

    [Header("Dashing")]
    public bool CanDash;
    public float DashDistance = 4;
    public float DashTime = 1f;
    public float DashPercentage;
    public bool Dashing;
    public bool DashRecharging;
    public float DashWait;

    [Header("Teleportation")]
    public float MaxTeleportationCooldown = 100;
    public float TeleportationCooldownRate = 0.1f;
    public float TeleportationCooldown;
    public bool TeleportationRecharging;

    public float DashActiveValue;
    public float CurrentDashTimer;
    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; private set; }
    public bool hasJumpedThisFrame { get; private set; }
    public bool isDead { get; private set; }
    public bool isCrouching;
    public float RotationMultiplier
    {
        get
        {
            if (m_WeaponsManager.isAiming)
            {
                return aimingRotationMultiplier;
            }

            return 1f;
        }
    }

    public Health m_Health;
    public PlayerInputHandler m_InputHandler;
    public CharacterController m_Controller;
    public Rigidbody m_rigidBody;
    public PlayerWeaponsManager m_WeaponsManager;
    public Actor m_Actor;
    Vector3 m_GroundNormal;
    Vector3 m_CharacterVelocity;
    Vector3 m_LatestImpactSpeed;
    float m_LastTimeJumped = 0f;
    public float m_CameraVerticalAngle = 0f;
    float m_footstepDistanceCounter;
    public float m_TargetCharacterHeight;

    const float k_JumpGroundingPreventionTime = 0.2f;
    const float k_GroundCheckDistanceInAir = 0.07f;

    [Header("Original Stats")]
    public float OriginalHealth;
    public float OriginalmaxSpeedOnGround;
    public float OriginaljumpForce;
    public float OriginalmaxSpeedInAir;
    public float OriginalDashRechargeRate;
    public float OriginalNumberOfDashes;

    [Header("Stats")]
    public float Health = 0;
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    public float maxSpeedOnGround = 10f;
    [Tooltip("Force applied upward when jumping")]
    public float jumpForce = 9f;
    [Tooltip("Max movement speed when not grounded")]
    public float maxSpeedInAir = 10f;
    public float DashRechargeRate = 0.01f;
    public float NumberOfDashes = 1;

    void Start()
    {
        Me = this;
        MyEffects = GetComponent<CharacterStatus>();
        CanDash = true;
        DashWait = NumberOfDashes;
        Ani = GetComponentInChildren<Animator>();
        m_grapplingGun = GetComponentInChildren<GrapplingGun>();
        m_rigidBody = GetComponent<Rigidbody>();
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler, this, gameObject);

        m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(m_WeaponsManager, this, gameObject);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

        m_Controller.enableOverlapRecovery = true;

        m_Health.onDie += OnDie;

        TeleportationCooldown = MaxTeleportationCooldown;

        // force the crouch state to false when starting
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);

        foreach (PlayerHealthBar H in FindObjectsOfType<PlayerHealthBar>())
        {
            foreach (TextMeshProUGUI T in H.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (T.name == "Wave")
                {
                    T.text = "";
                }
                if (T.name == "Enemy Number")
                {
                    T.text = "";
                }
                if (T.name == "Wave T")
                {
                    T.text = "";
                }
                if (T.name == "Enemy Number T")
                {
                    T.text = "";
                }
            }
        }

        foreach (PlayerAbilityBar b in FindObjectsOfType<PlayerAbilityBar>())
            if (b.name == "Dashing")
                DashAbility = b;

        Health = m_Health.maxHealth;
        OriginalHealth = Health;
        OriginalmaxSpeedOnGround = maxSpeedOnGround;
        OriginaljumpForce = jumpForce;
        OriginalmaxSpeedInAir = maxSpeedInAir;
        OriginalDashRechargeRate = DashRechargeRate;
        OriginalNumberOfDashes = NumberOfDashes;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            m_Health.Kill(gameObject);

        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        float groundSpeed = OriginalmaxSpeedOnGround;
        float airSpeed = OriginalmaxSpeedInAir;
        if (SuitUpgrades.Me)
            foreach (MySuitUpgrades U in SuitUpgrades.Me.Upgrades)
            {
                if (U.Name == "Increased Health" & U.Level > 0)
                    m_Health.maxHealth = OriginalHealth + (OriginalHealth / 100 * U.Levels[U.Level - 1].TotalAmount);
                if (U.Name == "Increased Movement Speed" & U.Level > 0)
                    groundSpeed = OriginalmaxSpeedOnGround + (OriginalmaxSpeedOnGround / 100 * U.Levels[U.Level - 1].TotalAmount);
                if (U.Name == "Increased Jump Height" & U.Level > 0)
                    jumpForce = OriginaljumpForce + (OriginaljumpForce / 100 * U.Levels[U.Level - 1].TotalAmount);
                if (U.Name == "Increased Air Movement" & U.Level > 0)
                    airSpeed = OriginalmaxSpeedInAir + (OriginalmaxSpeedInAir / 100 * U.Levels[U.Level - 1].TotalAmount);
                if (U.Name == "Increased Dash Recharge Rate" & U.Level > 0)
                    DashRechargeRate = OriginalDashRechargeRate + (OriginalDashRechargeRate / 100 * U.Levels[U.Level - 1].TotalAmount);
                if (U.Name == "Increased Number Of Dashes" & U.Level > 0)
                    NumberOfDashes = OriginalNumberOfDashes + (OriginalNumberOfDashes / 100 * U.Levels[U.Level - 1].TotalAmount);
            }

        maxSpeedOnGround = groundSpeed + MyEffects.EffectedSpeed;
        maxSpeedInAir = airSpeed + MyEffects.EffectedSpeed;

        // check for Y kill
        if (!isDead && transform.position.y < killHeight)
        {
            if (m_Health.SpawnParent)
            {
                recievesFallDamage = false;
                int PickSpawn = UnityEngine.Random.Range(0, m_Health.Spawns.Count);
                GetComponent<CharacterController>().enabled = false;
                transform.position = m_Health.Spawns[PickSpawn].position;
                StartCoroutine(AfterFallOff());
                GetComponent<CharacterController>().enabled = true;
                float RightAmount = m_Health.currentHealth / 100 * 40;
                GetComponentInChildren<Damageable>().InflictDamage(RightAmount, false, "", gameObject);
            }
            else
            {
                GetComponentInChildren<Health>().Kill(null);
            }
        }

        if (m_grapplingGun.IsGrappling())
        {
            if (Vector3.Distance(transform.position, m_grapplingGun.GetGrapplePoint()) > 1)
            {
                Transform NewLook = new GameObject().transform;
                NewLook.position = transform.position;
                NewLook.SetParent(transform.parent);
                NewLook.LookAt(m_grapplingGun.GetGrapplePoint());
                Me.transform.localEulerAngles = Vector3.Slerp(transform.localEulerAngles, new Vector3(0, NewLook.eulerAngles.y, 0), 0.8f);
                //   Me.m_CameraVerticalAngle = NewLook.localEulerAngles.x;
                //    Me.m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
                //  playerCamera.transform.localEulerAngles = Vector3.Slerp(playerCamera.transform.localEulerAngles, new Vector3(PlayerCharacterController.Me.m_CameraVerticalAngle, 0, 0), 0.4f);
                Destroy(NewLook.gameObject);
            }
        }

        foreach (Ability a in PlayerAbilities.Me.Abilities)
        {
            if (a.Name == "Teleportation")
            {
                if (a.CurrentVersion.Name == "Teleportation Beacon")
                {
                    if (a.CurrentVersion.Level == 0)
                        maxBeaconCount = 0;
                    if (a.CurrentVersion.Level == 1)
                        maxBeaconCount = 1;
                    if (a.CurrentVersion.Level == 2)
                        maxBeaconCount = 2;
                    if (a.CurrentVersion.Level == 3)
                        maxBeaconCount = 3;
                    if (a.CurrentVersion.Level == 4)
                        maxBeaconCount = 4;
                }
            }
        }

        if (GameManager.Me.CurrentPlayer.Active & TeleportationCooldown == MaxTeleportationCooldown)
            foreach (Ability a in PlayerAbilities.Me.Abilities)
            {
                if (a.Name == "Teleportation")
                {
                    if (a.CurrentVersion.Name == "Teleportation")
                    {
                        isLookingToTeleportBeacon = false;
                        foreach (TeleportationBeacon b in TeleportingBeacons)
                        {
                            Destroy(b.gameObject);
                            TeleportingBeacons.Remove(b);
                            return;
                        }
                        if (Input.GetButtonDown("Teleport"))
                        {
                            if (!isLookingToTeleport)
                                isLookingToTeleport = true;
                            else
                            {
                                StartCoroutine(WaitTeleport(Teleporter.position, 100));
                                isLookingToTeleport = false;
                            }
                        }
                        if (Input.GetButtonDown("Remove Teleporter"))
                        {
                            if (isLookingToTeleport)
                                isLookingToTeleport = false;
                        }
                    }
                    if (a.CurrentVersion.Name == "Teleportation Beacon")
                    {
                        isLookingToTeleport = false;
                        bool pe = true;
                        if (isLookingToTeleportBeacon)
                        {
                            if (Input.GetButtonDown("Teleport"))
                            {
                                if (isLookingToTeleportBeacon)
                                {
                                    if (GameManager.Me.CurrentCamera.CurrentBeacon)
                                    {
                                        isLookingToTeleportBeacon = false;
                                        pe = false;
                                        StartCoroutine(WaitTeleport(GameManager.Me.CurrentCamera.CurrentBeacon.transform.position, 100));
                                        Destroy(GameManager.Me.CurrentCamera.CurrentBeacon.gameObject);
                                        TeleportingBeacons.Remove(GameManager.Me.CurrentCamera.CurrentBeacon);
                                    }
                                    else
                                    {
                                        if (TeleportingBeacons.Count < maxBeaconCount)
                                        {
                                            if (!GameManager.Me.CurrentCamera.TPBlocker)
                                            {
                                                isLookingToTeleportBeacon = false;
                                                pe = false;
                                                TeleportationBeacon newBeacon = Instantiate(TeleportingBeaconPref);
                                                newBeacon.transform.SetParent(FPSPlayerCamera.Me.transform);
                                                newBeacon.transform.position = FPSPlayerCamera.Me.transform.position;
                                                newBeacon.transform.localEulerAngles = new Vector3(0, 0, 0);
                                                newBeacon.transform.Translate(Vector3.down * 1);
                                                newBeacon.transform.Translate(Vector3.forward * 2);
                                                newBeacon.GetComponent<Rigidbody>().AddForce(transform.forward * 1300);
                                                newBeacon.transform.SetParent(null);
                                                TeleportingBeacons.Add(newBeacon);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!isLookingToTeleportBeacon & pe)
                        {
                            if (Input.GetButtonDown("Teleport"))
                            {
                                isLookingToTeleportBeacon = true;
                            }
                        }
                        if (Input.GetButtonDown("Remove Teleporter"))
                        {
                            isLookingToTeleportBeacon = false;
                            if (GameManager.Me.CurrentCamera.CurrentBeacon)
                            {
                                Destroy(GameManager.Me.CurrentCamera.CurrentBeacon.gameObject);
                                TeleportingBeacons.Remove(GameManager.Me.CurrentCamera.CurrentBeacon);
                            }
                        }
                    }
                }
            }

        if (TeleportationCooldown >= MaxTeleportationCooldown)
            TeleportationRecharging = false;
        if (!TeleportationRecharging & TeleportationCooldown < MaxTeleportationCooldown)
        {
            TeleportationRecharging = true;
            StartCoroutine(RechargeTeleport());
        }

        if (isLookingToTeleport)
        {
            if(GameManager.Me.CurrentCamera.CurrentTPOP != new Vector3())
            {
                Teleporter.position = GameManager.Me.CurrentCamera.CurrentTPOP;
                Teleporter.eulerAngles = new Vector3(0, 0, 0);
             //   Teleporter.eulerAngles = GameManager.Me.CurrentCamera.CurrentTPOR;
            }
            else
            {
                Teleporter.position = TeleportingPos.position;
                Teleporter.eulerAngles = new Vector3(0, 0, 0);
            }
        }
        Teleporter.gameObject.SetActive(isLookingToTeleport);

        hasJumpedThisFrame = false;
        DashPercentage = DashWait / NumberOfDashes * 100;
        if (DashWait < NumberOfDashes & !DashRecharging)
        {
            StartCoroutine(RechargeDash());
        }

        bool NewCanDash = false;
        if (DashPercentage == 100f & NumberOfDashes == 1)
        {
            NewCanDash = true;
        }
        if (DashPercentage >= 50f & NumberOfDashes == 2)
        {
            NewCanDash = true;
        }
        if (DashPercentage >= 25f & NumberOfDashes == 3)
        {
            NewCanDash = true;
        }
        if (DashPercentage >= 12.5f & NumberOfDashes == 4)
        {
            NewCanDash = true;
        }



        if (NumberOfDashes == 1)
            DashActiveValue = 1f;
        if (NumberOfDashes == 2)
            DashActiveValue = 0.5f;
        if (NumberOfDashes == 3)
            DashActiveValue = 0.25f;
        if (NumberOfDashes == 4)
            DashActiveValue = 0.125f;

        DashAbility.fillBarColorChange.emptyValue = DashActiveValue;
        CanDash = NewCanDash;

        if (DashWait > NumberOfDashes)
        {
            DashWait = NumberOfDashes;
        }

        bool wasGrounded = isGrounded;
        GroundCheck();

        // landing
        if (isGrounded && !wasGrounded)
        {
            // Fall damage
            float fallSpeed = -Mathf.Min(characterVelocity.y, m_LatestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - minSpeedForFallDamage) / (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall = Mathf.Lerp(fallDamageAtMinSpeed, fallDamageAtMaxSpeed, fallSpeedRatio);
                m_Health.TakeDamage(dmgFromFall, null);

                // fall damage SFX
                audioSource.PlayOneShot(fallDamageSFX);
            }
            else
            {
                // land SFX
                audioSource.PlayOneShot(landSFX);
            }
        }

        // crouching
        if (m_InputHandler.GetCrouchInputDown())
        {
            SetCrouchingState(!isCrouching, false);
        }

        UpdateCharacterHeight(false);

        HandleCharacterMovement();
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (m_grapplingGun.IsGrappling())
        {
            if (m_grapplingGun.GrappleObject.transform == collision.transform)
                m_grapplingGun.UseObject = true;
            foreach (Transform t in collision.transform.GetComponentsInParent<Transform>())
                if (m_grapplingGun.GrappleObject.transform == t)
                    m_grapplingGun.UseObject = true;
        }
    }

    public IEnumerator WaitTeleport(Vector3 Pos, float cost)
    {
        if (TeleportingVFX)
        {
            GameObject vfx = Instantiate(TeleportingVFX, Pos, new Quaternion(), GameManager.Me.EffectsParent);
        }
        yield return new WaitForSeconds(0.2f);
        FindObjectOfType<FeedbackFlashHUD>().OnPlayerTeleport();
        yield return new WaitForSeconds(0.2f);
        transform.position = Pos;
        TeleportationCooldown -= cost;
    }

    public IEnumerator AfterFallOff()
    {
        float Reset = GetComponentInChildren<Damageable>().sensibilityToSelfdamage;
        GetComponentInChildren<Damageable>().sensibilityToSelfdamage = 1;
        yield return new WaitForSeconds(1f);
        recievesFallDamage = true;
        GetComponentInChildren<Damageable>().sensibilityToSelfdamage = Reset;
    }

    public IEnumerator Dash()
    {
        CanDash = false;
        Dashing = true;
      //  DashWait -= 0.5f;
        if (DashWait <= 0)
            DashWait = 0;
        CurrentDashTimer += 0.1f;
        if(CurrentDashTimer >= DashTime)
        {
            Dashing = false;
            CurrentDashTimer = 0;
        }
        else
        {
            yield return new WaitForSeconds(0.01f);
            StartCoroutine(Dash());
        }
    }
    public IEnumerator RechargeDash()
    {
        DashRecharging = true;
        if (DashWait < NumberOfDashes)
        {
            yield return new WaitForSeconds(0.01f);
            DashWait += DashRechargeRate;
            StartCoroutine(RechargeDash());
        }
        else
        {
            DashRecharging = false;
        }
    }
    public IEnumerator RechargeTeleport()
    {
        yield return new WaitForSeconds(TeleportationCooldownRate);
        if (TeleportationCooldown < MaxTeleportationCooldown)
        {
            TeleportationCooldown += 1f;
            StartCoroutine(RechargeTeleport());
        }
    }
    void OnDie()
    {
        isDead = true;
        FindObjectOfType<FeedbackFlashHUD>().OnDie();
        StartCoroutine(DeathWait());
        Ani.Play("Die");
    }
    public IEnumerator DeathWait()
    {
        yield return new WaitForSeconds(1f);
       // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        m_GroundNormal = Vector3.up;

        if (!FPSPlayerCamera.Me.DoingFatality & !FPSPlayerCamera.Me.AfterFatality)
        {
            // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
                {
                    // storing the upward direction for the surface found
                    m_GroundNormal = hit.normal;

                    // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                    // and if the slope angle is lower than the character controller's limit
                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        isGrounded = true;

                        // handle snapping to the ground
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }
    }

    public void HandleCharacterMovement()
    {
        // horizontal character rotation
        if (!stopRotate)
        {
            // rotate the transform with the input speed around its local Y axis
            transform.Rotate(new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * rotationSpeed * RotationMultiplier), 0f), Space.Self);

            // vertical camera rotation
            {
                // add vertical inputs to the camera's vertical angle
                m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * rotationSpeed * RotationMultiplier;

                // limit the camera's vertical angle to min/max
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

                // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
                playerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            }
        }

        bool grapple = false;
        if (GetComponentInChildren<GrapplingGun>().IsGrappling())
        {
            if (!GetComponentInChildren<GrapplingGun>().CanMove)
                grapple = true;
        }

        if (!GetComponent<PlayerController>().isZiplining & !FPSPlayerCamera.Me.DoingFatality & !GetComponent<PlayerController>().isSitting)
        {
            // character movement handling
            Vector3 Move = m_InputHandler.GetMoveInput();
            bool dp = false;
            foreach (Ability a in PlayerAbilities.Me.Abilities)
            {
                if (a.Name == "Dashing")
                    dp = a.CurrentVersion.Name == "Dashing";
            }

            if (m_InputHandler.GetSprintInputDown() & !Dashing & CanDash & dp)
            {
                Dashing = true;
                DashWait -= 1f;
                StartCoroutine(Dash());
            }

            bool isSprinting = Dashing;
            {
                if (isSprinting)
                {
                    isSprinting = SetCrouchingState(false, false);
                    if (!FindObjectOfType<FeedbackFlashHUD>().m_Dash)
                        FindObjectOfType<FeedbackFlashHUD>().OnDash();
                    audioSource.PlayOneShot(dashSFX);
                    if (Move.x > 0)
                    {
                        Move = new Vector3(DashDistance, Move.y, Move.z);
                    }
                    if (Move.x < 0)
                    {
                        Move = new Vector3(-DashDistance, Move.y, Move.z);
                    }
                    if (Move.z > 0)
                    {
                        Move = new Vector3(Move.x, Move.y, DashDistance);
                    }
                    if (Move.z < 0)
                    {
                        Move = new Vector3(Move.x, Move.y, -DashDistance);
                    }
                    if (Move == new Vector3(0, 0, 0))
                    {
                        Move = new Vector3(0, 0, DashDistance);
                    }
                }

                float speedModifier = Dashing ? DashDistance : 1f;
                float RightSprint = speedModifier;

                Vector3 worldspaceMoveInput = transform.TransformVector(Move);

                // converts move input to a worldspace vector based on our character's transform orientation

                // handle grounded movement
                if (isGrounded)
                {
                    RightSprint = speedModifier;
                    Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;
                    characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);
                    if (isCrouching)
                        targetVelocity *= maxSpeedCrouchedRatio;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                    // jumping
                    if (isGrounded && m_InputHandler.GetJumpInputDown())
                    {
                        // force the crouch state to false
                        if (SetCrouchingState(false, false))
                        {
                            // start by canceling out the vertical component of our velocity
                            characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                            // then, add the jumpSpeed value upwards
                            characterVelocity += Vector3.up * jumpForce;

                            // play sound
                            audioSource.PlayOneShot(jumpSFX);

                            // remember last time we jumped because we need to prevent snapping to ground for a short time
                            m_LastTimeJumped = Time.time;
                            hasJumpedThisFrame = true;

                            // Force grounding to false
                            isGrounded = false;
                            m_GroundNormal = Vector3.up;
                        }
                    }

                    // footsteps sound
                    float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
                    if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency)
                    {
                        m_footstepDistanceCounter = 0f;
                        audioSource.PlayOneShot(footstepSFX);
                    }

                    // keep track of distance traveled for footsteps sound
                    m_footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
                }
                // handle air movement
                else
                {
                    RightSprint = speedModifier * 4;
                    characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime * RightSprint;
                    float CurrentS = maxSpeedInAir;
                    if (Dashing)
                        CurrentS = 150;

                    float verticalVelocity = characterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, CurrentS);
                    characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    // apply the gravity to the velocity
                    if (!grapple)
                        characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
                }
                bool P = false;
                if (P)
                {
                    //  characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);
                    characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

                    // limit air speed to a maximum, but only horizontally
                    float verticalVelocity = characterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
                    characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
                }
            }

            // apply the final calculated velocity value as a character movement
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(characterVelocity * Time.deltaTime);

            // detect obstructions to adjust velocity accordingly
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore))
            {
                // We remember the last impact speed because the fall damage logic might need it
                m_LatestImpactSpeed = characterVelocity;

                characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
            }
        }
    }

    public void ResetMobility()
    {
        characterVelocity = new Vector3();
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    public void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        if (force)
        {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
           // m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
          //  m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        if (GetComponent<PlayerController>().isSitting)
        {
            m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
        }
    }

    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (GetComponent<PlayerController>().isSitting)
            return false;
        // set appropriate heights
        if (crouched)
        {
            m_TargetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(capsuleHeightStanding),
                    m_Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != m_Controller)
                    {
                        return false;
                    }
                }
            }

            m_TargetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null)
        {
            onStanceChanged.Invoke(crouched);
        }

        isCrouching = crouched;
        return true;
    }

    
}
