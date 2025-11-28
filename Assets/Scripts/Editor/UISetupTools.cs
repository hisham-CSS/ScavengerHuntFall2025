using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class UISetupTools : EditorWindow
{
    [MenuItem("ScavengerHunt/Create UI/Generate Full UI Structure")]
    public static void GenerateFullUI()
    {
        // 1. Setup Canvas & Managers
        GameObject canvasGO = SetupCanvas();
        GameObject uiManagerGO = SetupUIManager(canvasGO);

        // 2. Create Panels
        GameObject mainMenuPanel = CreatePanel(canvasGO, "MainMenuPanel");
        GameObject lobbyListPanel = CreatePanel(canvasGO, "LobbyListPanel");
        GameObject roomPanel = CreatePanel(canvasGO, "RoomPanel");

        // 3. Setup Main Menu
        SetupMainMenu(mainMenuPanel, out Button createBtn, out Button joinBtn);

        // 4. Setup Lobby List
        SetupLobbyList(lobbyListPanel, out Transform lobbyContainer, out Button refreshBtn, out Button backBtn);

        // 5. Setup Room Panel
        SetupRoomPanel(roomPanel, out Transform playerContainer, out Button readyBtn, out Button startBtn, out TMP_Text readyBtnText);

        // 6. Create Prefabs
        GameObject lobbyItemPrefab = CreateLobbyItemPrefab();
        GameObject playerItemPrefab = CreatePlayerItemPrefab();

        // 7. Wire up Scripts
        WireUpScripts(uiManagerGO, mainMenuPanel, lobbyListPanel, roomPanel, 
                      createBtn, joinBtn, 
                      lobbyContainer, lobbyItemPrefab, refreshBtn, backBtn,
                      playerContainer, playerItemPrefab, readyBtn, startBtn, readyBtnText);

        Debug.Log("Full UI Generation Complete!");
    }

    private static GameObject SetupCanvas()
    {
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas");
            Canvas c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile Portrait
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        if (Object.FindFirstObjectByType<UIAnimator>() == null)
        {
            new GameObject("UIAnimator").AddComponent<UIAnimator>();
        }

        return canvasGO;
    }

    private static GameObject SetupUIManager(GameObject canvas)
    {
        GameObject manager = GameObject.Find("LobbyUIManager");
        if (manager == null)
        {
            manager = new GameObject("LobbyUIManager");
            manager.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(manager, "Create LobbyUIManager");
        }
        return manager;
    }

    private static GameObject CreatePanel(GameObject parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Deep Blue/Black

        panel.AddComponent<CanvasGroup>();
        
        return panel;
    }

    private static void SetupMainMenu(GameObject panel, out Button createBtn, out Button joinBtn)
    {
        // Title
        CreateText(panel, "Title", "Scavenger Hunt AR", 80, new Vector2(0, 0.7f), new Vector2(1, 0.9f));

        // Buttons
        GameObject createGO = CreateModernButton(panel, "CreateLobbyButton", "Create Lobby", new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.6f));
        GameObject joinGO = CreateModernButton(panel, "JoinLobbyButton", "Join Lobby", new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.45f));

        createBtn = createGO.GetComponent<Button>();
        joinBtn = joinGO.GetComponent<Button>();
    }

    private static void SetupLobbyList(GameObject panel, out Transform container, out Button refreshBtn, out Button backBtn)
    {
        CreateText(panel, "Title", "Lobbies", 60, new Vector2(0, 0.85f), new Vector2(1, 0.95f));

        // Scroll View
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panel.transform, false);
        RectTransform rt = scrollObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.2f);
        rt.anchorMax = new Vector2(0.9f, 0.8f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        Image bg = scrollObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.3f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vRt = viewport.AddComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero;
        vRt.anchorMax = Vector2.one;
        vRt.pivot = new Vector2(0, 1);
        viewport.AddComponent<Mask>();
        viewport.AddComponent<Image>().color = new Color(1,1,1,0.01f);

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt = content.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1);
        cRt.sizeDelta = new Vector2(0, 300); // Initial height

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = cRt;
        scroll.viewport = vRt;
        container = content.transform;

        // Buttons
        GameObject refreshGO = CreateModernButton(panel, "RefreshButton", "Refresh", new Vector2(0.6f, 0.05f), new Vector2(0.9f, 0.15f));
        GameObject backGO = CreateModernButton(panel, "BackButton", "Back", new Vector2(0.1f, 0.05f), new Vector2(0.4f, 0.15f));

        refreshBtn = refreshGO.GetComponent<Button>();
        backBtn = backGO.GetComponent<Button>();
        
        // Hide initially
        panel.SetActive(false);
    }

    private static void SetupRoomPanel(GameObject panel, out Transform container, out Button readyBtn, out Button startBtn, out TMP_Text readyBtnText)
    {
        CreateText(panel, "Title", "Room", 60, new Vector2(0, 0.85f), new Vector2(1, 0.95f));

        // Player Grid
        GameObject gridObj = new GameObject("PlayerGrid");
        gridObj.transform.SetParent(panel.transform, false);
        RectTransform rt = gridObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.3f);
        rt.anchorMax = new Vector2(0.9f, 0.8f);
        
        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(200, 200);
        glg.spacing = new Vector2(20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        
        container = gridObj.transform;

        // Buttons
        GameObject readyGO = CreateModernButton(panel, "ReadyButton", "Ready", new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.2f));
        GameObject startGO = CreateModernButton(panel, "StartButton", "Start Game", new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.2f));

        readyBtn = readyGO.GetComponent<Button>();
        startBtn = startGO.GetComponent<Button>();
        readyBtnText = readyGO.GetComponentInChildren<TMP_Text>();
        
        // Hide initially
        panel.SetActive(false);
    }

    private static GameObject CreateModernButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent.transform, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btnGO.AddComponent<ModernUIButton>();

        // Text (RaycastTarget = false to allow button clicks)
        CreateText(btnGO, "Text", text, 32, Vector2.zero, Vector2.one, false);

        return btnGO;
    }

    private static void CreateText(GameObject parent, string name, string content, float fontSize, Vector2 anchorMin, Vector2 anchorMax, bool raycastTarget = true)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);
        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TMP_Text tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.raycastTarget = raycastTarget;
    }

    private static GameObject CreateLobbyItemPrefab()
    {
        string path = "Assets/Prefabs/UI";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        string prefabPath = path + "/LobbyItem.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        GameObject go = new GameObject("LobbyItem");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 120); // Increased height
        
        // Add LayoutElement to enforce height in VerticalLayoutGroup
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 120;
        le.preferredHeight = 120;
        le.flexibleHeight = 0;
        
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f);

        CreateText(go, "NameText", "Lobby Name", 36, new Vector2(0.05f, 0), new Vector2(0.7f, 1));
        
        GameObject joinBtn = CreateModernButton(go, "JoinButton", "Join", new Vector2(0.75f, 0.15f), new Vector2(0.95f, 0.85f));
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreatePlayerItemPrefab()
    {
        string path = "Assets/Prefabs/UI";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        string prefabPath = path + "/PlayerListItem.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        GameObject go = new GameObject("PlayerListItem");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f);

        CreateText(go, "NameText", "Player Name", 24, new Vector2(0, 0.2f), new Vector2(1, 0.4f));
        CreateText(go, "StatusText", "Ready", 18, new Vector2(0, 0.6f), new Vector2(1, 0.8f));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void WireUpScripts(GameObject managerGO, GameObject main, GameObject list, GameObject room,
                                      Button create, Button join,
                                      Transform lobbyContainer, GameObject lobbyPrefab, Button refresh, Button back,
                                      Transform playerContainer, GameObject playerPrefab, Button ready, Button start, TMP_Text readyText)
    {
        // LobbyUIManager
        LobbyUIManager uiManager = managerGO.GetComponent<LobbyUIManager>();
        if (uiManager == null) uiManager = managerGO.AddComponent<LobbyUIManager>();

        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("mainMenuPanel").objectReferenceValue = main;
        so.FindProperty("lobbyListPanel").objectReferenceValue = list;
        so.FindProperty("roomPanel").objectReferenceValue = room;
        so.FindProperty("createLobbyButton").objectReferenceValue = create;
        so.FindProperty("joinLobbyButton").objectReferenceValue = join;
        so.ApplyModifiedProperties();

        // LobbyListUI
        LobbyListUI listUI = managerGO.GetComponent<LobbyListUI>();
        if (listUI == null) listUI = managerGO.AddComponent<LobbyListUI>();

        SerializedObject listSO = new SerializedObject(listUI);
        listSO.FindProperty("lobbyContainer").objectReferenceValue = lobbyContainer;
        listSO.FindProperty("lobbyItemPrefab").objectReferenceValue = lobbyPrefab;
        listSO.FindProperty("refreshButton").objectReferenceValue = refresh;
        listSO.FindProperty("backButton").objectReferenceValue = back;
        listSO.ApplyModifiedProperties();

        // RoomUI
        RoomUI roomUI = managerGO.GetComponent<RoomUI>();
        if (roomUI == null) roomUI = managerGO.AddComponent<RoomUI>();

        SerializedObject roomSO = new SerializedObject(roomUI);
        roomSO.FindProperty("playerListContainer").objectReferenceValue = playerContainer;
        roomSO.FindProperty("playerListItemPrefab").objectReferenceValue = playerPrefab;
        roomSO.FindProperty("readyButton").objectReferenceValue = ready;
        roomSO.FindProperty("startButton").objectReferenceValue = start;
        roomSO.FindProperty("readyButtonText").objectReferenceValue = readyText;
        roomSO.ApplyModifiedProperties();
    }
}
