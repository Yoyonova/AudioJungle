using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicScaler : MonoBehaviour
{
    [SerializeField] private float scaleFactor = 0.1f, minSize = 0.1f, maxSize = 5f;

    void Update()
    {
        float scale = Spectrographer.currentVolume();
        scale -= 1;
        scale *= scaleFactor;
        scale += 1;
        Debug.Log(scale + "( min: " + minSize + ", max: " + maxSize);
        scale = Mathf.Clamp(scale, minSize, maxSize);
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
