using System.Collections.Generic;

public static class SceneDependencyResolver
{
    public static List<string> Resolve(SceneDependencyDatabase database, string rootGuid)
    {
        List<string> resolvedGuids = new List<string>();
        HashSet<string> visited = new HashSet<string>();

        Visit(database, rootGuid, resolvedGuids, visited);

        return resolvedGuids;
    }

    private static void Visit(SceneDependencyDatabase database, string currentGuid, List<string> resolvedGuids, HashSet<string> visited)
    {
        if (visited.Contains(currentGuid))
        {
            return;
        }

        visited.Add(currentGuid);
        resolvedGuids.Add(currentGuid);

        SceneDependencyEntry entry = database.EntryFinder(currentGuid);

        if (entry == null)
        {
            return;
        }

        foreach (string dependencyGuid in entry.DependencyGuids)
        {
            Visit(database, dependencyGuid, resolvedGuids, visited);
        }
    }
}