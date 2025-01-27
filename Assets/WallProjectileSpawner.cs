using System.Collections;
using UnityEngine;

public class WallProjectileSpawner : MonoBehaviour
{
    public GameObject wallMovingProjectilePrefab; // Assign the prefab in the inspector
    public Transform spawnPoint; // Optional: Assign a specific spawn point in the inspector
    public float spawnInterval = 4f; // Interval between spawns in seconds

    private void Start()
    {
        // Start the spawning coroutine
        StartCoroutine(SpawnProjectiles());
    }

    private IEnumerator SpawnProjectiles()
    {
        while (true) // Infinite loop for spawning
        {
            // Instantiate the projectile
            if (wallMovingProjectilePrefab != null)
            {
                if (spawnPoint != null)
                {
                    var inst = Instantiate(wallMovingProjectilePrefab, spawnPoint.position, spawnPoint.rotation);
                 //   var script = wallMovingProjectilePrefab.GetComponent<CircularMovement>();
                    // if (script)
                    // {
                    //     script.pivotPoint = transform.position;
                    //     script.instance = inst;
                    //     script.Initialize();
                    //     script.diameter = transform.localScale.x;
                    //     script.gameObject.transform.localScale = transform.localScale;
                    //     Destroy(inst.gameObject,10.0f);
                    // }
                }
                else
                {
                    Instantiate(wallMovingProjectilePrefab, transform.position, transform.rotation);
                }
            }

            // Wait for the interval before spawning the next projectile
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}