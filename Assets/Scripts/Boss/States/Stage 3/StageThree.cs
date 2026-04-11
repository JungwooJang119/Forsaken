using UnityEngine;

public class StageThree : State
{
    private BossStateMachine bossContext;
    public StageThree(BossStateMachine currentContext) : base(currentContext)
    {
        
        bossContext = currentContext;
        isBaseState = true;
        InitializeSubStates();
    }
    public override void InitializeSubStates()
    {
        if (bossContext.NextAttack == 1)
        {   
            SetSubState(new BossLaserWindupState(bossContext));
        } else if (bossContext.NextAttack == 2)
        {
            SetSubState(new BossMeleeAttackState(bossContext));
        } else if (bossContext.NextAttack == 3)
        {
            SetSubState(new BossChargedDashState(bossContext));
        } else if (bossContext.NextAttack == 4)
        {
            SetSubState(new BossGrappleState(bossContext));
        }
        else 
        {
            SetSubState(new BossIdleState(bossContext));
        }
    }
    public override void EnterState()
    {
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void ExitState()
    {
    }

    public override void CheckSwitchStates()
    {
        if (bossContext.IsTransitioning)
        {
            SwitchState(new BossTransitionState(bossContext));
        }
    }
}
