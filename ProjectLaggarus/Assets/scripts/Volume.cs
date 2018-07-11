using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Volume : MonoBehaviour
{

    public AudioMixer MasterMixer;

    public void SetSfxVolume(float sfxVolume)
    {
        MasterMixer.SetFloat("sfxVolume", sfxVolume);
    }

    public void SetMusicVolume(float musicVolume)
    {
        MasterMixer.SetFloat("musicVolume", musicVolume);
    }

    public void SetMasterVolume(float masterVolume)
    {
        MasterMixer.SetFloat("masterVolume", masterVolume);
    }
}
