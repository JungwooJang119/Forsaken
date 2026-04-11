using UnityEngine;
public class BossIdleState : State
{
    private BossStateMachine bossContext;
    private float curTime;
    private static int teleport = 0;
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
            // If stage 2 and boss can summon, always summon
            if (bossContext.CurrentStage == 2 && bossContext.CanSummon())
            {
                bossContext.NextAttack = 2;
                SwitchState(new BossBeginSummonsState(bossContext));
            }
            else
            {
                //Teleport every number of attacks
                //Consider also making it a random chance to teleport?
                teleport += 1;
                if (bossContext.CurrentStage == 3 && teleport % 5 == 0)
                {
                    SwitchState(new BossTeleportState(bossContext));
                }
                // If stage 3 and ultimate is available, rand chance to ult
                else if (bossContext.CurrentStage == 3 && bossContext.CanTriggerUltimate() && Random.value < 0.5f)
                {
                    SwitchState(new BossUltimateState(bossContext));
                }
                // If stage 3 and boss can charged dash, always charged dash
                else if (randomChance < 0.4f && bossContext.CurrentStage == 3 && bossContext.canDashAttack())
                {
                    bossContext.NextAttack = 3;
                    SwitchState(new BossChargedDashState(bossContext));
                }
                // Else, choose between melee and laser attack
                else if (
                    bossContext.CurrentStage >= 2
                    && randomChance >= 0.5f
                    && bossContext.GrappleInRange()
                )
                {
                    bossContext.NextAttack = 4;
                    SwitchState(new BossGrappleState(bossContext));
                }
                else if (randomChance < 0.5f)
                {
                    bossContext.NextAttack = 1;
                    SwitchState(new BossLaserWindupState(bossContext));
                }
                else
                {
                    SwitchState(new BossWalkState(bossContext));
                }
            }
        } 
    }
}