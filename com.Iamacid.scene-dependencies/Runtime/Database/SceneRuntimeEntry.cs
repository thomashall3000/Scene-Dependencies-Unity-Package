using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneRuntimeEntry
{
    [SerializeField] private string guid;
    [SerializeField] private string scenePath;
    [SerializeField] private List<string> dependencyGuids = new List<string>();

    public string Guid => guid;
    public string ScenePath => scenePath;
    public IReadOnlyList<string> DependencyGuids => dependencyGuids;

    public SceneRuntimeEntry(string guid, string scenePath, List<string> dependencyGuids)
    {
        this.guid = guid;
        this.scenePath = scenePath;
        this.dependencyGuids = new List<string>(dependencyGuids);
    }
}