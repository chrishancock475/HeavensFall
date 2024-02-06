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

    private float jumpDelay;
    private float groundTime;

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
        #endregion
        jumpDelay -= Time.deltaTime;
        groundTime -= Time.deltaTime;
        if (Input.GetButtonDown("Jump"))
        {
            jumpDelay = jumpBuffer;
        }

        /*
        Vector2 Speed = new Vector2(body.velocity.x, body.velocity.y);


        Speed.x += Time.deltaTime * speedMultiplier * Input.GetAxisRaw("Horizontal");

        Speed.x = Mathf.Clamp(Speed.x, -maxSpeed, maxSpeed);
        Speed.y = Mathf.Clamp(Speed.y, -maxJumpSpeed, maxJumpSpeed);

        body.velocity = Speed;
        */
    }

    private void FixedUpdate()
    {
        Run();
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableGround);
    }

    private void Run()
    {
        float targetSpeed = _moveInput.x * maxSpeed;

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
        float speedDif = targetSpeed - body.velocity.x;
        //Calculate force along x-axis to apply to thr player

        float movement = speedDif*accelRate;

        //Convert this to a vector and apply to rigidbody
        body.AddForce(movement * Vector2.right, ForceMode2D.Force);
        
        if (groundTime>0 && jumpDelay>0 && body.velocity.y <= 0)
        {
            body.velocity = new Vector2(body.velocity.x, jumpSpeedMultiplier);
            jumpDelay = 0;
        }
        if (Input.GetButton("Jump"))
        {
            body.gravityScale = lowGravMultiplier;
        } else
        {
            body.gravityScale = gravMultiplier;
        }
    }

}