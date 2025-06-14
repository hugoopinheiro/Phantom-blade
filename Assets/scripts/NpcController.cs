using UnityEngine;

public class NpcController : MonoBehaviour
{
    // NOVO: Vida do NPC (como um jogador)
    public int maxLife = 3;
    public int currentLife;

    // === Configurações de Movimento ===
    [Header("Movimento")]
    [SerializeField] private float baseMoveSpeed = 4f; // Velocidade base do NPC
    [SerializeField] private float runSpeedMultiplier = 1.5f; // Multiplicador para "correr"

    // NOVO: Velocidade atual para efeitos de lentidão
    private float currentMoveSpeed;

    // NOVO: Para o efeito de lentidão
    private float slowEffectTimer = 0f;
    [SerializeField] private float slowFactor = 0.5f; // 50% da velocidade normal


    [SerializeField] private Transform centerlineMarker;
    [SerializeField] private float mapRightBoundX = 19f;
    [SerializeField] private float mapLeftBoundX = 0f;
    [SerializeField] private float mapUpperBoundY = 10f;
    [SerializeField] private float mapLowerBoundY = 0f;

    // === Configurações de IA ===
    [Header("IA")]
    [SerializeField] private float reactionTime = 0.2f;
    [SerializeField] private float anticipationFactor = 0.5f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackDecisionBuffer = 0.1f;
    [SerializeField] private float defensePositionOffset = 1.0f;

    // === Referências ===
    [Header("Referências")]
    [SerializeField] private Transform hitPoint;
    private Rigidbody2D rb;
    private Animator animator;
    private BallController ball;

    public float hitRange = 1.5f;
    public float hitForce = 10f;

    // === Variáveis Internas da IA ===
    private float mapCenterlineX;
    private Vector2 targetPosition;
    // Remova 'private float currentSpeed;' se já tem 'currentMoveSpeed'
    private bool isAttacking = false;
    private float nextActionTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentLife = maxLife; // Inicializa a vida do NPC
        currentMoveSpeed = baseMoveSpeed; // Inicializa a velocidade atual com a base

        ball = FindFirstObjectByType<BallController>();
        if (ball == null)
        {
            Debug.LogError("BallController não encontrado na cena para o NPC!");
        }

        if (centerlineMarker != null)
        {
            mapCenterlineX = centerlineMarker.position.x;
        }
        else
        {
            Debug.LogError("Centerline Marker não atribuído ao NPC! Usando valor padrão.");
            mapCenterlineX = (mapLeftBoundX + mapRightBoundX) / 2f;
        }

        if (transform.position.x < mapCenterlineX)
        {
            transform.position = new Vector2(mapCenterlineX + defensePositionOffset, transform.position.y);
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

        if (isAttacking)
        {
            SetAnimatorMovement(2);
            return;
        }

        if (Time.time >= nextActionTime)
        {
            DecideAction();
            nextActionTime = Time.time + reactionTime;
        }

        // Ajustado para usar currentMoveSpeed (se necessário para animação baseada em velocidade)
        Vector2 currentVelocity = rb.linearVelocity;
        if (currentVelocity.sqrMagnitude > 0.1f)
        {
            SetAnimatorMovement(1);
        }
        else
        {
            SetAnimatorMovement(0);
        }
    }

    void FixedUpdate()
    {
        Vector2 currentPosition = rb.position;
        Vector2 moveDirection = (targetPosition - currentPosition).normalized;
        // Usa currentMoveSpeed aqui
        Vector2 newPosition = currentPosition + moveDirection * currentMoveSpeed * Time.fixedDeltaTime;

        float minX = mapCenterlineX;
        float maxX = mapRightBoundX;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, mapLowerBoundY, mapUpperBoundY);

        rb.MovePosition(newPosition);
    }

    void DecideAction()
    {
        if (ball == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, attackRange);
        bool ballInRange = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                ballInRange = true;
                break;
            }
        }

        if (ballInRange)
        {
            StartAttack();
        }
        else
        {
            CalculateMovementTarget();
        }
    }

    void CalculateMovementTarget()
    {
        Vector2 ballPosition = ball.transform.position;
        Vector2 ballVelocity = ball.GetComponent<Rigidbody2D>().linearVelocity;

        Vector2 predictedBallPosition = ballPosition + ballVelocity * anticipationFactor;

        targetPosition = new Vector2(
            Mathf.Clamp(predictedBallPosition.x, mapCenterlineX + defensePositionOffset, mapRightBoundX),
            Mathf.Clamp(predictedBallPosition.y, mapLowerBoundY, mapUpperBoundY)
        );

        // Se a bola estiver vindo para a área do NPC, ele pode "correr" para interceptar
        if (ballVelocity.x < 0 && ballPosition.x > mapCenterlineX)
        {
            // Aplica o multiplicador de corrida à velocidade base, mas respeita o slow effect
            if (slowEffectTimer <= 0)
            {
                currentMoveSpeed = baseMoveSpeed * runSpeedMultiplier;
            }
            else
            {
                // Se estiver lento, a corrida ainda pode ser um pouco mais rápida que a lentidão
                currentMoveSpeed = (baseMoveSpeed * slowFactor) * 1.2f; // Exemplo
            }
        }
        else
        {
            // Retorna à velocidade base, mas respeita o slow effect
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

    void StartAttack()
    {
        isAttacking = true;
        currentMoveSpeed = 0; // Para o NPC durante o ataque
        SetAnimatorMovement(2);

        Invoke(nameof(PerformHit), attackDecisionBuffer);
    }

    void PerformHit()
    {
        if (ball == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                ball.HitBall(direction, hitForce);
                break;
            }
        }
        EndAttack();
    }

    void EndAttack()
    {
        isAttacking = false;
        // Retorna a velocidade, respeitando o slow effect
        if (slowEffectTimer <= 0)
        {
            currentMoveSpeed = baseMoveSpeed;
        }
        else
        {
            currentMoveSpeed = baseMoveSpeed * slowFactor;
        }
        SetAnimatorMovement(0);
    }

    void SetAnimatorMovement(int value)
    {
        if (animator != null)
        {
            animator.SetInteger("Movimento", value);
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
            // Adicione aqui a lógica de fim de jogo ou desativação do NPC
            gameObject.SetActive(false); // Exemplo: desativa o GameObject do NPC
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
            Gizmos.DrawWireSphere(hitPoint.position, attackRange);
        }

        if (centerlineMarker != null)
        {
            Gizmos.color = Color.blue;
            float currentMinX = mapCenterlineX;
            float currentMaxX = mapRightBoundX;

            Vector3 minBounds = new Vector3(currentMinX, mapLowerBoundY, 0);
            Vector3 maxBounds = new Vector3(currentMaxX, mapUpperBoundY, 0);

            Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(mapCenterlineX, mapLowerBoundY, 0), new Vector3(mapCenterlineX, mapUpperBoundY, 0));
        }
    }
}