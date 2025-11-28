using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using Mirror;

public class ScavengerHuntSetup : EditorWindow
{
    [MenuItem("ScavengerHunt/Setup Game Scene")]
    public static void SetupGameScene()
    {
        // 1. Setup AR Session
        GameObject arSession = GameObject.Find("AR Session");
        if (arSession == null)
        {
            arSession = new GameObject("AR Session");
            arSession.AddComponent<ARSession>();
            arSession.AddComponent<ARInputManager>();
            Undo.RegisterCreatedObjectUndo(arSession, "Create AR Session");
        }

        // 2. Setup XR Origin
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            GameObject xrOriginGO = new GameObject("XR Origin (AR Rig)");
            xrOrigin = xrOriginGO.AddComponent<XROrigin>();
            
            // Create Camera Offset
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginGO.transform);
            
            // Create Main Camera
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera == null)
            {
                mainCamera = new GameObject("Main Camera");
                mainCamera.tag = "MainCamera";
                mainCamera.AddComponent<Camera>();
                mainCamera.AddComponent<AudioListener>();
            }
            mainCamera.transform.SetParent(cameraOffset.transform);
            
            // Link XR Origin
            xrOrigin.CameraFloorOffsetObject = cameraOffset;
            xrOrigin.Camera = mainCamera.GetComponent<Camera>();
            
            // Add AR Camera Manager & Background
            if (mainCamera.GetComponent<ARCameraManager>() == null) mainCamera.AddComponent<ARCameraManager>();
            if (mainCamera.GetComponent<ARCameraBackground>() == null) mainCamera.AddComponent<ARCameraBackground>();
            if (mainCamera.GetComponent<TrackedPoseDriver>() == null) mainCamera.AddComponent<TrackedPoseDriver>();

            // Add AR Session Origin (Legacy support if needed, but XROrigin is new standard)
            if (xrOriginGO.GetComponent<ARSessionOrigin>() == null) xrOriginGO.AddComponent<ARSessionOrigin>();

            Undo.RegisterCreatedObjectUndo(xrOriginGO, "Create XR Origin");
        }

        // 3. Add AR Tracked Image Manager to XR Origin
        ARTrackedImageManager trackedImageManager = xrOrigin.GetComponent<ARTrackedImageManager>();
        if (trackedImageManager == null)
        {
            trackedImageManager = xrOrigin.gameObject.AddComponent<ARTrackedImageManager>();
        }

        // 4. Setup Game Manager
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gameManager, "Create Game Manager");
        }

        // 5. Add Managers
        ARFoundationOriginManager originManager = gameManager.GetComponent<ARFoundationOriginManager>();
        if (originManager == null) originManager = gameManager.AddComponent<ARFoundationOriginManager>();

        GameSceneManager gameSceneManager = gameManager.GetComponent<GameSceneManager>();
        if (gameSceneManager == null) gameSceneManager = gameManager.AddComponent<GameSceneManager>();

        // 6. Link References
        SerializedObject originManagerSO = new SerializedObject(originManager);
        originManagerSO.FindProperty("trackedImageManager").objectReferenceValue = trackedImageManager;
        originManagerSO.FindProperty("arSessionOrigin").objectReferenceValue = xrOrigin.transform;
        originManagerSO.ApplyModifiedProperties();

        // 7. Create and Assign NetworkedCube Prefab
        string prefabPath = "Assets/Prefabs/NetworkedCube.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs")) System.IO.Directory.CreateDirectory("Assets/Prefabs");
        
        GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (cubePrefab == null)
        {
            GameObject cubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeGO.name = "NetworkedCube";
            cubeGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // 20cm cube
            cubeGO.AddComponent<NetworkIdentity>();
            cubeGO.AddComponent<NetworkedCube>();
            cubePrefab = PrefabUtility.SaveAsPrefabAsset(cubeGO, prefabPath);
            Object.DestroyImmediate(cubeGO);
            Debug.Log("Created NetworkedCube Prefab.");
        }

        SerializedObject gameSceneManagerSO = new SerializedObject(gameSceneManager);
        gameSceneManagerSO.FindProperty("sharedOriginManagerObject").objectReferenceValue = gameManager; // originManager is on gameManager
        gameSceneManagerSO.FindProperty("arMarkerPrefab").objectReferenceValue = cubePrefab;
        gameSceneManagerSO.ApplyModifiedProperties();

        // 8. Assign to NetworkManager
        ScavengerHuntNetworkManager netManager = Object.FindFirstObjectByType<ScavengerHuntNetworkManager>();
        if (netManager != null)
        {
            SerializedObject netSO = new SerializedObject(netManager);
            SerializedProperty spawnList = netSO.FindProperty("spawnPrefabs");
            
            bool exists = false;
            for (int i = 0; i < spawnList.arraySize; i++)
            {
                if (spawnList.GetArrayElementAtIndex(i).objectReferenceValue == cubePrefab)
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                spawnList.InsertArrayElementAtIndex(spawnList.arraySize);
                spawnList.GetArrayElementAtIndex(spawnList.arraySize - 1).objectReferenceValue = cubePrefab;
                netSO.ApplyModifiedProperties();
                Debug.Log("Assigned NetworkedCube to NetworkManager Spawn List.");
            }
        }

        Debug.Log("Game Scene Setup Complete! Don't forget to assign a Reference Image Library to the ARTrackedImageManager.");
    }
}
