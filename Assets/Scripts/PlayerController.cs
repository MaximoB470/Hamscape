using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private bool isGrounded;
    private float verticalVelocity = 0f;

    private void Update()
    {
        //horizontal
        float h = Input.GetAxis("Horizontal");
        Vector2 movement = new Vector2(h, 0f) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
        // Comprobar si está en el suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        // Saltar
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = jumpForce;
        }
        // Simular gravedad
        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = 0f;
        }
        //vertical
        transform.Translate(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }
}