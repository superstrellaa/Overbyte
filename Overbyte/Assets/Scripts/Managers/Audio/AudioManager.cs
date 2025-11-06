using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private GameObject audioSourcePrefab;

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;

    private Queue<AudioSource> audioPool = new Queue<AudioSource>();

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(audioSourcePrefab, transform);
            AudioSource source = go.GetComponent<AudioSource>();
            audioPool.Enqueue(source);
            go.SetActive(false);
        }
    }

    private AudioSource GetAudioSource()
    {
        if (audioPool.Count > 0)
        {
            AudioSource source = audioPool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }
        else
        {
            GameObject go = Instantiate(audioSourcePrefab, transform);
            return go.GetComponent<AudioSource>();
        }
    }

    private void ReturnAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.transform.parent = transform;
        source.outputAudioMixerGroup = null;
        source.gameObject.SetActive(false);
        audioPool.Enqueue(source);
    }

    public void PlaySound(SoundData sound, Vector3? position = null, Transform parent = null)
    {
        AudioSource source = GetAudioSource();
        source.clip = sound.clip;
        source.volume = sound.volume;
        source.pitch = sound.pitch;
        source.loop = sound.loop;
        source.spatialBlend = sound.is3D ? 1f : 0f;

        source.outputAudioMixerGroup = sound.type == SoundType.BGM ? bgmGroup : sfxGroup;

        if (position.HasValue)
            source.transform.position = position.Value;

        if (parent != null)
        {
            source.transform.parent = parent;
            source.transform.localPosition = Vector3.zero;
        }

        source.Play();

        if (!sound.loop)
            StartCoroutine(ReturnAfterFinish(source, sound.clip.length));
    }

    private IEnumerator ReturnAfterFinish(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        ReturnAudioSource(source);
    }

    public void SetMasterVolume(float volume)
    {

        mainMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void ResetMasterVolume()
    {
        mainMixer.SetFloat("MasterVolume", 0f);
    }

    public void SetVolume(SoundType type, float volume)
    {
        string parameter = type == SoundType.BGM ? "BGMVolume" : "SFXVolume";
        mainMixer.SetFloat(parameter, Mathf.Log10(volume) * 20);
    }
}
