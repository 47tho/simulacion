using UnityEngine;
using System.Collections;
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
    [Header("NPC Prefabs")]
    public GameObject remyPrefab;
    public GameObject ch22Prefab;
    public GameObject ch16Prefab;
    public GameObject ch33Prefab;
    
    [Header("NPC Type Distribution (%)")]
    [Range(0, 100)] public float normalChance = 50f;
    [Range(0, 100)] public float labCoatChance = 25f;
    [Range(0, 100)] public float uniformChance = 25f;

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
            foreach(var room in building.rooms) room.currentOccupancy = 0;
            
            // Pre-calculate reachable rooms to avoid NPCs getting stuck
            ValidateReachableRooms(building);
        }
        UpdateAllPopulations();
    }

    private Dictionary<BuildingData, List<RoomData>> reachableRoomsDict = new Dictionary<BuildingData, List<RoomData>>();

    void ValidateReachableRooms(BuildingData building)
    {
        List<RoomData> reachable = new List<RoomData>();
        if (building.spawnPoints.Count == 0 || building.rooms.Count == 0) return;

        foreach (var room in building.rooms)
        {
            if (room.transform == null) continue;
            
            bool canReachFromAnySpawn = false;
            foreach (var spawn in building.spawnPoints)
            {
                if (spawn.transform == null) continue;
                
                UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                if (UnityEngine.AI.NavMesh.CalculatePath(spawn.transform.position, room.transform.position, UnityEngine.AI.NavMesh.AllAreas, path))
                {
                    if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    {
                        canReachFromAnySpawn = true;
                        break;
                    }
                }
            }

            if (canReachFromAnySpawn)
            {
                reachable.Add(room);
            }
            else
            {
                Debug.LogError($"Building '{building.name}': Room '{room.name}' is UNREACHABLE from ALL spawn points and will be removed from simulation logic.");
            }
        }
        
        if (reachable.Count == 0 && building.rooms.Count > 0)
        {
            Debug.LogError($"Building '{building.name}' has NO reachable rooms! NPCs will never move.");
        }
        
        reachableRoomsDict[building] = reachable;
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

        float t = (minuteInBlock + 1) / 5.0f;
        int targetCount = Mathf.RoundToInt(Mathf.Lerp(prevValue, targetValue, t));

        int currentCount = activePeople.Count;

        if (currentCount < targetCount)
        {
            int toSpawn = targetCount - currentCount;
            StartCoroutine(SpawnGroupOverTime(building, activePeople, toSpawn, secondsPerMinute * 0.8f));
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
                    if (p != null)
                    {
                        PersonAgent agent = p.GetComponent<PersonAgent>();
                        if (agent != null) agent.ForceReturn();
                        else Destroy(p);
                    }
                }
            }
        }
    }

    IEnumerator SpawnGroupOverTime(BuildingData building, List<GameObject> activeList, int count, float duration)
    {
        float interval = duration / Mathf.Max(1, count);
        for (int i = 0; i < count; i++)
        {
            SpawnPerson(building, activeList);
            yield return new WaitForSeconds(interval);
        }
    }

    void SpawnPerson(BuildingData building, List<GameObject> activeList)
{
        if (building.spawnPoints.Count == 0 || building.rooms.Count == 0) return;

        // Get only reachable rooms
        List<RoomData> targetPool = reachableRoomsDict.ContainsKey(building) ? reachableRoomsDict[building] : building.rooms;
        if (targetPool.Count == 0) return;

        // 1. Select Spawn Point by Weight
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

        // 2. Select Room (from reachable pool)
        RoomData targetRoom = targetPool[Random.Range(0, targetPool.Count)];
        
        // 3. Select Prefab
        GameObject prefabToSpawn = remyPrefab;
        float totalProb = normalChance + labCoatChance + uniformChance;
        float p = Random.Range(0, totalProb);
        if (p < normalChance) prefabToSpawn = (Random.value < 0.5f) ? remyPrefab : ch22Prefab;
        else if (p < normalChance + labCoatChance) prefabToSpawn = ch16Prefab;
        else prefabToSpawn = ch33Prefab;

        if (prefabToSpawn == null) return;

        // 4. Calculate Spawn Position with enough dispersion to avoid piles
        Vector3 offset = new Vector3(Random.Range(-5.0f, 5.0f), 0, Random.Range(-5.0f, 5.0f));
        Vector3 spawnPos = selectedSpawn.transform.position + offset;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 15.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        else
        {
            spawnPos = selectedSpawn.transform.position;
        }

        // 5. Instantiate and Initialize
        GameObject person = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        
        UnityEngine.AI.NavMeshAgent navAgent = person.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.avoidancePriority = Random.Range(1, 99);
        }

        PersonAgent agent = person.GetComponent<PersonAgent>();
        if (agent != null)
        {
            agent.Initialize(targetRoom, selectedSpawn.transform, minStayTime, maxStayTime, returnToSpawnChance);
        }
        
        activeList.Add(person);
    }
}