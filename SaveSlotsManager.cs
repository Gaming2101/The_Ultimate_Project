using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotsManager : MonoBehaviour
{
    public GameObject content;
    public List<Transform> slotInfo = new List<Transform>();
    public GameObject slotDeletion;
    public Text slotDeletionName;
    public Text deleteSlotPrompt;
    public Text slotName;
    public Text slotMenuName;

    public GameObject showSlot;
    public GameObject newSlot;

    public List<UI> saveSlots;
    public GameObject otherContent;
    public UI currentSlot;
    public string deleteSlotName;
    public Color SlotColour;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform o in slotInfo)
        {
            o.gameObject.SetActive(false);
        }
        foreach(UI i in content.GetComponentsInChildren<UI>())
        {
            if (!saveSlots.Contains(i))
                saveSlots.Add(i);
        }
        SlotColour = saveSlots[0].GetComponent<Image>().color;
    }

    // Update is called once per frame
    void Update()
    {
        currentSlot = null;
        foreach (UI s in saveSlots)
        {
            if (UIControl.Me.currentUIB == s & SaveManager.Me.LoadhasStarted(saveSlots.IndexOf(s) + 1))
                currentSlot = s;
            if (SaveManager.Me.LoadhasStarted(saveSlots.IndexOf(s) + 1))
                s.normalColour = Color.blue;
            else
                s.normalColour = SlotColour;
            foreach (Text t in s.GetComponentsInChildren<Text>())
            {
                if (t.name == "Info Text")
                {
                    if (SaveManager.Me.LoadhasStarted(saveSlots.IndexOf(s) + 1))
                        t.text = "";
                    else
                        t.text = "(Empty)";
                }
            }
        }
        foreach (Transform o in slotInfo)
        {
            o.gameObject.SetActive(currentSlot);
            if (o.name == "Info")
                o.gameObject.SetActive(true);
        }
        if (currentSlot || otherContent.activeSelf & UIControl.Me.isMainMenu)
        {
            foreach (Transform o in slotInfo)
            {
                UIControl.Me.Fix(o.gameObject, 1);
            }
            int slot = 0;
            if (currentSlot)
            {
                slotName.text = currentSlot.name;
                slot = saveSlots.IndexOf(currentSlot) + 1;
            }
            else
                slot = SaveManager.Me.currentUISlot;

            GameModeManager.Me.Load(slot);
            PlayerCharacterController.Me.GetComponent<CharacterStats>().Load(slot);

            foreach (Transform o in slotInfo)
            {
                foreach (Text t in o.GetComponentsInChildren<Text>())
                {
                    if (t.name == "Info Text" & t.transform.parent.name == "Level")
                    {
                        t.text = PlayerCharacterController.Me.GetComponent<CharacterStats>().Level.ToString();
                    }
                    if(t.name == "Min" & t.transform.parent.name == "Xp")
                    {
                        t.text = PlayerCharacterController.Me.GetComponent<CharacterStats>().Xp.ToString();
                    }
                    if (t.name == "Max" & t.transform.parent.name == "Xp")
                    {
                        t.text = PlayerCharacterController.Me.GetComponent<CharacterStats>().RequiredXp.ToString();
                    }
                    if (t.name == "Info Text" & t.transform.parent.name == "Money")
                    {
                         
                    }
                    if (t.name == "Info Text" & t.transform.parent.name == "Total Completion")
                    {
                        t.text = GameModeManager.Me.TotalCompletion.ToString();
                    }
                    if (t.name == "Info Text" & t.transform.parent.name == "Main Story")
                    {
                        t.text = GameModeManager.Me.MainStory.ToString();
                    }
                    if (t.name == "Info Text" & t.transform.parent.name == "Side Missions")
                    {
                        t.text = GameModeManager.Me.SideMissions.ToString();
                    }

                    if (t.name == "Info Text" & t.transform.parent.name == "Game Modes Completed")
                    {
                        t.text = GameModeManager.Me.GetCompletedGameModes().ToString() + "/" + GameModeManager.Me.GameModes.Count;
                    }
                }
                foreach (Slider s in o.GetComponentsInChildren<Slider>())
                {
                    if (s.name == "Xp")
                    {
                        s.minValue = 0;
                        s.value = PlayerCharacterController.Me.GetComponent<CharacterStats>().Xp;
                        s.maxValue = PlayerCharacterController.Me.GetComponent<CharacterStats>().RequiredXp;
                        s.fillRect.gameObject.SetActive(s.value != 0);
                    }
                }
            }
        }

        if (currentSlot)
        {
            if (SaveManager.Me.LoadhasStarted(saveSlots.IndexOf(currentSlot) + 1))
            {
                deleteSlotPrompt.gameObject.SetActive(true);
                bool p = false;
                if (GameManager.Me.currentdevice == "KeyBoard")
                {
                    deleteSlotPrompt.text = "(R) Delete Save";
                    if (Input.GetKeyDown(KeyCode.R))
                        p = true;
                }
                if (GameManager.Me.currentdevice == "GamePad")
                {
                    deleteSlotPrompt.text = "(X) Delete Save";
                    if (Input.GetKeyDown("joystick button 2"))
                        p = true;
                }
                if (p)
                {
                    deleteSlotName = currentSlot.name;
                    slotDeletionName.text = "Do you want to Delete " + deleteSlotName + "?";
                    FindObjectOfType<UIControl>().ChangeUI(slotDeletion);
                    FindObjectOfType<UIControl>().ChangeSaveSlot(saveSlots.IndexOf(currentSlot) + 1);
                }
            }
            else
            {
                deleteSlotPrompt.gameObject.SetActive(false);
            }
        }
    }

    public void SelectSave(UI save)
    {
        if (saveSlots.IndexOf(save) + 1 == 1)
            slotMenuName.text = "Slot One";
        if (saveSlots.IndexOf(save) + 1 == 2)
            slotMenuName.text = "Slot Two";
        if (saveSlots.IndexOf(save) + 1 == 3)
            slotMenuName.text = "Slot Three";
        UIControl.Me.ChangeSaveSlot(saveSlots.IndexOf(save) + 1);
        if (SaveManager.Me.LoadhasStarted(saveSlots.IndexOf(save) + 1))
        {
            UIControl.Me.ChangeUI(showSlot);
        }
        else
        {
            UIControl.Me.ChangeUI(newSlot);
        }
    }

    public void CreateSave()
    {
        SaveManager.Me.SavehasStartedS(SaveManager.Me.currentUISlot);
        UIControl.Me.ChangeUI(showSlot);
    }

    public void DeleteSave()
    {
        SaveManager.Me.onDelete(SaveManager.Me.currentUISlot);
    }
}
