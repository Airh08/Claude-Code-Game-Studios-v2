#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ccgs.EditorScanner;

[Serializable]
public sealed class CcgsProjectSnapshot
{
    public string generatedAtUtc = string.Empty;
    public string unityVersion = string.Empty;
    public List<CcgsSceneSnapshot> scenes = new();
    public List<CcgsAssetSnapshot> scriptableObjects = new();
    public List<string> inputActionAssets = new();
}

[Serializable]
public sealed class CcgsSceneSnapshot
{
    public string path = string.Empty;
    public List<CcgsGameObjectSnapshot> roots = new();
}

[Serializable]
public sealed class CcgsGameObjectSnapshot
{
    public string name = string.Empty;
    public string hierarchyPath = string.Empty;
    public bool active;
    public List<string> components = new();
    public List<string> missingComponents = new();
    public List<CcgsGameObjectSnapshot> children = new();
}

[Serializable]
public sealed class CcgsAssetSnapshot
{
    public string path = string.Empty;
    public string type = string.Empty;
}

public static class CcgsEditorScanner
{
    private const string MenuPath = "CCGS/Project/Generate Editor Snapshot";
    private const string OutputPath = "Library/CCGS/project-snapshot.json";

    [MenuItem(MenuPath)]
    public static void GenerateSnapshot()
    {
        var snapshot = new CcgsProjectSnapshot
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            scenes = ScanScenes(),
            scriptableObjects = ScanScriptableObjects(),
            inputActionAssets = FindInputActionAssets()
        };

        var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        AssetDatabase.Refresh();
        Debug.Log($"CCGS snapshot generated: {output}");
    }

    private static List<CcgsSceneSnapshot> ScanScenes()
    {
        var results = new List<CcgsSceneSnapshot>();
        foreach (var sceneGuid in AssetDatabase.FindAssets("t:Scene"))
        {
            var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                results.Add(new CcgsSceneSnapshot
                {
                    path = path,
                    roots = scene.GetRootGameObjects().Select(root => ScanGameObject(root, root.name)).ToList()
                });
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        return results;
    }

    private static CcgsGameObjectSnapshot ScanGameObject(GameObject gameObject, string hierarchyPath)
    {
        var result = new CcgsGameObjectSnapshot
        {
            name = gameObject.name,
            hierarchyPath = hierarchyPath,
            active = gameObject.activeSelf
        };

        foreach (var component in gameObject.GetComponents<Component>())
        {
            if (component == null)
            {
                result.missingComponents.Add("MissingComponent");
                continue;
            }

            result.components.Add(component.GetType().FullName ?? component.GetType().Name);
        }

        foreach (Transform child in gameObject.transform)
        {
            result.children.Add(ScanGameObject(child.gameObject, $"{hierarchyPath}/{child.name}"));
        }

        return result;
    }

    private static List<CcgsAssetSnapshot> ScanScriptableObjects()
    {
        return AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => new CcgsAssetSnapshot { path = path, type = "ScriptableObject" })
            .ToList();
    }

    private static List<string> FindInputActionAssets()
    {
        return AssetDatabase.FindAssets("t:InputActionAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();
    }
}
#endif
