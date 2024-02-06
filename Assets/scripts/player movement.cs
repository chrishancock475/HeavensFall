using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [Header("Inscribed")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxJumpSpeed;
    [SerializeField] private float speedMultiplier;
    [SerializeField] private float jumpSpeedMultiplier;

    private Rigidbody2D body;

    [Header("Dynamic")]
    [SerializeField] private bool OnGround;



    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Vector2 Speed = new Vector2(body.velocity.x, body.velocity.y);


        Speed.x += Time.deltaTime * speedMultiplier * Input.GetAxisRaw("Horizontal");


        if (OnGround)
        {
            Speed.y = jumpSpeedMultiplier * Input.GetAxisRaw("Vertical");
        }

        Speed.x = Mathf.Clamp(Speed.x, -maxSpeed, maxSpeed);
        Speed.y = Mathf.Clamp(Speed.y, -maxJumpSpeed, maxJumpSpeed);

        body.velocity = Speed;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7) OnGround = true; // Layer 7 is ground
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7) OnGround = false;
    }


}