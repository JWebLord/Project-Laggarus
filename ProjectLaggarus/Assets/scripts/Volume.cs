using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Volume : MonoBehaviour
{

    public AudioMixer masterMixer;

    public void SetSfxVolume(float sfxVolume)
    {
        masterMixer.SetFloat("sfxVolume", sfxVolume);
    }

    public void SetMusicVolume(float musicVolume)
    {
        masterMixer.SetFloat("musicVolume", musicVolume);
    }

    public void SetMasterVolume(float masterVolume)
    {
        masterMixer.SetFloat("masterVolume", masterVolume);
    }
}
