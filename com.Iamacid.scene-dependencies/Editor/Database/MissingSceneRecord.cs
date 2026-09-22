using System;
using UnityEngine;

[Serializable]
public class MissingSceneRecord
{
    [SerializeField]
    private string guid;

    [SerializeField]
    private string lastKnownName;

    [SerializeField]
    private string lastKnownPath;

    public string Guid => guid;
    public string LastKnownName => lastKnownName;
    public string LastKnownPath => lastKnownPath;

    public MissingSceneRecord(SceneDependencyEntry entry)
    {
        guid = entry.Guid;
        lastKnownName = entry.LastKnownName;
        lastKnownPath = entry.LastKnownPath;
    }
}