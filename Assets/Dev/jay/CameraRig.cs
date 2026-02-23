using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public enum SpaceMode
    {
        LocalToPlayer, //camera = player + dummyCam.localOffset
        World          //camera = dummyCam.worldPosition
    }

    public SpaceMode spaceMode = SpaceMode.LocalToPlayer;
    public Camera dummyCamera;

    private void OnValidate()
    {
        if (!dummyCamera) dummyCamera = GetComponentInChildren<Camera>(true);
    }

    private void Reset()
    {
        dummyCamera = GetComponentInChildren<Camera>(true);
    }

}
