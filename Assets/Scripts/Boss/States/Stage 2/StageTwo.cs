using UnityEngine;

public class StageTwo : State
{
    private BossStateMachine bossContext;
    public StageTwo(BossStateMachine currentContext) : base(currentContext)
    {
        
        bossContext = currentContext;
        isBaseState = true;
        InitializeSubStates();
    }
    public override void InitializeSubStates()
    {
        if (bossContext.CanSummon())
        {
            SetSubState(new BossBeginSummonsState(bossContext));
        }
        else if (bossContext.NextAttack == 1)
        {   
            SetSubState(new BossLaserWindupState(bossContext));
        } else if (bossContext.NextAttack == 2)
        {
            SetSubState(new BossMeleeAttackState(bossContext));
        } else 
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
