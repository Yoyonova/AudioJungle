//PIXELBOY BY @WTFMIG
using UnityEngine;
using System.Collections;

public class Pixelate : MonoBehaviour
{
    public int w = 720;
    int h;

    private void Update()
    {
        float ratio = Camera.main.pixelHeight / (float)Camera.main.pixelWidth;
        h = Mathf.RoundToInt(w * ratio);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        source.filterMode = FilterMode.Point;
        RenderTexture buffer = RenderTexture.GetTemporary(w, h, -1);
        buffer.filterMode = FilterMode.Point;
        Graphics.Blit(source, buffer);
        Graphics.Blit(buffer, destination);
        RenderTexture.ReleaseTemporary(buffer);
    }
}