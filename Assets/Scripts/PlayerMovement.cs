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

    [Header("Dynamic")] // For ease of testing and debugging
    [SerializeField] private float jumpDelay;
    [SerializeField] private float groundTime;
    [SerializeField] private bool isGrounded;
    public int magSwitch 
    {
        get 
        {
            int a = 0;
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableGround))
            {
                a+=1;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.right, .1f, jumpableGround))
            {
                a+=8;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.left, .1f, jumpableGround))
            {
                a+=2;
            }
            if (Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.up, .1f, jumpableGround))
            {
                a+=4;
            }
            return a;
            
        }
    } // Zero is default (down), 1 is left, 2 is up, 3 is right

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

        if (magSwitch != 0) body.gravityScale = 0;
        else body.gravityScale = gravMultiplier;

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
        if (magSwitch%4>=2||magSwitch%16>=8)
        {
            body.AddForce(movementY * Vector2.up, ForceMode2D.Force);
        }
        if (magSwitch%8>=4)
        {
            body.AddForce(movementY * downJumpReduction * Vector2.up, ForceMode2D.Force);
        }

        if (magSwitch%2 >= 1 && groundTime>0 && jumpDelay>0 && body.velocity.y <= 0.1f)
        {
            body.velocity = new Vector2(body.velocity.x, body.velocity.y+jumpSpeedMultiplier);
            jumpDelay = 0.001f;
        }
        if (magSwitch%4 >= 2 && groundTime>0 && jumpDelay>0 && body.velocity.x <= 0.1f)
        {
            body.velocity = new Vector2(body.velocity.x+jumpSpeedMultiplier, body.velocity.y);
            jumpDelay = 0.001f;
        }
        if (magSwitch%8 >= 4 && groundTime>0 && jumpDelay>0 && body.velocity.y >= -0.1f)
        {
            body.velocity = new Vector2(body.velocity.x, body.velocity.y-jumpSpeedMultiplier*downJumpReduction);
            jumpDelay = 0.001f;
        }
        if (magSwitch%16 >= 8 && groundTime>0 && jumpDelay>0 && body.velocity.x >= -0.1f)
        {
            body.velocity = new Vector2(body.velocity.x-jumpSpeedMultiplier, body.velocity.y);
            jumpDelay = 0.001f;
        }
        if (Input.GetButton("Jump") && magSwitch == 0)
        {
            body.gravityScale = lowGravMultiplier;
        }
    }

}