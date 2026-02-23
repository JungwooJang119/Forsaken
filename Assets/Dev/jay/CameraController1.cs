using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraController1 : MonoBehaviour
{
    public static CameraController1 I { get; private set; }

    [Serializable]
    public class RigData
    {
        public CameraRig rig;
        public Transform rigRoot;
        public Camera dummyCamera;

        public float x;
        public Vector3 localOffset;
        public Vector3 worldPos;
        public float orthoSize;
        public CameraRig.SpaceMode spaceMode;
    }

    [Header("Refs")]
    public Transform player;
    public Camera runtimeCamera;

    [Header("Bounds")]
    public bool clampToExtremeRigs = true;

    [Header("Rig discovery")]
    public bool AutomaticallyDiscoverRigs = true;
    public List<CameraRig> ManuallySetRigs = new();

    [Header("Smoothing")]
    public float SmoothingX = 0.10f;
    public float SmoothingY = 0.10f;
    public float CameraZoomSmoothing = 0.10f;

    [Header("Runtime Z")]
    public float CameraOffsetZ = 0f;

    private readonly List<RigData> cameraRigs = new();

    private float velocityX;
    private float velocityY;
    private float velocityZoom;

    public float PrevPlayerX { get; private set; }
    public float CurrPlayerX { get; private set; }

    private void Reset()
    {
        runtimeCamera = GetComponentInChildren<Camera>();
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (!runtimeCamera) runtimeCamera = Camera.main;

        if (CameraOffsetZ == 0f && runtimeCamera)
            CameraOffsetZ = runtimeCamera.transform.position.z;

        RebuildCache();

        if (player) PrevPlayerX = CurrPlayerX = player.position.x;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    private void OnValidate()
    {
        if (!runtimeCamera) runtimeCamera = GetComponentInChildren<Camera>();
        if (Application.isPlaying) return;
        RebuildCache();
    }

    private int FindFirstRigRightOfX(float x)
    {
        for (int i = 0; i < cameraRigs.Count; i++)
            if (cameraRigs[i].x >= x) return i;

        return cameraRigs.Count;
    }



    private void LateUpdate()
    {
        if (!player || !runtimeCamera) return;
        if (cameraRigs.Count == 0) return;

        PrevPlayerX = CurrPlayerX;
        CurrPlayerX = player.position.x;

        ApplyRigSample(CurrPlayerX);
    }
    private void GetCameraBounds(out Vector3 leftPosition, out float leftOrthographic, out Vector3 rightPosition, out float rightOrthographic)
    {
        var left = cameraRigs[0];
        var right = cameraRigs[^1];
        leftPosition = EvaluateRigPose(left);
        rightPosition = EvaluateRigPose(right);
        leftOrthographic = left.orthoSize;
        rightOrthographic = right.orthoSize;
    }




    public void RebuildCache()
    {
        cameraRigs.Clear();

        List<CameraRig> found = new();



        if (AutomaticallyDiscoverRigs)
            found.AddRange(FindObjectsByType<CameraRig>(FindObjectsSortMode.InstanceID));
        else
            found.AddRange(ManuallySetRigs);




        foreach (var rig in found)
        {
            if (!rig) continue;

            var dummy = rig.dummyCamera ? rig.dummyCamera : rig.GetComponentInChildren<Camera>(true);
            if (!dummy) continue;



            var d = new RigData
            {
                rig = rig,
                rigRoot = rig.transform,
                dummyCamera = dummy
            };

            CacheRig(d);
            cameraRigs.Add(d);
        }

        cameraRigs.Sort((a, b) => a.x.CompareTo(b.x));
    }





    private void CacheRig(RigData d)
    {
        d.x = d.rigRoot.position.x;
        d.localOffset = d.dummyCamera.transform.localPosition;
        d.worldPos = d.dummyCamera.transform.position;
        d.orthoSize = d.dummyCamera.orthographic
            ? d.dummyCamera.orthographicSize
            : runtimeCamera.orthographicSize;
        d.spaceMode = d.rig.spaceMode;
    }

    private void ApplyRigSample(float playerX)
    {
        for (int i = 0; i < cameraRigs.Count; i++)
            CacheRig(cameraRigs[i]);

        Vector3 desiredPosition = new();
        float desiredOrthographic;

        if (cameraRigs.Count == 1)
        {
            var r = cameraRigs[0];
            desiredPosition = EvaluateRigPose(r);
            desiredOrthographic = r.orthoSize;
            ApplyPose(VectorContextClamp(desiredPosition, desiredOrthographic, out desiredOrthographic), desiredOrthographic);
            return;
        }

        int right = FindFirstRigRightOfX(playerX);

        if (right <= 0)
        {
            var r = cameraRigs[0];
            desiredPosition = EvaluateRigPose(r);
            desiredOrthographic = r.orthoSize;
            ApplyPose(VectorContextClamp(desiredPosition, desiredOrthographic, out desiredOrthographic), desiredOrthographic);
            return;
        }

        if (right >= cameraRigs.Count)
        {
            var r = cameraRigs[^1];
            desiredPosition = EvaluateRigPose(r);
            desiredOrthographic = r.orthoSize;
            ApplyPose(VectorContextClamp(desiredPosition, desiredOrthographic, out desiredOrthographic), desiredOrthographic);
            return;
        }

        var a = cameraRigs[right - 1];
        var b = cameraRigs[right];

        float t = Mathf.InverseLerp(a.x, b.x, playerX);

        Vector3 posA = EvaluateRigPose(a);
        Vector3 posB = EvaluateRigPose(b);

        desiredPosition = Vector3.LerpUnclamped(posA, posB, t);
        desiredOrthographic = Mathf.LerpUnclamped(a.orthoSize, b.orthoSize, t);

        desiredPosition = VectorContextClamp(desiredPosition, desiredOrthographic, out desiredOrthographic);
        ApplyPose(desiredPosition, desiredOrthographic);
    }

    private Vector3 VectorContextClamp(Vector3 desiredPostion, float desiredOrthographic, out float clampedOrthographic)
    {
        clampedOrthographic = desiredOrthographic;
        if (!clampToExtremeRigs || cameraRigs.Count == 0) return desiredPostion;

        GetCameraBounds(out var leftPos, out var leftOrtho, out var rightPos, out var rightOrtho);

        desiredPostion.x = Mathf.Clamp(desiredPostion.x, leftPos.x, rightPos.x);
        clampedOrthographic = Mathf.Clamp(desiredOrthographic, Mathf.Min(leftOrtho, rightOrtho), Mathf.Max(leftOrtho, rightOrtho));

        return desiredPostion;
    }


    private Vector3 EvaluateRigPose(RigData r)
    {
        Vector3 desired = r.spaceMode == CameraRig.SpaceMode.World
            ? r.worldPos
            : player.position + r.localOffset;

        desired.z = CameraOffsetZ;
        return desired;
    }

    private void ApplyPose(Vector3 desiredPos, float desiredOrtho)
    {
        Vector3 current = runtimeCamera.transform.position;

        float x = SmoothingX <= 0f
            ? desiredPos.x
            : Mathf.SmoothDamp(current.x, desiredPos.x, ref velocityX, SmoothingX);

        float y = SmoothingY <= 0f
            ? desiredPos.y
            : Mathf.SmoothDamp(current.y, desiredPos.y, ref velocityY, SmoothingY);

        runtimeCamera.transform.position = new Vector3(x, y, CameraOffsetZ);

        if (!runtimeCamera.orthographic) return;

        runtimeCamera.orthographicSize =
            CameraZoomSmoothing <= 0f
                ? desiredOrtho
                : Mathf.SmoothDamp(runtimeCamera.orthographicSize, desiredOrtho, ref velocityZoom, CameraZoomSmoothing);
    }


}
