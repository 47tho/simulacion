using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnPointData
{
    public Transform transform;
    [Range(0, 100)]
    public float weight = 100f;
}

[System.Serializable]
public class RoomData
{
    public string name;
    public Transform transform;
    public int capacity = 20;
    [System.NonSerialized]
    public int currentOccupancy = 0;
}

[System.Serializable]
public class BuildingData
{
    public string name = "Building";
    public List<SpawnPointData> spawnPoints = new List<SpawnPointData>();
    public List<RoomData> rooms = new List<RoomData>();
    public List<int> populationData = new List<int> { 20, 40, 60, 80, 100 };
}

public class SimulationManager : MonoBehaviour
{
    public GameObject personPrefab;
    
    [Header("Building Configurations")]
    public List<BuildingData> buildings = new List<BuildingData>();

    [Header("Behavior Settings")]
    public float minStayTime = 10f;
    public float maxStayTime = 30f;
    public float returnToSpawnChance = 0.5f;
    
    [Header("Simulation Speed")]
    public float secondsPerMinute = 10f;
    
    private int currentMinute = 0;
    private float timer = 0f;
    private Dictionary<BuildingData, List<GameObject>> buildingPopulations = new Dictionary<BuildingData, List<GameObject>>();

    void Start()
    {
        foreach (var building in buildings)
        {
            buildingPopulations[building] = new List<GameObject>();
            // Reset occupancy
            foreach(var room in building.rooms) room.currentOccupancy = 0;
        }
        UpdateAllPopulations();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            currentMinute++;
            UpdateAllPopulations();
        }
    }

    void UpdateAllPopulations()
    {
        foreach (var building in buildings)
        {
            UpdateBuildingPopulation(building);
        }
    }

    void UpdateBuildingPopulation(BuildingData building)
    {
        if (building.populationData.Count == 0) return;

        if (!buildingPopulations.ContainsKey(building))
            buildingPopulations[building] = new List<GameObject>();

        List<GameObject> activePeople = buildingPopulations[building];
        activePeople.RemoveAll(p => p == null);

        int blockIndex = currentMinute / 5;
        int minuteInBlock = currentMinute % 5;

        int targetValue = building.populationData[Mathf.Clamp(blockIndex, 0, building.populationData.Count - 1)];
        int prevValue = (blockIndex > 0) ? building.populationData[Mathf.Clamp(blockIndex - 1, 0, building.populationData.Count - 1)] : 0;

        // Distribuir equitativamente: Rampa lineal entre el valor previo y el objetivo del bloque
        float t = (minuteInBlock + 1) / 5.0f;
        int targetCount = Mathf.RoundToInt(Mathf.Lerp(prevValue, targetValue, t));

        int currentCount = activePeople.Count;

        if (currentCount < targetCount)
        {
            int toSpawn = targetCount - currentCount;
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnPerson(building, activePeople);
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
    }

    void SpawnPerson(BuildingData building, List<GameObject> activeList)
    {
        if (personPrefab == null || building.spawnPoints.Count == 0 || building.rooms.Count == 0) return;

        RoomData targetRoom = building.rooms[Random.Range(0, building.rooms.Count)];
        if (targetRoom.transform == null) return;

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
            agent.Initialize(targetRoom, selectedSpawn.transform, minStayTime, maxStayTime, returnToSpawnChance);
        }
        
        activeList.Add(person);
    }
}
