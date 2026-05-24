using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnPointData
{
    public Transform transform;
    [Range(0, 100)]
    public float weight = 100f; // Relative probability weight
}

[System.Serializable]
public class RoomData
{
    public string name;
    public Transform transform;
    public int capacity = 20;
    public int currentOccupancy = 0;
}

[System.Serializable]
public class BuildingData
{
    public string name = "Building";
    public List<SpawnPointData> spawnPoints = new List<SpawnPointData>();
    public List<RoomData> rooms = new List<RoomData>();
}

[CreateAssetMenu(fileName = "SimulationConfig", menuName = "Simulacion/Config")]
public class SimulationConfig : ScriptableObject
{
    public List<BuildingData> buildings = new List<BuildingData>();
    
    [Header("Behavior Settings")]
    public float minStayTime = 10f;
    public float maxStayTime = 30f;
    public float returnToSpawnChance = 0.5f;

    [Header("Population Data (People per minute)")]
    public List<int> populationData = new List<int> { 
        100, 110, 131, 142, 172, 147, 162, 178, 193, 193, 
        213, 206, 209, 217, 247, 238, 238, 266, 254, 252, 253 
    };
}
