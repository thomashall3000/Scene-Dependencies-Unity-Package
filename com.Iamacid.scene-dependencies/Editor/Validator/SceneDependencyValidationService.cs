using System.Collections.Generic;
using System.IO;
using UnityEditor;

public static class SceneDependencyValidationService
{
    public static List<string> FindMissingScenes(SceneDependencyDatabase database, string rootGuid)
    {
        List<string> resolvedGuids = SceneDependencyResolver.Resolve(database, rootGuid);
        List<string> missingGuids = new List<string>();

        foreach (string guid in resolvedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                missingGuids.Add(guid);
                continue;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            if (sceneAsset == null)
            {
                missingGuids.Add(guid);
            }
        }

        return missingGuids;
    }

    public static bool HasMissingScenes(SceneDependencyDatabase database, string rootGuid)
    {
        return FindMissingScenes(database, rootGuid).Count > 0;
    }

    public static List<ValidationIssue> ValidateScene(SceneDependencyDatabase database, string rootGuid)
    {
        List<ValidationIssue> issues = new List<ValidationIssue>();

        foreach (string missingGuid in FindMissingScenes(database, rootGuid))
        {
            List<string> dependencyPath = new List<string>();
            HashSet<string> visited = new HashSet<string>();

            if (!FindDependencyPath(database, rootGuid, missingGuid, dependencyPath, visited))
            {
                continue;
            }

            string rootName = GetSceneName(database, rootGuid);
            string missingName = GetSceneName(database, missingGuid);

            MissingSceneRecord missingRecord = database.MissingRecordFinder(missingGuid);

            string formerPath = missingRecord != null
                ? missingRecord.LastKnownPath
                : "";

            string pathDetail = string.IsNullOrEmpty(formerPath)
                ? ""
                : $" Last known path: {formerPath}.";

            if (dependencyPath.Count == 2)
            {
                string message = $"{rootName} directly depends on missing scene {missingName}.{pathDetail}";

                issues.Add(new ValidationIssue(ValidationSeverity.Error, "SCENE003", message, rootGuid));
            }
            else
            {
                List<string> sceneNames = new List<string>();

                foreach (string guid in dependencyPath)
                {
                    sceneNames.Add(GetSceneName(database, guid));
                }

                string readableChain = string.Join(" -> ", sceneNames);

                string message = $"{rootName} has an invalid dependency chain: {readableChain} (missing).{pathDetail}";

                issues.Add(new ValidationIssue(ValidationSeverity.Error, "SCENE008", message, rootGuid));
            }
        }

        return issues;
    }

    private static string GetSceneName(SceneDependencyDatabase database, string guid)
    {
        SceneDependencyEntry entry = database.EntryFinder(guid);

        if (entry != null && !string.IsNullOrEmpty(entry.LastKnownName))
        {
            return entry.LastKnownName;
        }

        MissingSceneRecord missingRecord = database.MissingRecordFinder(guid);

        if (missingRecord != null && !string.IsNullOrEmpty(missingRecord.LastKnownName))
        {
            return missingRecord.LastKnownName;
        }

        string currentPath = AssetDatabase.GUIDToAssetPath(guid);

        if (!string.IsNullOrEmpty(currentPath))
        {
            return Path.GetFileNameWithoutExtension(currentPath);
        }

        return $"unknown scene (GUID: {guid})";
    }

    private static bool FindDependencyPath(SceneDependencyDatabase database, string currentGuid, string targetGuid, List<string> path, HashSet<string> visited)
    {
        if (!visited.Add(currentGuid))
        {
            return false;
        }

        path.Add(currentGuid);

        if (currentGuid == targetGuid)
        {
            return true;
        }

        SceneDependencyEntry entry = database.EntryFinder(currentGuid);

        if (entry != null)
        {
            foreach (string dependencyGuid in entry.DependencyGuids)
            {
                if (FindDependencyPath(database, dependencyGuid, targetGuid, path, visited))
                {
                    return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        visited.Remove(currentGuid);

        return false;
    }
}