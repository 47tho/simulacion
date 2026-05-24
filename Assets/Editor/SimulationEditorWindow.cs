using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SimulationEditorWindow : EditorWindow
{
    private SimulationConfig config;
    private Vector2 scrollPos;
    private bool showPopulation = false;

    [MenuItem("Simulacion/Control de Simulacion")]
    public static void ShowWindow()
    {
        GetWindow<SimulationEditorWindow>("Control de Simulacion");
    }

    private void OnEnable()
    {
        if (config == null)
        {
            config = AssetDatabase.LoadAssetAtPath<SimulationConfig>("Assets/DefaultSimulationConfig.asset");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical("box");
        config = (SimulationConfig)EditorGUILayout.ObjectField("Configuracion", config, typeof(SimulationConfig), false);
        
        if (config == null)
        {
            if (GUILayout.Button("Crear Nueva Configuracion"))
            {
                CreateNewConfig();
            }
            EditorGUILayout.HelpBox("Asigna o crea un archivo de configuracion para empezar.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space();
        
        showPopulation = EditorGUILayout.Foldout(showPopulation, "Datos de Población (personas por minuto)");
        if (showPopulation)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < config.populationData.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Minuto {i}", GUILayout.Width(80));
                config.populationData[i] = EditorGUILayout.IntField(config.populationData[i]);
                if (GUILayout.Button("-", GUILayout.Width(20))) { config.populationData.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Añadir Minuto")) config.populationData.Add(0);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Añadir Nuevo Edificio"))
        {
            config.buildings.Add(new BuildingData());
            EditorUtility.SetDirty(config);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        for (int i = 0; i < config.buildings.Count; i++)
        {
            DrawBuilding(i);
        }

        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ajustes Globales", EditorStyles.boldLabel);
        config.minStayTime = EditorGUILayout.FloatField("Tiempo Min en Salon", config.minStayTime);
        config.maxStayTime = EditorGUILayout.FloatField("Tiempo Max en Salon", config.maxStayTime);
        config.returnToSpawnChance = EditorGUILayout.Slider("Probabilidad de Regreso", config.returnToSpawnChance, 0f, 1f);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(config);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuilding(int index)
    {
        var building = config.buildings[index];
        EditorGUILayout.BeginVertical("helpBox");
        
        EditorGUILayout.BeginHorizontal();
        building.name = EditorGUILayout.TextField("Nombre Edificio", building.name);
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            config.buildings.RemoveAt(index);
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

            if (GUILayout.Button("F", GUILayout.Width(20))) { Selection.activeObject = sp.transform; EditorGUIUtility.PingObject(sp.transform); }
            if (GUILayout.Button("-", GUILayout.Width(20))) building.spawnPoints.RemoveAt(j);
EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir Spawn"))
        {
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
            room.name = EditorGUILayout.TextField(room.name, GUILayout.Width(100));
            room.transform = (Transform)EditorGUILayout.ObjectField(room.transform, typeof(Transform), true);
            room.capacity = EditorGUILayout.IntField("Cap", room.capacity, GUILayout.Width(60));
            if (GUILayout.Button("F", GUILayout.Width(20))) { Selection.activeObject = room.transform; EditorGUIUtility.PingObject(room.transform); }
            if (GUILayout.Button("-", GUILayout.Width(20))) building.rooms.RemoveAt(j);
EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir Salon"))
        {
            GameObject go = new GameObject("Salon_" + building.name + "_" + building.rooms.Count);
            building.rooms.Add(new RoomData { name = "Salon " + building.rooms.Count, transform = go.transform });
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    private void CreateNewConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject("Guardar Configuracion", "SimulationConfig", "asset", "Elige donde guardar la configuracion");
        if (string.IsNullOrEmpty(path)) return;

        SimulationConfig newConfig = ScriptableObject.CreateInstance<SimulationConfig>();
        AssetDatabase.CreateAsset(newConfig, path);
        AssetDatabase.SaveAssets();
        config = newConfig;
    }
}
