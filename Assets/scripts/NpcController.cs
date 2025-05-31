using UnityEngine;

public class NpcController : MonoBehaviour
{
    // === Configurações de Movimento ===
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 4f; // Velocidade base do NPC
    [SerializeField] private float runSpeedMultiplier = 1.5f; // Multiplicador para "correr"
    [SerializeField] private Transform centerlineMarker; // Referência para a linha central do mapa
    [SerializeField] private float mapRightBoundX = 19f; // Limite direito do mapa
    [SerializeField] private float mapLeftBoundX = 0f; // Limite esquerdo do mapa (não usado diretamente pelo NPC direito)
    [SerializeField] private float mapUpperBoundY = 10f;
    [SerializeField] private float mapLowerBoundY = 0f;
      
    // === Configurações de IA ===
    [Header("IA")]
    [SerializeField] private float reactionTime = 0.2f; // Tempo de reação da IA (quanto menor, mais rápido)
    [SerializeField] private float anticipationFactor = 0.5f; // Quanto a IA "prevê" a posição da bola
    [SerializeField] private float attackRange = 1.6f; // Alcance para a IA considerar um ataque (um pouco maior que o hitRange da bola)
    [SerializeField] private float attackDecisionBuffer = 0.1f; // Buffer para decidir atacar
    [SerializeField] private float defensePositionOffset = 1.0f; // Quão longe da linha central a IA se posiciona para defesa

    // === Referências ===
    [Header("Referências")]
    [SerializeField] private Transform hitPoint; // Ponto de onde a IA ataca (igual ao PlayerController)
    private Rigidbody2D rb;
    private Animator animator;
    private BallController ball;

    public float hitRange = 1.5f;
    public float hitForce = 10f;

    // === Variáveis Internas da IA ===
    private float mapCenterlineX;
    private Vector2 targetPosition;
    private float currentSpeed;
    private bool isAttacking = false;
    private float nextActionTime; // Para controlar o tempo de reação

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentSpeed = moveSpeed;

        // Encontra a bola na cena
        ball = FindFirstObjectByType<BallController>();
        if (ball == null)
        {
            Debug.LogError("BallController não encontrado na cena para o NPC!");
        }

        // Define a coordenada X da linha central
        if (centerlineMarker != null)
        {
            mapCenterlineX = centerlineMarker.position.x;
        }
        else
        {
            Debug.LogError("Centerline Marker não atribuído ao NPC! Usando valor padrão.");
            mapCenterlineX = (mapLeftBoundX + mapRightBoundX) / 2f;
        }

        // Garante que o NPC comece na sua área (na metade direita)
        if (transform.position.x < mapCenterlineX)
        {
            transform.position = new Vector2(mapCenterlineX + defensePositionOffset, transform.position.y);
        }
    }

    void Update()
    {
        // Se a IA está atacando, paralisa o movimento e pula o resto da lógica
        if (isAttacking)
        {
            SetAnimatorMovement(2); // Animação de ataque
            return;
        }

        // Lógica de IA para decidir movimento e ataque
        if (Time.time >= nextActionTime)
        {
            DecideAction();
            nextActionTime = Time.time + reactionTime;
        }

        // Atualiza a animação de movimento
        Vector2 currentVelocity = rb.linearVelocity;
        if (currentVelocity.sqrMagnitude > 0.1f) // Se estiver se movendo significativamente
        {
            SetAnimatorMovement(1); // Animação de movimento
        }
        else
        {
            SetAnimatorMovement(0); // Animação parado
        }
    }

    void FixedUpdate()
    {
        // Move o Rigidbody para a posição alvo
        Vector2 currentPosition = rb.position;
        Vector2 moveDirection = (targetPosition - currentPosition).normalized;
        Vector2 newPosition = currentPosition + moveDirection * currentSpeed * Time.fixedDeltaTime;

        // Limita a movimentação do NPC à sua metade direita do campo
        float minX = mapCenterlineX;
        float maxX = mapRightBoundX;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, mapLowerBoundY, mapUpperBoundY);

        rb.MovePosition(newPosition);
    }

    void DecideAction()
    {
        if (ball == null) return;

        // Detecta se a bola está ao alcance para um ataque
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

        // === Lógica de Decisão ===
        if (ballInRange)
        {
            // Se a bola está ao alcance, o NPC tenta atacar
            StartAttack();
        }
        else
        {
            // Se a bola não está ao alcance, o NPC se posiciona
            CalculateMovementTarget();
        }
    }

    void CalculateMovementTarget()
    {
        Vector2 ballPosition = ball.transform.position;
        Vector2 ballVelocity = ball.GetComponent<Rigidbody2D>().linearVelocity;

        // Previsão da posição da bola
        Vector2 predictedBallPosition = ballPosition + ballVelocity * anticipationFactor;

        // Decide a posição alvo do NPC
        // O NPC tenta se posicionar na mesma altura Y da bola, mas em sua metade do campo
        targetPosition = new Vector2(
            Mathf.Clamp(predictedBallPosition.x, mapCenterlineX + defensePositionOffset, mapRightBoundX), // Mover para a frente da linha central
            Mathf.Clamp(predictedBallPosition.y, mapLowerBoundY, mapUpperBoundY)
        );

        // Se a bola estiver vindo para a área do NPC, ele pode "correr" para interceptar
        if (ballVelocity.x < 0 && ballPosition.x > mapCenterlineX) // Bola vindo da direita para a esquerda e ainda na área do NPC
        {
            currentSpeed = moveSpeed * runSpeedMultiplier;
        }
        else
        {
            currentSpeed = moveSpeed; // Velocidade normal de movimento
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        currentSpeed = 0; // Para parar o movimento durante o ataque
        SetAnimatorMovement(2); // Animação de ataque

        // Lógica de "K" apertado para a IA
        // Atrasar um pouco o "soltar" do ataque para simular um clique
        Invoke(nameof(PerformHit), attackDecisionBuffer);
    }

    void PerformHit()
    {
        if (ball == null) return;

        // Reverifica se a bola ainda está ao alcance (pode ter se movido muito rápido)
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                ball.HitBall(direction, hitForce); // Chama o método HitBall da bola
                break;
            }
        }

        EndAttack();
    }

    void EndAttack()
    {
        isAttacking = false;
        currentSpeed = moveSpeed; // Retorna à velocidade normal
        SetAnimatorMovement(0); // Animação parado (ou volta para movimento se a DecideAction chamar)
    }

    void SetAnimatorMovement(int value)
    {
        if (animator != null)
        {
            animator.SetInteger("Movimento", value);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint.position, attackRange);
        }

        // Desenhar os limites para visualização no editor (similar ao PlayerController)
        if (centerlineMarker != null)
        {
            Gizmos.color = Color.blue;
            float currentMinX = mapCenterlineX;
            float currentMaxX = mapRightBoundX;

            Vector3 minBounds = new Vector3(currentMinX, mapLowerBoundY, 0);
            Vector3 maxBounds = new Vector3(currentMaxX, mapUpperBoundY, 0);

            Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);

            // Desenhar a linha central também no Gizmos para visualização
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(mapCenterlineX, mapLowerBoundY, 0), new Vector3(mapCenterlineX, mapUpperBoundY, 0));
        }
    }
}