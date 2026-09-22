using System;
using System.Collections.Generic;

public static class SceneDependencyValidator
{
    public static (bool isValid, ValidationIssue issue) CanAddDependency(SceneDependencyDatabase database, string sourceGuid, string dependencyGuid)
    {
        if (sourceGuid == dependencyGuid)
        {
            return (false, CreateIssue(ValidationSeverity.Error, "SCENE001", "Scene cannot depend on itself", sourceGuid));
        }

        SceneDependencyEntry sourceEntry = database.EntryFinder(sourceGuid);

        if (sourceEntry == null)
        {
            return (false, CreateIssue(ValidationSeverity.Error, "SCENE009", "The selected scene could not be found in the Scene Dependencies database.", sourceGuid));
        }

        if (sourceEntry.HasDependency(dependencyGuid))
        {
            return (false, CreateIssue(ValidationSeverity.Warning, "SCENE006", "The dependency already exists, adding it again is unnecessary.", sourceGuid));
        }

        if (database.EntryFinder(dependencyGuid) == null)
        {
            return (false, CreateIssue(ValidationSeverity.Error, "SCENE003", "The scene you are trying to add as a dependency could not be found.", sourceGuid));
        }

        HashSet<string> visited = new HashSet<string>();
        Stack<string> scenesToCheck = new Stack<string>();

        scenesToCheck.Push(dependencyGuid);

        while (scenesToCheck.Count > 0)
        {
            string currentGuid = scenesToCheck.Pop();

            if (currentGuid == sourceGuid)
            {
                return (false, CreateIssue(ValidationSeverity.Error, "SCENE002", "Adding this dependency would create a circular dependency.", sourceGuid));
            }

            if (!visited.Add(currentGuid))
            {
                continue;
            }

            SceneDependencyEntry currentEntry = database.EntryFinder(currentGuid);

            if (currentEntry == null)
            {
                return (false, CreateIssue(ValidationSeverity.Error, "SCENE003", "The proposed dependency contains a reference to a scene that could not be found.", sourceGuid));
            }

            foreach (string nextGuid in currentEntry.DependencyGuids)
            {
                scenesToCheck.Push(nextGuid);
            }
        }

        return (true, null);
    }

    private static ValidationIssue CreateIssue(ValidationSeverity severity, string code, string message, string sourceGuid)
    {
        return new ValidationIssue(severity, code, message, sourceGuid);
    }
}