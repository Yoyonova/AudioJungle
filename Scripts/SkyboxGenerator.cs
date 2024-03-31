using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuggingFace.API;

public class SkyboxGenerator : MonoBehaviour
{
    public static SkyboxGenerator instance;
    public Material baseMaterial;

    public static bool isGenerated = false;

    private void Awake()
    {
        instance = this;
    }

    public static void Generate6SidedSkybox()
    {
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        HuggingFaceAPI.TextToImage("The distant horizon of " + MusicController.worldDescription, image => { RenderSettings.skybox.SetTexture("_FrontTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
        HuggingFaceAPI.TextToImage("The very distant horizon of " + MusicController.worldDescription, image => {RenderSettings.skybox.SetTexture("_BackTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
        HuggingFaceAPI.TextToImage("The far horizon of " + MusicController.worldDescription + " with the sun in the sky", image => {RenderSettings.skybox.SetTexture("_LeftTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
        HuggingFaceAPI.TextToImage("The far horizon of " + MusicController.worldDescription, image => {RenderSettings.skybox.SetTexture("_RightTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
        HuggingFaceAPI.TextToImage("A shot of only the sky that you would see from " + MusicController.worldDescription, image => {RenderSettings.skybox.SetTexture("_UpTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
        HuggingFaceAPI.TextToImage("Aerial shot looking down at the ground of " + MusicController.worldDescription, image => {RenderSettings.skybox.SetTexture("_DownTex", image); DynamicGI.UpdateEnvironment(); Debug.Log("Skybox Updated"); }, error => Debug.Log(error));
    }

    public static void GeneratePanoramicSkybox()
    {
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        HuggingFaceAPI.TextToImage("The distant horizon of " + MusicController.worldDescription, image => UpdateSkybox(image), error => Debug.Log(error));
    }

    private static void UpdateSkybox(Texture2D image)
    {
        RenderSettings.skybox.mainTexture = image;
        DynamicGI.UpdateEnvironment();
        RenderSettings.fogColor = CalculateAverageColor(image);

        instance.baseMaterial.color = GenerateRandomRelatedColor();

        isGenerated = true;
        Debug.Log("Skybox Updated");
    }

    private static Color CalculateAverageColor(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        Color averageColor = Color.black;

        foreach (Color pixel in pixels)
        {
            averageColor += pixel;
        }

        averageColor /= pixels.Length;

        return averageColor;
    }

    public static Color GenerateRandomRelatedColor()
    {
        Texture2D texture = (Texture2D) RenderSettings.skybox.mainTexture;
        if (!texture) return Color.black;
        int width = texture.width;
        int height = texture.height;
        int randomX = Random.Range(0, width);
        int randomY = Random.Range(0, height);
        return texture.GetPixel(randomX, randomY);
    }
}
