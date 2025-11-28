using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFoundationOriginManager : MonoBehaviour, ISharedOriginManager
{
    [Header("Dependencies")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Transform arSessionOrigin; // Reference to the XR Origin / AR Session Origin

    [Header("Configuration")]
    [SerializeField] private string referenceImageName = "SharedOrigin";

    public event Action<Pose> OnOriginSet;

    private bool originSet = false;

    private void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        if (originSet) return;

        foreach (var trackedImage in args.added)
        {
            ProcessImage(trackedImage);
        }

        foreach (var trackedImage in args.updated)
        {
            ProcessImage(trackedImage);
        }
    }

    private void ProcessImage(ARTrackedImage trackedImage)
    {
        if (originSet) return;

        if (trackedImage.referenceImage.name == referenceImageName && trackedImage.trackingState == TrackingState.Tracking)
        {
            Debug.Log($"[ARFoundationOriginManager] Found Shared Origin Image: {trackedImage.referenceImage.name}");
            
            // Set the world origin to this image
            SetWorldOrigin(trackedImage.transform);
        }
    }

    private void SetWorldOrigin(Transform targetTransform)
    {
        if (arSessionOrigin == null)
        {
            Debug.LogError("[ARFoundationOriginManager] AR Session Origin is not assigned!");
            return;
        }

        // We want the targetTransform to become (0,0,0) with identity rotation.
        // To do this, we move the ARSessionOrigin.
        // The logic is: MakeContentAppearAt(content, position, rotation)
        // But here we want the "content" (the world) to stay put relative to the camera? 
        // No, we want the camera to move such that the image is at 0,0,0.
        
        // Actually, the easiest way in AR Foundation is often to use the MakeContentAppearAt helper 
        // if we were placing content. But here we are defining the coordinate system.
        
        // Let's simply rotate and move the ARSessionOrigin so that the targetTransform aligns with World Zero.
        // This is effectively the inverse operation of the target's current local transform relative to the session origin?
        // Not exactly because the target is updated by the system.

        // Simpler approach for now: Just invoke the event and let the GameSceneManager handle spawning 
        // relative to this anchor, OR actually move the world.
        // Moving the world (SessionOrigin) is better for shared coordinates.

        arSessionOrigin.position += Vector3.zero - targetTransform.position;
        
        // Rotation is trickier, usually we only want to correct Y rotation (yaw) to keep gravity up.
        // But for a full 6DOF sync, we might want full rotation.
        // Let's stick to position for a safe start, or full sync if the image is flat on a table.
        
        // For now, let's just mark it as set and fire the event. 
        // We will refine the "Move World" logic in the next step if needed.
        
        originSet = true;
        OnOriginSet?.Invoke(Pose.identity); // We pretend the origin is now at 0,0,0
    }

    public void ResetOrigin()
    {
        originSet = false;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Debug: Press 'O' to simulate finding the origin
        if (Input.GetKeyDown(KeyCode.O) && !originSet)
        {
            SimulateOriginFound();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!originSet)
        {
            if (GUI.Button(new Rect(10, 10, 200, 50), "Debug: Set Shared Origin"))
            {
                SimulateOriginFound();
            }
        }
    }

    private void SimulateOriginFound()
    {
        Debug.Log("[ARFoundationOriginManager] DEBUG: Simulating Shared Origin found (Editor Mode).");
        GameObject dummyTarget = new GameObject("Debug_SharedOrigin_Target");
        dummyTarget.transform.position = Vector3.zero;
        dummyTarget.transform.rotation = Quaternion.identity;
        
        SetWorldOrigin(dummyTarget.transform);
        
        Destroy(dummyTarget);
    }
#endif
}

