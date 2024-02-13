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

    [Header("Dynamic")] // For ease of testing and debugging
    [SerializeField] private float jumpDelay;
    [SerializeField] private float groundTime;
    [SerializeField] private int magSwitch = 0; // Zero is default (down), 1 is left, 2 is up, 3 is right

    // We need to simulate our own gravity to change its direction
    public Vector2 gravityDirection 
    {
        get 
        {
            if (magSwitch == 0) return new Vector2(0, -1);
            else if (magSwitch == 1) return new Vector2(-1, 0);
            else if (magSwitch == 2) return new Vector2(0, 1);
            else return new Vector2(1, 0);
        }
    }
    [SerializeField] private float gravityScale;

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
        body.gravityScale = 0;
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

        if (Input.GetKeyDown(KeyCode.Alpha1)) // This way of handling mag switch input is most likely temporary
        {
            magSwitch = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            magSwitch = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            magSwitch = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            magSwitch = 3;
        }


    }

    private void FixedUpdate()
    {
        Run();
        body.AddForce(gravityDirection * Physics2D.gravity.magnitude * gravityScale, ForceMode2D.Force);
    }

    private bool IsGrounded()
    {
        if (magSwitch == 0) return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableGround);
        else if (magSwitch == 1) return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.left, .1f, jumpableGround);
        else if (magSwitch == 2) return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.up, .1f, jumpableGround);
        else return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.right, .1f, jumpableGround);
    } // Returns a physics2d boxcast searching for jumpable ground in the direction of gravity

    private void Run()
    {
        float targetSpeed;
        if (magSwitch == 0 || magSwitch == 2)
        {
            targetSpeed = _moveInput.x * maxSpeed;
        }
        else
        {
            targetSpeed = _moveInput.y * maxSpeed;
        }


        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (IsGrounded())
        {
            accelRate = speedMultiplier;
            groundTime = coyoteTime;
        }
        else
            accelRate = airMultiplier;
        #endregion
        
        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (conserveMomentum && Mathf.Abs(body.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(body.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && !IsGrounded())
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        //Calculate difference between current velocity and desired velocity
        float speedDif;
        if (magSwitch == 0 || magSwitch == 2)
        {
            speedDif = targetSpeed - body.velocity.x;
        }
        else
        {
            speedDif = targetSpeed - body.velocity.y;
        }
        //Calculate force along x-axis to apply to thr player

        float movement = speedDif*accelRate;

        //Convert this to a vector and apply to rigidbody
        
        if (magSwitch == 0 || magSwitch == 2)
        {
            body.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }
        else
        {
            body.AddForce(movement * Vector2.up, ForceMode2D.Force);
        }

        if (magSwitch == 0 && groundTime>0 && jumpDelay>0 && body.velocity.y <= 0)
        {
            body.velocity = new Vector2(body.velocity.x, jumpSpeedMultiplier);
            jumpDelay = 0;
        }
        else if (magSwitch == 1 && groundTime>0 && jumpDelay>0 && body.velocity.x <= 0)
        {
            body.velocity = new Vector2(jumpSpeedMultiplier, body.velocity.y);
            jumpDelay = 0;
        }
        else if (magSwitch == 2 && groundTime>0 && jumpDelay>0 && body.velocity.y >= 0)
        {
            body.velocity = new Vector2(body.velocity.x, -jumpSpeedMultiplier);
            jumpDelay = 0;
        }
        else if (magSwitch == 3 && groundTime>0 && jumpDelay>0 && body.velocity.x >= 0)
        {
            body.velocity = new Vector2(-jumpSpeedMultiplier, body.velocity.y);
            jumpDelay = 0;
        }

        if (Input.GetButton("Jump"))
        {
            gravityScale = lowGravMultiplier;
        } else
        {
            gravityScale = gravMultiplier;
        }
    }

}