using System.Collections.Generic;
using System.IO;
using UnityEditor;

public static class SceneDiscovery
{
    public static void FindScenes(SceneDependencyDatabase database)
    {
        string[] foundSceneGuids = AssetDatabase.FindAssets("t:Scene");
        HashSet<string> existingSceneGuids = new HashSet<string>();

        bool changed = false;

        foreach (string guid in foundSceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                continue;
            }

            existingSceneGuids.Add(guid);

            string name = Path.GetFileNameWithoutExtension(path);
            SceneDependencyEntry entry = database.EntryFinder(guid);

            if (entry == null)
            {
                entry = new SceneDependencyEntry(guid);
                database.AddEntry(entry);
                changed = true;
            }

            if (entry.LastKnownName != name || entry.LastKnownPath != path)
            {
                entry.UpdateMetadata(name, path);
                changed = true;
            }

            if (database.RemoveMissingRecord(guid))
            {
                changed = true;
            }
        }

        List<string> removedSceneGuids = new List<string>();

        foreach (SceneDependencyEntry entry in database.SceneEntries)
        {
            if (!existingSceneGuids.Contains(entry.Guid))
            {
                removedSceneGuids.Add(entry.Guid);
            }
        }

        HashSet<string> referencedGuids = new HashSet<string>();

        foreach (SceneDependencyEntry entry in database.SceneEntries)
        {
            if (!existingSceneGuids.Contains(entry.Guid))
            {
                continue;
            }

            foreach (string dependencyGuid in entry.DependencyGuids)
            {
                referencedGuids.Add(dependencyGuid);
            }
        }

        foreach (string guid in removedSceneGuids)
        {
            SceneDependencyEntry entry = database.EntryFinder(guid);

            if (entry == null)
            {
                continue;
            }

            if (referencedGuids.Contains(guid) && database.MissingRecordFinder(guid) == null)
            {
                database.AddMissingRecord(new MissingSceneRecord(entry));
                changed = true;
            }

            if (database.RemoveEntry(guid))
            {
                changed = true;
            }
        }

        List<string> recordsToRemove = new List<string>();

        foreach (MissingSceneRecord record in database.MissingSceneRecords)
        {
            if (existingSceneGuids.Contains(record.Guid) || !referencedGuids.Contains(record.Guid))
            {
                recordsToRemove.Add(record.Guid);
            }
        }

        foreach (string guid in recordsToRemove)
        {
            if (database.RemoveMissingRecord(guid))
            {
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
}