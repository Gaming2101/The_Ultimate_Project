using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityStandardAssets.CrossPlatformInput;

public class UIControl : MonoBehaviour
{
    public static UIControl Me;
    public GameObject realMainMenu;
    public GameObject realPauseMenu;
    public bool isMainMenu;
    public bool canhighlight;
    public UI currentUIB;
    public GameObject mainMenu;
    public AudioSource myAudio;

    public Transform m_cursorPos;
    public Texture2D m_defaultMouse;
    public Texture2D m_newMouse;
    public Image controllerCursor;
    public Image currentControllerCursor;

    public AudioClip pressedClip;
    public AudioClip selectedClip;
    public AudioClip unSelectedClip;
    public AudioClip disabledClip;

    public GameObject currentUI;
    public GameObject previousUI;

    public float amount;
    public bool Changing;

    // Start is called before the first frame update
    void Start()
    {
        Me = this;
        canhighlight = true;
        currentUI = null;
        if (isMainMenu)
        {
            realMainMenu.gameObject.SetActive(true);
            realPauseMenu.gameObject.SetActive(false);
        }
        else
        {
            realMainMenu.gameObject.SetActive(false);
            realPauseMenu.gameObject.SetActive(true);
        }
        myAudio = GetComponent<AudioSource>();
     //   Cursor.visible = true;
     //   Cursor.lockState = CursorLockMode.None;
        foreach (TiltWindow t in GetComponentsInChildren<TiltWindow>())
        {
            if (t.name != mainMenu.name)
                t.gameObject.SetActive(false);
        }
        ChangeUI(mainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Me.currentdevice == "GamePad" & controllerCursor)
        {
            if (currentControllerCursor)
            {
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
                currentControllerCursor.transform.Translate(h, v, 0);
            }
            else
            {
                currentControllerCursor = Instantiate(controllerCursor);
                currentControllerCursor.name = controllerCursor.name;
                currentControllerCursor.transform.SetParent(transform);
                currentControllerCursor.transform.position = m_cursorPos.position;
            }
        }
        else
            if (currentControllerCursor)
            Destroy(currentControllerCursor.gameObject);
        if (m_newMouse)
            Cursor.SetCursor(m_newMouse, new Vector2(0, 0), CursorMode.Auto);
        else
            Cursor.SetCursor(m_defaultMouse, new Vector2(0, 0), CursorMode.Auto);
        if (currentUIB)
            if (!currentUIB.gameObject.activeInHierarchy)
                currentUIB = null;
        Cursor.SetCursor(m_defaultMouse, new Vector2(0, 0), CursorMode.Auto);
        if (currentUIB)
            if (!currentUIB.mouseO & GameManager.Me.currentdevice == "KeyBoard")
                currentUIB = null;
        if (!currentUIB & GameManager.Me.currentdevice == "GamePad")
            foreach (UI ui in FindObjectsOfType<UI>())
            {
                if (ui.isMain)
                    currentUIB = ui;
            }
    }

    public IEnumerator HighLightTimer()
    {
        canhighlight = false;
        yield return new WaitForSecondsRealtime(0.1f);
        canhighlight = true;
    }

    public void ChangeUI(GameObject New)
    {
        if (!Changing)
        {
            Changing = true;
            if (isMainMenu)
            {
                if (currentUI)
                    Close(currentUI, New);
                if (!currentUI || currentUI == mainMenu)
                {
                    Changing = false;
                    New.SetActive(true);
                    currentUI = New;
                    if (New != mainMenu)
                        Open(New, New);
                }
            }
            else
            {
                if (currentUI)
                    Close(currentUI, New);
                if (!currentUI)
                {
                    New.SetActive(true);
                    currentUI = New;
                    Open(New, New);
                }
            }
            foreach (UI ui in FindObjectsOfType<UI>())
            {
                ui.isPressed = false;
                ui.mouseO = false;
            }
            foreach (Scrollbar s in FindObjectsOfType<Scrollbar>())
            {
                if (s.direction == Scrollbar.Direction.BottomToTop)
                    s.value = 1;
                if (s.direction == Scrollbar.Direction.TopToBottom)
                    s.value = 0;
                if (s.direction == Scrollbar.Direction.LeftToRight)
                    s.value = 0;
                if (s.direction == Scrollbar.Direction.RightToLeft)
                    s.value = 1;
            }
        }
    }
    public void Back()
    {
        if (previousUI)
            ChangeUI(previousUI);
    }

    public void Open(GameObject Current, GameObject New)
    {
        amount = 0;
        Fix(New, amount);
        StartCoroutine(Wait(.1f, Current, New));
    }
    public void Close(GameObject Current, GameObject New)
    {
        amount = 1;
        Fix(Current, amount);
        StartCoroutine(Wait(-.1f, Current, New));
    }
    public void Fix(GameObject Current, float value)
    {
        foreach (UI i in Current.GetComponentsInChildren<UI>())
        {
            i.openAmount = value;
           // i.changeMouseO(false);
        }
        foreach (Image i in Current.GetComponentsInChildren<Image>())
        {
            i.color = new Color(i.color.r, i.color.g, i.color.b, value);
        }
        foreach (Text t in Current.GetComponentsInChildren<Text>())
        {
            t.color = new Color(t.color.r, t.color.g, t.color.b, value);
        }
        foreach (TextMeshPro t in Current.GetComponentsInChildren<TextMeshPro>())
        {
            t.color = new Color(t.color.r, t.color.g, t.color.b, value);
        }
        foreach (TextMeshProUGUI t in Current.GetComponentsInChildren<TextMeshProUGUI>())
        {
            t.color = new Color(t.color.r, t.color.g, t.color.b, value);
        }
    }
    public IEnumerator Wait(float num, GameObject Current, GameObject New)
    {
        yield return new WaitForSecondsRealtime(.01f);
        amount += num;
        Fix(Current, amount);
        canhighlight = false;
        if (num < 0)
        {
            if (amount <= 0)
            {
                Changing = false;
                previousUI = currentUI;
                Current.SetActive(false);
                currentUI = null;
                ChangeUI(New);
            }
            else
            {
                Changing = true;
                Current.SetActive(true);
                StartCoroutine(Wait(num, Current, New));
            }
        }
        if (num > 0)
        {
            foreach (UI ui in FindObjectsOfType<UI>())
            {
                if (ui.isMain & !currentUI & GameManager.Me.currentdevice == "GamePad")
                    currentUIB = ui;
            }
            currentUI = New;
            if (amount >= 1)
            {
                Changing = false;
                canhighlight = true;
                currentUI = New;
            }
            else
            {
                Changing = true;
                Current.SetActive(true);
                StartCoroutine(Wait(num, Current, New));
            }
        }
    }

    public void ChangeSaveSlot(int save)
    {
        SaveManager.Me.currentUISlot = save;
    }
    public void ChangeCurrentSave(int save)
    {
        SaveManager.Me.SaveCurrentS(save);
    }
    public void Save()
    {
        StartCoroutine(WaitSave());
    }
    public IEnumerator WaitSave()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (FindObjectOfType<SaveManager>())
            if (FindObjectOfType<SaveManager>().onSave != null)
                FindObjectOfType<SaveManager>().onSave(FindObjectOfType<SaveManager>().currentSave);
    }
}
