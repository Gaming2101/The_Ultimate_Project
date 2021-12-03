using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerController : MonoBehaviour
{
    public bool StartPlayer;
    public bool IsPlayer;
    public bool Active = true;
    public bool isSitting;
    public bool isZiplining;
    public bool isGrappling;
    public MechController CurrentMech;
    public PlayerCamera MyCam;
    public CharacterDriving DrivingScript;
    public PlayerCharacterController FPS;

    public CharacterController Controller;
    public List<Collider> Colliders = new List<Collider>();
    public List<Collider> NoColliders = new List<Collider>();

    [Header("Inputs")]
    public PlayerInputHandler FPSController;

    // Start is called before the first frame update
    void Start()
    {
        if (FindObjectOfType<GameManager>() & IsPlayer)
            FindObjectOfType<GameManager>().CurrentPlayer = this;
        foreach (Collider C in GetComponentsInChildren<Collider>())
        {
            if (C != GetComponent<CharacterController>() & !NoColliders.Contains(C))
                Colliders.Add(C);
        }
        Controller = GetComponent<CharacterController>();
        FPS = GetComponent<PlayerCharacterController>();
        DrivingScript = GetComponent<CharacterDriving>();
        MyCam = GetComponentInChildren<PlayerCamera>();
        FPSController = GetComponentInChildren<PlayerInputHandler>();

        if (StartPlayer)
            FindObjectOfType<GameManager>().CurrentCamera = MyCam;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Me.CurrentPlayer == this)
        {
            IsPlayer = true;
        }

        if (IsPlayer & UIControl.Me.isMainMenu)
            Active = false;
        if (IsPlayer & FPSPlayerCamera.Me.DoingFatality)
            Active = false;
        if (IsPlayer & FPSPlayerCamera.Me.AfterFatality)
            Active = false;
        if (Controller)
            Controller.enabled = Active & IsPlayer;
        if (FPSController)
            FPSController.Active = Active & IsPlayer;
        if (!Active & !FPSPlayerCamera.Me.DoingFatality & !FPSPlayerCamera.Me.AfterFatality & !InGameMenuManager.Me.menuRoot.activeSelf & GetComponent<PlayerCharacterController>())
            GetComponent<PlayerCharacterController>().m_CameraVerticalAngle = 0;
        foreach (Collider C in Colliders)
        {
            C.enabled = Active;
        }
    }
}
