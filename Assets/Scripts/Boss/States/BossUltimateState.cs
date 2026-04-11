using System.Collections;
using UnityEngine;
public class BossUltimateState : State
{
    private class SlashInfo
    {
        public LineRenderer LineRenderer;
        public BoxCollider2D Collider;
        public Vector3 Origin;
        public Vector3 End;
        public SlashInfo(LineRenderer lr, BoxCollider2D collider, Vector3 origin, Vector3 end)
        {
            LineRenderer = lr;
            Collider = collider;
            Origin = origin;
            End = end;
        }
    }

    private BossStateMachine bossContext;
    private SlashInfo[] slashes;
    private LineRenderer chain;
    private float slashLifetime = 1.5f;
    private Coroutine ultimateCoroutine;

    // Slash config constants
    private int numSlashes = 8;
    private float minLength = 50f;
    private float maxLength = 80f;
    private float angleSpread = 270f;
    private float originRadius = 2.5f;
    private float slashColliderThickness = 0.35f;

    public BossUltimateState(BossStateMachine currentContext) : base(currentContext)
    {
        bossContext = currentContext;
        isBaseState = true;
    }

    public override void EnterState()
    {
        Debug.Log("Boss Ultimate entered");
        
        bossContext.AppliedMovementX = 0;
        bossContext.AppliedMovementY = 0;

        Vector3 bossOrigin = bossContext.transform.position + Vector3.up * 2f;
        slashes = new SlashInfo[numSlashes];

        chain = bossContext.GetComponentInChildren<LineRenderer>(true);

        // Create slashes gameobject with HUE's chain linerenderer
        for (int i = 0; i < numSlashes; i++)
        {
            LineRenderer lr = null;
            BoxCollider2D slashCollider = null;
            if (chain != null)
            {
                lr = Object.Instantiate(chain);
                lr.transform.SetParent(null);
                lr.gameObject.name = $"UltimateSlash_{i}";
                lr.gameObject.SetActive(true);
            }
            else
            {
                GameObject slashObj = new GameObject($"UltimateSlash_{i}");
                lr = slashObj.AddComponent<LineRenderer>();
                lr.widthMultiplier = 0.2f;
            }

            // Box collider needed to deal dmg
            slashCollider = lr.GetComponent<BoxCollider2D>();
            if (slashCollider == null)
            {
                slashCollider = lr.gameObject.AddComponent<BoxCollider2D>();
            }
            slashCollider.isTrigger = true;
            slashCollider.enabled = true;

            lr.positionCount = 2;

            float angle = Random.Range(0f, angleSpread);
            float length = Random.Range(minLength, maxLength);
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;

            float offsetAngle = Random.Range(0f, 360f);
            float offsetDist = Random.Range(0f, originRadius);
            Vector3 offset = Quaternion.Euler(0, 0, offsetAngle) * Vector3.right * offsetDist;
            Vector3 origin = bossOrigin + offset;
            Vector3 end = origin + dir * length;

            // Start both points at origin
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin);
            slashes[i] = new SlashInfo(lr, slashCollider, origin, end);
        }

        // animate the chains expanding
        ultimateCoroutine = bossContext.StartCoroutine(RunUltimate());
    }

    public override void UpdateState()
    {
    }

    private IEnumerator RunUltimate()
    {
        float elapsed = 0f;
        while (elapsed < slashLifetime)
        {
            elapsed += Time.deltaTime;
            // t = normalized progress from 0 to 1, divide by 0.0001 to avoid divide by zero
            float t = Mathf.Clamp01(elapsed / Mathf.Max(slashLifetime, 0.0001f));

            // dividing by 0.2 means last 20% of the animation time is chains fading out
            // adjust 0.2 if needed
            float alphaMultiplier = Mathf.Clamp01((1f - t) / 0.2f);

            // for each slash, lerp the end point from origin to end, and fade out over time
            for (int i = 0; i < slashes.Length; i++)
            {
                if (slashes[i] != null && slashes[i].LineRenderer != null)
                {
                    SlashInfo slash = slashes[i];
                    Vector3 currentEnd = Vector3.Lerp(slash.Origin, slash.End, t);
                    slash.LineRenderer.SetPosition(1, currentEnd);

                    // scale each collider to match line render
                    if (slash.Collider != null)
                    {
                        Vector3 start = slash.Origin;
                        Vector3 delta = currentEnd - start;
                        float length = delta.magnitude;

                        slash.Collider.enabled = true;
                        Vector3 midpoint = start + (delta * 0.5f); // centered on the slash
                        // rotate collider to match angle of the slash (convert to rad)
                        slash.Collider.transform.SetPositionAndRotation(midpoint, Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg));
                        slash.Collider.size = new Vector2(length, slashColliderThickness);
                        slash.Collider.offset = Vector2.zero;

                    }

                    // grab the line render colors and apply the fade
                    Color startColor = slash.LineRenderer.startColor;
                    Color endColor = slash.LineRenderer.endColor;
                    startColor.a = alphaMultiplier;
                    endColor.a = alphaMultiplier;
                    slash.LineRenderer.startColor = startColor;
                    slash.LineRenderer.endColor = endColor;
                }
            }
            yield return null;
        }
        

        ultimateCoroutine = null;

        // automatically exit
        SwitchState(new BossTransitionState(bossContext));
    }

    public override void ExitState()
    {
        Debug.Log("Boss Ultimate exited");
        if (ultimateCoroutine != null)
        {
            bossContext.StopCoroutine(ultimateCoroutine);
            ultimateCoroutine = null;
        }

        // Clean up slashes
        if (slashes != null)
        {
            foreach (var slash in slashes)
                if (slash != null && slash.LineRenderer != null) Object.Destroy(slash.LineRenderer.gameObject);
            slashes = null;
        }
    }

    public override void CheckSwitchStates()
    {
    }
}