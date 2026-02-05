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
        public CameraRigTrigger rig;     // the rig root object
        public Transform rigRoot;        // cached rig transform
        public Camera dummyCamera;       // cached dummy cam

        [NonSerialized] public float x;  // rigRoot.position.x
        [NonSerialized] public Vector3 localOffset; // dummyCamera.localPosition
        [NonSerialized] public Vector3 worldPos;    // dummyCamera.worldPosition
        [NonSerialized] public float orthoSize;
        [NonSerialized] public CameraRigTrigger.SpaceMode spaceMode;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera runtimeCamera;

    [Header("Rig discovery")]
    [Tooltip("If true, automatically finds all CameraRigTrigger in the scene on Awake/OnValidate.")]
    [SerializeField] private bool autoFindRigs = true;

    [Tooltip("If autoFindRigs is false, fill this list manually.")]
    [SerializeField] private List<CameraRigTrigger> rigsManual = new();

    [Header("Smoothing")]
    [Tooltip("0 = instant. Higher = smoother.")]
    [SerializeField] private float positionSmoothTime = 0.10f;

    [Tooltip("0 = instant. Higher = smoother.")]
    [SerializeField] private float sizeSmoothTime = 0.10f;

    [Header("Runtime Z")]
    [Tooltip("Runtime camera Z (2D typically -10). If 0, uses runtime camera's current Z on Awake.")]
    [SerializeField] private float cameraZ = 0f;

    private readonly List<RigData> _rigs = new();
    private Vector3 _posVel;
    private float _sizeVel;

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

        if (cameraZ == 0f && runtimeCamera)
            cameraZ = runtimeCamera.transform.position.z;

        RebuildRigCache();

        if (player) PrevPlayerX = CurrPlayerX = player.position.x;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    private void OnValidate()
    {
        if (!runtimeCamera) runtimeCamera = GetComponentInChildren<Camera>();
        if (Application.isPlaying) return; // keep validate cheap during play
        RebuildRigCache();
    }

    private void LateUpdate()
    {
        if (!player || !runtimeCamera) return;
        if (_rigs.Count == 0) return;

        PrevPlayerX = CurrPlayerX;
        CurrPlayerX = player.position.x;

        ApplySample(CurrPlayerX);
    }

    public void RebuildRigCache()
    {
        _rigs.Clear();

        List<CameraRigTrigger> found = new();

        if (autoFindRigs)
        {
            // Finds active + inactive objects in scene.
            found.AddRange(FindObjectsByType<CameraRigTrigger>(FindObjectsSortMode.InstanceID));
        }
        else
        {
            found.AddRange(rigsManual);
        }

        for (int i = 0; i < found.Count; i++)
        {
            var rig = found[i];
            if (!rig) continue;

            var root = rig.transform;
            var dummy = rig.dummyCamera ? rig.dummyCamera : rig.GetComponentInChildren<Camera>(true);
            if (!dummy) continue;

            var d = new RigData
            {
                rig = rig,
                rigRoot = root,
                dummyCamera = dummy,
            };

            CacheRig(d);
            _rigs.Add(d);
        }

        _rigs.Sort((a, b) => a.x.CompareTo(b.x));
    }

    private void CacheRig(RigData d)
    {
        d.x = d.rigRoot.position.x;
        d.localOffset = d.dummyCamera.transform.localPosition;
        d.worldPos = d.dummyCamera.transform.position;
        d.orthoSize = d.dummyCamera.orthographic ? d.dummyCamera.orthographicSize : runtimeCamera.orthographicSize;
        d.spaceMode = d.rig.spaceMode;
    }

    private void ApplySample(float playerX)
    {
        // Update cached values (in case rigs were moved in-editor during play, or animated)
        for (int i = 0; i < _rigs.Count; i++)
            CacheRig(_rigs[i]);

        if (_rigs.Count == 1)
        {
            var only = _rigs[0];
            ApplyPose(EvaluateRigPose(only), only.orthoSize);
            return;
        }

        int right = FindFirstRigRightOfX(playerX);

        if (right <= 0)
        {
            var r = _rigs[0];
            ApplyPose(EvaluateRigPose(r), r.orthoSize);
            return;
        }

        if (right >= _rigs.Count)
        {
            var r = _rigs[_rigs.Count - 1];
            ApplyPose(EvaluateRigPose(r), r.orthoSize);
            return;
        }

        var a = _rigs[right - 1];
        var b = _rigs[right];

        float t = Mathf.InverseLerp(a.x, b.x, playerX);

        Vector3 poseA = EvaluateRigPose(a);
        Vector3 poseB = EvaluateRigPose(b);

        Vector3 desiredPos = Vector3.LerpUnclamped(poseA, poseB, t);
        float desiredSize = Mathf.LerpUnclamped(a.orthoSize, b.orthoSize, t);

        ApplyPose(desiredPos, desiredSize);
    }

    private Vector3 EvaluateRigPose(RigData r)
    {
        Vector3 desired;

        if (r.spaceMode == CameraRigTrigger.SpaceMode.World)
        {
            desired = r.worldPos;
        }
        else
        {
            // LocalToPlayer
            desired = player.position + r.localOffset;
        }

        desired.z = cameraZ;
        return desired;
    }

    private void ApplyPose(Vector3 desiredPos, float desiredOrtho)
    {
        Vector3 current = runtimeCamera.transform.position;

        if (positionSmoothTime <= 0f)
            runtimeCamera.transform.position = desiredPos;
        else
            runtimeCamera.transform.position = Vector3.SmoothDamp(current, desiredPos, ref _posVel, positionSmoothTime);

        if (!runtimeCamera.orthographic) return;

        float currentSize = runtimeCamera.orthographicSize;

        if (sizeSmoothTime <= 0f)
            runtimeCamera.orthographicSize = desiredOrtho;
        else
            runtimeCamera.orthographicSize = Mathf.SmoothDamp(currentSize, desiredOrtho, ref _sizeVel, sizeSmoothTime);
    }

    private int FindFirstRigRightOfX(float x)
    {
        for (int i = 0; i < _rigs.Count; i++)
        {
            if (_rigs[i].x >= x) return i;
        }
        return _rigs.Count;
    }
}
