using UnityEngine;
public class EnemyMovement : IUpdatable
{
    private Transform _transform;
    private Rigidbody2D _rb;
    private float _moveSpeed;
    private float _horizontalDistance;

    private Vector2 _startPos;
    private Vector2 _leftLimit;
    private Vector2 _rightLimit;
    private bool _movingRight = true;

    public EnemyMovement(
        Transform transform,
        Rigidbody2D rb,
        float moveSpeed,
        float horizontalDistance)
    {
        _transform = transform;
        _rb = rb;
        _moveSpeed = moveSpeed;
        _horizontalDistance = horizontalDistance;

        _startPos = transform.position;
        _leftLimit = _startPos - new Vector2(_horizontalDistance, 0);
        _rightLimit = _startPos + new Vector2(_horizontalDistance, 0);
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        _moveSpeed = moveSpeed;
    }

    public void SetHorizontalDistance(float distance)
    {
        _horizontalDistance = distance;
        _leftLimit = _startPos - new Vector2(_horizontalDistance, 0);
        _rightLimit = _startPos + new Vector2(_horizontalDistance, 0);
    }

    public void Tick(float deltaTime)
    {
        PatrolMovement(deltaTime);
    }

    private void PatrolMovement(float deltaTime)
    {
        if (_movingRight)
        {
            _rb.velocity = new Vector2(_moveSpeed, _rb.velocity.y);

            if (_transform.position.x >= _rightLimit.x)
            {
                _movingRight = false;
                FlipDirection();
            }
        }
        else
        {
            _rb.velocity = new Vector2(-_moveSpeed, _rb.velocity.y);

            if (_transform.position.x <= _leftLimit.x)
            {
                _movingRight = true;
                FlipDirection();
            }
        }
    }

    private void FlipDirection()
    {
        Vector3 currentScale = _transform.localScale;
        currentScale.x *= -1;
        _transform.localScale = currentScale;
    }
    public void ResetStartPosition()
    {
        _startPos = _transform.position;
        _leftLimit = _startPos - new Vector2(_horizontalDistance, 0);
        _rightLimit = _startPos + new Vector2(_horizontalDistance, 0);
    }
}
