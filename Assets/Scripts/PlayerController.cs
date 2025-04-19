using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Rigidbody2D RigidBody_;
    [SerializeField] private float MovSpeed_ = 0;
    [SerializeField] private float AttackDamage_ = 0;
    [SerializeField] private float JumpForce_ = 0;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    private bool isGrounded;

    private void Start()
    {
        RigidBody_ = GetComponent<Rigidbody2D>();
    }

    public void Update()
    {
        float h = Input.GetAxis("Horizontal");
        //float v = Input.GetAxis("Vertical");

        Vector2 movement = new Vector3(h,0) * MovSpeed_ * Time.deltaTime;
        transform.Translate(movement);

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            RigidBody_.velocity = new Vector2(RigidBody_.velocity.x, JumpForce_);
        }
    }

}
