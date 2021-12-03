using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class AIHit
{
    public string Name;
    public float Length;
}

public class AIBehaviour : MonoBehaviour
{
    public NavMeshAgent c_agent;
    public EnemyController c_enemyController;
    public DetectionModule c_detectionModule;
    public Transform CurrentModel;
    public Animator c_ani;
    public List<Animator> c_models = new List<Animator>();
    public CharacterDriving c_drivingScript;
    public Info c_myInfo;
    public ParticleSystem c_alertFX;
    public List<ParticleSystem> c_angryFXs = new List<ParticleSystem>();
    public List<ParticleSystem> c_hitFXs = new List<ParticleSystem>();
    public ParticleSystem c_deadFX;
    public WallDetector c_wallD;
    public AudioSource c_moveAudio;
    public Transform c_midAttackPos;
    public Transform c_lowAttackPos;
    public Transform c_fowardPos;
    public Transform c_backPos;
    public Transform c_leftPos;
    public Transform c_rightPos;

    [Header("Animations")]
    public string s_walkAni = "Walk";
    public string s_runAni = "Run";
    public string s_moveBackAni = "Move Backward";
    public string s_idle = "Idle";
    public string s_combatStance = "Combat Stance";
    public bool s_overlapHit = true;
    public float s_deathAniT;

    [Header("Combat")]
    public float s_hesitation = 4f;
    public bool s_randomHesitation = true;
    public float s_backOffDistance = 3f;
    public float s_attackDistanceOffset = 1;
    public WeaponController s_currentWeapon;
    public bool s_canStrafeWhileFollowing = true;
    public float s_moveHesitation = 5f;
    public bool s_randomMoveHesitation = true;
    public float s_detectAniLength = 1f;
    public float s_KnockDownTime = 3f;

    [Header("Aggression")]
    public bool s_canAggro = true;
    public float s_aggroSpeed = 7;
    public float s_aggroChargeMinTime = 6;
    public float s_aggroChargeMaxTime = 13;
    public float s_aggroMinTime = 7;
    public float s_aggroMaxTime = 15;

    [Header("Stats")]
    public float s_targetTimeOut = 12;
    public float s_dangerTimeOut = 50;

    [Header("Destination")]
    public float s_destinationDistance = 3f;
    public float s_destinationWaitTime = 6f;
    public Transform s_destinationParent;
    public List<Transform> s_destinations;
    public bool s_randomDestination = true;

    public enum AIState {Idle, Traveling, Patrolling, Following, MovingToTarget, InDanger, CombatStance, Driving, MoveForward, MoveBackward, MoveLeft, MoveRight, Attack, Hit, OnAlert, Grappled, FixGrapple, KnockedDown, Dead}
    public AIState v_aiState;
    public int v_movementType;
    public float v_moveSpeed;
    public bool v_crouched;
    public string v_lastAttackName;
    public bool v_spawning;

    public List<WeaponController> v_weapons = new List<WeaponController>();
    public string v_lastModel;

    public bool v_startCombatStance;

    public bool v_isKnockedDown;
    public bool v_KnockedDownAni;

    public bool v_dead;
    public bool v_deadAni;
    public bool v_doneDeadAni;

    public bool v_startWalk;
    public bool v_startRun;

    public bool v_moveWait;
    public string v_moveDirection;

    public bool v_isAlerted;

    public bool v_hitWait;
    public string v_hitName;

    public bool v_attackWait;
    public string v_attackName;

    public bool v_aggroWait;
    public bool v_isAggro;

    public bool v_isGrappled;
    public bool v_isPulled;
    public bool v_isTethered;
    public bool v_isFixGrapple;

    public Transform v_followingNPC;
    public Transform v_destination;

    public void BeforeStart()
    {
        v_weapons.Clear();
        c_myInfo = GetComponent<Info>();
        if (c_myInfo.ModelName == "")
            c_myInfo.ModelName = GetComponentInChildren<Animator>().name;
        foreach (Transform m in GetComponentsInChildren<Transform>())
        {
            if (m.transform.parent)
                if (m.transform.parent.name == "Models")
                {
                    c_models.Add(m.GetComponentInChildren<Animator>());
                }
        }
        foreach (Animator m in c_models)
        {
            m.gameObject.SetActive(false);
        }
        GetComponent<EnemyController>().CheckNameT();
        foreach(WeaponController w in GetComponentsInChildren<WeaponController>())
        {
            v_weapons.Add(w);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        BeforeStart();
        v_spawning = true;
        c_agent = GetComponent<NavMeshAgent>();
        c_moveAudio = GetComponent<AudioSource>();
        c_enemyController = GetComponent<EnemyController>();
        c_detectionModule = GetComponentInChildren<DetectionModule>();
        c_drivingScript = GetComponent<CharacterDriving>();
        c_wallD = GetComponentInChildren<WallDetector>();
        c_enemyController.onDamaged += OnHit;
        c_enemyController.onDetectedTarget += OnDetectTarget;
        c_enemyController.onLostTarget += OnLostTarget;
        GetComponent<Health>().onDie += OnDeath;

        if (s_attackDistanceOffset > 0)
        {
            float r = Random.Range(0, s_attackDistanceOffset);
            c_detectionModule.attackRange += r;
        }

        if (s_destinationParent)
        {
            foreach (Transform t in s_destinationParent.GetComponentsInChildren<Transform>())
            {
                if (t != s_destinationParent)
                    s_destinations.Add(t);
            }
            foreach (Transform t in s_destinations)
            {
                if (t == s_destinationParent)
                    s_destinations.Remove(t);
            }
        }           
        if (s_randomDestination & s_destinations.Count > 0)
        {
            int pick = Random.Range(0, s_destinations.Count - 1);
            v_destination = s_destinations[pick].transform;
        }

        Transform Poses = Instantiate(FindObjectOfType<AIManager>().PosesPref);
        Poses.transform.position = transform.position;
        Poses.SetParent(transform);
        Poses.name = "Poses";
        foreach(Transform p in Poses.GetComponentsInChildren<Transform>())
        {
            if (p != Poses)
            {
                if (p.name == "Forward Pos")
                    c_fowardPos = p;
                if (p.name == "Back Pos")
                    c_backPos = p;
                if (p.name == "Left Pos")
                    c_leftPos = p;
                if (p.name == "Right Pos")
                    c_rightPos = p;

                if (p.name == "Attack Mid Pos")
                    c_midAttackPos = p;
                if (p.name == "Attack Low Pos")
                    c_lowAttackPos = p;
            }
        }

        foreach (WeaponController t in GetComponentsInChildren<WeaponController>())
        {
            if (t.name == name)
                s_currentWeapon = t;
        }

        StartCoroutine(AfterSpawn());
    }

    // Update is called once per frame
    void Update()
    {
        bool pe = true;
        if (pe)
        {
            bool isFatality = false;
            if (!v_dead & c_ani)
                if (FPSPlayerCamera.Me.FatalityTarget)
                    if (FPSPlayerCamera.Me.FatalityTarget.transform == transform)
                    {
                        isFatality = true;
                        DoFatalityPos();
                    }

            if (c_drivingScript)
            {
                if (!c_drivingScript.CurrentVehicle & GetComponent<EnemyController>().isOpenWorld)
                {
                    if (Vector3.Distance(GameManager.Me.CurrentPlayer.transform.position, transform.position) <= AIManager.Me.SpawnDistance)
                    {
                        if (!AIManager.Me.AIList.Contains(this))
                            AIManager.Me.AIList.Add(this);
                    }
                    else
                    {
                        if (GetComponent<CompassElement>())
                            GetComponent<CompassElement>().UnRegister();
                        Destroy(gameObject);
                        return;
                    }
                }
                if (!AIManager.Me.AIs.Contains(this))
                    AIManager.Me.AIs.Add(this);
            }

            if (c_myInfo.ModelName != v_lastModel)
            {
                c_alertFX = null;
                c_angryFXs.Clear();
                c_hitFXs.Clear();
                c_deadFX = null;

                foreach (Animator m in c_models)
                {
                    if (m.name == c_myInfo.ModelName & !c_drivingScript.CurrentVehicle)
                    {
                        m.gameObject.SetActive(true);
                        CurrentModel = m.transform;
                        c_ani = CurrentModel.GetComponentInChildren<Animator>();
                        if (!c_alertFX & c_enemyController.c_alertFXName != "")
                            foreach (ParticleSystem fx in CurrentModel.GetComponentsInChildren<ParticleSystem>())
                            {
                                if (fx.name == c_enemyController.c_alertFXName)
                                    c_alertFX = fx;
                            }
                        if (c_angryFXs.Count == 0 & c_enemyController.c_angryFXName != "")
                            foreach (ParticleSystem fx in CurrentModel.GetComponentsInChildren<ParticleSystem>())
                            {
                                if (fx.name == c_enemyController.c_angryFXName)
                                    c_angryFXs.Add(fx);
                            }
                        if (c_hitFXs.Count == 0 & c_enemyController.c_hitFXName != "")
                            foreach (ParticleSystem fx in CurrentModel.GetComponentsInChildren<ParticleSystem>())
                            {
                                if (fx.name == c_enemyController.c_hitFXName)
                                    c_hitFXs.Add(fx);
                            }
                        if (!c_deadFX)
                            foreach (ParticleSystem fx in CurrentModel.GetComponentsInChildren<ParticleSystem>())
                            {
                                if (fx.name == "Death VFX")
                                    c_deadFX = fx;
                            }
                    }
                    else
                    {
                        m.gameObject.SetActive(false);
                    }
                }
                foreach (Damageable d in GetComponentsInChildren<Damageable>())
                {
                    foreach (WeaponD s in c_myInfo.Weaknessess)
                    {
                        bool p = true;
                        foreach (WeaponD w in d.WeaponAffects)
                        {
                            if (s.Name == w.Name)
                                p = false;
                        }
                        if (p)
                            d.WeaponAffects.Add(s);
                    }
                }
            }
            v_lastModel = c_myInfo.ModelName;

            if (v_movementType == 0)
                v_moveSpeed = 0;
            if (v_movementType == 1)
                v_moveSpeed = c_myInfo.WalkSpeed;
            if (v_movementType == 2)
            {
                if (!v_isAggro)
                    v_moveSpeed = c_myInfo.RunSpeed;
                else
                    v_moveSpeed = c_myInfo.RunSpeed + s_aggroSpeed;
            }

            if (GetComponent<SpringJoint>())
                v_isTethered = true;
            else
                v_isTethered = false;

            if (c_ani)
            {
                if(!isFatality)
                {
                    c_ani.transform.position = c_ani.transform.parent.transform.position;
                    c_ani.transform.rotation = c_ani.transform.parent.transform.rotation;
                    c_ani.SetBool("Dead", v_dead);
                    c_ani.SetInteger("Movement Type", v_movementType);
                    c_ani.SetBool("Alerted", c_enemyController.knownDetectedTarget);
                    c_ani.SetBool("Target In Range", c_detectionModule.isTargetInAttackRange);
                    c_ani.SetBool("Crouched", v_crouched);
                }
                if (v_hitName == "")
                    c_ani.SetInteger("Hit Number", 0);
                else
                    c_ani.SetInteger("Hit Number", 1);
                if (v_attackName == "")
                    c_ani.SetInteger("Attack Number", 0);
                else
                    c_ani.SetInteger("Attack Number", 1);
            }
            if (v_aiState != AIState.Grappled)
                GetComponent<Rigidbody>().isKinematic = true;
            else
            {
                if (v_isGrappled)
                    GetComponent<Rigidbody>().isKinematic = true;
                if (v_isPulled || v_isTethered)
                    GetComponent<Rigidbody>().isKinematic = false;
            }

            if (c_ani)
            {
                if (!v_dead)
                {
                    if(Time.timeScale > 0)
                    {
                        RaycastHit GetGround;
                        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out GetGround, 10000000) & PlayerCharacterController.Me.m_grapplingGun.GrappleObject != transform & v_aiState != AIState.Grappled)
                        {
                            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * GetGround.distance, Color.yellow);
                            if (GetGround.transform)
                                if (GetGround.transform != transform)
                                {
                                    //    c_agent.enabled = false;
                                    transform.position = GetGround.point;
                                    // c_agent.enabled = true;
                                }
                        }
                    }
                    c_agent.speed = v_moveSpeed;
                    c_agent.stoppingDistance = s_destinationDistance;

                    if (v_isAggro)
                    {
                        if (c_angryFXs.Count > 0)
                        {
                            ParticleSystem currentFX = null;
                            foreach (ParticleSystem fx in c_angryFXs)
                            {
                                fx.gameObject.SetActive(true);
                                if (fx.isPlaying)
                                    currentFX = fx;
                            }
                            if (!currentFX)
                                c_angryFXs[Random.Range(0, c_angryFXs.Count)].Play();
                        }
                    }
                    else
                    {
                        if (c_angryFXs.Count > 0)
                        {
                            if (c_angryFXs[0].gameObject.activeSelf)
                                foreach (ParticleSystem fx in c_angryFXs)
                                {
                                    fx.Clear();
                                    fx.gameObject.SetActive(false);
                                }
                        }
                    }

                    c_enemyController.m_Health.maxHealth = c_myInfo.maxHealth;

                    if (!v_isFixGrapple)
                    {
                        if (!c_drivingScript.CurrentVehicle)
                        {
                            if (!v_isKnockedDown)
                            {
                                if (v_isAlerted)
                                {
                                    v_aiState = AIState.OnAlert;
                                }
                                else
                                {
                                    if (c_detectionModule.RightknownDetectedTarget)
                                    {
                                        if (CanFight())
                                        {
                                            if (s_canAggro & !v_aggroWait & !v_isAggro)
                                            {
                                                v_aggroWait = true;
                                                StartCoroutine(AggroWait());
                                            }

                                            if (!s_currentWeapon)
                                                ChangeWeapon("Unarmed");
                                            c_detectionModule.knownTargetTimeout = s_targetTimeOut;
                                            if (s_currentWeapon)
                                                if (s_currentWeapon.weaponName != "Unarmed" & s_currentWeapon.weaponName != "Shield Bearer")
                                                    s_currentWeapon.transform.parent.transform.LookAt(c_detectionModule.RightknownDetectedTarget.transform.position);

                                            bool InLevel = false;
                                            if (transform.position.y == c_detectionModule.RightknownDetectedTarget.transform.position.y)
                                                InLevel = true;
                                            if (transform.position.y < c_detectionModule.RightknownDetectedTarget.transform.position.y & transform.position.y > (c_detectionModule.RightknownDetectedTarget.transform.position.y - 4))
                                                InLevel = true;
                                            if (transform.position.y > c_detectionModule.RightknownDetectedTarget.transform.position.y & transform.position.y < (c_detectionModule.RightknownDetectedTarget.transform.position.y + 4))
                                                InLevel = true;
                                            if (c_detectionModule.isTargetInAttackRange & InLevel)
                                            {
                                                if (Vector3.Distance(transform.position, c_detectionModule.RightknownDetectedTarget.transform.position) < s_backOffDistance)
                                                {
                                                    if (c_ani)
                                                    {
                                                        c_ani.SetTrigger(s_moveBackAni);
                                                        StartCoroutine(Moving("Backward", 2));
                                                    }
                                                }
                                                else
                                                {
                                                    v_aiState = AIState.CombatStance;
                                                }
                                            }
                                            else
                                            {
                                                v_moveDirection = "";
                                                v_aiState = AIState.MovingToTarget;
                                            }
                                        }
                                        else
                                        {
                                            c_detectionModule.knownTargetTimeout = s_dangerTimeOut;
                                            if (v_aiState != AIState.InDanger & s_destinations.Count > 0)
                                                v_destination = s_destinations[0].transform;
                                            v_aiState = AIState.InDanger;
                                        }
                                    }
                                    else
                                    {
                                        v_moveDirection = "";
                                        if (v_followingNPC)
                                        {
                                            v_aiState = AIState.Following;
                                        }
                                        else
                                        {
                                            if (v_destination)
                                                v_aiState = AIState.Traveling;
                                            else
                                                v_aiState = AIState.Idle;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            v_moveDirection = "";
                            v_aiState = AIState.Driving;
                        }

                        if (v_moveDirection == "Left")
                        {
                            v_aiState = AIState.MoveLeft;
                        }
                        if (v_moveDirection == "Right")
                        {
                            v_aiState = AIState.MoveRight;
                        }
                        if (v_moveDirection == "Forward")
                        {
                            v_aiState = AIState.MoveForward;
                        }
                        if (v_moveDirection == "Backward")
                        {
                            v_aiState = AIState.MoveBackward;
                        }
                        if (v_hitName != "")
                        {
                            v_aiState = AIState.Hit;
                        }
                        if (v_attackName != "")
                        {
                            v_aiState = AIState.Attack;
                        }

                        if (v_isKnockedDown)
                            v_aiState = AIState.KnockedDown;
                    }

                    if (v_isGrappled || v_isPulled || v_isTethered)
                        v_aiState = AIState.Grappled;
                    if (v_isFixGrapple)
                        v_aiState = AIState.FixGrapple;

                    if (v_aiState == AIState.Traveling & v_destination)
                        if (!AIManager.Me.CheckNavMesh(transform.position) || !AIManager.Me.CheckNavMesh(v_destination.position))
                            v_aiState = AIState.Idle;

                    if (v_aiState == AIState.InDanger & v_destination)
                        if (!AIManager.Me.CheckNavMesh(transform.position) || !AIManager.Me.CheckNavMesh(v_destination.position))
                            v_aiState = AIState.Idle;

                    if (v_aiState == AIState.MovingToTarget & c_detectionModule.RightknownDetectedTarget)
                        if (!AIManager.Me.CheckNavMesh(transform.position) || !AIManager.Me.CheckNavMesh(c_detectionModule.RightknownDetectedTarget.transform.position))
                            v_aiState = AIState.Idle;

                    if (v_movementType == 0)
                    {
                        v_startWalk = false;
                        v_startRun = false;
                        c_moveAudio.Stop();
                        if (v_aiState != AIState.Grappled)
                        {
                            c_agent.enabled = true;
                            if (c_agent.isOnNavMesh)
                                c_agent.isStopped = true;
                        }
                        else
                        {
                            if (v_isGrappled)
                            {
                                c_agent.enabled = true;
                                if (c_agent.isOnNavMesh)
                                    c_agent.isStopped = true;
                            }
                            if (v_isPulled || v_isTethered)
                            {
                                c_agent.enabled = false;
                                if (c_agent.isOnNavMesh)
                                    c_agent.isStopped = true;
                            }
                        }
                    }
                    if (v_movementType == 1)
                    {
                        v_startRun = false;
                        if (!c_moveAudio.isPlaying)
                            c_moveAudio.Play();
                    }
                    if (v_movementType == 2)
                    {
                        v_startWalk = false;
                        if (!c_moveAudio.isPlaying)
                            c_moveAudio.Play();
                    }

                    //States
                    if (v_aiState == AIState.KnockedDown)
                    {
                        v_movementType = 0;
                        v_crouched = false;
                        if (c_agent.isOnNavMesh)
                            c_agent.isStopped = true;

                        if (!v_KnockedDownAni & c_ani)
                        {
                            c_ani.CrossFadeInFixedTime("Knockdown", 0.2f);
                            v_KnockedDownAni = true;
                            StartCoroutine(WaitGetUp());
                        }
                    }

                    if (!v_isKnockedDown)
                    {
                        if (v_aiState == AIState.Driving)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            c_agent.enabled = false;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.isStopped = true;
                            }
                        }

                        if (v_aiState == AIState.Grappled)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                                c_agent.isStopped = true;
                            if (v_isPulled)
                                c_agent.enabled = false;
                            else
                                c_agent.enabled = true;
                        }
                        if (v_aiState == AIState.FixGrapple)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.enabled = true;
                                c_agent.isStopped = true;
                            }
                        }

                        if (v_aiState == AIState.InDanger & v_destination)
                        {
                            if (AIManager.Me.CheckNavMesh(transform.position) & AIManager.Me.CheckNavMesh(v_destination.position))
                            {
                                if (!v_startRun & c_ani)
                                {
                                    c_ani.CrossFade(s_runAni, 0.9f);
                                    v_startRun = true;
                                }
                                v_movementType = 2;
                                c_agent.stoppingDistance = s_destinationDistance;
                                v_crouched = false;
                                c_agent.enabled = true;
                                c_agent.isStopped = false;
                                c_agent.SetDestination(v_destination.position);
                            }
                            else
                            {
                                v_movementType = 0;
                            }
                            if (Vector3.Distance(transform.position, v_destination.position) <= s_destinationDistance)
                            {
                                StartCoroutine(WaitNextDestination(v_destination, true));
                                v_destination = null;
                            }
                        }

                        if (v_aiState == AIState.OnAlert)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.enabled = true;
                                c_agent.isStopped = true;
                            }
                        }

                        if (v_aiState == AIState.Hit)
                        {
                            if (!c_enemyController.s_canMoveWhileHit)
                            {
                                v_movementType = 0;
                                v_crouched = false;
                                if (c_agent.isOnNavMesh)
                                {
                                    c_agent.enabled = true;
                                    c_agent.isStopped = true;
                                }
                            }
                            else
                            {
                                if (c_detectionModule.RightknownDetectedTarget)
                                {
                                    if (c_detectionModule.isTargetInAttackRange)
                                    {
                                        v_movementType = 0;
                                        v_crouched = false;
                                        if (c_agent.isOnNavMesh)
                                        {
                                            c_agent.enabled = true;
                                            c_agent.isStopped = true;
                                        }
                                        Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                                    }
                                    else
                                    {
                                        v_movementType = 2;
                                        v_crouched = false;
                                        if (AIManager.Me.CheckNavMesh(transform.position))
                                        {
                                            c_agent.enabled = true;
                                        }
                                        c_agent.isStopped = false;
                                        c_agent.SetDestination(c_detectionModule.RightknownDetectedTarget.transform.position);
                                    }
                                }
                                else
                                {
                                    v_movementType = 0;
                                    v_crouched = false;
                                    if (c_agent.isOnNavMesh)
                                    {
                                        c_agent.enabled = true;
                                        c_agent.isStopped = true;
                                    }
                                }
                            }
                        }
                        if (v_aiState == AIState.Attack)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.enabled = true;
                                c_agent.isStopped = true;
                            }
                        }

                        if (v_aiState == AIState.MoveForward & c_detectionModule.RightknownDetectedTarget)
                        {
                            v_movementType = 1;
                            v_crouched = false;
                            c_agent.enabled = true;
                            Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                            transform.LookAt(pos);
                            c_agent.speed = c_myInfo.WalkSpeed;
                            c_agent.isStopped = false;
                            c_agent.SetDestination(c_fowardPos.position);
                            if (s_currentWeapon.mobileAttack & !v_attackWait & v_attackName == "")
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                        }
                        if (v_aiState == AIState.MoveBackward & c_detectionModule.RightknownDetectedTarget)
                        {
                            if (!c_detectionModule.isTargetInAttackRange)
                            {
                                v_moveDirection = "";
                                return;
                            }
                            v_movementType = 1;
                            v_crouched = false;
                            c_agent.enabled = true;
                            Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                            transform.LookAt(pos);
                            c_agent.speed = 2;
                            c_agent.isStopped = false;
                            c_agent.SetDestination(c_backPos.position);
                            if (s_currentWeapon.mobileAttack & !v_attackWait & v_attackName == "")
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                        }
                        if (v_aiState == AIState.MoveLeft & c_detectionModule.RightknownDetectedTarget)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            c_agent.enabled = true;
                            Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                            transform.LookAt(pos);
                            c_agent.enabled = true;
                            c_agent.speed = c_myInfo.WalkSpeed;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.isStopped = false;
                                c_agent.SetDestination(c_leftPos.position);
                            }
                            if (s_currentWeapon.mobileAttack & !v_attackWait & v_attackName == "")
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                        }
                        if (v_aiState == AIState.MoveRight & c_detectionModule.RightknownDetectedTarget)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            c_agent.enabled = true;
                            Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                            transform.LookAt(pos);
                            c_agent.speed = c_myInfo.WalkSpeed;
                            c_agent.isStopped = false;
                            c_agent.SetDestination(c_rightPos.position);
                            if (s_currentWeapon.mobileAttack & !v_attackWait & v_attackName == "")
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                        }

                        if (v_aiState == AIState.CombatStance)
                        {
                            v_startCombatStance = true;
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                                c_agent.enabled = true;
                            Vector3 pos = new Vector3(c_detectionModule.RightknownDetectedTarget.transform.position.x, transform.position.y, c_detectionModule.RightknownDetectedTarget.transform.position.z);
                            transform.LookAt(pos);
                            if (!v_attackWait & v_attackName == "" & c_detectionModule.isTargetInAttackRange)
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                            if (!v_moveWait & v_moveDirection == "")
                            {
                                v_moveWait = true;
                                StartCoroutine(WaitMove());
                            }
                        }
                        else
                        {
                            v_startCombatStance = false;
                        }
                        if (v_aiState == AIState.Following & v_followingNPC)
                        {
                            if (AIManager.Me.CheckNavMesh(transform.position) & AIManager.Me.CheckNavMesh(v_followingNPC.position))
                            {
                                if (Vector3.Distance(transform.position, v_followingNPC.position) <= 10)
                                {
                                    if (!v_startWalk & c_ani)
                                    {
                                        c_ani.CrossFade(s_walkAni, 0.9f);
                                        v_startWalk = true;
                                        v_startRun = false;
                                    }
                                    v_movementType = 1;
                                }
                                else
                                {
                                    if (!v_startRun & c_ani)
                                    {
                                        c_ani.CrossFade(s_runAni, 0.9f);
                                        v_startWalk = false;
                                        v_startRun = true;
                                    }
                                    v_movementType = 2;
                                }
                                c_agent.stoppingDistance = c_detectionModule.attackRange;
                                v_crouched = false;
                                c_agent.enabled = true;
                                c_agent.isStopped = false;
                                c_agent.SetDestination(v_followingNPC.position);
                            }
                            else
                            {
                                v_movementType = 0;
                            }
                        }
                        if (v_aiState == AIState.MovingToTarget)
                        {
                            if (AIManager.Me.CheckNavMesh(transform.position) & AIManager.Me.CheckNavMesh(c_detectionModule.RightknownDetectedTarget.transform.position))
                            {
                                if (!v_startRun & c_ani)
                                {
                                    c_ani.CrossFade(s_runAni, 0.3f);
                                    v_startRun = true;
                                }
                                v_movementType = 2;
                                c_agent.stoppingDistance = c_detectionModule.attackRange;
                                v_crouched = false;
                                c_agent.enabled = true;
                                if (c_agent.isOnNavMesh)
                                {
                                    c_agent.isStopped = false;
                                    c_agent.SetDestination(c_detectionModule.RightknownDetectedTarget.transform.position);
                                }
                            }
                            else
                            {
                                v_movementType = 0;
                            }
                            if (s_currentWeapon.mobileAttack & !v_attackWait & v_attackName == "" & c_detectionModule.isTargetInAttackRange)
                            {
                                v_attackWait = true;
                                StartCoroutine(WaitAttack());
                            }
                            if (!v_moveWait & v_moveDirection == "" & s_canStrafeWhileFollowing)
                            {
                                v_moveWait = true;
                                StartCoroutine(WaitMove());
                            }
                        }
                        if (v_aiState == AIState.Traveling)
                        {
                            if (v_destination)
                            {
                                if (AIManager.Me.CheckNavMesh(transform.position) & AIManager.Me.CheckNavMesh(v_destination.position))
                                {
                                    if (!v_startWalk & c_ani)
                                    {
                                        c_ani.CrossFade(s_walkAni, 0.2f);
                                        v_startWalk = true;
                                    }
                                    v_movementType = 1;
                                    v_crouched = false;
                                    c_agent.enabled = true;
                                    c_agent.isStopped = false;
                                    c_agent.SetDestination(v_destination.position);
                                }
                                else
                                {
                                    v_movementType = 0;
                                }
                                if (Vector3.Distance(transform.position, v_destination.position) <= s_destinationDistance)
                                {
                                    StartCoroutine(WaitNextDestination(v_destination, false));
                                    v_destination = null;
                                }
                            }
                            else
                            {
                                v_movementType = 0;
                            }
                        }
                        if (v_aiState == AIState.Idle)
                        {
                            v_movementType = 0;
                            v_crouched = false;
                            if (c_agent.isOnNavMesh)
                            {
                                c_agent.enabled = true;
                                c_agent.isStopped = true;
                            }
                        }
                    }
                }
                else
                {
                    if (GetComponent<CompassElement>())
                        GetComponent<CompassElement>().UnRegister();
                    if (v_aiState != AIState.Dead)
                        v_deadAni = false;
                    v_aiState = AIState.Dead;
                    if (GetComponent<CompassElement>())
                        GetComponent<CompassElement>().enabled = false;
                    if (!v_deadAni)
                    {
                        foreach (BoxCollider c in GetComponentsInChildren<BoxCollider>())
                        {
                            c.enabled = false;
                        }
                        foreach (SphereCollider c in GetComponentsInChildren<SphereCollider>())
                        {
                            c.enabled = false;
                        }
                    }
                    if (!v_deadAni & c_ani)
                    {
                        c_ani.Play("Knockdown");
                        if (c_deadFX)
                            c_deadFX.Play();
                        v_deadAni = true;
                    }
                    v_isAggro = false;
                    v_hitName = "";
                    v_attackName = "";
                    c_agent.speed = 0;
                    if (!v_doneDeadAni)
                        c_ani.enabled = true;
                    v_isKnockedDown = false;
                    v_KnockedDownAni = false;
                }
            }
        }
    }

    public void DoFatalityPos()
    {
        c_ani.transform.position = c_ani.transform.parent.transform.position;
        c_ani.transform.rotation = c_ani.transform.parent.transform.rotation;

        c_ani.SetBool("Dead", false);
        c_ani.SetInteger("Movement Type", 0);
        c_ani.SetBool("Alerted", false);
        c_ani.SetBool("Target In Range", false);
        c_ani.SetBool("Crouched", false);
    }

    public IEnumerator AfterSpawn()
    {
        yield return new WaitForSeconds(1);
        v_spawning = false;
    }

    public void KnockDown()
    {
        v_isKnockedDown = true;
    }

    public IEnumerator WaitGetUp()
    {
        yield return new WaitForSeconds(s_KnockDownTime);
        if (!v_dead)
        {
            c_ani.Play("Get up");
            yield return new WaitForSeconds(1f);
            v_isKnockedDown = false;
            v_KnockedDownAni = false;
        }
    }

    public bool CanFight()
    {
        bool CanFight = false;
        if (c_myInfo.Skills.Contains("Martial Arts"))
            CanFight = true;
        if (c_myInfo.Skills.Contains("Combat") || c_myInfo.Skills.Contains("Sword And Shield"))
            CanFight = true;

        return CanFight;
    }

    public IEnumerator WaitMove()
    {
        float random = s_moveHesitation;
        if (s_randomMoveHesitation)
            random = Random.Range(0, s_moveHesitation);
        yield return new WaitForSeconds(random);
        if (!v_dead & !v_isKnockedDown)
        {
            if (v_moveDirection == "" || v_aiState == AIState.CombatStance)
            {
                float randomTime = Random.Range(1, 3);
                int randomD = Random.Range(0, 4);
                string direction = "";
                if (randomD == 0)
                    direction = "Left";
                if (randomD == 1)
                    direction = "Right";
                if (randomD == 2)
                    direction = "Forward";
                if (randomD == 3)
                    direction = "Backward";
                bool p = true;
                if (v_aiState == AIState.MovingToTarget & s_canStrafeWhileFollowing)
                    p = false;
                if (direction == "Left" & c_wallD.WallL)
                    p = false;
                if (direction == "Right" & c_wallD.WallR)
                    p = false;
                if (direction == "Forward" & c_wallD.WallF)
                    p = false;
                if (direction == "Backward" & c_wallD.WallB)
                    p = false;
                if (c_detectionModule.RightknownDetectedTarget)
                    if (direction == "Backward" & Vector3.Distance(c_backPos.position, c_detectionModule.RightknownDetectedTarget.transform.position) >= c_detectionModule.attackRange)
                        p = false;
                if (p)
                {
                    if (v_attackName == "")
                    {
                        v_moveWait = false;
                        StartCoroutine(Moving(direction, randomTime));
                    }
                    else
                    {
                        v_moveWait = false;
                        v_moveDirection = "";
                    }
                }
            }
        }
    }
    public IEnumerator Moving(string direction, float randomTime)
    {
        if (!v_dead & !v_isKnockedDown)
        {
            v_moveDirection = direction;
            c_ani.SetTrigger("Move " + direction);
            c_ani.CrossFade("Move " + direction, 0.1f);
            yield return new WaitForSeconds(randomTime);
            if (!v_dead & !v_isKnockedDown)
            {
                v_moveWait = false;
                v_moveDirection = "";
                c_ani.CrossFade("Combat Stance", 0.1f);
            }
        }
    }

    public IEnumerator AggroWait()
    {
        float random = Random.Range(s_aggroChargeMinTime, s_aggroChargeMaxTime);
        yield return new WaitForSeconds(random);
        v_aggroWait = false;
        v_isAggro = true;
        StartCoroutine(AfterAggroWait());
    }

    public IEnumerator AfterAggroWait()
    {
        float random = Random.Range(s_aggroMinTime, s_aggroMaxTime);
        yield return new WaitForSeconds(random);
        v_isAggro = false;
    }

    public IEnumerator WaitAttack()
    {
        float random = s_hesitation;
        if (s_randomHesitation)
            random = Random.Range(0, s_hesitation);
        yield return new WaitForSeconds(random);
        s_currentWeapon.HandleShootInputs(true, true, false);
    }

    public IEnumerator WaitNextDestination(Transform destination, bool NoWait)
    {
        float random = Random.Range(0, s_destinationWaitTime);
        if (NoWait)
            random = 0;
        yield return new WaitForSeconds(random);
        NextDestination(destination, s_destinations);
    }
    public void NextDestination(Transform destination, List<Transform> destinationList)
    {
        bool p = true;
        List<Transform> rightDestinations = new List<Transform>();
        foreach (Transform t in destinationList)
        {
            if (AIManager.Me.CheckNavMesh(t.position))
            {
                rightDestinations.Add(t);
            }
        }
        int rightNum = rightDestinations.IndexOf(destination);
        if (rightNum >= rightDestinations.Count - 1)
        {
            if (rightDestinations.Count == 1)
            {
                v_destination = null;
            }
            else
            {
                v_destination = rightDestinations[0].transform;
            }
            p = false;
        }
        if (p)
            foreach (Transform t in rightDestinations)
            {
                if (p)
                {
                    rightNum = rightDestinations.IndexOf(destination);
                    rightNum += 1;
                    if (rightDestinations.IndexOf(t) == rightNum)
                    {
                        v_destination = t;
                        p = false;
                    }
                }
            }
    }

    public void OnHit()
    {
        if (v_hitName == "" || s_overlapHit || FPSPlayerCamera.Me.FatalityTarget == c_enemyController)
            HitVFX(-1, -1);
        CheckForWitnesses();
    }

    public void CheckForWitnesses()
    {
        foreach (AIBehaviour ai in AIManager.Me.AIs)
        {
            if (ai)
                if (ai != this)
                    if (ai & c_detectionModule)
                    {
                        if (ai.c_detectionModule)
                        {
                            if (c_detectionModule.RightknownDetectedTarget & !ai.c_detectionModule.RightknownDetectedTarget)
                            {
                                if (Vector3.Distance(transform.position, ai.transform.position) <= 100)
                                {
                                    ai.c_detectionModule.knownDetectedTarget = c_detectionModule.knownDetectedTarget;
                                    if (ResistanceManager.Me.wantelLevel == 0 & ResistanceManager.Me.CurrentResistance())
                                        ResistanceManager.Me.ChangeWantedLevel(1);
                                }
                            }
                        }
                    }
        }
    }

    public IEnumerator BeingHit(float hitLength)
    {
        yield return new WaitForSeconds(hitLength);
        s_currentWeapon.weaponRoot.SetActive(true);
        v_hitName = "";
    }

    public void HitVFX(int aniNumber, int fvxNumber, bool ignoreUseAni = false)
    {
        if (c_hitFXs.Count > 0)
        {
            int num = fvxNumber;
            if (fvxNumber == -1)
                num = Random.Range(0, c_hitFXs.Count);
            c_hitFXs[num].Play();
        }
        Stagger(aniNumber, ignoreUseAni);
    }

    public void Stagger(int aniNumber = -1, bool ignoreUseAni = false)
    {
        if (!v_isKnockedDown)
            if (c_enemyController.s_useHitAni || ignoreUseAni)
            {
                int num = aniNumber;
                if (aniNumber == -1)
                    num = Random.Range(0, c_enemyController.s_hits.Count);
                v_hitName = c_enemyController.s_hits[num].Name;
                float hitlength = c_enemyController.s_hits[num].Length;
                if (c_ani)
                    c_ani.CrossFade(c_enemyController.s_hits[num].Name, 0.1f);
                s_currentWeapon.weaponRoot.SetActive(false);

                StartCoroutine(BeingHit(hitlength));
            }
    }

    public void PerformFatality()
    {
        if (c_hitFXs.Count > 0)
            c_hitFXs[Random.Range(0, c_hitFXs.Count)].Play();
    }

    public void OnDetectTarget()
    {
        if (!v_dead)
        {
            if (c_alertFX)
            {
                c_alertFX.gameObject.SetActive(true);
                c_alertFX.Play();
            }
            if (!v_isKnockedDown)
                foreach (Animator a in GetComponentsInChildren<Animator>())
                {
                    a.CrossFadeInFixedTime("Alerted", 0.2f);
                }
            StartCoroutine(WaitDetect());
        }
    }
    public IEnumerator WaitDetect()
    {
        float fxTime = 0f;
        if (c_alertFX)
        {
            fxTime = 1.0f;
            yield return new WaitForSeconds(fxTime);
            c_alertFX.gameObject.SetActive(false);
        }
        v_isAlerted = true;
        yield return new WaitForSeconds(s_detectAniLength - fxTime);
        v_isAlerted = false;
    }
    public void OnLostTarget()
    {

    }
    public void ChangeWeapon(string name)
    {
        foreach(WeaponController w in v_weapons)
        {
            if(w.name == name)
            {
                s_currentWeapon = w;
            }
        }
    }
    public void OnDeath()
    {
        if (PlayerWeaponsManager.CurrentWeapon & v_aiState == AIState.InDanger)
            PlayerWeaponsManager.Me.RefillWeapon(PlayerWeaponsManager.CurrentWeapon.m_CurrentAmmo, PlayerWeaponsManager.CurrentWeapon.maxAmmo, PlayerWeaponsManager.CurrentWeapon.ammoGainedOnFatality);
        v_dead = true;
        CheckForWitnesses();
        if (s_deathAniT > 0)
            StartCoroutine(WaitDeathAni());
    }

    public IEnumerator WaitDeathAni()
    {
        yield return new WaitForSeconds(s_deathAniT);
        v_doneDeadAni = true;
        c_ani.enabled = false;
    }
}
