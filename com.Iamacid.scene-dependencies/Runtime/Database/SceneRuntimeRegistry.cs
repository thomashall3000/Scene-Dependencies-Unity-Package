using System.Collections.Generic;
using UnityEngine;

public class SceneRuntimeRegistry : ScriptableObject
{
    [SerializeField] private List<SceneRuntimeEntry> sceneEntries = new List<SceneRuntimeEntry>();

    public IReadOnlyList<SceneRuntimeEntry> SceneEntries => sceneEntries;

    public SceneRuntimeEntry EntryFinder(string guid)
    {
        foreach (SceneRuntimeEntry entry in sceneEntries)
        {
            if (entry.Guid == guid)
            {
                return entry;
            }
        }

        return null;
    }

    public void Clear()
    {
        sceneEntries.Clear();
    }

    public void AddEntry(SceneRuntimeEntry entry)
    {
        sceneEntries.Add(entry);
    }
}