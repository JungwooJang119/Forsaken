using UnityEngine;

public class PlayerAttackState : State
{
    private PlayerStateMachine playerContext;
    public PlayerAttackState(PlayerStateMachine currentContext) : base(currentContext)
    {
        playerContext = currentContext;
        isBaseState = true;
        InitializeSubStates();
    }
    public override void EnterState()
    {
        playerContext.CanMove = false;
        playerContext.AppliedMovementX = 0f;
        playerContext.AttackFinished = false; 
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void ExitState()
    {
        playerContext.CanMove = true;
    }

    public override void InitializeSubStates()
    {
        SetSubState(new PlayerMeleeOneState(playerContext));
    }

    public override void CheckSwitchStates()
    {
        if (playerContext.IsHurt)
        {
            SwitchState(new PlayerHurtState(playerContext));
        } else if (playerContext.IsRunPressed)
        {
            SwitchState(new PlayerDashState(playerContext));
        } else if (playerContext.IsMovementPressed){
            SwitchState(new PlayerWalkState(playerContext));
        } else if (playerContext.AttackFinished)
        {
            SwitchState(new PlayerIdleState(playerContext));
        }
    }
}
