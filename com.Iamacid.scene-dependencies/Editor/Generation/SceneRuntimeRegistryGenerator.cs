using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SceneRuntimeRegistryGenerator
{
    public static void CreateRuntimeGeneration()
    {
        SceneDependencyDatabase database = SceneDependencyDatabaseCreation.GetDatabase();

        if (!AssetDatabase.IsValidFolder("Assets/SceneDependencies"))
        {
            AssetDatabase.CreateFolder("Assets", "SceneDependencies");
        }

        if (!AssetDatabase.IsValidFolder("Assets/SceneDependencies/Resources"))
        {
            AssetDatabase.CreateFolder("Assets/SceneDependencies", "Resources");
        }

        SceneRuntimeRegistry registry = AssetDatabase.LoadAssetAtPath<SceneRuntimeRegistry>("Assets/SceneDependencies/Resources/SceneRuntimeRegistry.asset");

        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<SceneRuntimeRegistry>();
            AssetDatabase.CreateAsset(registry, "Assets/SceneDependencies/Resources/SceneRuntimeRegistry.asset");
        }

        registry.Clear();

        foreach (SceneDependencyEntry entry in database.SceneEntries)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(entry.Guid);
            List<string> dependencyGuids = new List<string>(entry.DependencyGuids);

            SceneRuntimeEntry runtimeEntry = new SceneRuntimeEntry(entry.Guid, scenePath, dependencyGuids);
            registry.AddEntry(runtimeEntry);
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
    }
}