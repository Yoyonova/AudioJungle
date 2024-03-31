using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicController : MonoBehaviour
{
    [SerializeField] private AudioClip testClip;
    [SerializeField] private string testDescription;
    [SerializeField] private RunMusicGen musicGenerator;
    [SerializeField] private Canvas musicPickUI, loadingUI, restartUI;
    [SerializeField] private TMP_InputField promptText;
    [SerializeField] private FPSController playerController;

    public AudioSource audioSource;
    public int sampleRate = 44100;
    [HideInInspector] public bool isMusicPicked, isMusicPlaying;

    public static MusicController instance;
    public static string worldDescription;

    private void Awake()
    {
        instance = this;
    }
    private void Update()
    {
        if (!isMusicPlaying && testClip)
        {
            Debug.Log("Playing test music");
            audioSource.clip = testClip;
            audioSource.Play();
            EndMusicPick();

            worldDescription = testDescription;
            StartWorldGeneration();
        }

        if (!isMusicPlaying && RunMusicGen.clip)
        {
            Debug.Log("Playing generated music");
            audioSource.clip = RunMusicGen.clip;
            audioSource.Play();

            sampleRate = 16000;
            instance.loadingUI.enabled = false;

            worldDescription = promptText.text;
            StartWorldGeneration();
        }

        if (!isMusicPlaying && MusicImporter.clip)
        {
            Debug.Log("Playing imported music");
            audioSource.clip = MusicImporter.clip;
            audioSource.Play();

            worldDescription = "An environment that matches this music: " + MusicImporter.description;
            StartWorldGeneration();
        }
    }

    public static void EndMusicPick()
    {
        instance.isMusicPicked = true;
        instance.musicPickUI.enabled = false;
    }

    public void StartMusicGeneration()
    {
        if (promptText.text.Length == 0) return;

        EndMusicPick();

        instance.loadingUI.enabled = true;
        musicGenerator.enabled = true;
        musicGenerator.prompt = promptText.text;
    }

    private void StartWorldGeneration()
    {
        Debug.Log("Generating world: " + worldDescription);
        isMusicPlaying = true;
        playerController.enabled = true;
        restartUI.enabled = true;

        SkyboxGenerator.GeneratePanoramicSkybox();
    }

    public static int ClipTimeSamples()
    {
        return instance.audioSource.timeSamples;
    }
}
