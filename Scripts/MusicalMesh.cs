using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicalMesh : MonoBehaviour
{
    [SerializeField] private int meshLength = 126;
    [SerializeField] private float audioScale = 1f, lengthScale = 5f, widthScale = 1f;
    [SerializeField] private Color color;

    private bool colorIsGenerated = false;

    private void Start()
    {
        CreateMesh(meshLength);
    }

    private void Update()
    {
        DeformToAudio();
    }

    private void CreateMesh(int audioLength)
    {
        int width = 3;
        int length = audioLength + 2;

        Vector3[] vertices = new Vector3[width * length];
        Vector2[] uvs = new Vector2[width * length];

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                vertices[x * width + y] = new Vector3(x * lengthScale / meshLength, 0, y * widthScale);
                uvs[x * width + y] = new Vector2(x / length, y / width);
            }
        }

        List<int> triangles = new();
        for (int x = 0; x < length - 1; x++)
        {
            for (int y = 0; y < width - 1; y++)
            {
                triangles.Add(x * width + y);
                triangles.Add((x+1) * width + y+1);
                triangles.Add((x+1) * width + y);

                triangles.Add(x * width + y);
                triangles.Add(x * width + y+1);
                triangles.Add((x+1) * width + y+1);

                triangles.Add(x * width + y);
                triangles.Add((x + 1) * width + y);
                triangles.Add((x + 1) * width + y + 1);

                triangles.Add(x * width + y);
                triangles.Add((x + 1) * width + y + 1);
                triangles.Add(x * width + y + 1);
            }
        }

        Mesh mesh = new();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
    }

    private void DeformToAudio()
    {
        float[] audio = Spectrographer.CurrentSpectrogramSlice();
        if (audio == null) return;

        float[] scaledAudio = new float[meshLength];
        for(int i = 0; i < meshLength; i++)
        {
            float sum = 0;
            float count = 0;
            int limit = (int) ((i + 1) * audio.Length / (float) meshLength);
            limit = Mathf.Min(limit, audio.Length);
            for(int j = (int) (i * audio.Length / (float) meshLength); j < limit; j++)
            {
                sum += audio[j];
                count++;
            }

            scaledAudio[i] = sum / count;
        }

        int width = 3;
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < meshLength; i++)
        {
            vertices[(i + 1) * width + 1].y = audio[i] * audioScale;
        }

        mesh.vertices = vertices;
    }

    
}
