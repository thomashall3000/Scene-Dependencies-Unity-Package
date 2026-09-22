using UnityEditor;
using UnityEngine;

public class SceneDependencyDatabaseCreation
{

    public static SceneDependencyDatabase GetDatabase()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SceneDependencies")) 
        {
            AssetDatabase.CreateFolder("Assets", "SceneDependencies");
        }

        string[] databaseGuids = AssetDatabase.FindAssets("t:SceneDependencyDatabase");

        SceneDependencyDatabase database;

        if (databaseGuids.Length == 0)
        {
            database = ScriptableObject.CreateInstance<SceneDependencyDatabase>();

            AssetDatabase.CreateAsset(database, "Assets/SceneDependencies/SceneDependencyDatabase.asset");

            SceneDiscovery.FindScenes(database);
        }
        else
        {
            string databasePath = AssetDatabase.GUIDToAssetPath(databaseGuids[0]);

            database = AssetDatabase.LoadAssetAtPath<SceneDependencyDatabase>(databasePath);
        }

        return database;
    }
}
