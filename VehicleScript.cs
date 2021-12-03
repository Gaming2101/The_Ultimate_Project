using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Cameras;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.Vehicles.Aeroplane;
using UnityStandardAssets.Vehicles.Ball;

public class VehicleScript : MonoBehaviour
{
    public int posNum = 1;
    public bool startDriver;
    public GameObject VehicleCameraPref;
    public CharacterDriving Driver;
    public NavMeshAgent c_agent;
    public Info c_myInfo;
    public WallDetector Detector;
    public List<WallDetector> Checkers = new List<WallDetector>();
    public List<Transform> ExitPos = new List<Transform>();
    public Transform CurrentCamPos;
    public Health m_Health;

    public bool HasRadio = true;

    [Header("Car")]
    public VehicleControl CarController2;

    public CarUserControl CarInput;
    public CarController CarController;
    public CarAudio CarSound;

    [Header("Plane")]
    public AeroplaneUserControl2Axis Plane2AxisInput;
    public AeroplaneUserControl4Axis Plane4AxisInput;
    public AeroplaneController PlaneController;
    public AeroplaneAudio PlaneSound;


    [Header("Destination")]
    public float s_destinationDistance = 3f;
    public float s_destinationWaitTime = 6f;
    public Transform s_destinationParent;
    public List<Transform> s_destinations;
    public bool s_randomDestination = true;
    public Transform v_destination;

    // Start is called before the first frame update
    void Start()
    {
        m_Health = GetComponent<Health>();
        c_myInfo = GetComponent<Info>();
        c_agent = GetComponent<NavMeshAgent>();

        CarController2 = GetComponent<VehicleControl>();

        CarInput = GetComponent<CarUserControl>();
        CarController = GetComponent<CarController>();
        CarSound = GetComponent<CarAudio>();

        Plane2AxisInput = GetComponent<AeroplaneUserControl2Axis>();
        Plane4AxisInput = GetComponent<AeroplaneUserControl4Axis>();
        PlaneController = GetComponent<AeroplaneController>();
        PlaneSound = GetComponent<AeroplaneAudio>();

        Detector = GetComponent<WallDetector>();
        foreach (WallDetector d in GetComponentsInChildren<WallDetector>())
        {
            Checkers.Add(d);
        }
        foreach (Transform E in GetComponentsInChildren<Transform>())
        {
            if (E.name.Contains("Exit Pos"))
                ExitPos.Add(E);
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

        if (startDriver)
            StartCoroutine(WaitSpawnDriver());
     //   StartCoroutine(Wait());
    }

    public IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.2f);
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().isKinematic = true;
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().isKinematic = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Driver)
        {
            if (CarController2)
                CarController2.ControlSound(true);
            if (Vector3.Distance(GameManager.Me.CurrentPlayer.transform.position, transform.position) <= AIManager.Me.SpawnDistance)
            {
                if (!AIManager.Me.ActiveVehicleList.Contains(this))
                    AIManager.Me.ActiveVehicleList.Add(this);
            }
            else
            {
                if (Driver)
                    Destroy(Driver.gameObject);
                Destroy(gameObject);
            }

            if (m_Health.currentHealth <= 0)
                Driver.StartExit();

            if(Driver.gameObject == GameManager.Me.CurrentPlayer.gameObject)
            {
                GetComponent<Radio>().On = true;
                if (Input.GetAxis("Radio Volume") > 0 || Input.GetAxis("Joystick Dpad Vertical") > 0)
                    GetComponent<Radio>().ChangeVolume(0.2f);
                if (Input.GetAxis("Radio Volume") < 0 || Input.GetAxis("Joystick Dpad Vertical") < 0)
                    GetComponent<Radio>().ChangeVolume(-0.2f);
            }
            else
                GetComponent<Radio>().On = false;

            if(Driver.Exiting)
                GetComponent<Radio>().On = false;
        }
        else
        {
            if (GetComponent<PoliceLights>())
                GetComponent<PoliceLights>().activeLight = false;
            if (CarController2)
                CarController2.ControlSound(false);
            if (CarSound)
                CarSound.StopSound();
            GetComponent<Radio>().On = false;
            if (Vector3.Distance(GameManager.Me.CurrentPlayer.transform.position, transform.position) <= AIManager.Me.VehicleDistance)
            {
                if (!AIManager.Me.VehicleList.Contains(this))
                    AIManager.Me.VehicleList.Add(this);
            }
            else
            {
                if (Driver)
                {
                    if (Driver.GetComponent<CompassElement>())
                        Driver.GetComponent<CompassElement>().UnRegister();
                    Destroy(Driver.gameObject);
                }
                Destroy(gameObject);
            }
        }

        Rigidbody Rigi = GetComponent<Rigidbody>();
        ChangeActive();

        bool agentActive = false;
        if (Driver)
        {
            if (Driver.GetComponent<PlayerController>() == GameManager.Me.CurrentPlayer)
            {
                Rigi.isKinematic = false;
                Rigi.drag = 0f;
                agentActive = false;
            }
            else
            {
                RaycastHit GetGround;
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out GetGround, 10000000))
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

                int p = 2;
                foreach(WallDetector d in Checkers)
                {
                    if (d.WallF)
                        if (d.WallF == GameManager.Me.CurrentPlayer.transform || d.WallF.GetComponentInParent<VehicleScript>() || d.WallF.GetComponentInParent<Actor>())
                        {
                            p = 1;
                        }
                }

                if (p == 2)
                {
                    Rigi.isKinematic = false;
                    Rigi.drag = 0f;
                    float rightSpeed = GetComponent<VehicleControl>().carSetting.LimitForwardSpeed / 6;
                    Transform rightDest = v_destination;
                    if (c_agent)
                    {
                        c_agent.stoppingDistance = 2;
                        if (Driver.GetComponent<AIBehaviour>())
                        {
                            if (Driver.GetComponent<AIBehaviour>().c_detectionModule.RightknownDetectedTarget)
                            {
                                if (Driver.GetComponent<EnemyController>().isResistance)
                                {
                                    rightSpeed = GetComponent<VehicleControl>().carSetting.LimitForwardSpeed;
                                    rightDest = Driver.GetComponent<AIBehaviour>().c_detectionModule.RightknownDetectedTarget.transform;
                                    c_agent.stoppingDistance = 7;
                                    if (GetComponent<PoliceLights>())
                                        GetComponent<PoliceLights>().activeLight = true;
                                }
                                else
                                {
                                    if (GetComponent<PoliceLights>())
                                        GetComponent<PoliceLights>().activeLight = false;
                                    rightSpeed = GetComponent<VehicleControl>().carSetting.LimitForwardSpeed / 2;
                                    rightDest = v_destination;
                                }
                            }
                        }
                        c_agent.speed = rightSpeed;
                        c_agent.enabled = true;
                        agentActive = true;
                        if (!rightDest)
                            p = 0;
                        if (c_agent.enabled & rightDest)
                            c_agent.SetDestination(rightDest.position);
                    }
                    if (rightDest)
                    {
                        if (s_destinations.Contains(rightDest))
                        {
                            if (Vector3.Distance(transform.position, rightDest.position) <= s_destinationDistance)
                            {
                                StartCoroutine(WaitNextDestination(v_destination, true));
                                v_destination = null;
                            }
                        }
                        else
                        {
                            bool exit = true;
                            if (rightDest.GetComponent<CharacterDriving>())
                            {
                                if (rightDest.GetComponent<CharacterDriving>().CurrentVehicle)
                                {
                                    exit = false;
                                }
                            }
                            if (Vector3.Distance(transform.position, rightDest.position) <= 20 & exit)
                            {
                                Driver.StartExit();
                            }
                        }
                    }
                }
                if (p == 0)
                {
                    Rigi.isKinematic = false;
                    agentActive = true;
                    if (c_agent.enabled)
                        c_agent.speed = 0;
                }
                if (p == 1)
                {
                    Rigi.isKinematic = true;
                    agentActive = false;
                }
            }
            if (Driver)
                if (!Driver.Exiting)
                    Driver.transform.position = ExitPos[0].position;
        }
        else
        {
            Rigi.drag = 1f;
            if (Rigi.velocity.magnitude > 5)
            {
             //   Rigi.drag = 1;
            }
            if (Rigi.velocity.magnitude <= 3)
            {
            //    Rigi.drag = 400f;
            }
            if (Rigi.velocity.magnitude <= 0)
            {
           //     Rigi.drag = 0.1f;
            }
            ChangeMotion(0, 0, 0, 0, false);
        }

        if(c_agent)
            c_agent.enabled = agentActive;
    }

    public IEnumerator WaitSpawnDriver()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnDriver();
    }

    public void SpawnDriver()
    {
        AIBehaviour ai = AIManager.Me.SpawnAI(s_destinationParent.GetComponent<PopulatedLocation>());
        ai.GetComponent<CharacterDriving>().EnterVehicle(this);
        foreach (PopulatedLocation le in AIManager.Me.LocationsParent.GetComponentsInChildren<PopulatedLocation>())
        {
            foreach (Transform de in le.GetComponentsInChildren<Transform>())
            {
                if (de != le & de.parent != AIManager.Me.LocationsParent)
                {
                    ai.s_destinationParent = de.parent;
                }
            }
        }
    }

    public void ChangeActive()
    {
        bool DriverE = false;
        if (Driver)
            DriverE = Driver.Exiting;
        bool carIActive = false;
        carIActive = Driver & !DriverE;
        if (Driver)
            if (Driver.GetComponent<PlayerController>() != GameManager.Me.CurrentPlayer)
                carIActive = false;
        if (CarController2)
            CarController2.activeControl = carIActive;
        if (CarInput)
            CarInput.Active = carIActive;
        if (Plane2AxisInput)
            Plane2AxisInput.Active = Driver & !DriverE;
     //   if (Plane4AxisInput)
           // Plane4AxisInput.Active = Driver & !DriverE;
    }
    public void ChangeMotion(float a, float b, float c, float d, bool e)
    {
        if (CarController)
            CarController.Move(a, b, c, d);
        if (PlaneController)
            PlaneController.Move(a, b, c, d, e);
    }
    public void Enter()
    {
        ChangeMotion(0, 0, -0.01f, -0.01f, false);
        if (CarSound)
        {
            CarSound.enabled = true;
            CarSound.StartSound();
        }
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
        int rightNum = destinationList.IndexOf(destination);
        if (rightNum >= destinationList.Count - 1)
        {
            if (destinationList.Count == 1)
            {
                v_destination = null;
            }
            else
            {
                v_destination = destinationList[0].transform;
            }
            p = false;
        }
        if (p)
            foreach (Transform t in destinationList)
            {
                rightNum = destinationList.IndexOf(destination);
                rightNum += 1;
                if (destinationList.IndexOf(t) == rightNum)
                {
                    v_destination = t;
                }
            }
    }
}