using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerStateMachine : StateMachine, IDamageable, ISetDifficulty
{   
    #region SerializableElements
    [Header("Movement Control Variables")]
    [SerializeField] private  float runSpeed = 7f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float slashForce = 30f;
    [SerializeField] private float recoilForce = 30f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.18f; // Duration based dash
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashEndDrag = 0f; // drag after doing dash
    [SerializeField] private bool dashDisableGravity = true; // for a more floaty/controllable mvmt feel

    [Header("Jump")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 2.6f; // adjust for a snappy fall feel
    [SerializeField] private float jumpCutGravityMultiplier = 2.2f; // short hop/jump

    [Header("Horizontal Acceleration")]
    [SerializeField] private float groundAccel = 80f;
    [SerializeField] private float groundDecel = 90f;
    [SerializeField] private float airAccel = 45f;
    [SerializeField] private float airDecel = 30f;
    [SerializeField] private float inputDeadzone = 0.02f;

    [Header("Parry")]
    [SerializeField] private float parryTiming = 2.5f;
    [SerializeField] private float parryCooldown = 2.5f;

    [Header("Stamina/Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float dashCost = 10f;
    [SerializeField] private float shootCost = 10f;
    [SerializeField] private float attackGain = 10f;

    [Header("Object References")]
    [SerializeField] private GameManager manager;
    [SerializeField] private BoxCollider2D swordHitbox;
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private TextMeshProUGUI dashBar;
    [SerializeField] private GameObject shootIcon;
    [SerializeField] private Image energyFill;


    [SerializeField]private DialogueUI dialogueUI;
    public DialogueUI DialogueUI => dialogueUI;

    [Header("Ground/Wall Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.12f);
    [SerializeField] private LayerMask groundMask;

    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.45f;
    [SerializeField] private LayerMask wallMask;

    #endregion
    
    #region PlayerStateInfo
    private PlayerInput playerInput;
    private Vector2 currentMovementInput;

    private bool isMovementPressed;
    private bool canMove = true;
    private bool shootUnlocked = true;
    private bool canDash = true;

    private bool isRunPressed;
    private bool isHitPressed;
    private bool isShootPressed;
    private bool isBlockPressed;
    
    private bool isJumpPressed;
    private bool jumpPressedThisFrame;
    private bool jumpReleasedThisFrame;
    private float lastJumpPressedTime;

    private bool isDashPressed;
    private float lastDashPressedTime;
    [SerializeField] private float dashBufferTime = 0.12f;

    private bool isHurt; 
    private bool attackFinished = false;
    private bool blockFinished = true;
    private bool shootStarted = false;
    private bool shootFinished = false;
    private bool hurtFinished = false;

    // Physics state
    private bool grounded = true;
    private bool hitWall = false;

    //Dash
    private bool isDashing = false;
    private float dashEndTime;
    private float defaultGravityScale;
    private float dashDir;
    private bool dashStarted = false;
    private bool dashFinished = false;

    //Parry
    private bool isParrying = false;
    private int currentParryCooldownId;
    private bool canParry = false;
    private bool isBlocking = false;

    public IInteractable Interactable { get; set; }
    #endregion

    #region PlayerHealth
    //player info
    private int health;
    private float damageCooldown;
    private float canTakeDamage;
    #endregion

    private float currentEnergy;

    #region VFX Items
    //additional game objects
    private GameObject dashTrail;
    private Player_Ranged rangedWeapon;
    
    private ParticleSystem damageTakenParticles;
    [SerializeField] private ParticleSystem parryParticles;
    #endregion

    #region Getters and Setters
    public GameManager Manager {get {return manager;}}

    public bool CanMove {get {return canMove;} set {canMove = value;}}
    public bool IsMovementPressed {get {return isMovementPressed;} set {isMovementPressed = value;}}
    public bool IsRunPressed {get {return isRunPressed;} set {isRunPressed = value;}}
    public bool IsHitPressed {get {return isHitPressed;} set {isHitPressed = value;}}
    public bool IsShootPressed {get {return isShootPressed;} set {isShootPressed = value;}}

    public bool IsJumpPressed {get {return isJumpPressed;} set {isJumpPressed = value;}}

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
    public bool HitWall{get {return hitWall;} set {hitWall = value;}}

    public bool AttackFinished {get {return attackFinished; } set {attackFinished = value;}}
    public bool BlockFinished {get {return blockFinished; } set {blockFinished = value;}}
    public bool ShootStarted {get {return shootStarted; } set {shootStarted = value;}}
    public bool ShootFinished {get {return shootFinished; } set {shootFinished = value;}}

    public bool IsDashing {get {return isDashing; } set {isDashing = value;}}

    public bool CanDash {get {return canDash && !hitWall;}}
    public bool ShootUnlocked {get {return shootUnlocked;}}

    public bool Grounded {get {return grounded;} set {grounded = value;}}

    public Vector2 CurrentMovementInput {get {return currentMovementInput;}}

    public float RunSpeed {get {return runSpeed;}}
    public float JumpForce {get {return jumpForce;}}
    public float SlashForce {get {return slashForce;}}
    public float DashSpeed {get {return dashSpeed;}}

    public int Health {get {return health;} set {health = value;}}
    public float Cooldown {get {return damageCooldown;} set {damageCooldown = value;}}
    
    public GameObject DashTrail {get {return dashTrail;}}
    public BoxCollider2D SwordHitbox {get {return swordHitbox;}}
    public Player_Ranged RangedWeapon { get { return rangedWeapon; } }

    public float Energy {get {return currentEnergy;} set {currentEnergy = value;}}
    public float DashCost {get {return dashCost;}}
    public float ShootCost {get {return shootCost;}}
    public float AttackGain {get {return attackGain;}}


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

    public bool DashStarted {get {return dashStarted; } set {dashStarted = value;}}
    public bool DashFinished {get {return dashFinished; } set {dashFinished = value;}}
    
    public bool HurtFinished {get {return hurtFinished; } set {hurtFinished = value;}}
    // public bool Grounded {get {return grounded;} set {grounded = value;}}
    // public Vector2 CurrentMovementInput {get {return currentMovementInput;}}

    public float DashDistance {get {return dashDistance;}}
    
    #endregion

    #region StateMachine Updates
    protected override void Init()
    {
        base.Init();

        //set reference variables
        playerInput = new PlayerInput();

        dashTrail = transform.Find("ghost trail").gameObject;
        swordHitbox = sprite.Find("sword").GetComponent<BoxCollider2D>();
        rangedWeapon = GetComponentInChildren<Player_Ranged>();
        damageTakenParticles = sprite.Find("hit received particles").GetComponent<ParticleSystem>();

        // for jump/dash gravity adjust
        defaultGravityScale = rb.gravityScale;

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

        playerInput.CharacterControls.Dash.started += OnDash;
        playerInput.CharacterControls.Dash.canceled += OnDash;

        playerInput.CharacterControls.Interact.performed += OnInteractPressed;
        playerInput.CharacterControls.Interact.canceled += OnInteractPressed;

        Health = 100;
        Energy = maxEnergy;
        Cooldown = 3f;
        canTakeDamage = 0f; 
        energyFill.fillAmount = 1;
    }

    protected override void EnterBeginningState()
    {
        currentState = new PlayerIdleState(this);
        currentState.EnterState();
    }

    
    private void Update()
    {
        if (dialogueUI != null && dialogueUI.IsOpen) return;

        currentState?.UpdateStates();

        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
        isDashPressed = false;
    }


    protected override void UpdateState()
    {
        if (dialogueUI != null && dialogueUI.IsOpen) return;

        currentState.UpdateStates();

        print($"State: {currentState.GetType().Name}  grounded={grounded}  movePressed={isMovementPressed}  xIn={currentMovementInput.x}");

        // Reset one frame inputs per update
        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
        isDashPressed = false;
        // HandleMovement();
    }

    private void FixedUpdate() {
        if (dialogueUI != null && dialogueUI.IsOpen) return;

        MotorTick(Time.fixedDeltaTime);
        FaceMovement();
    }
    #endregion

    #region Motor (Movement System improvements)
    private float lastGroundedTime;

    private void MotorTick(float dt) {
        
        // check ground and wall
        grounded = CheckGrounded();
        if (grounded) {
            lastGroundedTime = Time.time;
        }

        hitWall = CheckWall();

        // dash override normal run
        if (isDashing) {
            if (Time.time >= dashEndTime) {
                EndDash();
            } else {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f); // constant dash speed
                return;
            }
        }

        // horiz smoothing (this might be cooked)
        float inputX = Mathf.Abs(currentMovementInput.x) < inputDeadzone ? 0f : currentMovementInput.x;

        // holding = faster over time slightly
        float maxSpeed = isRunPressed ? runSpeed : runSpeed * 0.85f; // run vs walk basically

        float targetVelX = canMove ? inputX * maxSpeed : rb.linearVelocity.x;

        float accel = grounded
            ? (Mathf.Abs(targetVelX) > 0.01f ? groundAccel : groundDecel)
            : (Mathf.Abs(targetVelX) > 0.01f ? airAccel : airDecel);

        float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelX, accel * dt);
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

        // the jump buffer with coyote time
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        bool canUseCoyote = (Time.time - lastGroundedTime) <= coyoteTime;

        // jump feeling
        if (bufferedJump && canUseCoyote && !IsParrying && !isHurt) {
            lastJumpPressedTime = -9999f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            grounded = false;
        }

        // buffer for dash
        bool bufferedDash = (Time.time - lastDashPressedTime) <= dashBufferTime;
        if (bufferedDash && CanDash && !IsParrying && !isHurt && Energy >= dashCost) {
            lastDashPressedTime = -9999f;
            StartDashInternal();
        }

        ApplyBetterGravity(); // gravity falling feeling better
    }

    private void ApplyBetterGravity() {
        if (isDashing && dashDisableGravity) {
            rb.gravityScale = 0f;
            return;
        }

        // more snappy falling
        if (rb.linearVelocity.y < -0.01f) {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        } else if (rb.linearVelocity.y > 0.01f && !isJumpPressed) { // short hop
            rb.gravityScale = defaultGravityScale * jumpCutGravityMultiplier;
        } else {
            rb.gravityScale = defaultGravityScale;
        }
    }

    private void StartDashInternal() {
        if (isDashing) return;

        updateEnergy(-dashCost);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;

        dashDir = Mathf.Sign(sprite.localScale.x);
        if (dashDir == 0) {
            dashDir = 1f;
        }

        if (dashDisableGravity) {
            rb.gravityScale = 0f;
        }

        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
        if (dashTrail != null) {
            dashTrail.SetActive(true);
        }
    }

    private void EndDash() {
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = dashEndDrag;

        if (dashTrail != null) {
            dashTrail.SetActive(false);
        }
    }

    private bool CheckGrounded() {
        if (groundCheck == null) return grounded;

        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask);
    }

    private bool CheckWall() {
        if (wallCheck == null) return hitWall;

        float dir = Mathf.Sign(sprite.localScale.x);
        if (dir == 0) dir = 1f;

        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * dir, wallCheckDistance, wallMask);
        return hit.collider != null;
    }

    protected override void FaceMovement() {
        float inputX = currentMovementInput.x;
        if (Mathf.Abs(inputX) > 0.05f) {
            sprite.localScale = new Vector3(Mathf.Sign(inputX), 1, 1);
        } else if (Mathf.Abs(rb.linearVelocity.x) > 0.05f) {
            sprite.localScale = new Vector3(Mathf.Sign(rb.linearVelocity.x), 1, 1);
        }
    }

    public void ApplyDamage(int damage) {
        if (isBlocking && canParry)
        {
            StartParry();
            ApplyRecoil(new Vector3(sprite.localScale.x * -1 * recoilForce, 0f, 0f));
            return;
        }
        if (Time.time > canTakeDamage && !IsParrying)
        { 
            canTakeDamage = Time.time + Cooldown;
            Health -= damage; 
            IsHurt = true;
            currentState.SwitchState(new PlayerHurtState(this));
            damageTakenParticles.Play();
        }
        if (Health <= 0f)
        {
            manager.CheckWinStatus();
        }
    }

    public void UnlockAbility(int abilityNum)
    {
        //ability 2 is shooting
        //ability 3 is dashing
        if (abilityNum == 2)
        {
            shootUnlocked = true;
            Debug.Log("you can now shoot! click LMB to shoot at your mouse position");
        } else if (abilityNum == 3)
        {
            canDash = true;
            Debug.Log("you can now shoot! press shift to launch yourself!");
        }
    }

    public void ApplyRecoil(Vector3 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    public void updateEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        if (energyFill != null) {
            energyFill.fillAmount = currentEnergy / maxEnergy;
        }
    }
    #endregion

    #region Parry Controls
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
        parryParticles.Play();
        StartCoroutine(StartParryInternal());
        IsHurt = false;
        CanParry = false; 
        IsBlocking = false;
    }
    
    public void StartParryCooldown() {
        StartCoroutine(StartParryCooldownInternal());
    }
    #endregion

    #region Player Input Controls
    void OnMovementPerformed(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        isMovementPressed = Mathf.Abs(currentMovementInput.x) > inputDeadzone;
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
        if (context.started) {
            jumpPressedThisFrame = true;
            lastJumpPressedTime = Time.time;

            isJumpPressed = true;
        } else if (context.canceled) {
            isJumpPressed = false;
            jumpReleasedThisFrame = true;
        }
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
        if (context.started) {
            isDashPressed = true;
            lastDashPressedTime = Time.time;
        }
    }
    void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (Interactable != null && Interactable.CanInteract())
        {
           Interactable?.Interact(this); 
        }
    }

    public void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }
    public void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
    #endregion

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

    }

    void OnBlockAnimationFinish()
    {
        BlockFinished = true;
    }

    void OnShootAnimationStart()
    {
        ShootFinished = false;
    }
    void TriggerBulletShooting()
    {   
        if (Energy < 10) {return;}
        ShootStarted = true;
        updateEnergy(-shootCost);
           
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
    
    public void HandleDifficulty(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                Health = 200;
                Cooldown = 6f;
                break;
            case Difficulty.Normal:
                Health = 100;
                Cooldown = 3f;
                break;
            case Difficulty.Hard:
                Health = 50;
                Cooldown = 1.5f;
                break;
        }
    }

    private void OnDrawGizmosSelected() {
        if (groundCheck != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null) {
            Gizmos.color = Color.red;
            float dir = (sprite != null) ? Mathf.Sign(sprite.localScale.x) : 1f;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * dir * wallCheckDistance);
        }
    }
}
