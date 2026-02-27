using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerStateMachine : StateMachine, IDamageable
{
    //control variables
    [Header("Movement Control Variables")]
    [SerializeField] private  float runSpeed = 7f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float slashForce = 30f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float parryTiming = 2.5f;
    [SerializeField] private float parryCooldown = 2.5f;

    [Header("Object References")]
    [SerializeField] private GameManager manager;
    [SerializeField] private BoxCollider2D swordHitbox;
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private TextMeshProUGUI dashBar;
    [SerializeField] private GameObject shootIcon;

    //player input system
    private PlayerInput playerInput;
    private Vector2 currentMovementInput;
    private bool isMovementPressed;
    private bool canMove = true;
    private bool shootUnlocked = false;
    private bool canDash = false;
    private bool isRunPressed;
    private bool isJumpPressed;
    private bool isHitPressed;
    private bool isShootPressed;
    private bool isDashPressed;
    private bool isHurt; 
    private bool attackFinished = false;
    private bool blockFinished = true;
    private bool shootStarted = false;
    private bool shootFinished = false;
    private bool dashStarted = false;
    private bool dashFinished = false;
    private bool isDashing = false;
    private bool hurtFinished = false;
    private bool grounded = true;
    private bool isBlocking = false;
    private bool isParrying = false;
    private bool canParry = false;

    //player info
    private int health;
    private float damageCooldown;
    private float canTakeDamage;

    //additional game objects
    private GameObject dashTrail;
    private Transform groundCheck;
    private Player_Ranged rangedWeapon;
    private int currentParryCooldownId;
    private ParticleSystem damageTakenParticles;
    //getters and settesr
    public GameManager Manager {get {return manager;}}
    public bool CanMove {get {return canMove;} set {canMove = value;}}
    public bool IsMovementPressed {get {return isMovementPressed;} set {isMovementPressed = value;}}
    public bool IsRunPressed {get {return isRunPressed;} set {isRunPressed = value;}}
    public bool IsJumpPressed {get {return isJumpPressed;} set {isJumpPressed = value;}}
    public bool IsHitPressed {get {return isHitPressed;} set {isHitPressed = value;}}
    public bool IsShootPressed {get {return isShootPressed;} set {isShootPressed = value;}}
    public bool IsAimingForward {get {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane screen = new Plane(Vector3.forward, Vector3.zero);
        float distanceFromScreen;
        Vector3 mouseWorldPos = Vector3.zero;
        if (screen.Raycast(mouseRay, out distanceFromScreen))
        {
            mouseWorldPos = mouseRay.GetPoint(distanceFromScreen);
        }
        float mouseDirX = mouseWorldPos.x - sprite.position.x;
        float facing = Mathf.Sign(sprite.localScale.x);
        return Mathf.Sign(mouseDirX) == facing;
    }}
    public bool IsDashPressed {get {return isDashPressed;} set {isDashPressed = value;}}
    public bool IsBlocking {get {return isBlocking;} set {isBlocking = value;}}

    public bool CanParry {
        get {
            return canParry;
        }
        set {
            canParry = value;
            currentParryCooldownId++;
        }
    } // every change of the can parry variable makes a new id

    public bool IsParrying {get {return isParrying;} set {isParrying = value;}}
    public bool IsHurt{get {return isHurt;} set {isHurt = value;}}
    public bool AttackFinished {get {return attackFinished; } set {attackFinished = value;}}
    public bool BlockFinished {get {return blockFinished; } set {blockFinished = value;}}
    public bool ShootStarted {get {return shootStarted; } set {shootStarted = value;}}
    public bool ShootFinished {get {return shootFinished; } set {shootFinished = value;}}
    public bool DashStarted {get {return dashStarted; } set {dashStarted = value;}}
    public bool DashFinished {get {return dashFinished; } set {dashFinished = value;}}
    public bool IsDashing {get {return isDashing; } set {isDashing = value;}}
    public bool CanDash {get {return canDash;}}
    public bool ShootUnlocked {get {return shootUnlocked;}}
    public bool HurtFinished {get {return hurtFinished; } set {hurtFinished = value;}}
    public bool Grounded {get {return grounded;} set {grounded = value;}}
    public Vector2 CurrentMovementInput {get {return currentMovementInput;}}
    public float RunSpeed {get {return runSpeed;}}
    public float JumpForce {get {return jumpForce;}}
    public float SlashForce {get {return slashForce;}}
    public float DashSpeed {get {return dashSpeed;}}
    public float DashDistance {get {return dashDistance;}}
    public int Health {get {return health;} set {health = value;}}
    public float Cooldown {get {return damageCooldown;} set {damageCooldown = value;}}
    public GameObject DashTrail {get {return dashTrail;}}
    public Player_Ranged RangedWeapon { get { return rangedWeapon; } }

    protected override void Init()
    {
        base.Init();

        //set reference variables
        playerInput = new PlayerInput();
        dashTrail = transform.Find("ghost trail").gameObject;
        groundCheck = transform.Find("groundedCheck");
        swordHitbox = sprite.Find("sword").GetComponent<BoxCollider2D>();
        rangedWeapon = GetComponentInChildren<Player_Ranged>();
        damageTakenParticles = sprite.Find("hit received particles").GetComponent<ParticleSystem>();

        //set player input callbacks
        playerInput.CharacterControls.Move.started += OnMovementPerformed;
        playerInput.CharacterControls.Move.canceled += OnMovementCancelled;
        playerInput.CharacterControls.Move.performed += OnMovementPerformed;
        playerInput.CharacterControls.Run.started += OnRunStart;
        playerInput.CharacterControls.Run.canceled += OnRunEnd;
        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;
        playerInput.CharacterControls.Hit.started += OnHit;
        playerInput.CharacterControls.Hit.canceled += OnHit;
        playerInput.CharacterControls.Shoot.started += OnShoot;
        playerInput.CharacterControls.Shoot.canceled += OnShoot;
        playerInput.CharacterControls.Block.performed += OnBlock;
        playerInput.CharacterControls.Block.canceled += OnBlock;

        Health = 100;
        Cooldown = 1f;
        canTakeDamage = 0f; 
    }

    protected override void EnterBeginningState()
    {
        currentState = new PlayerIdleState(this);
        currentState.EnterState();
        UpdateHealthText();
    }

    protected override void UpdateState()
    {
        HandleMovement();
        currentState.UpdateStates();
    }

    private void HandleMovement()
    {
        if (canMove)
        {
            rb.linearVelocity = appliedMovement;
        } else
        {
            rb.AddForce(appliedMovement, ForceMode2D.Impulse);
        }
    }

    protected override void FaceMovement()
    {
        if (rb.linearVelocity.x != 0)
        {
            sprite.localScale = new Vector3(Mathf.Sign(rb.linearVelocity.x), 1, 1);
        }
    }

    void OnMovementPerformed(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        isMovementPressed = currentMovementInput.x != 0f;
        
    }

    void OnMovementCancelled(InputAction.CallbackContext context)
    {
        currentMovementInput = Vector2.zero;
        isMovementPressed = false;
    }

    void OnRunStart(InputAction.CallbackContext context)
    {
        isRunPressed = true;
        
    }
    void OnRunEnd(InputAction.CallbackContext context)
    {
        isRunPressed = false;
        
    }

    void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
        
    }
    void OnHit(InputAction.CallbackContext context)
    {
        isHitPressed = context.ReadValueAsButton();
    }
    void OnShoot(InputAction.CallbackContext context)
    {
        isShootPressed = shootUnlocked && context.ReadValueAsButton();
    }
    void OnBlock(InputAction.CallbackContext context) {
        isBlocking = context.ReadValueAsButton();
    }
    void OnDash(InputAction.CallbackContext context)
    {
        isDashPressed = context.ReadValueAsButton();
    }

    public void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    public void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
    
    private IEnumerator StartParryCooldownInternal() {
        CanParry = true;
        int targetParryCooldownId = currentParryCooldownId;
        yield return new WaitForSeconds(parryCooldown);
        if (targetParryCooldownId == currentParryCooldownId) {
            CanParry = false; // nothing was changed during the wait so was in the same parry
        }
    }
    
    private IEnumerator StartParryInternal() {
        if (IsParrying) yield break;
        IsParrying = true;
        yield return new WaitForSeconds(parryTiming);
        IsParrying = false;
    }

    public void StartParry()
    {
        Debug.Log("starting parry");
        StartCoroutine(StartParryInternal());
        IsHurt = false;
        CanParry = false; 
        IsBlocking = false;
    }
    public void StartParryCooldown() {
        StartCoroutine(StartParryCooldownInternal());
    }
    public void ApplyDamage(int damage) {
        if (isBlocking && canParry)
        {
            StartParry();
            return;
        }
        if (Time.time > canTakeDamage && !IsParrying)
        {   Debug.Log("taking damage");
            canTakeDamage = Time.time + Cooldown;
            Health -= damage; 
            IsHurt = true;
            currentState.SwitchState(new PlayerHurtState(this));
            damageTakenParticles.Play();
        }
        UpdateHealthText();
        if (Health <= 0f)
        {
            manager.CheckWinStatus();
        }
    }

    #region animation events
    void OnAttackAnimationStart()
    {
        AttackFinished = false;
        swordHitbox.enabled = true;

    }

    void OnAttackAnimationFinish()
    {
        AttackFinished = true;
        swordHitbox.enabled = false;
    }

    void OnBlockAnimationStart()
    {
        BlockFinished = false;
        swordHitbox.enabled = true;

    }

    void OnBlockAnimationFinish()
    {
        BlockFinished = true;
        swordHitbox.enabled = false;
    }

    void OnShootAnimationStart()
    {
        ShootFinished = false;
    }
    void TriggerBulletShooting()
    {
        ShootStarted = true;
    }
    void OnShootAnimationFinish()
    {
        ShootFinished = true;
        ShootStarted = false;
    }

    void OnHurtAnimationStart()
    {
        HurtFinished = false;
    }
    void OnHurtAnimationFinish()
    {
        HurtFinished = true;
    }
    #endregion

    public void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            grounded = true;
        }
    }

    public void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            grounded = false;
        }
    }

    //ability 2 is shooting
    //ability 3 is dashing
    public void UnlockAbility(int abilityNum)
    {
        if (abilityNum == 2)
        {
            shootUnlocked = true;
            Debug.Log("you can now shoot! click LMB to shoot at your mouse position");
            shootIcon.SetActive(true);
        } else if (abilityNum == 3)
        {
            canDash = true;
            Debug.Log("you can now shoot! press shift to launch yourself!");
            dashBar.gameObject.SetActive(true);
        }
    }

    public void UpdateHealthText()
    {
        healthBar.text = "Health: " + Health.ToString();
    }


}
