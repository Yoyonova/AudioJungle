using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomScaler : MonoBehaviour
{
    [SerializeField] float minExp = 0f, maxExp = 2f;

    private void Start()
    {
        float exp = Random.Range(minExp, maxExp);
        float scale = Mathf.Pow(10, exp);
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
