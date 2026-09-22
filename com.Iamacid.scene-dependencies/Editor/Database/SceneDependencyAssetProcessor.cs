using UnityEditor;

public class SceneDependencyAssetProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        bool sceneChanged = ContainsScene(importedAssets) || ContainsScene(deletedAssets) || ContainsScene(movedAssets) || ContainsScene(movedFromAssetPaths);

        if (!sceneChanged)
        {
            return;
        }

        SceneDependencyDatabase database = SceneDependencyDatabaseCreation.GetDatabase();

        if (database == null)
        {
            return;
        }

        SceneDiscovery.FindScenes(database);

        EditorApplication.delayCall += SceneRuntimeRegistryGenerator.CreateRuntimeGeneration;
    }

    private static bool ContainsScene(string[] assetPaths)
    {
        foreach (string path in assetPaths)
        {
            if (path.EndsWith(".unity"))
            {
                return true;
            }
        }

        return false;
    }
}