using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuggingFace.API;

public class MusicalTexture : MonoBehaviour
{
    [SerializeField] private string[] possiblePrompts = { "A creature found in " };

    private void Start()
    {
        CreateTexture();
    }

    private void CreateTexture()
    {
        string prompt = possiblePrompts[Random.Range(0, possiblePrompts.Length)] + MusicController.worldDescription + " " + Random.Range(0, 1000000);
        HuggingFaceAPI.TextToImage(prompt, image => ApplyTexture(image), error => Debug.Log(error));
    }

    private void ApplyTexture(Texture2D texture)
    {
        GetComponent<MeshRenderer>().material.mainTexture = texture;
    }
}
