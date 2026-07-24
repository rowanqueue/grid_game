#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class WinScreenActionButtonsSetup
{
    const string MenuPath = "Tools/Flora/Setup Win Screen Action Buttons";
    const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        Setup(showDialogs: true);
    }

    /// <summary>
    /// Batch-mode entry: Unity.exe -batchmode -quit -projectPath ... -executeMethod WinScreenActionButtonsSetup.SetupBatch
    /// </summary>
    public static void SetupBatch()
    {
        if (!EditorSceneManager.GetActiveScene().path.EndsWith("Gameplay.unity"))
        {
            EditorSceneManager.OpenScene(GameplayScenePath);
        }
        Setup(showDialogs: false);
        EditorSceneManager.SaveOpenScenes();
    }

    public static void Setup(bool showDialogs)
    {
        GameObject winScreen = null;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            winScreen = FindInChildren(root.transform, "WinScreen");
            if (winScreen != null)
            {
                break;
            }
        }

        if (winScreen == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Win Screen Buttons", "Could not find WinScreen in the open scene.", "OK");
            }
            else
            {
                Debug.LogError("WinScreenActionButtonsSetup: Could not find WinScreen.");
            }
            return;
        }

        GameController controller = Object.FindObjectOfType<GameController>(true);
        if (controller == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Win Screen Buttons", "Could not find GameController.", "OK");
            }
            else
            {
                Debug.LogError("WinScreenActionButtonsSetup: Could not find GameController.");
            }
            return;
        }

        Transform actionRootTf = winScreen.transform.Find("ActionButtons");
        GameObject actionRoot = actionRootTf != null ? actionRootTf.gameObject : null;
        if (actionRoot == null)
        {
            actionRoot = new GameObject("ActionButtons");
            Undo.RegisterCreatedObjectUndo(actionRoot, "Create ActionButtons");
            actionRoot.transform.SetParent(winScreen.transform, false);
            actionRoot.transform.localPosition = new Vector3(0f, -1.15f, -0.1f);
        }

        flora.Button highScore = EnsureButton(
            actionRoot.transform,
            "HighScoresButton",
            "High Scores",
            new Vector3(-1.35f, 0f, 0f),
            controller,
            nameof(GameController.OpenHighScoresFromWin));

        flora.Button restart = EnsureButton(
            actionRoot.transform,
            "RestartButton",
            "Restart",
            new Vector3(1.35f, 0f, 0f),
            controller,
            nameof(GameController.RestartFromWin));

        WinScreenDisplay display = winScreen.GetComponent<WinScreenDisplay>();
        if (display == null)
        {
            display = Undo.AddComponent<WinScreenDisplay>(winScreen);
        }

        SerializedObject so = new SerializedObject(display);
        so.FindProperty("actionButtonsRoot").objectReferenceValue = actionRoot;
        so.FindProperty("highScoreButton").objectReferenceValue = highScore;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Start hidden; ShowRoutine reveals after the score animation.
        actionRoot.SetActive(false);

        EditorUtility.SetDirty(winScreen);
        EditorUtility.SetDirty(display);
        EditorSceneManager.MarkSceneDirty(winScreen.scene);

        if (showDialogs)
        {
            Selection.activeGameObject = actionRoot;
            EditorGUIUtility.PingObject(actionRoot);
        }

        Debug.Log("WinScreen ActionButtons ready under WinScreen (HighScoresButton, RestartButton).");
    }

    static GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent.gameObject;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found = FindInChildren(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    static flora.Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector3 localPos,
        GameController controller,
        string methodName)
    {
        Transform existing = parent.Find(name);
        GameObject root = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
        }
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        root.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = root.AddComponent<BoxCollider2D>();
        }
        collider.size = new Vector2(6.000322f, 1.5530369f);

        Transform panelTf = root.transform.Find("9-Sliced");
        GameObject panel = panelTf != null ? panelTf.gameObject : new GameObject("9-Sliced");
        panel.transform.SetParent(root.transform, false);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = new Vector3(2f, 2f, 1f);

        SpriteRenderer panelSr = panel.GetComponent<SpriteRenderer>();
        if (panelSr == null)
        {
            panelSr = panel.AddComponent<SpriteRenderer>();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Settings_RemoveAd.PNG");
        panelSr.sprite = sprite;
        panelSr.drawMode = SpriteDrawMode.Sliced;
        panelSr.size = new Vector2(3f, 1f);
        panelSr.color = Color.white;

        Transform labelTf = panel.transform.Find("Label");
        if (labelTf == null)
        {
            labelTf = panel.transform.Find("Game");
        }
        GameObject labelGo = labelTf != null ? labelTf.gameObject : new GameObject("Label");
        labelGo.name = "Label";
        labelGo.transform.SetParent(panel.transform, false);
        labelGo.transform.localPosition = Vector3.zero;
        labelGo.transform.localScale = Vector3.one;

        TextMeshPro tmp = labelGo.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            tmp = labelGo.AddComponent<TextMeshPro>();
        }
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Nunito-Bold SDF.asset");
        if (font == null)
        {
            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
            }
        }
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = label;
        tmp.fontSize = 5.35f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 72f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2264151f, 0.2264151f, 0.2264151f, 1f);
        RectTransform rect = labelGo.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }

        flora.Button button = root.GetComponent<flora.Button>();
        if (button == null)
        {
            button = root.AddComponent<flora.Button>();
        }
        button.type = flora.ButtonType.None;
        button.display = panelSr;
        button.hoverColor = new Color(0.7529412f, 0.7529412f, 0.7529412f, 1f);
        button.disabled = false;

        SerializedObject buttonSo = new SerializedObject(button);
        buttonSo.FindProperty("display").objectReferenceValue = panelSr;
        buttonSo.FindProperty("type").enumValueIndex = 0;
        buttonSo.FindProperty("disabled").boolValue = false;
        buttonSo.ApplyModifiedPropertiesWithoutUndo();

        while (button._event != null && button._event.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button._event, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            button._event,
            (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), controller, methodName));

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(button);
        return button;
    }
}
#endif
