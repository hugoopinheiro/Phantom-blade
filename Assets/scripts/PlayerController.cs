using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private bool isLeftPlayer = true;

    // NOVO: Vida do jogador
    public int maxLife = 3;
    public int currentLife;

    // NOVO: Velocidade base e velocidade atual
    public float baseMoveSpeed = 5f; // Antigo _playerSpeed, agora é a velocidade base
    private float currentMoveSpeed;   // NOVO: Esta será a velocidade usada no FixedUpdate
    public float playerRunSpeedMultiplier = 1.5f; // Multiplicador para a corrida, se houver

    // NOVO: Para o efeito de lentidão
    private float slowEffectTimer = 0f;
    [SerializeField] private float slowFactor = 0.5f; // 50% da velocidade normal

    [SerializeField] private Transform centerlineMarker;

    // Os limites do mapa (bordas externas)
    [SerializeField] private float mapRightBoundX = 19f;
    [SerializeField] private float mapLeftBoundX = 0f;
    [SerializeField] private float mapUpperBoundY = 10f;
    [SerializeField] private float mapLowerBoundY = 0f;

    private float mapCenterlineX;

    private BallController currentBall;

    [SerializeField] private float maxMoveDistance = 5f; // Distância máxima do ponto inicial
    private Vector2 initialPosition;

    private Rigidbody2D _playerRigidBody2d;
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

        currentLife = maxLife; // Inicializa a vida
        currentMoveSpeed = baseMoveSpeed; // Inicializa a velocidade atual com a base

        currentBall = FindFirstObjectByType<BallController>();
        if (currentBall == null)
        {
            Debug.LogError("BallController não encontrado na cena! Certifique-se de que a bola tem o script BallController.");
        }

        if (centerlineMarker != null)
        {
            mapCenterlineX = centerlineMarker.position.x;
        }
        else
        {
            Debug.LogError("Centerline Marker não foi atribuído no Inspector do PlayerController! Usando valor padrão.");
            mapCenterlineX = (mapLeftBoundX + mapRightBoundX) / 2f;
        }

        if (isLeftPlayer && transform.position.x > mapCenterlineX)
        {
            transform.position = new Vector2(mapCenterlineX - 0.1f, transform.position.y);
        }
        else if (!isLeftPlayer && transform.position.x < mapCenterlineX)
        {
            transform.position = new Vector2(mapCenterlineX + 0.1f, transform.position.y);
        }
    }

    void Update()
    {
        // NOVO: Lógica do timer do efeito de lentidão
        if (slowEffectTimer > 0)
        {
            slowEffectTimer -= Time.deltaTime;
            if (slowEffectTimer <= 0)
            {
                currentMoveSpeed = baseMoveSpeed; // Reseta a velocidade para a base
                Debug.Log($"{name} voltou à velocidade normal.");
            }
        }

        _playerDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (_playerAnimator != null)
        {
            if (_playerDirection.sqrMagnitude > 0)
                _playerAnimator.SetInteger("Movimento", _isAttack ? 2 : 1);
            else
                _playerAnimator.SetInteger("Movimento", _isAttack ? 2 : 0);
        }

        // speedRun(); // Esta função precisa ser atualizada para usar 'currentMoveSpeed'
        // Refatorada para usar a nova variável de velocidade:
        HandleSpeedInput();
        OnAttack();
    }

    private void FixedUpdate()
    {
        // Usa currentMoveSpeed aqui
        Vector2 newPosition = _playerRigidBody2d.position + _playerDirection * currentMoveSpeed * Time.fixedDeltaTime;

        float currentMinX;
        float currentMaxX;

        if (isLeftPlayer)
        {
            currentMinX = mapLeftBoundX;
            currentMaxX = mapCenterlineX;
        }
        else
        {
            currentMinX = mapCenterlineX;
            currentMaxX = mapRightBoundX;
        }

        newPosition.x = Mathf.Clamp(newPosition.x, currentMinX, currentMaxX);
        newPosition.y = Mathf.Clamp(newPosition.y, mapLowerBoundY, mapUpperBoundY);

        _playerRigidBody2d.MovePosition(newPosition);
    }

    // Antiga speedRun renomeada e atualizada para usar currentMoveSpeed e baseMoveSpeed
    void HandleSpeedInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            // Aplica o multiplicador de corrida à velocidade base, mas só se não estiver lento
            if (slowEffectTimer <= 0)
            {
                currentMoveSpeed = baseMoveSpeed * playerRunSpeedMultiplier;
            }
            else
            {
                // Se estiver lento, a corrida ainda pode ser um pouco mais rápida que a lentidão,
                // mas não atinge a velocidade normal de corrida. Ajuste conforme o desejado.
                currentMoveSpeed = (baseMoveSpeed * slowFactor) * 1.2f; // Exemplo: 20% mais rápido que a velocidade lenta
            }
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            // Retorna à velocidade base, mas ainda respeita o efeito de lentidão
            if (slowEffectTimer <= 0)
            {
                currentMoveSpeed = baseMoveSpeed;
            }
            else
            {
                currentMoveSpeed = baseMoveSpeed * slowFactor;
            }
        }
    }

    void OnAttack()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            _isAttack = true;
            currentMoveSpeed = 0; // Para o jogador durante o ataque

            if (currentBall != null)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, hitRange, LayerMask.GetMask("Ball"));
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Ball"))
                    {
                        Vector2 direction = (hit.transform.position - transform.position).normalized;
                        currentBall.HitBall(direction, hitForce);
                        break;
                    }
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            _isAttack = false;
            // Retorna a velocidade, respeitando o slow effect
            if (slowEffectTimer <= 0)
            {
                currentMoveSpeed = baseMoveSpeed;
            }
            else
            {
                currentMoveSpeed = baseMoveSpeed * slowFactor;
            }
        }
    }

    // NOVO MÉTODO: Para o GameManager aplicar dano
    public void TakeDamage(int damageAmount)
    {
        currentLife -= damageAmount;
        Debug.Log($"{name} levou {damageAmount} de dano! Vida restante: {currentLife}");

        if (currentLife <= 0)
        {
            Debug.Log($"{name} foi derrotado!");
            // Adicione aqui a lógica de fim de jogo ou desativação do jogador
            gameObject.SetActive(false); // Exemplo: desativa o GameObject do jogador
        }
    }

    // NOVO MÉTODO: Para o GameManager aplicar o efeito de lentidão
    public void ApplySlowEffect(float duration)
    {
        slowEffectTimer = duration; // Define a duração do efeito
        currentMoveSpeed = baseMoveSpeed * slowFactor; // Aplica o fator de lentidão
        Debug.Log($"{name} foi lentificado! Nova velocidade: {currentMoveSpeed}");
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint.position, hitRange);
        }

        if (centerlineMarker != null)
        {
            Gizmos.color = Color.blue;
            float currentMinX;
            float currentMaxX;
            float currentCenterlineX = centerlineMarker.position.x;

            if (isLeftPlayer)
            {
                currentMinX = mapLeftBoundX;
                currentMaxX = currentCenterlineX;
            }
            else
            {
                currentMinX = currentCenterlineX;
                currentMaxX = mapRightBoundX;
            }

            Vector3 minBounds = new Vector3(currentMinX, mapLowerBoundY, 0);
            Vector3 maxBounds = new Vector3(currentMaxX, mapUpperBoundY, 0);

            Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(currentCenterlineX, mapLowerBoundY, 0), new Vector3(currentCenterlineX, mapUpperBoundY, 0));
        }
    }
}