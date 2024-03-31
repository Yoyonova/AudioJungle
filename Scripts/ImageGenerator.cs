using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuggingFace.API;

public class ImageGenerator : MonoBehaviour
{
    [SerializeField] private MeshRenderer imageRenderer;
    [SerializeField] private string imageDescription;

    private void Start()
    {
        GenerateImage(imageDescription);
    }

    public void GenerateImage(string inputText)
    {
        HuggingFaceAPI.TextToImage(inputText, image => ApplyImage(image), error => Debug.Log(error));
    }

    private void ApplyImage(Texture2D image)
    {
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.mainTexture = image;
        imageRenderer.material = newMaterial;
    }
}
