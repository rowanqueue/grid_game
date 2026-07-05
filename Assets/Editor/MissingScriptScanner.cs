using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scenes and prefabs for MonoBehaviour slots with no script (Missing Script).
/// Menu: Tools &gt; Grid Game &gt; Find Missing Scripts
/// Batch: Unity -batchmode -quit -executeMethod MissingScriptScanner.RunBatchMode
/// </summary>
public static class MissingScriptScanner
{
    private const string MenuRoot = "Tools/Grid Game/";

    [MenuItem(MenuRoot + "Find Missing Scripts")]
    public static void RunFromMenu()
    {
        int n = ScanAll();
        if (n == 0)
            Debug.Log("[MissingScriptScanner] OK — no missing MonoBehaviour scripts found.");
        else
            Debug.LogError($"[MissingScriptScanner] Found {n} missing script slot(s). See errors above.");
    }

    public static void RunBatchMode()
    {
        int n = ScanAll();
        Debug.Log($"[MissingScriptScanner] Batch complete. Missing count: {n}");
        EditorApplication.Exit(n > 0 ? 1 : 0);
    }

    static int ScanAll()
    {
        int total = 0;
        total += ScanAllScenes();
        total += ScanAllPrefabs();
        return total;
    }

    static int ScanAllScenes()
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/"))
                    continue;

                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Scene scene = SceneManager.GetActiveScene();
                foreach (GameObject root in scene.GetRootGameObjects())
                    total += ScanGameObjectRecursive(root, path, "scene");
            }
        }
        finally
        {
            // Batch mode often starts with no scene loaded; restoring an empty setup throws.
            if (sceneSetup != null && sceneSetup.Length > 0)
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                }
                catch (System.ArgumentException ex)
                {
                    Debug.LogWarning("[MissingScriptScanner] Could not restore previous scene setup: " + ex.Message);
                }
            }
        }

        return total;
    }

    static int ScanAllPrefabs()
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/"))
                continue;

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                total += ScanGameObjectRecursive(root, path, "prefab");
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return total;
    }

    static int ScanGameObjectRecursive(GameObject go, string assetPath, string context)
    {
        int count = 0;
        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (missing > 0)
        {
            count += missing;
            string hierarchyPath = GetHierarchyPath(go);
            Debug.LogError(
                $"[MissingScript] ({context}) {assetPath}\n  GameObject: {hierarchyPath}\n  Missing script slot(s): {missing}",
                go);
        }

        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            count += ScanGameObjectRecursive(t.GetChild(i).gameObject, assetPath, context);

        return count;
    }

    static string GetHierarchyPath(GameObject go)
    {
        if (go == null)
            return "(null)";
        if (go.transform.parent == null)
            return go.name;
        return GetHierarchyPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
