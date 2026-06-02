using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SimulationEditorWindow : EditorWindow
{
    private SimulationManager manager;
    private Vector2 scrollPos;

    [MenuItem("Simulacion/Control de Simulacion")]
    public static void ShowWindow()
    {
        GetWindow<SimulationEditorWindow>("Control de Simulacion");
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            manager = Object.FindAnyObjectByType<SimulationManager>();
        }

        if (manager == null)
        {
            EditorGUILayout.HelpBox("No se encontró un SimulationManager en la escena activa.", MessageType.Error);
            if (GUILayout.Button("Crear SimulationManager"))
            {
                GameObject go = new GameObject("SimulationManager");
                manager = go.AddComponent<SimulationManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create SimulationManager");
            }
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Configuración de Escena (Persistente)", EditorStyles.boldLabel);
        
        manager.personPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab NPC", manager.personPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Añadir Nuevo Edificio"))
        {
            Undo.RecordObject(manager, "Add Building");
            manager.buildings.Add(new BuildingData());
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        for (int i = 0; i < manager.buildings.Count; i++)
        {
            DrawBuilding(i);
        }

        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ajustes Globales", EditorStyles.boldLabel);
        manager.minStayTime = EditorGUILayout.FloatField("Tiempo Min en Salon", manager.minStayTime);
        manager.maxStayTime = EditorGUILayout.FloatField("Tiempo Max en Salon", manager.maxStayTime);
        manager.returnToSpawnChance = EditorGUILayout.Slider("Probabilidad de Regreso", manager.returnToSpawnChance, 0f, 1f);
        manager.secondsPerMinute = EditorGUILayout.FloatField("Segundos por Minuto", manager.secondsPerMinute);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
            // In modern Unity, we don't need to manually save scene here, 
            // but setting dirty ensures it's saved with the scene.
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuilding(int index)
    {
        var building = manager.buildings[index];
        EditorGUILayout.BeginVertical("helpBox");
        
        EditorGUILayout.BeginHorizontal();
        building.name = EditorGUILayout.TextField("Nombre Edificio", building.name);
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            Undo.RecordObject(manager, "Remove Building");
            manager.buildings.RemoveAt(index);
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel++;
        
        // Spawn Points
        EditorGUILayout.LabelField("Puntos de Aparicion (Spawns)");
        float totalWeight = 0;
        foreach(var sp in building.spawnPoints) totalWeight += sp.weight;

        for (int j = 0; j < building.spawnPoints.Count; j++)
        {
            var sp = building.spawnPoints[j];
            EditorGUILayout.BeginHorizontal();
            
            sp.transform = (Transform)EditorGUILayout.ObjectField(sp.transform, typeof(Transform), true);
            
            float pct = totalWeight > 0 ? (sp.weight / totalWeight) * 100f : 0;
            EditorGUILayout.LabelField($"{pct:F1}%", GUILayout.Width(45));
            sp.weight = EditorGUILayout.FloatField(sp.weight, GUILayout.Width(40));

            if (GUILayout.Button("F", GUILayout.Width(20))) 
            { 
                if (sp.transform != null) { Selection.activeObject = sp.transform; EditorGUIUtility.PingObject(sp.transform); }
                else { Debug.LogWarning("No hay un objeto asignado a este Spawn."); }
            }
            if (GUILayout.Button("-", GUILayout.Width(20))) building.spawnPoints.RemoveAt(j);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir Spawn"))
        {
            Undo.RecordObject(manager, "Add Spawn");
            GameObject go = new GameObject("Spawn_" + building.name + "_" + building.spawnPoints.Count);
            building.spawnPoints.Add(new SpawnPointData { transform = go.transform, weight = 100f });
        }

        EditorGUILayout.Space();

        // Rooms
        EditorGUILayout.LabelField("Salones");
        for (int j = 0; j < building.rooms.Count; j++)
        {
            var room = building.rooms[j];
            EditorGUILayout.BeginHorizontal();
            room.name = EditorGUILayout.TextField(room.name, GUILayout.Width(80));
            
            room.transform = (Transform)EditorGUILayout.ObjectField(room.transform, typeof(Transform), true);
            room.capacity = EditorGUILayout.IntField("Cap", room.capacity, GUILayout.Width(60));
            
            if (GUILayout.Button("F", GUILayout.Width(20))) 
            { 
                if (room.transform != null) { Selection.activeObject = room.transform; EditorGUIUtility.PingObject(room.transform); }
                else { Debug.LogWarning("No hay un objeto asignado a este Salón."); }
            }
            if (GUILayout.Button("-", GUILayout.Width(20))) building.rooms.RemoveAt(j);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir Salon"))
        {
            Undo.RecordObject(manager, "Add Room");
            GameObject go = new GameObject("Salon_" + building.name + "_" + building.rooms.Count);
            building.rooms.Add(new RoomData { name = "Salon_" + building.rooms.Count, transform = go.transform });
        }

        EditorGUILayout.Space();
        
        // Population per building
        EditorGUILayout.LabelField("Datos de Población (personas por intervalo de 5 min)");
        for (int k = 0; k < building.populationData.Count; k++)
        {
            EditorGUILayout.BeginHorizontal();
            int startMin = k * 5;
            int endMin = (k + 1) * 5;
            EditorGUILayout.LabelField($"Mins {startMin}-{endMin}", GUILayout.Width(80));
            building.populationData[k] = EditorGUILayout.IntField(building.populationData[k]);
            if (GUILayout.Button("-", GUILayout.Width(20))) { building.populationData.RemoveAt(k); break; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir Intervalo (5 min)")) building.populationData.Add(0);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }
}
