using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicColorizer : MonoBehaviour
{
    [SerializeField] private bool isGradient = false;
    private bool isColorGenerated = false;

    private void Update()
    {
        TryApplyMusicColor();
    }

    private void TryApplyMusicColor()
    {
        if (isColorGenerated || !SkyboxGenerator.isGenerated) return;

        Color newColor = SkyboxGenerator.GenerateRandomRelatedColor();
        isColorGenerated = true;
        Material material = new Material(Shader.Find("Standard"));

        if(isGradient)
        {
            Color newColor2 = SkyboxGenerator.GenerateRandomRelatedColor();
            int texRes = 128;
            Texture2D colorGradient = new(texRes, texRes);
            for(int x = 0; x < texRes; x++)
            {
                for(int y = 0; y < texRes; y++)
                {
                    colorGradient.SetPixel(x, y, Color.Lerp(newColor, newColor2, (x+y) / (texRes*2)));
                }
            }
            material.mainTexture = colorGradient;
        }
        else material.color = newColor;

        GetComponent<MeshRenderer>().material = material;
    }
}
