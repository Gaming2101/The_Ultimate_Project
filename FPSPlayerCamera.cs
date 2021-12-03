using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Fatality
{
    public string Name;
    public Animator Controller;
    public float ShowLength;
    public float AttackLength;
    public float HitVFXLength;
    public float AudioLength;
    public float Length;
    public AudioClip Sound;
}
[Serializable]
public class EnemyAvailableFatality
{
    public string Name;
}


public class FPSPlayerCamera : MonoBehaviour
{
    public static FPSPlayerCamera Me;
    public List<Actor> ActorsOnScreen = new List<Actor>();
    public float Range = 1.9f;
    public float FatalityRange = 3;
    public LayerMask LayerMask;
    public GameObject BlackWall;
    public Camera MainCam;
    public Camera FatalityCam;

    public EnemyController FatalityTarget;
    [SerializeField] public List<Fatality> FatalityList = new List<Fatality>();
    public List<Animator> FatalityAnis = new List<Animator>();
    [SerializeField] public List<Fatality> KnowckDownList = new List<Fatality>();
    public Transform FatalityP;
    public int PreviousFatality;
    public Fatality CurrentFatality;
    public bool DoingFatality;
    public bool AfterFatality;
    public bool FatalityFinishing;
    public bool ShowFatality;
    public bool CanDoFatality;

    public string BeforeW;

    // Start is called before the first frame update
    void Start()
    {
        Me = this;
        MainCam = GetComponent<Camera>();
        CanDoFatality = true;

        foreach (Camera C in GetComponentsInChildren<Camera>())
        {
            if(C.name == "Fatality Camera")
            {
                FatalityCam = C;
            }
        }
        if (FindObjectOfType<TrophyManager>())
            FindObjectOfType<TrophyManager>().BlackWall = BlackWall;
        foreach (Animator A in FatalityP.GetComponentsInChildren<Animator>())
        {
            FatalityAnis.Add(A);
        }
    }

    // Update is called once per frame
    void Update()
    {
        List<Actor> NewEL = new List<Actor>();
        float closestSqrDistance = Mathf.Infinity;
        float RightSee = 90000;
        float sqrDetectionRange = RightSee * RightSee;
        if (FindObjectOfType<ActorsManager>())
            foreach (Actor otherActor in FindObjectOfType<ActorsManager>().actors)
            {
                float sqrDistance = (otherActor.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                {
                    // Check for obstructions
                    RaycastHit closestValidHit = new RaycastHit();
                    bool foundValidHit = false;
                    RaycastHit H;
                    if (Physics.Raycast(transform.position, (otherActor.aimPoint.position - transform.position).normalized, out H, RightSee))
                    {
                        // Debug.DrawRay(transform.position, (otherActor.aimPoint.position - transform.position).normalized * H.distance, Color.red);
                        closestValidHit = H;
                        foundValidHit = true;
                    }

                    if (foundValidHit)
                    {
                        Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                        if (hitActor == otherActor)
                        {
                            NewEL.Add(hitActor);
                        }
                    }
                }
            }
        ActorsOnScreen = NewEL;
        if (Time.timeScale != 0 & Input.GetButtonDown("Fatality") & CanDoFatality)
        {
            CanDoFatality = false;
            StartCoroutine(WaitFatality());
            CheckFatality();
        }
        if(ItemWheelManager.Me)
            if (Input.GetButtonDown("Interact") & Time.timeScale != 0 & !DoingFatality & !PlayerCharacterController.Me.m_grapplingGun.LookingForMultiGrappleTargets & PlayerAbilities.Me.CurrentAbility.Name == "" & !ItemWheelManager.Me.Open)
            {
                Interact();
                List<ZipMovement> zs = new List<ZipMovement>();
                Transform zsp = null;
                foreach (ZipMovement z in FindObjectsOfType<ZipMovement>())
                {
                    foreach (Transform t in z.GetComponentsInChildren<Transform>())
                    {
                        if (Vector3.Distance(PlayerCharacterController.Me.transform.position, t.position) <= z.Range & !z.CurrentUser)
                        {
                            zsp = t;
                            zs.Add(z);
                        }
                    }
                }
                if (zs.Count > 0)
                {
                    if (GrapplingGun.Me.IsGrappling())
                        GrapplingGun.Me.StopGrapple();
                    zs[UnityEngine.Random.Range(0, zs.Count)].Interact(PlayerCharacterController.Me.transform, zsp);
                }
            }
        if (Input.GetButtonDown("Holster") & !GameManager.Me.CurrentPlayer.CurrentMech)
        {
            if (PlayerAbilities.Me.CurrentAbility.Name == "")
                if (!GameManager.Me.CurrentCamera.CurrentInventory || GameManager.Me.currentdevice == "KeyBoard")
                    if (!PlayerWeaponsManager.Me.IsHolstering)
                        PlayerWeaponsManager.Me.HolsterWeapon();
                    else
                        PlayerWeaponsManager.Me.ReadyWeapon();
        }


        if (PlayerCharacterController.Me.m_Health.currentHealth <= 0 & DoingFatality)
        {
            StopFatality();
            DoingFatality = false;
        }

        if (DoingFatality)
        {
            MainCam.GetComponent<AudioListener>().enabled = false;
            FatalityCam.GetComponent<AudioListener>().enabled = true;
            GameManager.Me.CurrentCamera = FatalityCam.GetComponent<PlayerCamera>();
            MainCam.enabled = false;
            FatalityCam.enabled = true;
            PlayerCharacterController.Me.GetComponent<PlayerController>().Active = false;
            PlayerCharacterController.Me.ResetMobility();
            PlayerCharacterController.Me.m_Controller.enabled = false;
            PlayerCharacterController.Me.m_Health.invincible = true;
            PlayerCharacterController.Me.stopRotate = true;
            PlayerWeaponsManager.Me.isAiming = false;
            if (CurrentFatality != null)
            {
                foreach (Animator A in FatalityAnis)
                {
                    if (A == CurrentFatality.Controller & ShowFatality)
                    {
                        ChangeA(A, true);
                    }
                    else
                    {
                        ChangeA(A, false);
                    }
                }
            }
            if (FatalityTarget)
            {
                if (FatalityTarget.GetComponent<AIBehaviour>())
                {
                    FatalityTarget.GetComponent<AIBehaviour>().DoFatalityPos();
                    FatalityTarget.GetComponent<AIBehaviour>().enabled = false;
                    FatalityTarget.GetComponent<AIBehaviour>().v_aiState = AIBehaviour.AIState.CombatStance;
                }
                // PlayerCharacterController.Me.transform.position = FatalityTarget.FatalityP.transform.position;
                PlayerCharacterController.Me.transform.position = Vector3.Lerp(PlayerCharacterController.Me.transform.position, FatalityTarget.FatalityP.transform.position, 0.4f);
                FatalityTarget.GetComponentInChildren<Health>().invincible = true;
                FatalityTarget.GetComponentInChildren<Animator>().Play("Combat Idle");
                FatalityTarget.GetComponentInChildren<Animator>().enabled = true;
                if (FatalityTarget.GetComponent<UnityEngine.AI.NavMeshAgent>())
                {
                    if (FatalityTarget.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled)
                        FatalityTarget.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(FatalityTarget.transform.position);
                    FatalityTarget.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = 0;
                }
                if (FatalityTarget.FatalityLook)
                {
                    if (Vector3.Distance(transform.position, FatalityTarget.FatalityLook.position) > 1)
                    {
                        Transform NewLook = new GameObject().transform;
                        NewLook.position = transform.position;
                        NewLook.SetParent(transform.parent);
                        NewLook.LookAt(FatalityTarget.FatalityLook.transform);
                        PlayerCharacterController.Me.transform.localEulerAngles = Vector3.Slerp(PlayerCharacterController.Me.transform.localEulerAngles, new Vector3(0, NewLook.eulerAngles.y, 0), 0.4f);
                        PlayerCharacterController.Me.m_CameraVerticalAngle = NewLook.localEulerAngles.x;
                        PlayerCharacterController.Me.m_CameraVerticalAngle = Mathf.Clamp(PlayerCharacterController.Me.m_CameraVerticalAngle, -89f, 89f);
                        PlayerCharacterController.Me.playerCamera.transform.localEulerAngles = Vector3.Slerp(PlayerCharacterController.Me.playerCamera.transform.localEulerAngles, new Vector3(PlayerCharacterController.Me.m_CameraVerticalAngle, 0, 0), 0.9f);
                        Destroy(NewLook.gameObject);
                    }
                }
                if (FatalityTarget.m_NavMeshAgent)
                    FatalityTarget.m_NavMeshAgent.speed = 0;
                AIBehaviour AI = FatalityTarget.GetComponent<AIBehaviour>();
                FatalityTarget.m_Health.dontDoKillFX = true;
                if (AI)
                    AI.enabled = true;
            }
            else if (!FatalityFinishing)
            {
                StopFatality();
            }
            if (!AfterFatality)
                if (PlayerWeaponsManager.CurrentWeapon)
                    if (!PlayerWeaponsManager.CurrentWeapon.IsHolster)
                        PlayerWeaponsManager.Me.HolsterWeapon();
        }
        else
        {
            if (GameManager.Me.CurrentCamera == GetComponent<PlayerCamera>())
                MainCam.enabled = true;
          //  FatalityCam.enabled = false;
            foreach (Animator A in FatalityAnis)
            {
                ChangeA(A, false);
            }
        }
        if (AfterFatality)
        {
            PlayerCharacterController.Me.GetComponent<PlayerController>().Active = false;
            PlayerCharacterController.Me.ResetMobility();

            if (PlayerCharacterController.Me.playerCamera.transform.localEulerAngles != new Vector3(0, 0, 0))
            {
                PlayerCharacterController.Me.m_CameraVerticalAngle = 0;
                PlayerCharacterController.Me.playerCamera.transform.localEulerAngles = Vector3.Slerp(PlayerCharacterController.Me.playerCamera.transform.localEulerAngles, new Vector3(0, 0, 0), 0.8f);
            }
            else
            {
                AfterFatality = false;
                DoingFatality = false;
                FatalityFinishing = false;

                PlayerCharacterController.Me.m_Controller.enabled = true;
                PlayerCharacterController.Me.m_Health.invincible = false;
                PlayerCharacterController.Me.stopRotate = false;
                GameManager.Me.CurrentCamera = MainCam.GetComponent<PlayerCamera>();
            }
        }
        FatalityCam.transform.localEulerAngles = new Vector3(FatalityCam.transform.localEulerAngles.x, FatalityCam.transform.localEulerAngles.y, 0);
    }
    public void ChangeA(Animator A, bool Active)
    {
        A.transform.position = A.transform.parent.position;
        foreach (MeshRenderer R in A.GetComponentsInChildren<MeshRenderer>())
        {
            R.enabled = Active;
        }
        foreach (SkinnedMeshRenderer S in A.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            S.enabled = Active;
        }
    }
    public IEnumerator WaitFatality()
    {
        yield return new WaitForSeconds(0.3f);
        CanDoFatality = true;
    }

    public void CheckFatality()
    {
        List<EnemyController> AllAisOnScreen = new List<EnemyController>();
        foreach (Actor ai in ActorsOnScreen)
        {
            if (ai.GetComponent<EnemyController>())
                AllAisOnScreen.Add(ai.GetComponent<EnemyController>());
        }
        List<EnemyController> ais = new List<EnemyController>();
        foreach (EnemyController ai in AllAisOnScreen)
        {
            if (Vector3.Distance(transform.position, ai.transform.position) <= FatalityRange)
            {
                bool p = false;
                if (FatalityAble(ai))
                    p = true;

                if (p)
                    ais.Add(ai);
            }
        }
        if (ais.Count > 0)
            PerformFatality(ais[UnityEngine.Random.Range(0, ais.Count)], 1);
    }
    public bool FatalityAble(EnemyController ai, bool includeH = true)
    {
        bool p = false;
        bool useA = false;
        if (PlayerAbilities.Me.CurrentAbility.Name != "")
        {
            if (PlayerAbilities.Me.CurrentAbility.CanFatality)
                useA = true;
        }
        else
        {
            useA = true;
        }
        if (!DoingFatality & ai.m_Health.currentHealth > 0)
        {
            if (includeH)
            {
                if (ai.FatalityReady)
                    p = true;
            }
            else
                p = true;
        }
        if (!useA)
            p = false;
        if (GameManager.Me.CurrentPlayer.Active & !InGameMenuManager.Me.menuRoot.activeSelf & !GameManager.Me.CurrentPlayer.DrivingScript.CurrentVehicle & !PlayerEffectsManager.Me.CurrentConsumable & !PlayerCharacterController.Me.m_grapplingGun.IsGrappling())
            return p;
        else
            return false;
    }
    public IEnumerator FatalitySound(float Time)
    {
        yield return new WaitForSeconds(Time);
        if (FatalityTarget)
            FatalityCam.GetComponent<AudioSource>().PlayOneShot(CurrentFatality.Sound);
    }
    public IEnumerator FatalityVFX(float Time)
    {
        yield return new WaitForSeconds(Time);
        if (FatalityTarget)
            if (FatalityTarget.GetComponent<AIBehaviour>())
                FatalityTarget.GetComponent<AIBehaviour>().HitVFX(-1, -1, true);
    }
    public void PerformFatality(EnemyController Enemy, int type)
    {
        StartCoroutine(WaitPerformFatality(Enemy, type));
    }
    public IEnumerator WaitPerformFatality(EnemyController Enemy, int type)
    {
        bool p = true;
        List<Fatality> CurrentList = new List<Fatality>();
        if (type == 1)
        {
            List<Fatality> AvailableList = new List<Fatality>();
            foreach (Fatality PlayerF in FatalityList)
            {
                foreach (EnemyAvailableFatality EnemyF in Enemy.AvailableFatalities)
                {
                    if (EnemyF.Name == PlayerF.Name & !Enemy.BlockedFatalities.Contains(PlayerF.Name))
                        AvailableList.Add(PlayerF);
                }
            }
            CurrentList = AvailableList;
        }
        if (type == 2)
            CurrentList = KnowckDownList;
        int Random = UnityEngine.Random.Range(0, CurrentList.Count);
        if(Random == PreviousFatality)
        {
            StartCoroutine(WaitPerformFatality(Enemy, type));
            p = false;
        }
        if (p)
        {
            BeforeW = "";
            if (PlayerWeaponsManager.CurrentWeapon)
                BeforeW = PlayerWeaponsManager.CurrentWeapon.weaponName;
            FatalityCam.transform.position = transform.position;
            FatalityFinishing = false;
            PlayerWeaponsManager.Me.HolsterWeapon();
            MainCam.GetComponent<AudioListener>().enabled = false;
            FatalityCam.GetComponent<AudioListener>().enabled = true;
            Enemy.GetComponentInChildren<Health>().invincible = true;
            yield return new WaitForSeconds(0.001f);
            DoingFatality = true;
            FatalityTarget = Enemy;

            CurrentFatality = CurrentList[Random];
            CurrentFatality.Controller.gameObject.SetActive(true);
            ShowFatality = false;
            CurrentFatality.Controller.CrossFadeInFixedTime(CurrentFatality.Name, 0.1f);
            StartCoroutine(FatalitySound(CurrentFatality.AudioLength));
            if (type == 1)
                if (CurrentFatality.HitVFXLength > 0)
                    StartCoroutine(FatalityVFX(CurrentFatality.HitVFXLength));
            yield return new WaitForSeconds(CurrentFatality.ShowLength);
            ShowFatality = true;
            yield return new WaitForSeconds(CurrentFatality.AttackLength - CurrentFatality.ShowLength);
            if (FatalityTarget)
            {
                if(type == 1)
                    FatalityTarget.PerformFatality();
                if (type == 2)
                    FatalityTarget.PerformKnockDown();
            }
            FatalityTarget = null;
            Vector3 rot = new Vector3(Enemy.transform.position.x, Enemy.transform.position.y, Enemy.transform.position.z);
            FatalityFinishing = true;
            yield return new WaitForSeconds(CurrentFatality.Length - CurrentFatality.AttackLength - CurrentFatality.ShowLength);
            StopFatality(CurrentList);
        }
    }


    public void StopFatality(List<Fatality> l = null)
    {
        AfterFatality = true;
        PreviousFatality = l.IndexOf(CurrentFatality);
        FatalityTarget = null;
        if (BeforeW != "" & BeforeW != "Holster")
            PlayerWeaponsManager.Me.CheckWeapon();
        BeforeW = "";
        if (CurrentFatality != null)
        {
            CurrentFatality.Controller.gameObject.SetActive(false);
            CurrentFatality = null;
        }
    }
    public void Interact()
    {
        RaycastHit Hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out Hit, Range, LayerMask))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * Hit.distance, Color.yellow);
            RaycastHit(Hit.transform);
            if (GrapplingGun.Me.IsGrappling())
                GrapplingGun.Me.StopGrapple();
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
        }
    }
    public void RaycastHit(Transform Target)
    {
        Collectible Collectable_Script = Target.GetComponent<Collectible>();
        if (Collectable_Script)
        {
            Collectable_Script.OnPicked(PlayerCharacterController.Me);
        }
    }
}
