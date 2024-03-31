using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrogramTexturer : MonoBehaviour
{
    private void Start()
    {
        ApplyTexture();
    }

    private void ApplyTexture()
    {
        GetComponent<MeshRenderer>().material.mainTexture = Spectrographer.instance.spectrogramTexture;
    }
}
