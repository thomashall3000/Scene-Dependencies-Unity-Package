using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    private static HashSet<string> loadingScenes = new HashSet<string>();
    private static HashSet<string> unloadingScenes = new HashSet<string>();

    private static Dictionary<string, HashSet<string>> rootScenes = new Dictionary<string, HashSet<string>>();
    private static Dictionary<string, HashSet<string>> sceneOwners = new Dictionary<string, HashSet<string>>();

    public void Load(string rootGuid)
    {
        SceneRuntimeRegistry registry = Resources.Load<SceneRuntimeRegistry>("SceneRuntimeRegistry");

        if (registry == null)
        {
            Debug.LogError("Scene Runtime Registry could not be found.");
            return;
        }

        SceneRuntimeResolver resolver = new SceneRuntimeResolver();
        List<string> dependencyGuids = resolver.Resolve(rootGuid);
        List<string> scenePaths = new List<string>();

        foreach (string dependencyGuid in dependencyGuids)
        {
            SceneRuntimeEntry entry = registry.EntryFinder(dependencyGuid);

            if (entry == null)
            {
                Debug.LogError($"Scene {dependencyGuid} does not exist.");
                return;
            }

            if (string.IsNullOrEmpty(entry.ScenePath))
            {
                Debug.LogError($"Scene {dependencyGuid} has no valid scene path.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(entry.ScenePath))
            {
                Debug.LogError($"Scene {dependencyGuid} is not available for loading: {entry.ScenePath}");
                return;
            }

            scenePaths.Add(entry.ScenePath);
        }

        RegisterRoot(rootGuid, dependencyGuids);

        for (int i = 0; i < scenePaths.Count; i++)
        {
            string sceneGuid = dependencyGuids[i];
            string scenePath = scenePaths[i];

            if (!BeginLoad(sceneGuid, scenePath))
            {
                FailRootsRequiringScene(sceneGuid);
                return;
            }
        }
    }

    public void UnloadRoot(string rootGuid)
    {
        SceneRuntimeRegistry registry = Resources.Load<SceneRuntimeRegistry>("SceneRuntimeRegistry");

        if (registry == null)
        {
            Debug.LogError("Scene Runtime Registry could not be found.");
            return;
        }

        ReleaseAndUnloadRoot(rootGuid, registry);
    }

    private void RegisterRoot(string rootGuid, List<string> resolvedGuids)
    {
        if (rootScenes.ContainsKey(rootGuid))
        {
            return;
        }

        rootScenes.Add(rootGuid, new HashSet<string>(resolvedGuids));

        foreach (string sceneGuid in resolvedGuids)
        {
            if (!sceneOwners.ContainsKey(sceneGuid))
            {
                sceneOwners.Add(sceneGuid, new HashSet<string>());
            }

            sceneOwners[sceneGuid].Add(rootGuid);
        }
    }

    private List<string> ReleaseRootOwnership(string rootGuid)
    {
        if (!rootScenes.ContainsKey(rootGuid))
        {
            return new List<string>();
        }

        List<string> scenesToUnload = new List<string>();

        rootScenes.Remove(rootGuid);

        foreach (KeyValuePair<string, HashSet<string>> entry in sceneOwners)
        {
            entry.Value.Remove(rootGuid);

            if (entry.Value.Count == 0)
            {
                scenesToUnload.Add(entry.Key);
            }
        }

        foreach (string sceneGuid in scenesToUnload)
        {
            sceneOwners.Remove(sceneGuid);
        }

        return scenesToUnload;
    }

    private void ReleaseAndUnloadRoot(string rootGuid, SceneRuntimeRegistry registry)
    {
        List<string> scenesToUnload = ReleaseRootOwnership(rootGuid);

        foreach (string sceneGuid in scenesToUnload)
        {
            SceneRuntimeEntry entry = registry.EntryFinder(sceneGuid);

            if (entry == null)
            {
                Debug.LogError($"Cannot unload scene {sceneGuid}: registry entry not found.");
                continue;
            }

            BeginUnload(sceneGuid, entry.ScenePath);
        }
    }

    private void FailRootsRequiringScene(string failedSceneGuid)
    {
        if (!sceneOwners.TryGetValue(failedSceneGuid, out HashSet<string> owners))
        {
            return;
        }

        List<string> affectedRoots = new List<string>(owners);

        SceneRuntimeRegistry registry = Resources.Load<SceneRuntimeRegistry>("SceneRuntimeRegistry");

        if (registry == null)
        {
            Debug.LogError("Cannot clean up failed scene requests: runtime registry not found.");
            return;
        }

        foreach (string rootGuid in affectedRoots)
        {
            Debug.LogError(
                $"Cancelling root {rootGuid} because required scene {failedSceneGuid} failed to load."
            );

            ReleaseAndUnloadRoot(rootGuid, registry);
        }
    }

    private bool BeginLoad(string sceneGuid, string scenePath)
    {
        if (unloadingScenes.Contains(scenePath))
        {
            return true;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);

        if (scene.IsValid() && scene.isLoaded)
        {
            return true;
        }

        if (loadingScenes.Contains(scenePath))
        {
            return true;
        }

        loadingScenes.Add(scenePath);

        AsyncOperation operation;

        try
        {
            operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            loadingScenes.Remove(scenePath);
            Debug.LogError($"Could not begin loading scene {scenePath}: {exception}");
            return false;
        }

        if (operation == null)
        {
            loadingScenes.Remove(scenePath);
            Debug.LogError($"Could not begin loading scene: {scenePath}");
            return false;
        }

        operation.completed += completedOperation =>
        {
            loadingScenes.Remove(scenePath);

            Scene completedScene = SceneManager.GetSceneByPath(scenePath);

            if (!completedScene.IsValid() || !completedScene.isLoaded)
            {
                Debug.LogError($"Scene did not finish loading successfully: {scenePath}");
                FailRootsRequiringScene(sceneGuid);
                return;
            }

            if (!sceneOwners.ContainsKey(sceneGuid))
            {
                BeginUnload(sceneGuid, scenePath);
            }
        };

        return true;
    }

    private void BeginUnload(string sceneGuid, string scenePath)
    {
        if (unloadingScenes.Contains(scenePath))
        {
            return;
        }

        if (sceneOwners.ContainsKey(sceneGuid))
        {
            return;
        }

        if (loadingScenes.Contains(scenePath))
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        unloadingScenes.Add(scenePath);

        AsyncOperation operation;

        try
        {
            operation = SceneManager.UnloadSceneAsync(scene);
        }
        catch (Exception exception)
        {
            unloadingScenes.Remove(scenePath);
            Debug.LogError($"Could not begin unloading scene {scenePath}: {exception}");
            return;
        }

        if (operation == null)
        {
            unloadingScenes.Remove(scenePath);
            Debug.LogError($"Could not begin unloading scene: {scenePath}");
            return;
        }

        operation.completed += completedOperation =>
        {
            unloadingScenes.Remove(scenePath);

            if (sceneOwners.ContainsKey(sceneGuid))
            {
                if (!BeginLoad(sceneGuid, scenePath))
                {
                    FailRootsRequiringScene(sceneGuid);
                }
            }
        };
    }
}