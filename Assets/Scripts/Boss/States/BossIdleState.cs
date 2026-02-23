using UnityEngine;
public class BossIdleState : State
{
    private BossStateMachine bossContext;
    private float curTime;
    public BossIdleState(BossStateMachine currentContext) : base(currentContext)
    {
        bossContext = currentContext;
    }
    public override void EnterState()
    {
        bossContext.Anim.SetTrigger("idle");
        bossContext.AppliedMovementX = 0f;
        bossContext.AppliedMovementY = 0f;
        curTime = 0f;
    }
    public override void UpdateState()
    {
        curTime += Time.deltaTime;
        CheckSwitchStates();
    }
    public override void ExitState()
    {
        bossContext.Anim.ResetTrigger("idle");
    }

    public override void CheckSwitchStates()
    {
        if (curTime > bossContext.TimeInIdle)
        {
            float randomChance = Random.Range(0f, 1f);
            if (bossContext.CurrentStage >= 2 && randomChance > 0.7f && bossContext.GrappleInRange())
            {
                SwitchState(new BossGrappleState(bossContext));
            } else 
            if (bossContext.CurrentStage == 2 && bossContext.CanSummon())
            {
                bossContext.NextAttack = 2;
                SwitchState(new BossBeginSummonsState(bossContext));
            }
            else if (bossContext.CurrentStage == 3 && randomChance < 0.3f && bossContext.canDashAttack())
            {
                bossContext.NextAttack = 3;
                SwitchState(new BossChargedDashState(bossContext));
            }
            else if (randomChance < 0.3f)
            {
                bossContext.NextAttack = 1;
                SwitchState(new BossLaserWindupState(bossContext));
            }
            else {
                SwitchState(new BossWalkState(bossContext));
            }
            
        } 
    }
}