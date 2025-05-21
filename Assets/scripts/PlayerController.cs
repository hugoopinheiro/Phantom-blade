using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _playerRigidBody2d;
    public float _playerSpeed;
    private Vector2 _playerDirection;
    void Start()
    {
        _playerRigidBody2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        _playerDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    }
    private void FixedUpdate()
    {
        _playerRigidBody2d.MovePosition(_playerRigidBody2d.position + _playerDirection * _playerSpeed * Time.fixedDeltaTime);
    }
}
