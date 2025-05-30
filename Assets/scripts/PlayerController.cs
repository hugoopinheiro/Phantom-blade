using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float maxMoveDistance = 5f; // Distância máxima do ponto inicial
    private Vector2 initialPosition;
    [SerializeField] private bool isLeftPlayer = true;

    private Rigidbody2D _playerRigidBody2d;
    public float _playerSpeed;
    private float _playerInitialSpeed;
    public float _playerRunSpeed;
    private bool _isAttack = false;
    private Vector2 _playerDirection;
    private Animator _playerAnimator;

    public float hitRange = 1.5f;
    public float hitForce = 10f;
    public Transform hitPoint;

    void Start()
    {
        initialPosition = transform.position;

        _playerRigidBody2d = GetComponent<Rigidbody2D>();
        _playerAnimator = GetComponent<Animator>();
        _playerInitialSpeed = _playerSpeed;
    }

    void Update()
    {
        _playerDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (_playerAnimator != null)
        {
            if (_playerDirection.sqrMagnitude > 0)
                _playerAnimator.SetInteger("Movimento", _isAttack ? 2 : 1);
            else
                _playerAnimator.SetInteger("Movimento", _isAttack ? 2 : 0);
        }

        speedRun();
        OnAttack();
    }

    private void FixedUpdate()
    {
        // Calcula nova posição
        Vector2 newPosition = _playerRigidBody2d.position + _playerDirection * _playerSpeed * Time.fixedDeltaTime;

        // Limita a movimentação ao redor da posição inicial
        float minX = isLeftPlayer ? 0f : 9.5f;
        float maxX = isLeftPlayer ? 9.5f : 19f;
        float minY = 0f;
        float maxY = 10f;


        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

        _playerRigidBody2d.MovePosition(newPosition);
    }

    void speedRun()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            _playerSpeed = _playerRunSpeed;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            _playerSpeed = _playerInitialSpeed;
    }

    void OnAttack()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            _isAttack = true;
            _playerSpeed = 0;

            TryHitBall();
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            _isAttack = false;
            _playerSpeed = _playerInitialSpeed;
        }
    }

    void TryHitBall()
    {
        if (hitPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, hitRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                Rigidbody2D ballRb = hit.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    Vector2 direction = (hit.transform.position - transform.position).normalized;
                    ballRb.linearVelocity = direction * hitForce;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint.position, hitRange);
        }
    }
}
