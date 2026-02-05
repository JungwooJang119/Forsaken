using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CharacterController2 : StateMachine, IDamageable
{
    //control variables
    [Header("Movement Control Variables")]
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float slashForce = 30f;
    [SerializeField] private float dashForce = 30f;
    [SerializeField] private int dashMeter = 10;

    [Header("Object References")]
    [SerializeField] private GameManager manager;
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private TextMeshProUGUI dashBar;
    [SerializeField] private GameObject shootIcon;

    //player input system
    private PlayerInput playerInput;
    private Vector2 currentMovementInput;
    private bool isMovementPressed;
    private bool canMove = true;
    private bool shootUnlocked = false;
    private bool dashUnlocked = false;
    private bool isRunPressed;
    private bool isJumpPressed;
    private bool isHitPressed;
    private bool isShootPressed;
    private bool isDashPressed;
    private bool isHurt;
    private bool attackFinished = false;
    private bool shootStarted = false;
    private bool shootFinished = false;
    private bool dashStarted = false;
    private bool dashFinished = false;
    private bool isDashing = false;
    private bool hurtFinished = false;
    private bool grounded = true;

    //player info
    private int health;
    private float damageCooldown;
    private float canTakeDamage;
    private int currentDashMeter;

    //additional game objects
    private GameObject dashTrail;
    private GameObject dashArrow;
    private Transform groundCheck;

    //getters and settesr
    public GameManager Manager { get { return manager; } }
    public bool CanMove { get { return canMove; } set { canMove = value; } }
    public bool IsMovementPressed { get { return isMovementPressed; } set { isMovementPressed = value; } }
    public bool IsRunPressed { get { return isRunPressed; } set { isRunPressed = value; } }
    public bool IsJumpPressed { get { return isJumpPressed; } set { isJumpPressed = value; } }
    public bool IsHitPressed { get { return isHitPressed; } set { isHitPressed = value; } }
    public bool IsShootPressed { get { return isShootPressed; } set { isShootPressed = value; } }
    public bool IsAimingForward
    {
        get
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float mouseDirX = mousePos.x - sprite.position.x;
            float facing = Mathf.Sign(sprite.localScale.x);
            return Mathf.Sign(mouseDirX) == facing;
        }
    }
    public bool IsDashPressed { get { return isDashPressed; } set { isDashPressed = value; } }
    public bool IsHurt { get { return isHurt; } set { isHurt = value; } }
    public bool AttackFinished { get { return attackFinished; } set { attackFinished = value; } }
    public bool ShootStarted { get { return shootStarted; } set { shootStarted = value; } }
    public bool ShootFinished { get { return shootFinished; } set { shootFinished = value; } }
    public bool DashStarted { get { return dashStarted; } set { dashStarted = value; } }
    public bool DashFinished { get { return dashFinished; } set { dashFinished = value; } }
    public bool IsDashing { get { return isDashing; } set { isDashing = value; } }
    public int CurrentDashMeter { get { return currentDashMeter; } set { currentDashMeter = value; } }
    public bool DashUnlocked { get { return dashUnlocked; } }
    public bool CanDash { get { return dashUnlocked && currentDashMeter >= dashMeter; } }
    public bool HurtFinished { get { return hurtFinished; } set { hurtFinished = value; } }
    public bool Grounded { get { return grounded; } set { grounded = value; } }
    public Vector2 CurrentMovementInput { get { return currentMovementInput; } }
    public float RunSpeed { get { return runSpeed; } }
    public float JumpForce { get { return jumpForce; } }
    public float SlashForce { get { return slashForce; } }
    public float DashForce { get { return dashForce; } }
    public int Health { get { return health; } set { health = value; } }
    public float Cooldown { get { return damageCooldown; } set { damageCooldown = value; } }
    public GameObject DashTrail { get { return dashTrail; } }
    public GameObject DashArrow { get { return dashArrow; } }

    protected override void Init()
    {
        base.Init();

        //set reference variables
        playerInput = new PlayerInput();
        dashTrail = transform.Find("ghost trail").gameObject;
        dashArrow = transform.Find("dash arrow").gameObject;
        groundCheck = transform.Find("groundedCheck");

        //set player input callbacks
        playerInput.CharacterControls.Move.started += OnMovementPerformed;
        playerInput.CharacterControls.Move.canceled += OnMovementCancelled;
        playerInput.CharacterControls.Move.performed += OnMovementPerformed;
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;
        playerInput.CharacterControls.Hit.started += OnHit;
        playerInput.CharacterControls.Hit.canceled += OnHit;
        playerInput.CharacterControls.Shoot.started += OnShoot;
        playerInput.CharacterControls.Shoot.canceled += OnShoot;
        playerInput.CharacterControls.Dash.started += OnDash;
        playerInput.CharacterControls.Dash.canceled += OnDash;

        Health = 100;
        Cooldown = 1f;
        canTakeDamage = 0f;
        currentDashMeter = 0;
    }

    protected override void EnterBeginningState()
    {
        currentState = new PlayerIdleState(this);
        currentState.EnterState();
        UpdateHealthText();
        UpdateDashText();
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
        }
        else
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

    void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();

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
        UpdateDashText();
    }
    void OnDash(InputAction.CallbackContext context)
    {
        isDashPressed = context.ReadValueAsButton();
        UpdateDashText();
    }

    public void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    public void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }

    public void ApplyDamage(int damage)
    {
        if (Time.time > canTakeDamage && !isDashing)
        {
            canTakeDamage = Time.time + Cooldown;
            Health -= damage;
            IsHurt = true;
        }
        UpdateHealthText();
        if (Health <= 0f)
        {
            manager.CheckWinStatus();
        }

    }


    void OnAttackAnimationStart()
    {
        AttackFinished = false;

    }

    void OnAttackAnimationFinish()
    {
        AttackFinished = true;
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

    public void UnlockAbility(int stage)
    {
        if (stage == 2)
        {
            shootUnlocked = true;
            Debug.Log("you can now shoot! click LMB to shoot at your mouse position");
            shootIcon.SetActive(true);
        }
        else if (stage == 3)
        {
            dashUnlocked = true;
            Debug.Log("you can now shoot! click and drag RMB to launch yourself!");
            dashBar.gameObject.SetActive(true);
        }
    }

    void UpdateHealthText()
    {
        healthBar.text = "Health: " + Health.ToString();
    }
    public void UpdateDashText()
    {
        dashBar.text = "Dash Meter: " + currentDashMeter.ToString();
        if (currentDashMeter >= dashMeter)
        {
            dashBar.color = Color.green;
        }
        else
        {
            dashBar.color = Color.black;
        }
    }


}
