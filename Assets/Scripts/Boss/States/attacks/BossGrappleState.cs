using UnityEngine;
using System.Collections;

public class BossGrappleState : State
{
    private BossStateMachine bossContext;
    private LineRenderer lineRenderer;
    private Transform chainStart;

    public BossGrappleState(BossStateMachine currentContext) : base(currentContext)
    {
        bossContext = currentContext;
    }

    public override void EnterState()
    {
        Debug.Log("entered grapple");
        bossContext.GrapplingFinished = 0;
        bossContext.Anim.SetTrigger("grapple");

        lineRenderer = bossContext.GetComponentInChildren<LineRenderer>(true);
        if (lineRenderer == null)
        {
            Debug.Log("LineRenderer component not found on boss GameObject");
            SwitchState(new BossTransitionState(bossContext));
            return;
        }
        lineRenderer.enabled = true;
        chainStart = lineRenderer.transform;

        bossContext.StartCoroutine(AnimateGrapple());
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Debug.Log("exiting grapple");
        bossContext.Anim.ResetTrigger("grapple");
        lineRenderer.enabled = false;
    }

    public override void CheckSwitchStates()
    {
        if (bossContext.GrapplingFinished == 1)
        {
            SwitchState(new BossIdleState(bossContext));
        }
    }

    private IEnumerator AnimateGrapple()
    {
        float elapsed = 0f;
        float duration = bossContext.GrappleDuration;
        float stopDistance = 2f;
        Vector3 grappleTarget = bossContext.Player.GetComponent<Collider2D>().bounds.center;
        grappleTarget.y = 0f;

        // The throwing of the chain
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            Vector3 chainTip = Vector3.Lerp(chainStart.position, grappleTarget, percent);

            lineRenderer.SetPosition(0, chainStart.position);
            lineRenderer.SetPosition(1, chainTip);

            //yield return null;
        }


        // The pulling of the boss towards the grapple target
        while (Vector3.Distance(bossContext.transform.position, grappleTarget) > stopDistance)
        {
            lineRenderer.SetPosition(0, bossContext.GetComponent<Collider2D>().bounds.center);
            lineRenderer.SetPosition(1, grappleTarget);
            bossContext.transform.position = Vector3.MoveTowards(bossContext.transform.position, grappleTarget, bossContext.GrappleSpeed * Time.deltaTime);
            yield return null;
        }
        bossContext.GrapplingFinished = 1;
    }

}