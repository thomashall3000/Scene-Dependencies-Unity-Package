using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneDependencyDatabase : ScriptableObject
{

    [SerializeField]
    private int schemaVersion = 1;

    [SerializeField]
    private List<SceneDependencyEntry> sceneEntries = new List<SceneDependencyEntry>();

    [SerializeField]
    private List<MissingSceneRecord> missingSceneRecords = new List<MissingSceneRecord>();

    public int SchemaVersion => schemaVersion;
    public IReadOnlyList<SceneDependencyEntry> SceneEntries => sceneEntries;
    public IReadOnlyList<MissingSceneRecord> MissingSceneRecords => missingSceneRecords;

    public void AddEntry(SceneDependencyEntry entry)
    {

        if (!sceneEntries.Exists(existingEntry => existingEntry.Guid == entry.Guid))
        {

            sceneEntries.Add(entry);

        }

    }

    public SceneDependencyEntry EntryFinder(string guid)
    {

        foreach (SceneDependencyEntry entry in sceneEntries)
        {
            if (entry.Guid == guid)
            {
                return entry;
            }
        }

        return null;
    }

    public bool RemoveEntry(string guid)
    {
        return sceneEntries.RemoveAll(entry => entry.Guid == guid) > 0;
    }

    public MissingSceneRecord MissingRecordFinder(string guid)
    {
        foreach (MissingSceneRecord record in missingSceneRecords)
        {
            if (record.Guid == guid)
            {
                return record;
            }
        }

        return null;
    }

    public void AddMissingRecord(MissingSceneRecord record)
    {
        if (MissingRecordFinder(record.Guid) == null)
        {
            missingSceneRecords.Add(record);
        }
    }

    public bool RemoveMissingRecord(string guid)
    {
        return missingSceneRecords.RemoveAll(record => record.Guid == guid) > 0;
    }

}

[Serializable]
public class SceneDependencyEntry
{
    [SerializeField]
    private string guid;

    [SerializeField]
    private string lastKnownName;

    [SerializeField]
    private string lastKnownPath;

    [SerializeField]
    private List<string> dependencyGUIDs = new List<string>();

    public string Guid => guid;
    public IReadOnlyList<string> DependencyGuids => dependencyGUIDs;
    public string LastKnownName => lastKnownName;
    public string LastKnownPath => lastKnownPath;


    public SceneDependencyEntry(string guid)
    {
        this.guid = guid;
    }

    public void UpdateMetadata(string name, string path)
    {
        lastKnownName = name;
        lastKnownPath = path;
    }

    public bool HasDependency(string dependencyGuid)
    {
        return dependencyGUIDs.Contains(dependencyGuid);
    }

    public void AddDependency(string dependencyGuid)
    {
        dependencyGUIDs.Add(dependencyGuid);
    }

    public bool RemoveDependency(string dependencyGuid)
    {
        return dependencyGUIDs.Remove(dependencyGuid);
    }
}
