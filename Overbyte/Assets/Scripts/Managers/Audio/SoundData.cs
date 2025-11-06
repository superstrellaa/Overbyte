using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    SFX,
    BGM
}

[System.Serializable]
public class SoundData
{
    public string name;
    public AudioClip clip;
    public SoundType type = SoundType.SFX;
    public bool is3D = false;
    public float volume = 1f;
    public float pitch = 1f;
    public bool loop = false;

    [HideInInspector] public AudioMixerGroup outputGroup;
}
