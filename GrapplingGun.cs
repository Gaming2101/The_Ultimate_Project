using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrapplingGun : MonoBehaviour
{
    public enum MyType { None, Player, Mech }
    public MyType Type;
    public bool Active;
    public bool UseObject;
    public static GrapplingGun Me;
    public List<Text> Prompts = new List<Text>();
    public Animator Ani;
    private LineRenderer lr;
    public AudioSource audioSource;
    public AudioClip grappleSFX;
    public AudioClip impactSFX;
    public AudioClip uiSFX;
    public AudioClip grapplingSFX;
    public ParticleSystem shootVFX;
    public ParticleSystem hitVFX;
    public Transform grapplingIcon;
    private Transform grapplePoint;
    public Transform MultiGrappleIconPref;
    public Vector3 originalIconScale;

    public Color StartingCol;
    public Color EnemyCol;
    public Color HealthCol;
    public Color WeaponCol;
    public Color ZiplineCol;
    public Color VehicleCol;

    public bool waitingGrapple;
    public bool isFireing;
    public LayerMask whatIsGrappleable;
    public Transform gunTip, camera, owner;
    public bool CanGrapple = true;
    public float Cooldown = 3;
    public float DechargeRate = 0.1f;
    public float TetherDechargeAmount = 2f;
    public float RechargeDelay = 3;
    public float RechargeRate = 0.1f;
    public int MaxMultiGrappleObjects = 2;
    public float MaxDistance = 200f;
    public float MinDistance = 0.18f;
    public float Spring = 4.5f;
    public float Damper = 0f;
    public float MassScale = 4.5f;
    private SpringJoint joint;
    public Color NormalCol;

    public bool IsDecharging;
    public bool IsWaitingRecharge;
    public bool IsRecharging;
    public float TetherDechargeProgress;
    public bool IsTetherDecharging;
    public bool HasGrappled;
    public bool isStarting;
    public float PressTime;
    public int Presses;
    public bool KnockDown;
    public bool Melee;

    public Transform LookingGrappleObject;
    public Transform GrappleObject;
    public GrapplingGun EnemyGrapplG;
    public float CurrentCooldown;
    public List<RaycastHit> MultiGrappleObjects = new List<RaycastHit>();
    public List<Transform> LookingMultiGrappleObjects = new List<Transform>();
    public List<Transform> LookingMultiGrappleObjectIcons = new List<Transform>();
    public bool LookingForMultiGrappleTargets;
    public bool PreviousLookingForMultiGrappleTargets;
    public bool GrappleDown;
    public bool CanMove;

    void Awake()
    {
        if (owner.GetComponent<PlayerCharacterController>())
            Type = MyType.Player;
        if (owner.GetComponent<MechController>())
            Type = MyType.Mech;

        if (Type == MyType.Player)
            Me = this;
        lr = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();
        CurrentCooldown = Cooldown;
        if (grapplingIcon)
        {
            originalIconScale = grapplingIcon.transform.localScale;
            NormalCol = grapplingIcon.GetComponentInChildren<SpriteRenderer>().color;
        }

        Prompts.Clear();
        foreach (Text t in FindObjectOfType<GrappleIconManager>().Icon.transform.parent.GetComponentsInChildren<Text>())
        {
            if (t.transform.parent.name == "Prompts")
                Prompts.Add(t);
        }
    }

    void Update()
    {
        if (Presses == 0)
            PressTime = 0;
        if (IsGrappling() & !grapplingIcon)
            StopGrapple();
        bool useA = false;
        if (Type == MyType.Player)
        {
            if (PlayerAbilities.Me.CurrentAbility.Name != "")
            {
                if (PlayerAbilities.Me.CurrentAbility.UseGrapplingHook)
                    useA = true;
            }
            else
            {
                useA = true;
            }
        }
        if (Type == MyType.Mech)
        {
            useA = true;
        }

        bool canTether = false;
        foreach (Ability a in PlayerAbilities.Me.Abilities)
        {
            if (a.Name == "Grappling Hook Tether")
                foreach (AbilityVersion v in a.Versions)
                {
                    if (v.Name == "Tether objects together")
                    {
                        if (v.Level > 0)
                            canTether = true;
                        if (v.Level == 1)
                            MaxMultiGrappleObjects = 3;
                        if (v.Level == 2)
                            MaxMultiGrappleObjects = 4;
                        if (v.Level == 3)
                            MaxMultiGrappleObjects = 5;
                    }
                }
        }

        if (ScannerUI.Me)
        {
            if(Active & !ScannerUI.Me.Scanning & !ItemWheelManager.Me.Open & !GameManager.Me.CurrentCamera.CurrentInventory)
            {
                if (Input.GetButtonDown("Release Tethers"))
                    foreach (TetherScript t in FindObjectsOfType<TetherScript>())
                        if (t.GrappleTethered)
                            t.Stop();
            }

            GrappleDown = Input.GetButton("Grapple");

            if (Type == MyType.Player)
                Active = !IsGrappling() & PlayerCharacterController.Me.GetComponent<PlayerController>().Active & !GameManager.Me.CurrentPlayer.DrivingScript.CurrentVehicle & !GameManager.Me.CurrentPlayer.CurrentMech;
            if (Type == MyType.Mech)
                Active = !IsGrappling() & owner.GetComponent<MechController>().Pilot == GameManager.Me.CurrentPlayer.transform;

            if (Active & CanGrapple & !FPSPlayerCamera.Me.DoingFatality & useA & !ScannerUI.Me.Scanning & !ItemWheelManager.Me.Open & !GameManager.Me.CurrentCamera.CurrentInventory)
            {
                DetectPoint();
                if (canTether)
                {
                    if (Input.GetButtonDown("Grapple"))
                    {
                        if (TetherableObjects().Count > 1)
                        {
                            Presses += 1;
                            if (Presses == 1)
                            {
                                StartCoroutine(WaitPress());
                            }
                            if (Presses == 2)
                            {
                                MultiGrapple();
                            }
                        }
                        else
                        {
                            Presses = 0;
                            if (Input.GetButtonDown("Grapple"))
                            {
                                StartGrapple();
                            }
                        }
                    }
                }
                else
                {
                    Presses = 0;
                    if (Input.GetButtonDown("Grapple"))
                    {
                        StartGrapple();
                    }
                }
            }
            else
            {
                Presses = 0;
                LookingGrappleObject = null;
                LookingForMultiGrappleTargets = false;
                if (!GrappleDown & IsGrappling())
                    StopGrapple();
            }
            if (Type == MyType.Player)
                if (FPSPlayerCamera.Me.DoingFatality)
                {
                    StopGrapple();
                }
        }

        if (TetherDechargeProgress > 0 & !IsTetherDecharging)
        {
            IsWaitingRecharge = false;
            IsRecharging = false;
            IsTetherDecharging = true;
            StartCoroutine(TetherDecharge());
        }

        if (LookingForMultiGrappleTargets & TetherableObjects().Count < 2)
            LookingForMultiGrappleTargets = false;
        if (FPSPlayerCamera.Me.DoingFatality)
            LookingForMultiGrappleTargets = false;
        if (LookingForMultiGrappleTargets)
        {
            if (Type == MyType.Player || Type == MyType.Mech)
                if (FindObjectOfType<FeedbackFlashHUD>())
                    if (!FindObjectOfType<FeedbackFlashHUD>().m_LookTether)
                        FindObjectOfType<FeedbackFlashHUD>().OnLookTether();
            Time.timeScale = 0.05f;
            foreach (Transform t in TetherableObjects())
            {
                if (t)
                    if (!LookingMultiGrappleObjects.Contains(t.transform))
                    {
                        if (t.GetComponent<WeaponPickup>())
                        {
                            LookingMultiGrappleObjects.Add(t.transform);
                            Transform i = Instantiate(MultiGrappleIconPref);
                            i.SetParent(t.transform);
                            i.position = t.transform.position;
                            if (t.transform.GetComponent<AIBehaviour>())
                                i.position = new Vector3(i.position.x, i.position.y + 3, i.position.z);
                            i.gameObject.SetActive(true);
                            i.name = MultiGrappleIconPref.name;
                            i.GetComponent<Animator>().Play("Start");
                            LookingMultiGrappleObjectIcons.Add(i);
                        }
                        if (t.GetComponent<Destructable>())
                        {
                            if (t.GetComponent<Destructable>().type == Destructable.myType.barrel)
                            {
                                LookingMultiGrappleObjects.Add(t.transform);
                                Transform i = Instantiate(MultiGrappleIconPref);
                                i.SetParent(t.transform);
                                i.position = t.transform.position;
                                if (t.transform.GetComponent<AIBehaviour>())
                                    i.position = new Vector3(i.position.x, i.position.y + 3, i.position.z);
                                i.gameObject.SetActive(true);
                                i.name = MultiGrappleIconPref.name;
                                i.GetComponent<Animator>().Play("Start");
                                LookingMultiGrappleObjectIcons.Add(i);
                            }
                        }
                        if (t.GetComponent<AIBehaviour>())
                        {
                            if (!t.GetComponent<AIBehaviour>().v_dead)
                                if (!LookingMultiGrappleObjects.Contains(t.transform))
                                {
                                    LookingMultiGrappleObjects.Add(t.transform);
                                    Transform i = Instantiate(MultiGrappleIconPref);
                                    i.SetParent(t.transform);
                                    i.position = t.transform.position;
                                    i.position = new Vector3(i.position.x, i.position.y + 3, i.position.z);
                                    i.gameObject.SetActive(true);
                                    i.name = MultiGrappleIconPref.name;
                                    i.GetComponent<Animator>().Play("Start");
                                    LookingMultiGrappleObjectIcons.Add(i);
                                }
                            if (t.GetComponent<AIBehaviour>().v_dead)
                                if (LookingMultiGrappleObjects.Contains(t.transform))
                                {
                                    Destroy(LookingMultiGrappleObjectIcons[LookingMultiGrappleObjectIcons.IndexOf(t.transform)].gameObject);
                                    LookingMultiGrappleObjectIcons.Remove(t.transform);
                                    return;
                                }
                        }
                        if (t.GetComponent<VehicleScript>())
                        {
                            LookingMultiGrappleObjects.Add(t.transform);
                            Transform i = Instantiate(MultiGrappleIconPref);
                            i.SetParent(t.transform);
                            i.position = t.transform.position;
                            i.position = new Vector3(i.position.x, i.position.y + 3, i.position.z);
                            i.gameObject.SetActive(true);
                            i.name = MultiGrappleIconPref.name;
                            i.GetComponent<Animator>().Play("Start");
                            LookingMultiGrappleObjectIcons.Add(i);
                        }
                    }
            }
            foreach (Transform t in LookingMultiGrappleObjects)
            {
                if (t)
                {
                    bool p = true;
                    foreach (Transform ii in t.transform.GetComponentsInChildren<Transform>())
                    {
                        if (ii.name == MultiGrappleIconPref.name)
                        {
                            p = true;
                        }
                    }

                    if (!p)
                    {
                        Transform i = Instantiate(MultiGrappleIconPref);
                        i.SetParent(t);
                        i.position = t.transform.position;
                        if (t.transform.GetComponent<AIBehaviour>())
                            i.position = new Vector3(i.position.x, i.position.y + 3, i.position.z);
                        i.gameObject.SetActive(true);
                        i.name = MultiGrappleIconPref.name;
                        i.GetComponent<Animator>().Play("Start");
                        LookingMultiGrappleObjectIcons.Add(i);
                    }
                }
            }
            foreach (Transform t in LookingMultiGrappleObjectIcons)
            {               
                t.transform.LookAt(FPSPlayerCamera.Me.transform);
                bool pe = false;
                foreach (RaycastHit r in MultiGrappleObjects)
                {
                    if (r.transform == t.parent)
                        pe = true;
                }
                if (pe)
                    t.GetComponent<Animator>().SetBool("Selected", true);
                else
                    t.GetComponent<Animator>().SetBool("Selected", false);
            }
            if (MultiGrappleObjects.Count <= MaxMultiGrappleObjects)
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.position, camera.forward, out hit, MaxDistance))
                {
                    if (LookingMultiGrappleObjects.Contains(hit.transform))
                    {
                        float distanceFromPoint = Vector3.Distance(owner.position, hit.point);

                        if (distanceFromPoint <= 3)
                            return;

                        foreach (Transform t in hit.transform.GetComponentsInChildren<Transform>())
                        {
                            if (t.name == MultiGrappleIconPref.name)
                                t.GetComponent<Animator>().SetBool("Selected", true);
                        }

                        bool pe = false;
                        foreach (RaycastHit r in MultiGrappleObjects)
                        {
                            if (hit.transform == r.transform)
                                pe = true;
                        }

                        if (Input.GetButtonDown("Interact"))
                        {
                            bool pee = false;
                            foreach (RaycastHit r in MultiGrappleObjects)
                            {
                                if (hit.transform == r.transform)
                                    pee = true;
                            }
                            if (!pee)
                                MultiGrappleObjects.Add(hit);
                        }
                        if (Input.GetButtonUp("Grapple"))
                        {
                            bool pee = false;
                            foreach (RaycastHit r in MultiGrappleObjects)
                            {
                                if (hit.transform == r.transform)
                                    pee = true;
                            }
                            if (!pee)
                                MultiGrappleObjects.Add(hit);
                        }
                    }
                }
            }

            if (Input.GetButtonUp("Grapple"))
            {
                LookingForMultiGrappleTargets = false;
                if (MultiGrappleObjects.Count > 1)
                    StartTether(MultiGrappleObjects);
            }
        }
        else
        {
            MultiGrappleObjects.Clear();
            LookingMultiGrappleObjects.Clear();
            foreach (Transform t in LookingMultiGrappleObjectIcons)
            {
                if (t)
                {
                    Destroy(t.gameObject);
                }
            }
            foreach (Transform t in LookingMultiGrappleObjectIcons)
            {
                if (!t)
                {
                    LookingMultiGrappleObjectIcons.Remove(t);
                    return;
                }
            }
        }

        if (Ani)
        {
            if (!isFireing)
                Ani.SetBool("Active", IsGrappling());
            else
                Ani.SetBool("Active", true);
        }
        CanGrapple = CurrentCooldown > 0 & TetherDechargeProgress == 0;
        if (!CanGrapple & IsGrappling())
            StopGrapple();

        if (Type == MyType.Player)
            PlayerCharacterController.Me.GetComponent<PlayerController>().isGrappling = IsGrappling();
        if (IsGrappling())
        {
            IsWaitingRecharge = false;
            IsRecharging = false;
            if (!IsDecharging)
            {
                StartCoroutine(Decharge());
                IsDecharging = true;
            }
            HasGrappled = true;
            if (Type == MyType.Player || Type == MyType.Mech)
                if (FindObjectOfType<FeedbackFlashHUD>())
                    FindObjectOfType<FeedbackFlashHUD>().OnGrapple();
            Color righCol = NormalCol;
            if (grapplingIcon & GameManager.Me)
            {
                Vector3 IconSize = originalIconScale;
                grapplingIcon.transform.position = grapplePoint.position;
                grapplingIcon.LookAt(GameManager.Me.CurrentCamera.transform);
                grapplingIcon.gameObject.SetActive(true);
                if (Vector3.Distance(transform.position, grapplingIcon.transform.position) <= 5)
                    IconSize = originalIconScale / 4;

                grapplingIcon.transform.localScale = Vector3.Lerp(grapplingIcon.transform.localScale, IconSize, 0.5f);
            }
            if (audioSource)
                if (!audioSource.isPlaying)
                    audioSource.PlayOneShot(grapplingSFX);
            if (Type == MyType.Player)
            {
                PlayerCharacterController.Me.recievesFallDamage = false;
                if (CanMove)
                    PlayerCharacterController.Me.GetComponent<PlayerController>().Active = true;
                else
                    PlayerCharacterController.Me.GetComponent<PlayerController>().Active = false;

                PlayerCharacterController.Me.m_rigidBody.isKinematic = false;
                PlayerCharacterController.Me.GetComponent<BoxCollider>().enabled = true;
            }
            if (Type == MyType.Mech)
            {
                owner.GetComponent<MechController>().recievesFallDamage = false;
               // owner.GetComponent<PlayerController>().Active = false;

                owner.GetComponent<MechController>().m_rigidBody.isKinematic = false;
                owner.GetComponent<BoxCollider>().enabled = true;
            }

            if (GrappleObject.transform.GetComponentInParent<EnemyController>())
            {
                foreach (Ability a in PlayerAbilities.Me.Abilities)
                {
                    if (a.Name == "Grappling Hook Interactable Objects")
                    {
                        if (a.CurrentVersion.Name == "Gapple to Enemies")
                        {
                            righCol = EnemyCol;
                            if (GrappleObject.transform.GetComponentInParent<AIBehaviour>())
                                GrappleObject.transform.GetComponentInParent<AIBehaviour>().v_isGrappled = true;
                        }
                        if (a.CurrentVersion.Name == "Gapple Enemies to You")
                        {
                            righCol = EnemyCol;
                            if (GrappleObject.transform.GetComponentInParent<AIBehaviour>())
                                GrappleObject.transform.GetComponentInParent<AIBehaviour>().v_isPulled = true;
                        }
                    }
                }
                foreach (DetectionModule d in FindObjectsOfType<DetectionModule>())
                {
                    if (!d.RightknownDetectedTarget)
                    {
                        if (Vector3.Distance(transform.position, d.transform.position) <= 100 & d.GetComponentInParent<FactionInfo>().faction != owner.GetComponent<FactionInfo>().faction)
                        {
                            d.RightknownDetectedTarget = owner.gameObject;
                        }
                    }
                }

                string AddOn = "";
                foreach (Ability a in PlayerAbilities.Me.Abilities)
                {
                    if (a.Name == "Grappling Hook Add Ons")
                    {
                        if (a.CurrentVersion.Name == "Knock Down" & Input.GetButtonDown("Fatality"))
                        {
                            AddOn = "Knock Down";
                            KnockDown = true;
                        }
                    }
                }

                foreach (Text t in Prompts)
                {
                    if (t.name == "Knock Down Prompt")
                    {
                        foreach (Ability a in PlayerAbilities.Me.Abilities)
                        {
                            if (a.Name == "Grappling Hook Add Ons")
                            {
                                if (a.CurrentVersion.Name == "Knock Down")
                                {
                                    t.gameObject.SetActive(true);
                                    if (GameManager.Me.currentdevice == "KeyBoard")
                                        t.text = "F Knock Down";
                                    if (GameManager.Me.currentdevice == "GamePad")
                                        t.text = "RS Knock Down";

                                    if (AddOn == "Knock Down")
                                        t.color = Color.yellow;
                                }
                                else
                                {
                                    t.gameObject.SetActive(false);
                                }
                            }
                        }
                    }

                    if (AddOn != "")
                        if (AddOn + " Prompt" == t.name)
                        {
                            t.GetComponent<Animator>().Play("Trigger");
                        }
                        else
                        {
                            t.GetComponent<Animator>().Play("Hide");
                        }
                }

                int p = 0;
                foreach (Ability a in PlayerAbilities.Me.Abilities)
                {
                    if (a.Name == "Grappling Hook Interactable Objects")
                    {
                        if (a.CurrentVersion.Name == "Gapple to Enemies")
                        {
                            if (!KnockDown & !Melee)
                            {
                                p = 1;
                                if (GrappleObject.transform.GetComponentInParent<AIBehaviour>())
                                    FixAI(GrappleObject.transform.GetComponentInParent<AIBehaviour>(), false);
                            }
                            if (KnockDown)
                            {
                                p = 2;
                            }
                            if (Melee)
                            {

                            }
                        }
                        if (a.CurrentVersion.Name == "Gapple Enemies to You")
                        {
                            if (!KnockDown & !Melee)
                            {
                                p = 1;
                                if (GrappleObject.transform.GetComponentInParent<AIBehaviour>())
                                    FixAI(GrappleObject.transform.GetComponentInParent<AIBehaviour>(), false);
                            }
                            if (KnockDown)
                            {
                                p = 2;
                            }
                        }
                    }
                }
                if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 5 & Type == MyType.Player)
                {
                    foreach (Text t in Prompts)
                        t.GetComponent<Animator>().Play("Hide");

                    StopGrapple();
                    if (p == 1)
                    {
                        if (FPSPlayerCamera.Me.FatalityAble(GrappleObject.transform.GetComponentInParent<EnemyController>(), false))
                            FPSPlayerCamera.Me.PerformFatality(GrappleObject.transform.GetComponentInParent<EnemyController>(), 1);
                    }
                    if (p == 2)
                        FPSPlayerCamera.Me.PerformFatality(GrappleObject.transform.GetComponentInParent<EnemyController>(), 2);
                }
            }
            if (GrappleObject.transform.GetComponentInParent<ZipMovement>())
            {
                righCol = ZiplineCol;
                if (Type == MyType.Player)
                    if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 5 || UseObject)
                    {
                        StopGrapple();
                        GrappleObject.transform.GetComponentInParent<ZipMovement>().Interact(PlayerCharacterController.Me.transform, GrappleObject);
                    }
            }
            if (GrappleObject.transform.GetComponentInParent<VehicleScript>())
            {
                righCol = VehicleCol;
                if (Type == MyType.Player)
                    if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 5 || UseObject)
                    {
                        StopGrapple(true);
                        owner.GetComponent<CharacterDriving>().EnterVehicle(GrappleObject.transform.GetComponentInParent<VehicleScript>());
                    }
            }
            if (GrappleObject.transform.GetComponentInParent<HealthPickup>())
            {
                righCol = HealthCol;
                GrappleObject.transform.GetComponentInParent<Pickup>().isGrappled = true;
                GrappleObject.transform.GetComponentInParent<Rigidbody>().isKinematic = false;
                if (Type == MyType.Player)
                    if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 5 || UseObject)
                    {
                        StopGrapple();
                        GrappleObject.transform.GetComponentInParent<Pickup>().Trigger();
                    }
            }
            if (GrappleObject.transform.GetComponentInParent<WeaponPickup>())
            {
                righCol = WeaponCol;
                GrappleObject.transform.GetComponentInParent<Pickup>().isGrappled = true;
                GrappleObject.transform.GetComponentInParent<Rigidbody>().isKinematic = false;
                if (Type == MyType.Player)
                    if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 5 || UseObject)
                    {
                        StopGrapple();
                        GrappleObject.transform.GetComponentInParent<Pickup>().Trigger();
                    }
            }
            if (GrappleObject.transform.GetComponentInParent<MechController>())
            {
                righCol = VehicleCol;
                if (Type == MyType.Player)
                    if (Vector3.Distance(owner.transform.position, GrappleObject.position) <= 7 || UseObject)
                    {
                        StopGrapple(true);
                        GrappleObject.transform.GetComponentInParent<MechController>().Enter(owner);
                    }
            }


            if (grapplingIcon)
                if (isStarting)
                    grapplingIcon.GetComponentInChildren<SpriteRenderer>().color = StartingCol;
                else
                    grapplingIcon.GetComponentInChildren<SpriteRenderer>().color = righCol;
        }
        else
        {
            IsDecharging = false;
            if (!IsRecharging & CurrentCooldown < Cooldown & !IsWaitingRecharge & TetherDechargeProgress == 0)
            {
                StartCoroutine(Recharge());
                IsRecharging = true;
            }

            foreach (Text t in Prompts)
            {
                t.enabled = true;
                t.gameObject.SetActive(false);
                if (t.name == "Knock Down Prompt")
                    t.color = Color.white;
            }

            if (grapplingIcon)
                grapplingIcon.gameObject.SetActive(false);
            if (audioSource)
                audioSource.Stop();
            if (Type == MyType.Player)
            {
                if (!GameManager.Me.CurrentPlayer.DrivingScript.CurrentVehicle & !GameManager.Me.CurrentPlayer.DrivingScript.Exiting)
                {
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.None;
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                    PlayerCharacterController.Me.m_rigidBody.isKinematic = true;
                    PlayerCharacterController.Me.GetComponent<BoxCollider>().enabled = false;
                }
                else
                {
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.None;
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.FreezePositionX;
                    PlayerCharacterController.Me.m_rigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
                    PlayerCharacterController.Me.m_rigidBody.isKinematic = false;
                    PlayerCharacterController.Me.GetComponent<BoxCollider>().enabled = false;
                }
            }
            if (Type == MyType.Mech)
            {
                if (!GameManager.Me.CurrentPlayer.DrivingScript.CurrentVehicle & !GameManager.Me.CurrentPlayer.DrivingScript.Exiting)
                {
                  owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.None;
                    owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                    owner.GetComponent<MechController>().m_rigidBody.isKinematic = true;
                    owner.GetComponent<MechController>().GetComponent<BoxCollider>().enabled = false;
                }
                else
                {
                    owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.None;
                    owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                    owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.FreezePositionX;
                    owner.GetComponent<MechController>().m_rigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
                    owner.GetComponent<MechController>().m_rigidBody.isKinematic = false;
                    owner.GetComponent<MechController>().GetComponent<BoxCollider>().enabled = false;
                }
            }
        }

        if (!GrappleDown & PreviousLookingForMultiGrappleTargets)
            Time.timeScale = 1;
        if (PreviousLookingForMultiGrappleTargets)
            if (PreviousLookingForMultiGrappleTargets != LookingForMultiGrappleTargets)
            {
                Time.timeScale = 1;
                if (FindObjectOfType<FeedbackFlashHUD>())
                {
                    FindObjectOfType<FeedbackFlashHUD>().m_LookTether = false;
                    FindObjectOfType<FeedbackFlashHUD>().ResetFlash(false);
                }
            }

        if (!LookingForMultiGrappleTargets)
        {
            if (FindObjectOfType<FeedbackFlashHUD>())
                if (FindObjectOfType<FeedbackFlashHUD>().m_LookTether)
                {
                    FindObjectOfType<FeedbackFlashHUD>().m_LookTether = false;
                    FindObjectOfType<FeedbackFlashHUD>().ResetFlash(false);
                }
        }

        PreviousLookingForMultiGrappleTargets = LookingForMultiGrappleTargets;

        UseObject = false;
    }

    //Called after Update
    void LateUpdate()
    {
        DrawRope();
    }

    public List<Transform> TetherableObjects()
    {
        List<Transform> Objects = new List<Transform>();
        foreach (WeaponPickup t in FindObjectsOfType<WeaponPickup>())
        {
            if (t)
                if (Vector3.Distance(t.transform.position, owner.transform.position) <= 100)
                    Objects.Add(t.transform);
        }
        foreach (Destructable t in FindObjectsOfType<Destructable>())
        {
            if (t)
                if (Vector3.Distance(t.transform.position, owner.transform.position) <= 100)
                    Objects.Add(t.transform);
        }
        foreach (AIBehaviour t in FindObjectsOfType<AIBehaviour>())
        {
            if (t)
                if (Vector3.Distance(t.transform.position, owner.transform.position) <= 100)
                    if (!t.v_dead)
                        Objects.Add(t.transform);
        }
        foreach (VehicleScript t in FindObjectsOfType<VehicleScript>())
        {
            if (t)
                if (Vector3.Distance(t.transform.position, owner.transform.position) <= 100)
                    Objects.Add(t.transform);
        }
        return Objects;
    }


    public IEnumerator WaitPress()
    {
        yield return new WaitForSeconds(0.05f);
        PressTime += 0.01f;
        if (Presses != 0)
            if (PressTime >= 0.04f)
            {
                if (!waitingGrapple & Presses == 1 & GrappleDown)
                    StartGrapple();
                PressTime = 0;
                Presses = 0;
            }
            else
            {
                StartCoroutine(WaitPress());
            }
    }

    public IEnumerator WaitGrapple()
    {
        yield return new WaitForSeconds(0);
        if (!waitingGrapple)
        {
            waitingGrapple = true;
            yield return new WaitForSeconds(0.5f);
            StartGrapple();
            waitingGrapple = false;
        }
    }

    public void DetectPoint()
    {
        RaycastHit hit;
        if (Physics.Raycast(camera.position, camera.forward, out hit, MaxDistance, whatIsGrappleable))
        {
            if (hit.transform != transform)
            {
                bool isChild = false;
                foreach (Transform o in owner.GetComponentsInChildren<Transform>())
                    if (hit.transform == o)
                        isChild = true;
                float distanceFromPoint = Vector3.Distance(owner.position, hit.point);
                if (!isChild & distanceFromPoint > 5)
                    LookingGrappleObject = hit.transform;
                else
                    LookingGrappleObject = null;
            }
            else
                LookingGrappleObject = null;
        }
        else
            LookingGrappleObject = null;
    }
    public IEnumerator WaitUI()
    {
        isStarting = true;
        yield return new WaitForSeconds(0.02f);
        if (audioSource)
            audioSource.PlayOneShot(uiSFX);
        yield return new WaitForSeconds(0.12f);
        isStarting = false;
    }

    public IEnumerator Fire(int type, List<RaycastHit> Objects)
    {
        isFireing = true;
        yield return new WaitForSeconds(0.1f);
        if (Type == MyType.Player || Type == MyType.Mech)
            FindObjectOfType<FeedbackFlashHUD>().OnGrapple();
        if (shootVFX)
            shootVFX.Play();
        if (audioSource)
            audioSource.PlayOneShot(grappleSFX);
        isFireing = false;
        if (type == 1)
        {
            Grapple();
            if (Type == MyType.Player)
                foreach (Ability a in PlayerAbilities.Me.Abilities)
                    if (a.Name == "Grappling Hook Movement" & a.CurrentVersion.Name == "Static")
                        PlayerCharacterController.Me.ResetMobility();
        }
        if (type == 2)
        {
            Tether(Objects);
        }
    }

    public bool CheckGrapple()
    {
        bool DoGrapple = false;
        RaycastHit check;
        if (Physics.Raycast(camera.position, camera.forward, out check, MaxDistance, whatIsGrappleable))
        {
            DoGrapple = true;
            bool isChild = false;
            foreach (Transform o in owner.GetComponentsInChildren<Transform>())
                if (check.transform == o)
                    isChild = true;
            float distanceFromPoint = Vector3.Distance(owner.position, check.point);
            if (isChild || distanceFromPoint < 5)
                DoGrapple = false;
        }
        return DoGrapple;
    }

    public void StartGrapple()
    {
        if (CheckGrapple())
        {
            Presses = 0;
            StartCoroutine(Fire(1, null));
        }
    }

    public void Grapple()
    {
        if (CheckGrapple())
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.position, camera.forward, out hit, MaxDistance, whatIsGrappleable))
            {
                float distanceFromPoint = Vector3.Distance(owner.position, hit.point);

                if (distanceFromPoint <= 5)
                    return;

                if (audioSource)
                    audioSource.PlayOneShot(impactSFX);
                if (audioSource)
                    audioSource.PlayOneShot(grapplingSFX);
                GrappleObject = hit.transform;
                grapplePoint = new GameObject().transform;
                grapplePoint.position = hit.point;
                grapplePoint.SetParent(hit.transform);

                if (grapplingIcon)
                {
                    grapplingIcon.transform.position = grapplePoint.position;
                    grapplingIcon.LookAt(GameManager.Me.CurrentCamera.transform);
                    grapplingIcon.gameObject.SetActive(true);
                    grapplingIcon.GetComponent<Animator>().Play("Start");
                    StartCoroutine(WaitUI());
                }

                if (hitVFX)
                {
                    ParticleSystem hitc = Instantiate(hitVFX);
                    hitc.transform.position = hit.point;
                    hitc.transform.LookAt(owner.position);
                    hitc.transform.Translate(Vector3.forward * 2);
                    hitc.transform.SetParent(GameManager.Me.EffectsParent);
                    Destroy(hitc.gameObject, 10f);
                    hitc.Play();
                }

                float up = 0;

                int type = 1;
                if (hit.transform.GetComponentInParent<ZipMovement>() & Type == MyType.Player)
                    type = 2;
                if (hit.transform.GetComponentInParent<AIBehaviour>() & Type == MyType.Player)
                    type = 3;
                if (hit.transform.GetComponentInParent<VehicleScript>() & Type == MyType.Player)
                    type = 4;
                if (hit.transform.GetComponentInParent<HealthPickup>() & Type == MyType.Player)
                    type = 5;
                if (hit.transform.GetComponentInParent<WeaponPickup>() & Type == MyType.Player)
                    type = 6;
                if (hit.transform.GetComponentInParent<MechController>() & Type == MyType.Player)
                    type = 4;

                if (type == 1)
                {
                    up = 1.8f;
                    float rightForce = 0;
                    if (Type == MyType.Player)
                        rightForce = 5;
                    if (Type == MyType.Mech)
                        rightForce = 1000;
                    joint = owner.gameObject.AddComponent<SpringJoint>();
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedAnchor = grapplePoint.position;

                    //The distance grapple will try to keep from grapple point. 
                    joint.maxDistance = distanceFromPoint * 0.4f;
                    joint.minDistance = distanceFromPoint * MinDistance;

                    //Adjust these values to fit your game.
                    joint.spring = rightForce;
                    joint.damper = Damper;
                    joint.massScale = MassScale;
                }
                if (type == 2)
                {
                    up = 1.8f;
                    joint = owner.gameObject.AddComponent<SpringJoint>();
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedAnchor = grapplePoint.position;

                    //The distance grapple will try to keep from grapple point. 
                    joint.maxDistance = distanceFromPoint * 0.4f;
                    joint.minDistance = distanceFromPoint * 0.1f;

                    //Adjust these values to fit your game.
                    joint.spring = 10;
                    joint.damper = Damper;
                    joint.massScale = MassScale;
                }
                if (type == 3)
                {
                    int mode = 1;
                    foreach (Ability a in PlayerAbilities.Me.Abilities)
                    {
                        if (a.Name == "Grappling Hook Interactable Objects")
                        {
                            if (a.CurrentVersion.Name == "Gapple to Enemies")
                                mode = 1;
                            if (a.CurrentVersion.Name == "Gapple Enemies to You")
                                if (hit.transform.GetComponentInParent<EnemyController>().PlayerCanGrapplePullFatality)
                                    mode = 2;
                                else
                                    mode = 1;
                        }
                    }

                    if (mode == 1)
                    {
                        up = 1.8f;
                        joint = owner.gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = grapplePoint.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 10;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                    if (mode == 2)
                    {
                        joint = grapplePoint.GetComponentInParent<EnemyController>().gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = owner.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 250;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                }
                if (type == 4)
                {
                    int mode = 1;
                    foreach (Ability a in PlayerAbilities.Me.Abilities)
                    {
                        if (a.Name == "Grappling Hook Interactable Objects")
                        {
                            if (a.CurrentVersion.Name == "Gapple to Enemies")
                                mode = 1;
                            if (a.CurrentVersion.Name == "Gapple Enemies to You")
                                mode = 2;
                        }
                    }

                    if (mode == 1)
                    {
                        up = 1.8f;
                        joint = owner.gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = grapplePoint.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 20;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                    if (mode == 2)
                    {
                        joint = grapplePoint.GetComponentInParent<VehicleScript>().gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = owner.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 70000;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                }
                if (type == 5)
                {
                    int mode = 1;
                    foreach (Ability a in PlayerAbilities.Me.Abilities)
                    {
                        if (a.Name == "Grappling Hook Interactable Objects")
                        {
                            if (a.CurrentVersion.Name == "Gapple to Enemies")
                                mode = 1;
                            if (a.CurrentVersion.Name == "Gapple Enemies to You")
                                mode = 2;
                        }
                    }

                    if (mode == 1)
                    {
                        up = 1.8f;
                        joint = owner.gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = grapplePoint.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 10;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                    if (mode == 2)
                    {
                        joint = grapplePoint.GetComponentInParent<HealthPickup>().gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = owner.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 10;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                }
                if (type == 6)
                {
                    int mode = 1;
                    foreach (Ability a in PlayerAbilities.Me.Abilities)
                    {
                        if (a.Name == "Grappling Hook Interactable Objects")
                        {
                            if (a.CurrentVersion.Name == "Gapple to Enemies")
                                mode = 1;
                            if (a.CurrentVersion.Name == "Gapple Enemies to You")
                                mode = 2;
                        }
                    }

                    if (mode == 1)
                    {
                        up = 1.8f;
                        joint = owner.gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = grapplePoint.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 10;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                    if (mode == 2)
                    {
                        joint = grapplePoint.GetComponentInParent<WeaponPickup>().gameObject.AddComponent<SpringJoint>();
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedAnchor = owner.position;

                        //The distance grapple will try to keep from grapple point. 
                        joint.maxDistance = distanceFromPoint * 0.4f;
                        joint.minDistance = distanceFromPoint * MinDistance;

                        joint.spring = 15;
                        joint.damper = Damper;
                        joint.massScale = MassScale;
                    }
                }

                lr.positionCount = 2;
                currentGrapplePosition = gunTip.position;

                if (Type == MyType.Player || Type == MyType.Mech)
                {
                    bool Do = false;
                    RaycastHit FGetGround;
                    if (Physics.Raycast(owner.position, owner.TransformDirection(Vector3.down), out FGetGround, 0.3f))
                        if (FGetGround.transform)
                            if (FGetGround.transform != transform)
                                Do = true;

                    if (Do)
                    {
                        owner.position = new Vector3(owner.position.x, owner.position.y + up, owner.position.z);
                        RaycastHit GetGround;
                        if (Physics.Raycast(owner.position, owner.TransformDirection(Vector3.down), out GetGround, up))
                            if (GetGround.transform)
                                if (GetGround.transform != transform)
                                    owner.position = GetGround.point;
                    }
                }
            }
        }
    }

    public void StopGrapple(bool force = false)
    {
        Presses = 0;
        KnockDown = false;
        Melee = false;
        if (force)
        {
            if (shootVFX)
                shootVFX.Stop();
            Ani.Play("Holstered Pos");
            PlayerWeaponsManager.CurrentWeapon.HideWeapon();
        }
        lr.positionCount = 0;
        if (grapplePoint)
            Destroy(grapplePoint.gameObject);
        if (joint)
            Destroy(joint);
        if (GrappleObject)
            if (GrappleObject.transform.GetComponentInParent<AIBehaviour>())
                FixAI(GrappleObject.transform.GetComponentInParent<AIBehaviour>(), false);
        if (GrappleObject & EnemyGrapplG)
        {
            if (GrappleObject.transform.GetComponentInParent<Pickup>())
            {
                GrappleObject.transform.GetComponentInParent<Pickup>().isGrappled = false;
                GrappleObject.transform.GetComponentInParent<Rigidbody>().isKinematic = true;
                GrappleObject.transform.GetComponentInParent<Pickup>().m_StartPosition = GrappleObject.transform.GetComponentInParent<Pickup>().transform.position;
            }
            EnemyGrapplG.StopGrapple();
            Destroy(EnemyGrapplG.gameObject, 1f);
        }
        StartCoroutine(Wait());
        if (audioSource)
            audioSource.Stop();

        if (Type == MyType.Player)
            owner.GetComponent<PlayerCharacterController>().ResetMobility();
        if (Type == MyType.Mech)
            owner.GetComponent<MechController>().ResetMobility();
    }

    public void MultiGrapple()
    {
        Presses = 0;

        LookingForMultiGrappleTargets = true;
        RaycastHit hit;
        if (Physics.Raycast(camera.position, camera.forward, out hit, MaxDistance, whatIsGrappleable))
        {
            float distanceFromPoint = Vector3.Distance(owner.position, hit.point);

            if (distanceFromPoint <= 3)
                return;

            MultiGrappleObjects.Add(hit);
        }
    }

    public void StartTether(List<RaycastHit> Objects)
    {
        List<RaycastHit> NewObjects = new List<RaycastHit>();
        foreach (RaycastHit h in Objects)
            NewObjects.Add(h);
        StartCoroutine(Fire(2, NewObjects));
    }

    public void Tether(List<RaycastHit> Objects)
    {
        TetherDechargeProgress = 0.01f;
        List<RaycastHit> NewObjects = new List<RaycastHit>();
        foreach (RaycastHit h in Objects)
            NewObjects.Add(h);
        foreach (RaycastHit hit in NewObjects)
        {
            bool p = false;
            RaycastHit r1 = new RaycastHit();
            RaycastHit r2 = new RaycastHit();

            if (NewObjects.IndexOf(hit) == 0)
            {
                p = true;
                r1 = NewObjects[NewObjects.IndexOf(hit) + 1];
                //  Debug.Log(r1.transform.name);
            }
            if (NewObjects.IndexOf(hit) == NewObjects.Count)
            {
                p = true;
                r1 = NewObjects[NewObjects.IndexOf(hit) - 1];
               // Debug.Log(r1.transform.name);
            }
            if (!p)
            {
                p = true;
                r1 = NewObjects[NewObjects.IndexOf(hit) - 1];
                // r2 = NewObjects[NewObjects.IndexOf(hit) + 1];
            }

            if (p)
            {
                float distanceFromPoint = Vector3.Distance(hit.point, r1.point);

                SpringJoint joint1 = hit.transform.gameObject.AddComponent<SpringJoint>();
                joint1.autoConfigureConnectedAnchor = false;
                joint1.connectedAnchor = r1.transform.position;

                SpringJoint joint2 = null;
                if (r2.transform)
                {
                    joint2 = r2.transform.gameObject.AddComponent<SpringJoint>();
                    joint2.autoConfigureConnectedAnchor = false;
                    joint2.connectedAnchor = hit.point;
                }

                //The distance grapple will try to keep from grapple point. 
                joint1.maxDistance = distanceFromPoint * 0.4f;
                joint1.minDistance = distanceFromPoint * 0.18f;

                if (joint2)
                {
                    joint2.maxDistance = distanceFromPoint * 0.4f;
                    joint2.minDistance = distanceFromPoint * MinDistance;
                }

                joint1.damper = Damper;
                joint1.massScale = MassScale;

                if (joint2)
                {
                    joint2.damper = Damper;
                    joint2.massScale = MassScale;
                }

                LineRenderer r = new GameObject().AddComponent<LineRenderer>();
                r.transform.SetParent(hit.transform);
                r.transform.position = hit.transform.position;
                r.name = "Tether";
                r.transform.LookAt(r1.point);

                TetherScript t = hit.transform.gameObject.AddComponent<TetherScript>();
                t.grappleLr = lr;
                t.lr = r;
                t.target1 = hit.transform;
                t.target2 = r1.transform;
                t.GrappleTethered = true;

                if (r1.transform.GetComponentInParent<EnemyController>() & hit.transform.GetComponentInParent<EnemyController>())
                {
                    if (hit.transform.GetComponentInParent<EnemyController>().FatalityLook)
                        t.target1 = hit.transform.GetComponentInParent<EnemyController>().FatalityLook;
                    if (r1.transform.GetComponentInParent<EnemyController>().FatalityLook)
                        t.target2 = r1.transform.GetComponentInParent<EnemyController>().FatalityLook;
                }

                int type = 1;
                if (hit.transform.GetComponentInParent<WeaponPickup>())
                    type = 2;
                if (hit.transform.GetComponentInParent<AIBehaviour>())
                    type = 3;
                if (hit.transform.GetComponentInParent<VehicleScript>())
                    type = 4;

                if (type == 1)
                {
                    joint1.spring = 190;
                    if (joint2)
                        joint2.spring = 190;
                }
                if (type == 2)
                {
                    joint1.spring = 10;
                    if (joint2)
                        joint2.spring = 10;
                }
                if (type == 3)
                {
                    joint1.spring = 600;
                    if (joint2)
                        joint2.spring = 600;
                }
                if (type == 4)
                {
                    joint1.spring = 70000;
                    if (joint2)
                        joint2.spring = 70000;
                }

                if (audioSource)
                    audioSource.PlayOneShot(impactSFX);
                if (audioSource)
                    audioSource.PlayOneShot(grapplingSFX);

                if (hitVFX)
                {
                    ParticleSystem hitc = Instantiate(hitVFX);
                    hitc.transform.position = hit.point;
                    hitc.transform.LookAt(owner.position);
                    hitc.transform.Translate(Vector3.forward * 2);
                    hitc.transform.SetParent(GameManager.Me.EffectsParent);
                    Destroy(hitc.gameObject, 10f);
                    hitc.Play();
                }
            }       
        }   
    }

    public void FixAI(AIBehaviour ai, bool f)
    {
        ai.v_isGrappled = false;
        ai.v_isPulled = false;
        ai.v_isFixGrapple = true;
        ai.c_agent.enabled = true;
        StartCoroutine(WaitFixAI(ai, f));
    }
    public IEnumerator WaitFixAI(AIBehaviour ai, bool f)
    {
        yield return new WaitForSeconds(0.1f);
        ai.v_isGrappled = false;
        ai.v_isPulled = false;
        if (ai.GetComponent<Rigidbody>())
            ai.GetComponent<Rigidbody>().isKinematic = true;
        ai.v_isFixGrapple = false;

        if (f)
        {
            if (FPSPlayerCamera.Me.FatalityAble(ai.GetComponent<EnemyController>(), false))
                FPSPlayerCamera.Me.PerformFatality(ai.GetComponent<EnemyController>(), 1);
        }
        else
        {
            if (ai.GetComponent<Rigidbody>())
                ai.GetComponent<Rigidbody>().isKinematic = false;
        }
    }


    private Vector3 currentGrapplePosition;

    void DrawRope()
    {
        //If not grappling, don't draw rope
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint.position, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }

    public bool IsGrappling()
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint.position;
    }

    public IEnumerator Wait()
    {
        yield return new WaitForSeconds(.05f);
        if (Type == MyType.Player)
        {
            PlayerCharacterController.Me.GetComponent<PlayerController>().Active = true;
            PlayerCharacterController.Me.recievesFallDamage = true;
        }
        if (Type == MyType.Mech)
        {
           // owner.GetComponent<PlayerController>().Active = true;
            owner.GetComponent<MechController>().recievesFallDamage = true;
        }
        if (GrappleObject)
        {
            StartCoroutine(WaitRecharge(0));
        }
        GrappleObject = null;
    }

    public IEnumerator Decharge()
    {
        IsDecharging = true;
        CurrentCooldown -= DechargeRate;
        yield return new WaitForSeconds(0.1f);
        IsDecharging = false;
    }

    public IEnumerator TetherDecharge()
    {
        IsTetherDecharging = true;
        CurrentCooldown -= 0.5f;
        TetherDechargeProgress += 0.5f;
        yield return new WaitForSeconds(0.01f);
        IsTetherDecharging = false;
        if (TetherDechargeProgress >= TetherDechargeAmount)
        {
            TetherDechargeProgress = 0;
            StartCoroutine(WaitRecharge(0));
        }
    }

    public IEnumerator WaitRecharge(float time)
    {
        IsWaitingRecharge = true;
        IsRecharging = false;
        yield return new WaitForSeconds(0.1f);
        if (!HasGrappled)
        {
            time += 0.1f;
            if (time >= RechargeDelay)
            {
                IsWaitingRecharge = false;
            }
            else
            {
                StartCoroutine(WaitRecharge(time));
            }
        }
        else
        {
            HasGrappled = false;
        }
    }

    public IEnumerator Recharge()
    {
        IsRecharging = true;
        yield return new WaitForSeconds(0.1f);
        if (!HasGrappled)
        {
            IsRecharging = false;
            CurrentCooldown += RechargeRate;
            if (CurrentCooldown < Cooldown)
                StartCoroutine(Recharge());
            if (CurrentCooldown >= Cooldown)
            {
                CanGrapple = true;
                CurrentCooldown = Cooldown;
            }
        }
        else
        {
            HasGrappled = false;
        }
    }
}
