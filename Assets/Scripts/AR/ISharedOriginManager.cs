using System;
using UnityEngine;

public interface ISharedOriginManager
{
    /// <summary>
    /// Invoked when the shared origin has been successfully established.
    /// The Pose represents the world space pose of the origin.
    /// </summary>
    event Action<Pose> OnOriginSet;

    /// <summary>
    /// Resets the origin, forcing a re-scan or re-alignment.
    /// </summary>
    void ResetOrigin();
}
