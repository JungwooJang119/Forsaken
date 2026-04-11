using Unity.VisualScripting;
using UnityEngine;
public class BossStateMachine : StateMachine, IDamageable
{   
    #region Serializable Entries
    [Header("Object References")]
    [SerializeField] private GameManager manager;
    [SerializeField] private Transform summonPosition;
    [SerializeField] private GameObject attackDog;

    [Header("Attack Controls")]
    [SerializeField] private float targetDistance;
    [SerializeField] private float timeInIdle;
    [SerializeField] private float stunTime;
    [SerializeField] private float stunInterval;
    [SerializeField] private int damage;
    [SerializeField] private float damageCooldown;

    [Header("Dash Targetting Controls")]
    [SerializeField] private float dashMovementCooldown;
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashSpeed;

    [Header("Grapple Settings")]
    [SerializeField] private float grappleTargetDistance;
    [SerializeField] private float grappleDuration;
    [SerializeField] private float grappleSpeed;

    [Header("Charged Attack Dash Settings")]
    [SerializeField] private float dashCD;
    [SerializeField] private float dashRange;

    [Header("Enemy Summons Settings")]
    [SerializeField] private float summonCooldown;
    [SerializeField] private int numEnemies;
    #endregion

    #region Boss State Info
    private int health;
    private bool isFlipped = false;
    private bool isStunned = false;
    private int introFinished = 0;
    private int attackFinished = 0;
    private int lasersFinished = 0;
    private int hurtFinished = 0;
    private int nextAttack;
    private int grapplingFinished = 0;
    private bool isDashing = false;
    private float lastDashTime = 0;
    private float lastDashMovementTime = 0;
    private bool windUpFinished = true;
    private float lastDroneSummon = 0;
    private int curEnemies = 0;
    #endregion

    #region VFX
    private ParticleSystem damageTakenParticles;
    private ParticleSystem attackIndicator;
    #endregion
    
    #region Getters and Setters
    public Transform SummonPos {get {return summonPosition;}}
    public bool FightStarted {get {return manager.FightStarted;}}
    public bool IsStunned {get {return isStunned;} set {isStunned = value;}}
    public bool IsDashing {get {return isDashing;} set {isDashing = value;}}
    public bool IsTransitioning {get {return manager.IsTransitioning;} set {manager.IsTransitioning = value;}}
    public int GrapplingFinished {get {return grapplingFinished;} set {grapplingFinished = value;}}
    public bool WindUpFinished { get {return windUpFinished;} set { windUpFinished = value; } }
    public int AttackFinished {get {return attackFinished; } set {attackFinished = value;}}
    public int LasersFinished {get {return lasersFinished; } set {lasersFinished = value;}}
    public bool Flipped { get {return isFlipped;}}
    public int HurtFinished {get {return hurtFinished; } set {hurtFinished = value;}}
    public int IntroFinished {get {return introFinished; } set {introFinished = value;}}
    public int Health {get {return health;} set {health = value;}}
    public int CurEnemies {get {return curEnemies;} set {curEnemies = value;}}
    public GameObject AttackDog {get {return attackDog;}}
    public int Damage {get {return damage;} set {damage = value;}}
    public float LastDashMovementTime { get { return lastDashMovementTime; } set { lastDashMovementTime = value; } }
    public float LastDashTime { get { return lastDashTime; } set { lastDashTime = value; } }
    public float LastDroneSummon { get { return lastDroneSummon; } set { lastDroneSummon = value; } }
    public float Cooldown {get {return damageCooldown;} set {damageCooldown = value;}}
    public float TimeInIdle {get {return timeInIdle;}}
    public float StunTime {get {return stunTime;}}
    public float StunInterval {get {return stunInterval;}}
    public float TargetDistance {get {return targetDistance;}}
    public float DashDistance {get {return dashDistance;}}
    public float DashSpeed {get {return dashSpeed;}}
    public float GrappleDuration {get {return grappleDuration;}}
    public float GrappleSpeed {get {return grappleSpeed;}}
    public float GrappleTargetDistance {get {return grappleTargetDistance;}}
    public int CurrentStage {get {return manager.CurrentStage;} set {manager.CurrentStage = value;}}
    public int NextAttack {get {return nextAttack;} set {nextAttack = value;}}
    public bool canDashAttack()
    {
        return !InRange() && Vector3.Distance(transform.position, Player.transform.position) <= dashRange && (Time.time >= lastDashTime + dashCD);
    }

    public bool canDashMove()
    {
        return !InDashRange() && (Time.time >= lastDashMovementTime + dashMovementCooldown);
    }
    public bool CanSummon()
    {
        return Time.time >= lastDroneSummon + summonCooldown && curEnemies < numEnemies;
    }

    public bool CanTriggerUltimate()
    {
        // make it a smaller chance later
        // bool lowHp = Health <= (Health * 0.2f);
        return Health <= (Health * 1f);
    }


    public bool InRange()
    {
        return Vector3.Distance(transform.position,Player.transform.position) <= TargetDistance;
    }

    public bool InDashRange()
    {
        return Vector3.Distance(transform.position,Player.transform.position) <= DashDistance;
    }

    public bool GrappleInRange()
    {
        return Vector2.Distance(transform.position, Player.transform.position) > GrappleTargetDistance;
    }
    #endregion

    #region State Machine Updates
    protected override void Init()
    {
        base.Init();
        sprite = transform.Find("Sprite");
        Health = 100;
        damageTakenParticles = sprite.Find("hit received particles").GetComponent<ParticleSystem>();
        attackIndicator = sprite.Find("Broadsword").Find("ShootPoint").Find("Attack Indicator").GetComponent<ParticleSystem>();
    }

    protected override void EnterBeginningState()
    {
        IsTransitioning = false;
        currentState = new BossStartState(this);
        currentState.EnterStates();
    }

    protected override void UpdateState()
    {
        if (!IsTransitioning)
        {
            rb.linearVelocity = appliedMovement;
        }
        currentState.UpdateStates();
    }
    #endregion
    
    #region Movement and Health Updates
    protected override void FaceMovement()
    {
        Vector3 flipped = sprite.localScale;
        flipped.x *= -1f;
        if (sprite.position.x < player.transform.position.x && isFlipped)
        {
            sprite.localScale = flipped;
            isFlipped = false;
        } else if (sprite.position.x > player.transform.position.x && !isFlipped)
        {
            sprite.localScale = flipped;
            isFlipped = true;
        }
    }
    public void ApplyDamage(int damage)
    {
        if (IntroFinished == 1 && manager.FightStarted)
        {
            Health -= damage;
            Debug.Log("Enemy Health: " + Health);
            //flashCharacter();
            damageTakenParticles.Play();

        }
        if (Health <= 0f)
        {
            manager.CheckWinStatus();
        }
    }
    public void Stun()
    {
        Debug.Log("stunned");
        currentState.SwitchState(new BossStunState(this));
    }
    #endregion

    #region Collision Events
    public void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.transform == player)
        {
            player.gameObject.GetComponent<PlayerStateMachine>().ApplyDamage(Damage);
        }
    }
    
    #endregion

    #region Animation Events
    public void onWindupStart()
    {
        windUpFinished = false;
    }

    public void onWindupEnd()
    {
        windUpFinished = true;
    }

    public void OnAttackStart()
    {
        attackFinished = 0;

    }

    public void OnAttackEnd()
    {
        attackFinished = 1;
    }

    public void OnLaserAttackStart()
    {
        lasersFinished = 0;

    }

    public void OnLaserAttackEnd()
    {
        lasersFinished = 1;

    }

    public void AttackIndicator()
    {
        attackIndicator.Play();
    }
    #endregion
 
}