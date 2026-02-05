using UnityEngine;

[DisallowMultipleComponent]
public class CameraRigTrigger : MonoBehaviour
{
    public enum SpaceMode
    {
        LocalToPlayer, // camera = player + dummyCam.localOffset
        World          // camera = dummyCam.worldPosition
    }

    [Header("Rig Settings")]
    public SpaceMode spaceMode = SpaceMode.LocalToPlayer;

    [Tooltip("Dummy camera used only for authoring pose/size. Should be a child of this rig object.")]
    public Camera dummyCamera;

    private void Reset()
    {
        dummyCamera = GetComponentInChildren<Camera>(true);
    }

    private void OnValidate()
    {
        if (!dummyCamera) dummyCamera = GetComponentInChildren<Camera>(true);
    }
}
