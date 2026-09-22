using UnityEditor;
using UnityEngine;

public static class SceneDependencyService
{
    public static bool TryAddDependency(SceneDependencyDatabase database, string sourceGuid, string dependencyGuid)
    {
        var result = SceneDependencyValidator.CanAddDependency(database, sourceGuid, dependencyGuid);

        if (result.isValid)
        {
            SceneDependencyEntry entry = database.EntryFinder(sourceGuid);

            entry.AddDependency(dependencyGuid);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            SceneRuntimeRegistryGenerator.CreateRuntimeGeneration();

            return true;
        }

        return false;
    }

    public static bool RemoveDependency(SceneDependencyDatabase database, string sourceGuid, string dependencyGuid)
    {

        SceneDependencyEntry entry = database.EntryFinder(sourceGuid);

        if (entry == null)
        {
            return false;
        }

        if (entry.RemoveDependency(dependencyGuid))
        {

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            SceneRuntimeRegistryGenerator.CreateRuntimeGeneration();

            return true;

        }

        return false;
    }
}
