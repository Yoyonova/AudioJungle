using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomStuffSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs;
    [SerializeField] float timeBetweenSpawns = 1f, spawnDistance = 100f, verticalFactor = 0.02f;
    [SerializeField] int maxSpawns = 100;

    private float timer = 0;
    private List<GameObject> spawnedThings = new();

    private void Update()
    {
        if (MusicController.instance.isMusicPlaying && SkyboxGenerator.isGenerated) timer += Time.deltaTime;
        if (timer >= timeBetweenSpawns)
        {
            Spawn();
            timer = 0;
        }
    }

    private void Spawn()
    {
        GameObject newThing = Instantiate(prefabs[Random.Range(0, prefabs.Length)]);
        Vector3 randomPosistion = Random.onUnitSphere * spawnDistance * Random.value;
        randomPosistion.y *= verticalFactor;
        newThing.transform.position = transform.position + randomPosistion;

        newThing.transform.rotation = Random.rotation;

        spawnedThings.Add(newThing);

        if(spawnedThings.Count > maxSpawns)
        {
            Destroy(spawnedThings[0]);
            spawnedThings.RemoveAt(0);
        }
    }
}
