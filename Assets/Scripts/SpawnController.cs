using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;

/* Written by Steven Lay, 2020.
 * This class checks if a new barrier needs spawning.
 * Easily extendable to more barriers and spawn points.
 */
public class SpawnController : MonoBehaviour
{
    private bool firstRun = true;

    [SerializeField]
    private Transform barrierSpawnerPan = null;

    // Interval to check if a new barrier needs to be spawned
    [SerializeField]
    private float checkSpawnInterval = 1.0f;

    // How far does the dragged barrier need to be displaced from its spawn point to spawn another
    [SerializeField]
    private float distanceToSpawn = 0.2f;

    private Transform[,] barrierPrefabs;

    void Start()
    {
        // Find Barriers that should be spawned in the scene
        GameObject[] prefabs = GameObject.FindGameObjectsWithTag("BarrierToSpawn");

        // Find Spawn Points
        GameObject[] spawnMarks = GameObject.FindGameObjectsWithTag("SpawnMarker");

        // Place Barriers and Spawn Points in one array
        barrierPrefabs = new Transform[prefabs.Length, prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            barrierPrefabs[i, 0] = prefabs[i].transform;
            barrierPrefabs[i, 1] = spawnMarks[i].transform;
        }

        StartCoroutine(CheckSpawn());
    }

    // Routine that checks if barrier spawning is necessary
    IEnumerator CheckSpawn()
    {
        while (true)
        {
            for (int i = 0; i < barrierPrefabs.GetLength(0); i++)
            {
                // Check if spawned barrier has been dragged away from its spawn point by at least distanceToSpawn metres
                if (Vector3.Distance(barrierPrefabs[i,0].position, barrierPrefabs[i, 1].position) > distanceToSpawn)
                {
                    // Spawn a new barrier at the spawn point
                    var newPrefab = Instantiate(barrierPrefabs[i, 0], barrierPrefabs[i, 1].position, barrierPrefabs[i, 1].rotation);
                    newPrefab.parent = barrierSpawnerPan;
                    newPrefab.name = barrierPrefabs[i, 0].name;

                    // Skip first run of this routine                    
                    if (!firstRun)
                    {
                        // Set proper properties for the Barrier that was dragged away
                        barrierPrefabs[i, 0].SetParent(null, true);
                        barrierPrefabs[i, 0].tag = "Barrier";
                        barrierPrefabs[i, 0].name = "Barrier" + VoiceCommands.barrierCount++;
                        barrierPrefabs[i, 0].GetComponent<BoundingBox>().enabled = true;
                    }

                    barrierPrefabs[i, 0] = newPrefab;
                }
            }

            firstRun = false;

            yield return new WaitForSeconds(checkSpawnInterval);
        }
    }
}
