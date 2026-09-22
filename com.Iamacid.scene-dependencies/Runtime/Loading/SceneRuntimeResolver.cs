using System.Collections.Generic;
using UnityEngine;

public class SceneRuntimeResolver
{

    public List<string> Resolve(string rootGuid)
    {

        SceneRuntimeRegistry registry = Resources.Load<SceneRuntimeRegistry>("SceneRuntimeRegistry");

        if (registry == null)
        {
            Debug.LogError("Scene Runtime Registry could not be found.");
            return new List<string>();
        }

        HashSet<string> visited = new HashSet<string>();
        List<string> resolvedGuids = new List<string>();

        Visit(rootGuid, registry, visited, resolvedGuids);

        return resolvedGuids;
    }

    private void Visit(string currentGuid, SceneRuntimeRegistry registry, HashSet<string> visited, List<string> resolvedGuids)
    {
        if (visited.Contains(currentGuid))
        {
            return;
        }

        visited.Add(currentGuid);
        resolvedGuids.Add(currentGuid);

        SceneRuntimeEntry entry = registry.EntryFinder(currentGuid);

        if (entry == null)
        {
            return;
        }

        foreach (string dependencyGuid in entry.DependencyGuids)
        {
            Visit(dependencyGuid, registry, visited, resolvedGuids);
        }
    }
}