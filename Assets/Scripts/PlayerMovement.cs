using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [Header("Inscribed")]
    [SerializeField] private float maxSpeed;
    //[SerializeField] private float maxJumpSpeed;
    [SerializeField] private float speedMultiplier;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float jumpSpeedMultiplier;
    [SerializeField] private float gravMultiplier;
    [SerializeField] private float lowGravMultiplier;
    [SerializeField] private float jumpBuffer;
    [SerializeField] private float coyoteTime;
    [SerializeField] private bool conserveMomentum;
    [SerializeField] private float downJumpReduction;
    [SerializeField] private float wallJumpSpeed;
    [SerializeField] private float rotationSpeed;

    [SerializeField] private GameObject sprite;
    public Animator animator;

    [Header("Dynamic")] // For ease of testing and debugging
    [SerializeField] private float jumpDelay;
    [SerializeField] private float groundTime;
    [SerializeField] private bool isGrounded;
    public int surface;
    private float activeRotation;
    private int currentDirection;

    
    public int MagSwitch
    {
        get
        {
            int a = 0;
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .05f, jumpableGround))
            {
                a += 1;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.right, .05f, jumpableGround))
            {
                a += 8;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.left, .05f, jumpableGround))
            {
                a += 2;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.up, .05f, jumpableGround))
            {
                a += 4;
            }
            if (a != 0)
            {
                surface = a;
            }
            return a;

        }
    } // Zero is default (down), 1 is left, 2 is up, 3 is right
    public float ToRotation //Get target rotation
    {
        get
        {
            int a = MagSwitch;
            int y = 0;
            int x = 0;
            if (a % 2 >= 1 || a == 0)
                x++;
            if (a % 4 >= 2)
                y--;
            if (a % 8 >= 4)
                x--;
            if (a % 16 >= 8)
                y++;
            return (Mathf.Rad2Deg * Mathf.Atan2(y, x) + 360) % 360;
        }
    }
    private Rigidbody2D body;
    private BoxCollider2D coll;

    [SerializeField] private LayerMask jumpableGround;

    #region INPUT PARAMETERS
    private Vector2 _moveInput;
    #endregion

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        activeRotation = 0;
        currentDirection = 1;
    }

    private void Update()
    {
        #region INPUT HANDLER
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical"); // For wall running
        #endregion
        jumpDelay -= Time.deltaTime;
        groundTime -= Time.deltaTime;
        if (Input.GetButtonDown("Jump"))
        {
            jumpDelay = jumpBuffer;
        }



    }

    private void FixedUpdate()
    {
        Run();
    }



    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            isGrounded = false;
        }
    }

    private void Run()
    {
        float targetSpeedX;
        float targetSpeedY;
        //if (magSwitch == 0 || magSwitch%2 == 1 || magSwitch%8 == 4)
        //{
        targetSpeedX = _moveInput.x * maxSpeed;
        //}

        targetSpeedY = _moveInput.y * maxSpeed;

        //Disable Gravity
        if (MagSwitch != 0 || isGrounded) body.gravityScale = 0;
        else body.gravityScale = gravMultiplier;

        #region Calculate AccelRate
        float accelRateX;
        float accelRateY;

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (isGrounded)
        {
            accelRateX = speedMultiplier;
            accelRateY = speedMultiplier;
            groundTime = coyoteTime;
        }
        else
        {
            accelRateX = airMultiplier;
            accelRateY = airMultiplier;
        }
        #endregion

        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (conserveMomentum && Mathf.Abs(body.velocity.x) > Mathf.Abs(targetSpeedX) && Mathf.Sign(body.velocity.x) == Mathf.Sign(targetSpeedX) && Mathf.Abs(targetSpeedX) > 0.01f && !isGrounded)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRateX = 0;
        }
        if (conserveMomentum && Mathf.Abs(body.velocity.y) > Mathf.Abs(targetSpeedY) && Mathf.Sign(body.velocity.y) == Mathf.Sign(targetSpeedY) && Mathf.Abs(targetSpeedY) > 0.01f && !isGrounded)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRateY = 0;
        }
        #endregion

        //Calculate difference between current velocity and desired velocity
        float speedDifX;
        float speedDifY;
        speedDifX = targetSpeedX - body.velocity.x;
        speedDifY = targetSpeedY - body.velocity.y;
        //Calculate force along x-axis to apply to thr player

        float movementX = speedDifX * accelRateX;
        float movementY = speedDifY * accelRateY;

        //Convert this to a vector and apply to rigidbody

        //if (magSwitch%2==0 || magSwitch == 2)
        //{
        body.AddForce(movementX * Vector2.right, ForceMode2D.Force);
        //}
        if (MagSwitch % 4 >= 2 || MagSwitch % 16 >= 8)
        {
            body.AddForce(movementY * Vector2.up, ForceMode2D.Force);
        }
        if (MagSwitch % 8 >= 4)
        {
            body.AddForce(movementY * downJumpReduction * Vector2.up, ForceMode2D.Force);
        }

        if (surface % 4 >= 2 && groundTime > 0 && jumpDelay > 0)
        {
            body.velocity = new Vector2(jumpSpeedMultiplier, Mathf.Max(body.velocity.y, wallJumpSpeed));
            jumpDelay = 0.001f;
        }
        if (surface % 16 >= 8 && groundTime > 0 && jumpDelay > 0)
        {
            body.velocity = new Vector2(-jumpSpeedMultiplier, Mathf.Max(body.velocity.y, wallJumpSpeed));
            jumpDelay = 0.001f;
        }
        if (surface % 2 >= 1 && groundTime > 0 && jumpDelay > 0 && body.velocity.y <= 0.1f)
        {
            body.velocity = new Vector2(body.velocity.x, body.velocity.y + jumpSpeedMultiplier);
            jumpDelay = 0.001f;
        }
        if (surface % 8 >= 4 && groundTime > 0 && jumpDelay > 0)
        {
            body.velocity = new Vector2(body.velocity.x, body.velocity.y - jumpSpeedMultiplier * downJumpReduction);
            jumpDelay = 0.001f;
        }

        if (Input.GetButton("Jump") && MagSwitch == 0)
        {
            body.gravityScale = lowGravMultiplier;
        }

        //Change rotation to match wall
        float newRotation = ToRotation;
        if (activeRotation - newRotation > 180)
            newRotation += 360;
        else if (newRotation - activeRotation > 180)
            activeRotation += 360;

        activeRotation = (activeRotation - newRotation) * rotationSpeed + newRotation;

        activeRotation = (activeRotation + 720) % 360;
        sprite.transform.rotation = Quaternion.Euler(0, 0, activeRotation);

        //Change direction to match input
        if (MagSwitch % 2 >= 1 || MagSwitch == 0)
        {
            if (currentDirection == 1 && _moveInput.x < 0)
                currentDirection = -1;
            else if (currentDirection == -1 && _moveInput.x > 0)
                currentDirection = 1;
        }
        if (MagSwitch % 4 >= 2)
        {
            if (currentDirection == 1 && _moveInput.y > 0)
                currentDirection = -1;
            else if (currentDirection == -1 && _moveInput.y < 0)
                currentDirection = 1;
        }
        if (MagSwitch % 8 >= 4)
        {
            if (currentDirection == 1 && _moveInput.x > 0)
                currentDirection = -1;
            else if (currentDirection == -1 && _moveInput.x < 0)
                currentDirection = 1;
        }
        if (MagSwitch % 16 >= 8)
        {
            if (currentDirection == 1 && _moveInput.y < 0)
                currentDirection = -1;
            else if (currentDirection == -1 && _moveInput.y > 0)
                currentDirection = 1;
        }
        //sprite.transform.rotation = Quaternion.Euler(0, currentDirection, activeRotation);
        sprite.transform.localScale = new Vector3(currentDirection, 1, 1);
        animator.SetFloat("Speed", body.velocity.magnitude);
        animator.SetBool("IsGrounded", MagSwitch != 0);
    }

    public void Die()
    {
        body.velocity = Vector3.zero;
        transform.position = GameManager.checkpoint.position;
    }

}