using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicLighter : MonoBehaviour
{
    [SerializeField] private float intensityFactor = 1f;
    private Color baseColor;

    private void Start()
    {
        baseColor = SkyboxGenerator.GenerateRandomRelatedColor();
    }

    void Update()
    {
        float intensity = Spectrographer.currentVolume();
        intensity *= intensityFactor;
        GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", baseColor * intensity);
    }
}
