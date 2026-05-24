using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    public GameObject personPrefab;
    public Transform spawnPoint;
    
    [Header("Simulation Settings")]
    public float secondsPerMinute = 5f; // Simulation speed
    
    private int[] populationData = { 
        100, 110, 131, 142, 172, 147, 162, 178, 193, 193, 
        213, 206, 209, 217, 247, 238, 238, 266, 254, 252, 253 
    };
    
    private int currentMinute = 0;
    private float timer = 0f;
    private List<GameObject> activePeople = new List<GameObject>();

    void Start()
    {
        if (spawnPoint == null) spawnPoint = GameObject.Find("SimulationSpawnPoint")?.transform;
        UpdatePopulation();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            currentMinute++;
            if (currentMinute < populationData.Length)
            {
                UpdatePopulation();
            }
        }
    }

    void UpdatePopulation()
    {
        int targetCount = populationData[currentMinute];
        int currentCount = activePeople.Count;

        if (currentCount < targetCount)
        {
            int toSpawn = targetCount - currentCount;
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnPerson();
            }
        }
        else if (currentCount > targetCount)
        {
            int toRemove = currentCount - targetCount;
            for (int i = 0; i < toRemove; i++)
            {
                if (activePeople.Count > 0)
                {
                    GameObject p = activePeople[0];
                    activePeople.RemoveAt(0);
                    Destroy(p);
                }
            }
        }
        
        Debug.Log($"Sim Time: {6:30 + currentMinute}m - Population: {activePeople.Count}");
    }

    void SpawnPerson()
    {
        if (personPrefab == null || spawnPoint == null) return;
        
        Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        GameObject person = Instantiate(personPrefab, spawnPoint.position + offset, Quaternion.identity);
        activePeople.Add(person);
    }
}
