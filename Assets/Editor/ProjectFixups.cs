using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;

public static class ProjectFixups
{
    private const string XrRigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    // GameObject.Find only searches active objects. Stairs/Entrance start inactive in
    // CustomCampusTourScene, so every lookup in this file must go through hierarchy
    // traversal instead, which works regardless of active state.
    private static GameObject FindInScene(string path)
    {
        var segments = path.Split('/');

        Transform current = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            current = FindDescendant(root.transform, segments[0]);
            if (current != null) break;
        }
        if (current == null) return null;

        for (int i = 1; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null) return null;
        }
        return current.gameObject;
    }

    // Depth-first search for a transform named `name`, starting at (and including) `root`.
    // Unlike GameObject.Find this works on inactive objects and objects that have been reparented.
    private static Transform FindDescendant(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDescendant(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    // ---------- Step A: import the stitched panorama photos + rewire materials ----------
    [MenuItem("Tools/Project Fixups/A - Import Panoramas And Rewire Materials")]
    public static void ImportPanoramasAndRewireMaterials()
    {
        ConfigurePanoramaTexture("Assets/CUSTOM IMG/Converted/Stairs_Equirect.png");
        ConfigurePanoramaTexture("Assets/CUSTOM IMG/Converted/Entrance_Equirect.png");
        ConfigurePanoramaTexture("Assets/CUSTOM IMG/Converted/FabLab_Equirect.png");

        RewireMaterialTexture("Assets/Materials/StairsMat.mat", "Assets/CUSTOM IMG/Converted/Stairs_Equirect.png");
        RewireMaterialTexture("Assets/Materials/EntranceMat.mat", "Assets/CUSTOM IMG/Converted/Entrance_Equirect.png");
        RewireMaterialTexture("Assets/Materials/FabLabMat.mat", "Assets/CUSTOM IMG/Converted/FabLab_Equirect.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Fixups] Step A complete.");
    }

    private static void ConfigurePanoramaTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[Fixups] Could not find TextureImporter at {path}");
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = true;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Compressed;

        var androidSettings = importer.GetPlatformTextureSettings("Android");
        androidSettings.overridden = true;
        androidSettings.maxTextureSize = 4096;
        androidSettings.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(androidSettings);

        importer.SaveAndReimport();
        Debug.Log($"[Fixups] Configured texture import settings for {path}");
    }

    private static void RewireMaterialTexture(string materialPath, string texturePath)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (mat == null) { Debug.LogError($"[Fixups] Material not found: {materialPath}"); return; }
        if (tex == null) { Debug.LogError($"[Fixups] Texture not found: {texturePath}"); return; }

        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(mat);
        Debug.Log($"[Fixups] Rewired {materialPath} -> {texturePath}");
    }

    // ---------- Step B: fix CustomCampusTourScene wiring ----------
    [MenuItem("Tools/Project Fixups/B - Fix Custom Campus Scene")]
    public static void FixCustomCampusScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CustomCampusTourScene.unity", OpenSceneMode.Single);

        // Fix wrong sphere materials
        SetMeshRendererMaterial("Stairs", "Assets/Materials/StairsMat.mat");
        SetMeshRendererMaterial("Entrance", "Assets/Materials/EntranceMat.mat");
        // Fab_Lab already correct, but reassign for safety/consistency
        SetMeshRendererMaterial("Fab_Lab", "Assets/Materials/FabLabMat.mat");

        // Add EventSystem (this scene is missing one entirely)
        var eventSystemGO = FindInScene("EventSystem");
        if (eventSystemGO == null)
        {
            eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[Fixups] Added missing EventSystem to CustomCampusTourScene");
        }

        // Create a persistent navigator GameObject
        var navGO = FindInScene("TourManager");
        if (navGO == null)
        {
            navGO = new GameObject("TourManager");
        }
        var nav = navGO.GetComponent<CampusTourNavigator>();
        if (nav == null) nav = navGO.AddComponent<CampusTourNavigator>();

        var stairs = FindInScene("Stairs");
        var entrance = FindInScene("Entrance");
        var fabLab = FindInScene("Fab_Lab");

        var so = new SerializedObject(nav);
        so.FindProperty("stairs").objectReferenceValue = stairs;
        so.FindProperty("entrance").objectReferenceValue = entrance;
        so.FindProperty("fabLab").objectReferenceValue = fabLab;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Only Fab_Lab should start active; Stairs/Entrance start inactive (matches existing authoring intent)
        stairs.SetActive(false);
        entrance.SetActive(false);
        fabLab.SetActive(true);

        // Rewire hotspot buttons to the navigator (replacing dangling SceneNavigator calls)
        WireHotspot("Entrance/Entrance_Canvas/StairsHotspot", nav, nav.GoToStairs);
        WireHotspot("Entrance/Entrance_Canvas/FabLabHotspot", nav, nav.GoToFabLab);
        WireHotspot("Stairs/Stairs_Canvas/EntranceHotspot", nav, nav.GoToEntrance);
        WireHotspot("Fab_Lab/FabLab_Canvas/EntranceHotspot", nav, nav.GoToEntrance);

        // Wire InfoButtons to toggle their location's description text, and hide the text by default
        WireInfoButton("Entrance/Entrance_Canvas/InfoButton", "Entrance/Entrance_Canvas/InfoButton/Text (TMP)");
        WireInfoButton("Stairs/Stairs_Canvas/InfoButton", "Stairs/Stairs_Canvas/InfoButton/Text (TMP)");
        WireInfoButton("Fab_Lab/FabLab_Canvas/InfoButton", "Fab_Lab/FabLab_Canvas/InfoButton/Text (TMP)");

        // Fix mislabeled Entrance description text
        SetTMPText("Entrance/Entrance_Canvas/InfoButton/Text (TMP)", "Welcome to the Entrance");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fixups] Step B complete.");
    }

    private static void SetMeshRendererMaterial(string objectName, string materialPath)
    {
        var go = FindInScene(objectName);
        if (go == null) { Debug.LogError($"[Fixups] GameObject not found: {objectName}"); return; }
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (renderer == null || mat == null) { Debug.LogError($"[Fixups] Missing renderer or material for {objectName}"); return; }
        renderer.sharedMaterial = mat;
        Debug.Log($"[Fixups] {objectName} MeshRenderer -> {materialPath}");
    }

    private static void WireHotspot(string path, CampusTourNavigator nav, UnityEngine.Events.UnityAction action)
    {
        var go = FindInScene(path);
        if (go == null) { Debug.LogError($"[Fixups] Hotspot not found: {path}"); return; }
        var button = go.GetComponent<Button>();
        if (button == null) { Debug.LogError($"[Fixups] No Button on {path}"); return; }

        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
        Debug.Log($"[Fixups] Wired hotspot {path} -> {action.Method.Name}");
    }

    private static void WireInfoButton(string buttonPath, string panelPath)
    {
        var buttonGO = FindInScene(buttonPath);
        var panelGO = FindInScene(panelPath);
        if (buttonGO == null || panelGO == null)
        {
            Debug.LogError($"[Fixups] InfoButton wiring failed: {buttonPath} / {panelPath}");
            return;
        }

        panelGO.SetActive(false);

        var toggle = buttonGO.GetComponent<InfoPanelToggle>();
        if (toggle == null) toggle = buttonGO.AddComponent<InfoPanelToggle>();
        var so = new SerializedObject(toggle);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.ApplyModifiedPropertiesWithoutUndo();

        var button = buttonGO.GetComponent<Button>();
        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }
        UnityEventTools.AddPersistentListener(button.onClick, toggle.Toggle);
        EditorUtility.SetDirty(button);
        Debug.Log($"[Fixups] Wired info button {buttonPath} -> toggles {panelPath}");
    }

    private static void SetTMPText(string path, string text)
    {
        var go = FindInScene(path);
        if (go == null) { Debug.LogError($"[Fixups] Text object not found: {path}"); return; }
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) { Debug.LogError($"[Fixups] No TextMeshProUGUI on {path}"); return; }
        tmp.text = text;
        EditorUtility.SetDirty(tmp);
        Debug.Log($"[Fixups] Set text on {path} -> \"{text}\"");
    }

    // ---------- Step C: add a working VR controller rig to all three scenes ----------
    [MenuItem("Tools/Project Fixups/C - Add XR Rig To All Scenes")]
    public static void AddXrRigToAllScenes()
    {
        AddXrRigToScene("Assets/Scenes/CustomCampusTourScene.unity", 1.1176f, createFadeOverlay: true);
        AddXrRigToScene("Assets/Scenes/IntranetTourScene.unity", 1.1176f, createFadeOverlay: false);
        AddXrRigToScene("Assets/Scenes/MainMenuScene.unity", 1.1176f, createFadeOverlay: false);
        Debug.Log("[Fixups] Step C complete.");
    }

    private static void AddXrRigToScene(string scenePath, float cameraYOffset, bool createFadeOverlay)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (FindInScene("XR Origin (XR Rig)") != null)
        {
            Debug.LogWarning($"[Fixups] {scenePath} already has an XR rig, skipping.");
            return;
        }

        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPrefabPath);
        if (rigPrefab == null)
        {
            Debug.LogError($"[Fixups] XR rig prefab not found at {XrRigPrefabPath}");
            return;
        }

        // Find the camera every Canvas currently points at, so we can remap references after swapping rigs
        var oldCamera = Camera.main;

        var rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
        rigInstance.name = "XR Origin (XR Rig)";
        var xrOrigin = rigInstance.GetComponent<XROrigin>();
        xrOrigin.CameraYOffset = cameraYOffset;
        var newCamera = xrOrigin.Camera;

        // Remap every Canvas's event camera from the old camera to the new rig camera.
        // Must include inactive objects: Stairs/Entrance (and their child Canvases) start disabled.
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.worldCamera == oldCamera || canvas.worldCamera == null)
            {
                canvas.worldCamera = newCamera;
                EditorUtility.SetDirty(canvas);
            }
        }

        // Remove the old hand-built rig / plain camera so there's only one camera+listener in the scene
        RemoveOldCameraRig(oldCamera);

        // Wire the EventSystem's XR tracking origin so ray interactors can drive UI
        var eventSystemGO = FindInScene("EventSystem");
        if (eventSystemGO != null)
        {
            var uiModule = eventSystemGO.GetComponent<InputSystemUIInputModule>();
            if (uiModule != null)
            {
                var so = new SerializedObject(uiModule);
                so.FindProperty("m_XRTrackingOrigin").objectReferenceValue = rigInstance.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(uiModule);
            }
        }

        if (createFadeOverlay)
        {
            var fadeImage = CreateCameraFadeOverlay(newCamera.transform);
            var navGO = FindInScene("TourManager");
            if (navGO != null)
            {
                var nav = navGO.GetComponent<CampusTourNavigator>();
                if (nav != null)
                {
                    var so = new SerializedObject(nav);
                    so.FindProperty("fadeImage").objectReferenceValue = fadeImage;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Fixups] XR rig added to {scenePath}");
    }

    // One-off repair for scenes already processed by AddXrRigToScene before the
    // FindObjectsInactive.Include fix: fills in any Canvas still left with a null worldCamera.
    [MenuItem("Tools/Project Fixups/C2 - Repair Canvas Camera Refs")]
    public static void RepairCanvasCameraRefs()
    {
        RepairCanvasCameraRefsInScene("Assets/Scenes/CustomCampusTourScene.unity");
        RepairCanvasCameraRefsInScene("Assets/Scenes/IntranetTourScene.unity");
        RepairCanvasCameraRefsInScene("Assets/Scenes/MainMenuScene.unity");
        Debug.Log("[Fixups] Step C2 complete.");
    }

    private static void RepairCanvasCameraRefsInScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var cam = Camera.main;
        if (cam == null) { Debug.LogError($"[Fixups] No main camera found in {scenePath}"); return; }

        int fixedCount = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = cam;
                EditorUtility.SetDirty(canvas);
                fixedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Fixups] Repaired {fixedCount} canvas camera refs in {scenePath}");
    }

    private static void RemoveOldCameraRig(Camera oldCamera)
    {
        if (oldCamera == null) return;
        var go = oldCamera.gameObject;

        // Walk up to the top-level root that isn't the new rig (covers "XR Origin (VR)" style hand-built rigs
        // as well as a bare "Main Camera" with no parent, like the old MainMenuScene).
        Transform root = go.transform;
        while (root.parent != null) root = root.parent;

        if (root.gameObject.name != "XR Origin (XR Rig)")
        {
            Object.DestroyImmediate(root.gameObject);
        }
    }

    private static Image CreateCameraFadeOverlay(Transform cameraTransform)
    {
        var canvasGO = new GameObject("FadeOverlayCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(cameraTransform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.001f;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cameraTransform.GetComponent<Camera>();
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 600);

        canvasGO.AddComponent<GraphicRaycaster>().enabled = false;

        var imageGO = new GameObject("FadeImage", typeof(RectTransform));
        imageGO.transform.SetParent(canvasGO.transform, false);
        var imgRt = imageGO.GetComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.sizeDelta = Vector2.zero;

        var image = imageGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        return image;
    }

    // ---------- Step D: convert Main Menu canvas to world space + polish visuals ----------
    [MenuItem("Tools/Project Fixups/D - Fix Main Menu Scene")]
    public static void FixMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);

        var canvasGO = FindInScene("Canvas");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var cam = Camera.main;
        canvas.worldCamera = cam;

        var canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.SetParent(cam.transform, false);
        canvasRt.localPosition = new Vector3(0f, 0f, 2.2f);
        canvasRt.localRotation = Quaternion.identity;
        canvasRt.localScale = Vector3.one * 0.0022f;
        canvasRt.sizeDelta = new Vector2(800, 600);

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // Visual polish: subtle press/hover feedback + consistent rounded look on the panel + buttons.
        PolishButton("Canvas/Panel/Button (1)", new Color(0.35f, 0.05f, 0.75f, 1f));   // Intranet Tour (purple)
        PolishButton("Canvas/Panel/Button", new Color(0.85f, 0.12f, 0.12f, 1f));       // Custom Campus Tour (red)
        PolishPanel("Canvas/Panel");

        // Wire buttons to actually load their scenes with a fade
        var loaderGO = FindInScene("MenuSceneLoader");
        if (loaderGO == null) loaderGO = new GameObject("MenuSceneLoader");
        var loader = loaderGO.GetComponent<MenuSceneLoader>();
        if (loader == null) loader = loaderGO.AddComponent<MenuSceneLoader>();

        var fadeImage = CreateCameraFadeOverlay(cam.transform);
        var loaderSo = new SerializedObject(loader);
        loaderSo.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        loaderSo.ApplyModifiedPropertiesWithoutUndo();

        WireSceneButton("Canvas/Panel/Button", loader, "CustomCampusTourScene");
        WireSceneButton("Canvas/Panel/Button (1)", loader, "IntranetTourScene");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fixups] Step D complete.");
    }

    private static void PolishButton(string path, Color accentColor)
    {
        var go = FindInScene(path);
        if (go == null) { Debug.LogError($"[Fixups] Button not found: {path}"); return; }
        var image = go.GetComponent<Image>();
        var button = go.GetComponent<Button>();
        if (image != null)
        {
            image.color = accentColor;
            image.type = Image.Type.Sliced;
        }
        if (button != null)
        {
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(accentColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(accentColor, Color.black, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }
        EditorUtility.SetDirty(go);
    }

    private static void PolishPanel(string path)
    {
        var go = FindInScene(path);
        if (go == null) return;
        var image = go.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.07f, 0.07f, 0.09f, 0.92f);
        }
        EditorUtility.SetDirty(go);
    }

    private static void WireSceneButton(string path, MenuSceneLoader loader, string sceneName)
    {
        var go = FindInScene(path);
        var button = go.GetComponent<Button>();
        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }
        UnityEventTools.AddStringPersistentListener(button.onClick, loader.LoadScene, sceneName);
        EditorUtility.SetDirty(button);
        Debug.Log($"[Fixups] Wired {path} -> load {sceneName}");
    }

    // ---------- Step C3: Intranet's fade canvas is Screen Space Overlay, which doesn't
    // render correctly in a VR headset. Give it the same camera-child world-space fade
    // used in the custom campus scene, and retire the old one. ----------
    [MenuItem("Tools/Project Fixups/C3 - Fix Intranet Fade Canvas")]
    public static void FixIntranetFadeCanvas()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/IntranetTourScene.unity", OpenSceneMode.Single);

        var cam = Camera.main;
        if (cam == null) { Debug.LogError("[Fixups] No main camera found in IntranetTourScene"); return; }

        var navGO = FindInScene("XR Interaction Manager");
        var nav = navGO != null ? navGO.GetComponent<SceneNavigator>() : null;
        if (nav == null) { Debug.LogError("[Fixups] SceneNavigator not found on XR Interaction Manager"); return; }

        var fadeImage = CreateCameraFadeOverlay(cam.transform);
        var so = new SerializedObject(nav);
        so.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        so.ApplyModifiedPropertiesWithoutUndo();

        var oldFadeCanvas = FindInScene("FadeCanvas");
        if (oldFadeCanvas != null)
        {
            Object.DestroyImmediate(oldFadeCanvas);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fixups] Step C3 complete: Intranet fade canvas replaced with world-space version.");
    }

    // ---------- Step E: fix Build Settings scene list ----------
    [MenuItem("Tools/Project Fixups/E - Fix Build Settings Scene List")]
    public static void FixBuildSettingsScenes()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/IntranetTourScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CustomCampusTourScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Fixups] Step E complete: Build Settings scene list updated.");
    }

    // Exports the raw Android Gradle project (no APK) so Gradle can be invoked manually
    // with an explicit -Djavax.net.ssl.trustStore flag, bypassing Unity's internal Gradle
    // invocation which doesn't reliably forward JAVA_TOOL_OPTIONS/GRADLE_OPTS from the
    // environment to its child java.exe process.
    [MenuItem("Tools/Project Fixups/F0 - Export Android Gradle Project")]
    public static void ExportAndroidGradleProject()
    {
        var scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var outputDir = System.IO.Path.GetFullPath("build/GradleProject");
        if (System.IO.Directory.Exists(outputDir))
        {
            System.IO.Directory.Delete(outputDir, true);
        }

        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = outputDir,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        Debug.Log($"[Fixups] Gradle project export result: {report.summary.result}, path: {outputDir}");
    }

    // ---------- Step F: build the Android/Quest APK ----------
    [MenuItem("Tools/Project Fixups/F - Build Quest APK")]
    public static void BuildQuestApk()
    {
        var scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var outputDir = "build";
        System.IO.Directory.CreateDirectory(outputDir);
        var outputPath = System.IO.Path.Combine(outputDir, "CampusVRTour.apk");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        Debug.Log($"[Fixups] Build result: {report.summary.result}, size: {report.summary.totalSize} bytes, path: {outputPath}");
    }

    [MenuItem("Tools/Project Fixups/Verify - Sanity Check All Scenes")]
    public static void VerifyAllScenes()
    {
        VerifyScene("Assets/Scenes/MainMenuScene.unity");
        VerifyScene("Assets/Scenes/IntranetTourScene.unity");
        VerifyScene("Assets/Scenes/CustomCampusTourScene.unity");
        Debug.Log("[Fixups] Verify complete.");
    }

    private static void VerifyScene(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[Verify] === {scenePath} ===");

        var rig = FindInScene("XR Origin (XR Rig)");
        Debug.Log($"[Verify] XR rig present: {rig != null}");

        var cam = Camera.main;
        Debug.Log($"[Verify] Camera.main: {(cam != null ? cam.name : "NULL")}");

        var eventSystem = FindInScene("EventSystem");
        var uiModule = eventSystem != null ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
        Debug.Log($"[Verify] EventSystem present: {eventSystem != null}, XRTrackingOrigin set: {(uiModule != null && uiModule.xrTrackingOrigin != null)}");

        int nullCanvasCamera = 0, totalCanvases = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            totalCanvases++;
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null) nullCanvasCamera++;
        }
        Debug.Log($"[Verify] Canvases: {totalCanvases}, world-space with null camera: {nullCanvasCamera}");

        int brokenButtons = 0, totalButtons = 0;
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            totalButtons++;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == null)
                {
                    brokenButtons++;
                    Debug.LogWarning($"[Verify] Button '{GetPath(button.transform)}' has a null persistent target (method: {button.onClick.GetPersistentMethodName(i)})");
                }
            }
        }
        Debug.Log($"[Verify] Buttons: {totalButtons}, with null persistent target: {brokenButtons}");

        var audioListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[Verify] AudioListeners: {audioListeners.Length}");
    }

    private static string GetPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    [MenuItem("Tools/Project Fixups/Run All (A-E, no build)")]
    public static void RunAll()
    {
        ImportPanoramasAndRewireMaterials();
        FixCustomCampusScene();
        AddXrRigToAllScenes();
        FixMainMenuScene();
        FixBuildSettingsScenes();
        Debug.Log("[Fixups] RunAll complete.");
    }
}
