using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {

    public EventSystem EventSystem;
    public GameObject SelectedObject;
    private bool ButtonSelected;
    public static int NewMapFlag;
    public List<Resolution> ResList = new List<Resolution>();
    public Dropdown ResDropdown;
    public Toggle WindowedToggle;

    public void Awake()
    {
        Resolution[] resolutions = Screen.resolutions;
        foreach (Resolution resolution in resolutions)
        {
            if (resolution.width >= 800 && resolution.height >= 600)
            {
                ResList.Add(resolution);
            }
        }
    }

    public void Start()
    {
        ManageResDropdown();
    }

    public void ManageResDropdown()
    {
        List<string> resStringList = new List<string>();
        foreach (Resolution resolution in ResList)
        {
            resStringList.Add(resolution.ToString());
        }
        ResDropdown = GetComponent<Dropdown>();
        if(ResDropdown)
        {
            ResDropdown.ClearOptions();
            ResDropdown.AddOptions(resStringList);
        }
    }

    public void SetRes()
    {
        Screen.SetResolution(ResList[ResDropdown.value].height, ResList[ResDropdown.value].width, !WindowedToggle.isOn);
    }

    public void ManageNewMapFlag(int assignator)
    {
        NewMapFlag = assignator;
    }

    public void LoadByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public void MasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void Exit()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }

    void Update()
    {
        if (Input.GetAxisRaw("Vertical")!=0 && ButtonSelected == false)
        {
            EventSystem.SetSelectedGameObject(SelectedObject);
            ButtonSelected = true;
        }
    }

    private void OnDisable()
    {
        ButtonSelected = false;
    }
}
