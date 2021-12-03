using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.Cameras;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Audio;

[System.Serializable]
public class TerrainSpawnerData
{
    public string Name;
    public Transform Pos;
    public Terrain TerrainPref;
    public Terrain CurrentTerrain;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Me;
    public AudioMixerGroup VehicleGroup;
    public TerrainSpawner TerrainSpawnerPref;
    public List<Radio> Radios = new List<Radio>();
    public List<ProjectileStandard> Projectiles = new List<ProjectileStandard>();
    public List<Damageable> Damageables = new List<Damageable>();
    public List<Camera> Cameras = new List<Camera>();
    public string lastinput;
    public string currentdevice;
    public bool SaveScene;
    public bool InCombat;
    public List<AIBehaviour> EngangedAIs = new List<AIBehaviour>();
    public List<AIBehaviour> FleeingAIs = new List<AIBehaviour>();
    public PlayerController PlayerPref;
    public Transform uiPref;
    public GrapplingGun GrapplingGunPref;
    public Transform SpawnPoint;
    public Transform TerrainParent;

    public PlayerController CurrentPlayer;
    public PlayerCamera CurrentCamera;

    public Transform EffectsParent;

    public bool CanEnter;
    public List<TerrainSpawnerData> TerrainSpawners = new List<TerrainSpawnerData>();
    public List<Terrain> Terrains = new List<Terrain>();
    public List<GameObject> Details = new List<GameObject>();

    public MySetting showGraphicContent;
    public MySetting displayMode;
    public MySetting resolution;

    public MySetting quality;
    public MySetting lightingQuality;
    public MySetting antiAliasing;
    public MySetting antiAliasingQuality;
    public MySetting colourGrading;
    public MySetting ambientOcclusion;
    public MySetting screenSpaceReflections;
    public MySetting vignette;
    public MySetting grain;
    public MySetting chromaticAberration;
    public MySetting bloom;

    public MySetting terrainDistance;
    public MySetting treeDistance;
    public MySetting treeDensity;
    public MySetting detailDistance;
    public MySetting grassDistance;
    public MySetting grassDensity;
    public MySetting BillboardStart;

    // Start is called before the first frame update
    void Start()
    {
        Me = this;
        CanEnter = true;
        currentdevice = "KeyBoard";
        EffectsParent = new GameObject().transform;
        EffectsParent.name = "Effects";



        showGraphicContent = FindObjectOfType<SettingsManager>().FindSetting("Show Graphic Content");
        displayMode = FindObjectOfType<SettingsManager>().FindSetting("Display Mode");
        resolution = FindObjectOfType<SettingsManager>().FindSetting("Resolution");

        quality = FindObjectOfType<SettingsManager>().FindSetting("Quality");
        lightingQuality = FindObjectOfType<SettingsManager>().FindSetting("Lighting Quality");
        antiAliasing = FindObjectOfType<SettingsManager>().FindSetting("Anti-Aliasing");
        antiAliasingQuality = FindObjectOfType<SettingsManager>().FindSetting("Anti-Aliasing Quality");
        colourGrading = FindObjectOfType<SettingsManager>().FindSetting("Colour Grading");
        ambientOcclusion = FindObjectOfType<SettingsManager>().FindSetting("Ambient Occlusion");
        screenSpaceReflections = FindObjectOfType<SettingsManager>().FindSetting("Screen Space Reflections");
        vignette = FindObjectOfType<SettingsManager>().FindSetting("Vignette");
        grain = FindObjectOfType<SettingsManager>().FindSetting("Grain");
        chromaticAberration = FindObjectOfType<SettingsManager>().FindSetting("Chromatic Aberration");
        bloom = FindObjectOfType<SettingsManager>().FindSetting("Bloom");

        terrainDistance = FindObjectOfType<SettingsManager>().FindSetting("Terrain Distance");
        treeDistance = FindObjectOfType<SettingsManager>().FindSetting("Tree Distance");
        treeDensity = FindObjectOfType<SettingsManager>().FindSetting("Tree Density");
        detailDistance = FindObjectOfType<SettingsManager>().FindSetting("Detail Distance");
        grassDistance = FindObjectOfType<SettingsManager>().FindSetting("Grass Distance");
        grassDensity = FindObjectOfType<SettingsManager>().FindSetting("Grass Density");
        BillboardStart = FindObjectOfType<SettingsManager>().FindSetting("Billboard Start");

        GameObject P = null;
        foreach (Transform T in FindObjectsOfType<Transform>())
        {
            if (T.name == "Player")
            {
                P = T.gameObject;
            }
            if (T.name == "Player Spawn Point")
            {
                SpawnPoint = T;
            }
        }
        if (!P)
        {
            if (SpawnPoint)
            {
             //   PlayerController PE = Instantiate(PlayerPref, SpawnPoint.position, SpawnPoint.localRotation);
             //   PE.name = PlayerPref.name;
              //  PE.Active = true;
             //   CurrentPlayer = PE;
             //   CurrentCamera = PE.GetComponentInChildren<PlayerCamera>();
            }
        }
        if (P & SpawnPoint)
        {
            StartCoroutine(WaitSpawn(P));
            P.transform.position = SpawnPoint.position;
            P.transform.localRotation = SpawnPoint.localRotation;
        }

        TerrainSpawners.Clear();
        foreach (TerrainSpawner t in FindObjectsOfType<TerrainSpawner>())
        {
            TerrainSpawnerData d = new TerrainSpawnerData();
            d.Name = t.name;
            d.TerrainPref = t.Terrain;
            d.Pos = t.transform;
            TerrainSpawners.Add(d);
            if (!t.Terrain.transform.GetComponent<TerrainTool>())
                Destroy(t);
        }

        foreach (Transform t in FindObjectsOfType<Transform>())
        {
            if (t.name == "Terrain" & !t.GetComponent<Terrain>())
                TerrainParent = t;
        }

        Terrains.Clear();
        foreach (Terrain t in FindObjectsOfType<Terrain>())
        {
            Terrains.Add(t);
        }
        Details.Clear();
        foreach (DynamicObject t in FindObjectsOfType<DynamicObject>())
        {
            Details.Add(t.gameObject);
            if (t.Disregard)
                Destroy(t);
        }
        if (FindObjectOfType<UIControl>())
            if (FindObjectOfType<UIControl>().isMainMenu)
                StartCoroutine(Wait());
        StartCoroutine(WaitDetectDamage());
    }

    public IEnumerator Wait()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yield return new WaitForSeconds(3f);
      //  Cursor.lockState = CursorLockMode.Locked;
      //  Cursor.visible = false;
    }

    public IEnumerator WaitSpawn(GameObject P)
    {
        yield return new WaitForSeconds(0.0000001f);
        P.transform.position = SpawnPoint.position;
        P.transform.localRotation = SpawnPoint.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        bool pe = false;

        foreach (Camera c in Cameras)
        {
            if (!c)
            {
                Cameras.Remove(c);
                return;
            }
        }

        foreach (Radio r in Radios)
        {
            if (!r)
            {
                Radios.Remove(r);
                return;
            }
        }
        foreach (ProjectileStandard p in Projectiles)
        {
            if (!p)
            {
                Projectiles.Remove(p);
                return;
            }
        }
        foreach (Damageable d in Damageables)
        {
            if (!d)
            {
                Damageables.Remove(d);
                return;
            }
        }

        if (displayMode.Value == 0)
            Screen.fullScreenMode = FullScreenMode.Windowed;
        if (displayMode.Value == 1)
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

        if (resolution.Value == 0)
            Screen.SetResolution(1280, 720, displayMode.Value == 1);
        if (resolution.Value == 1)
            Screen.SetResolution(1920, 1080, displayMode.Value == 1);
        if (resolution.Value == 2)
            Screen.SetResolution(2560, 1440, displayMode.Value == 1);
        if (resolution.Value == 3)
            Screen.SetResolution(3840, 2160, displayMode.Value == 1);

        QualitySettings.SetQualityLevel((int)quality.Value);

        foreach (Camera c in Cameras)
        {
            if (CurrentCamera.transform == c.transform & c.GetComponent<PostProcessLayer>())
            {
                FixCamera(c.GetComponent<Camera>());
            }
        }
        foreach (Camera c in Cameras)
        {
            if (c.GetComponent<PostProcessVolume>())
            {
                FixCamera(c.GetComponent<Camera>());
            }
        }

        if (CurrentPlayer)
            foreach (TerrainSpawnerData d in TerrainSpawners)
            {
                if (Vector3.Distance(d.Pos.position, CurrentPlayer.transform.position) <= terrainDistance.Value)
                {
                    if (!d.CurrentTerrain)
                    {
                        d.CurrentTerrain = Instantiate(d.TerrainPref);
                        d.CurrentTerrain.name = d.TerrainPref.name;
                        d.CurrentTerrain.transform.position = d.Pos.transform.position;
                        d.CurrentTerrain.transform.SetParent(TerrainParent);
                    }
                }
                else
                {
                    if (d.CurrentTerrain)
                    {
                        Destroy(d.CurrentTerrain.gameObject);
                        d.CurrentTerrain = null;
                    }
                }
                if (d.CurrentTerrain)
                {
                    d.CurrentTerrain.treeDistance = treeDistance.Value;
                    d.CurrentTerrain.treeMaximumFullLODCount = (int)treeDensity.Value;
                    d.CurrentTerrain.detailObjectDistance = grassDistance.Value;
                    d.CurrentTerrain.detailObjectDensity = grassDensity.Value;
                    d.CurrentTerrain.treeBillboardDistance = BillboardStart.Value;
                }
            }
        if (CurrentPlayer)
            foreach (GameObject t in Details)
            {
                if (Vector3.Distance(t.transform.position, CurrentPlayer.transform.position) <= detailDistance.Value)
                    t.gameObject.SetActive(true);
                else
                    t.gameObject.SetActive(false);
            }

        bool input = true;
        if (input)
        {
            // doesn't clear last input if no key is pressed
            if (Input.inputString != "")
            {
                lastinput = Input.inputString;
            }
            // tests for keyboard input
            if (Input.anyKeyDown)
                currentdevice = "KeyBoard";
            // tests for mouse input
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                lastinput = "mouse";
                currentdevice = "KeyBoard";
            }
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                lastinput = "mouse scroll wheel";
                currentdevice = "KeyBoard";
            }
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                lastinput = "mouse left click";
                currentdevice = "KeyBoard";
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                lastinput = "mouse right click";
                currentdevice = "KeyBoard";
            }
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                lastinput = "mouse middle button";
                currentdevice = "KeyBoard";
            }


            // tests for all joystick buttons
            if (Input.GetKeyDown("joystick button 0"))
            {
                lastinput = "joystick button 0";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 1"))
            {
                lastinput = "joystick button 1";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 2"))
            {
                lastinput = "joystick button 2";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 3"))
            {
                lastinput = "joystick button ";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 3"))
            {
                lastinput = "joystick button ";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 4"))
            {
                lastinput = "joystick button 4";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 5"))
            {
                lastinput = "joystick button 5";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 6"))
            {
                lastinput = "joystick button 6";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 7"))
            {
                lastinput = "joystick button 7";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 8"))
            {
                lastinput = "joystick button 8";
                currentdevice = "GamePad";
            }
            if (Input.GetKeyDown("joystick button 9"))
            {
                lastinput = "joystick button 9";
                currentdevice = "GamePad";
            }

            // tests for all joystick axis'
            if (Input.GetAxis("Joystick Left Trigger") != 0)
            {
                lastinput = "joystick axis lt";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Right Trigger") != 0)
            {
                lastinput = "joystick axis rt";
                currentdevice = "GamePad";
            }

            if (Input.GetAxis("Joystick Left Stick Horizontal") != 0)
            {
                lastinput = "joystick axis ls";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Left Stick Vertical") != 0)
            {
                lastinput = "joystick axis ls";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Right Stick Horizontal") != 0)
            {
                lastinput = "joystick axis rs";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Right Stick Vertical") != 0)
            {
                lastinput = "joystick axis rs";
                currentdevice = "GamePad";
            }

            if (Input.GetAxis("Joystick Dpad Vertical") > 0)
            {
                lastinput = "joystick dpad up";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Dpad Horizontal") < 0)
            {
                lastinput = "joystick dpad left";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Dpad Vertical") < 0)
            {
                lastinput = "joystick dpad down";
                currentdevice = "GamePad";
            }
            if (Input.GetAxis("Joystick Dpad Horizontal") > 0)
            {
                lastinput = "joystick dpad right";
                currentdevice = "GamePad";
            }
        }

        List<AIBehaviour> EnAIs = new List<AIBehaviour>();
        List<AIBehaviour> FleeAIs = new List<AIBehaviour>();
        if (AIManager.Me)
            foreach (AIBehaviour ai in AIManager.Me.AIs)
            {
                if (ai)
                    if (!ai.v_dead)
                        if (ai.GetComponentInChildren<DetectionModule>().RightknownDetectedTarget)
                            if (ai.GetComponentInChildren<DetectionModule>().RightknownDetectedTarget == CurrentPlayer.gameObject & Vector3.Distance(ai.transform.position, CurrentPlayer.transform.position) <= AIManager.Me.SpawnDistance)
                            {
                                if (ai.CanFight())
                                    EnAIs.Add(ai);
                                else
                                    FleeAIs.Add(ai);
                            }
            }

        EngangedAIs = EnAIs;
        FleeingAIs = FleeAIs;
        if (EngangedAIs.Count > 0)
            InCombat = true;
        else
            InCombat = false;

        if (CurrentPlayer & !InGameMenuManager.Me.menuRoot.activeSelf)
            if (CurrentPlayer.DrivingScript)
            {
                bool p = false;
                if (InGameMenuManager.Me)
                {
                    if (CurrentPlayer.DrivingScript.CurrentVehicle & !InGameMenuManager.Me.menuRoot.activeSelf)
                        p = true;
                }
                else
                {
                    if (CurrentPlayer.DrivingScript.CurrentVehicle)
                        p = true;
                }
                if (p)
                {
                    if (Input.GetButtonDown("Exit Vehicle") & CanEnter)
                    {
                        CanEnter = false;
                        StartCoroutine(WaitVehicle());
                        CurrentPlayer.DrivingScript.StartExit();
                    }
                }
                else
                {

                }         
    }
}

    public void FixCamera(Camera c)
    {
        PostProcessLayer l = c.GetComponent<PostProcessLayer>();
        PostProcessVolume v = c.GetComponent<PostProcessVolume>();

        if (antiAliasing.Value == 0)
            l.antialiasingMode = PostProcessLayer.Antialiasing.None;
        if (antiAliasing.Value == 1)
            l.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        if (antiAliasing.Value == 2)
            l.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        if (antiAliasing.Value == 3)
            l.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;

        bool p = false;
        if (CurrentCamera.transform == v.transform)
        {
            p = true;
        }
        if (CurrentCamera.ClippingCam)
            if (CurrentCamera.ClippingCam.transform == v.transform)
            {
                p = true;
            }
        if (v.gameObject.name == "FPS Weapon Camera" & CurrentCamera.transform.parent == PlayerCharacterController.Me.transform)
            p = true;

        if (p)
        {
            v.isGlobal = true;

            if (colourGrading.Value == 0)
                v.profile.GetSetting<ColorGrading>().active = false;
            if (colourGrading.Value == 1)
                v.profile.GetSetting<ColorGrading>().active = true;

            if (ambientOcclusion.Value == 0)
                v.profile.GetSetting<AmbientOcclusion>().active = false;
            if (ambientOcclusion.Value == 1)
                v.profile.GetSetting<AmbientOcclusion>().active = true;

            if (screenSpaceReflections.Value == 0)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.Lower;
            if (screenSpaceReflections.Value == 1)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.Low;
            if (screenSpaceReflections.Value == 2)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.Medium;
            if (screenSpaceReflections.Value == 3)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.High;
            if (screenSpaceReflections.Value == 4)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.Higher;
            if (screenSpaceReflections.Value == 5)
                v.profile.GetSetting<ScreenSpaceReflections>().preset.value = ScreenSpaceReflectionPreset.Ultra;

            if (vignette.Value == 0)
                v.profile.GetSetting<Vignette>().active = false;
            if (vignette.Value == 1)
                v.profile.GetSetting<Vignette>().active = true;

            if (grain.Value == 0)
                v.profile.GetSetting<Grain>().active = false;
            if (grain.Value == 1)
                v.profile.GetSetting<Grain>().active = true;

            if (chromaticAberration.Value == 0)
                v.profile.GetSetting<ChromaticAberration>().active = false;
            if (chromaticAberration.Value == 1)
                v.profile.GetSetting<ChromaticAberration>().active = true;

            if (bloom.Value == 0)
                v.profile.GetSetting<Bloom>().active = false;
            if (bloom.Value == 1)
                v.profile.GetSetting<Bloom>().active = true;
        }
        else
        {
            v.isGlobal = false;
        }
    }

    public IEnumerator WaitVehicle()
    {
        yield return new WaitForSeconds(0.1f);
        CanEnter = true;
    }

    public IEnumerator WaitDetectDamage()
    {
        yield return new WaitForSeconds(0.08f);
        StartCoroutine(WaitDetectDamage());
        foreach (Damageable d in Damageables)
        {
            if (d)
            {
                bool p = true;
                if (d.GetComponentInParent<PlayerCharacterController>())
                {
                    if (PlayerCharacterController.Me.m_grapplingGun.IsGrappling())
                        p = false;
                }
                if (p)
                    foreach (Rigidbody r in FindObjectsOfType<Rigidbody>())
                    {
                        if (r)
                        {
                            if(r.transform != d.transform)
                            {
                                
                                bool pe = true;
                                if (r.transform == PlayerCharacterController.Me.transform & PlayerCharacterController.Me.m_grapplingGun.IsGrappling())
                                    pe = false;
                                if (r.transform == PlayerCharacterController.Me.m_grapplingGun.GrappleObject)
                                    pe = false;
                                if (pe)
                                    if (Vector3.Distance(d.transform.position, r.transform.position) <= 7)
                                        d.CollisionDamage(r.transform);
                            }
                        }
                    }
            }
        }
    }
}
