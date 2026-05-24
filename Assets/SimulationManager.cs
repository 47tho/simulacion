using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    public GameObject personPrefab;
    public SimulationConfig config;
    
    [Header("Simulation Speed")]
    public float secondsPerMinute = 5f;
    
    private int currentMinute = 0;
    private float timer = 0f;
    private List<GameObject> activePeople = new List<GameObject>();

    void Start()
    {
        if (config == null)
        {
            Debug.LogError("No se ha asignado una configuracion al SimulationManager.");
            return;
        }
        UpdatePopulation();
    }

    void Update()
    {
        if (config == null) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            currentMinute++;
            // If we run out of data, stay at the last minute's population
            if (currentMinute >= config.populationData.Count)
            {
                currentMinute = config.populationData.Count - 1;
            }
            UpdatePopulation();
        }
    }

    void UpdatePopulation()
    {
        if (config == null || config.populationData.Count == 0) return;

        // Clean up null references
        activePeople.RemoveAll(p => p == null);

        int targetIndex = Mathf.Clamp(currentMinute, 0, config.populationData.Count - 1);
        int targetCount = config.populationData[targetIndex];
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
                    if (p != null) Destroy(p);
                }
            }
        }
        
        Debug.Log($"Sim Time: {6:30 + currentMinute}m - Population: {activePeople.Count} / Target: {targetCount}");
    }

    void SpawnPerson()
    {
        if (personPrefab == null || config == null || config.buildings.Count == 0) return;
        
        // Pick a random building
        BuildingData building = config.buildings[Random.Range(0, config.buildings.Count)];
        if (building.spawnPoints.Count == 0 || building.rooms.Count == 0) return;

        // Strictly pick a random room as requested
        RoomData targetRoom = building.rooms[Random.Range(0, building.rooms.Count)];
        
        // Check if room transform is null, try to find one that isn't
        if (targetRoom.transform == null)
        {
            foreach(var r in building.rooms)
            {
                if (r.transform != null) { targetRoom = r; break; }
            }
        }
        
        if (targetRoom.transform == null) return;

        // Weighted random for spawn point
        float totalWeight = 0;
        foreach (var sp in building.spawnPoints) totalWeight += sp.weight;
        float randomValue = Random.Range(0, totalWeight);
        float currentWeightSum = 0;
        SpawnPointData selectedSpawn = building.spawnPoints[0];

        foreach (var sp in building.spawnPoints)
        {
            currentWeightSum += sp.weight;
            if (randomValue <= currentWeightSum)
            {
                selectedSpawn = sp;
                break;
            }
        }

        if (selectedSpawn.transform == null) return;

        Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
        Vector3 spawnPos = selectedSpawn.transform.position + offset;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        GameObject person = Instantiate(personPrefab, spawnPos, Quaternion.identity);
        PersonAgent agent = person.GetComponent<PersonAgent>();
        
        if (agent != null)
        {
            agent.Initialize(targetRoom, selectedSpawn.transform, config.minStayTime, config.maxStayTime, config.returnToSpawnChance);
        }
        
        activePeople.Add(person);
    }
}
