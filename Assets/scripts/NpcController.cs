using UnityEngine;

public class NpcController : MonoBehaviour
{
    // === Configura��es de Movimento ===
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 4f; // Velocidade base do NPC
    [SerializeField] private float runSpeedMultiplier = 1.5f; // Multiplicador para "correr"
    [SerializeField] private Transform centerlineMarker; // Refer�ncia para a linha central do mapa
    [SerializeField] private float mapRightBoundX = 19f; // Limite direito do mapa
    [SerializeField] private float mapLeftBoundX = 0f; // Limite esquerdo do mapa (n�o usado diretamente pelo NPC direito)
    [SerializeField] private float mapUpperBoundY = 10f;
    [SerializeField] private float mapLowerBoundY = 0f;
      
    // === Configura��es de IA ===
    [Header("IA")]
    [SerializeField] private float reactionTime = 0.2f; // Tempo de rea��o da IA (quanto menor, mais r�pido)
    [SerializeField] private float anticipationFactor = 0.5f; // Quanto a IA "prev�" a posi��o da bola
    [SerializeField] private float attackRange = 1.6f; // Alcance para a IA considerar um ataque (um pouco maior que o hitRange da bola)
    [SerializeField] private float attackDecisionBuffer = 0.1f; // Buffer para decidir atacar
    [SerializeField] private float defensePositionOffset = 1.0f; // Qu�o longe da linha central a IA se posiciona para defesa

    // === Refer�ncias ===
    [Header("Refer�ncias")]
    [SerializeField] private Transform hitPoint; // Ponto de onde a IA ataca (igual ao PlayerController)
    private Rigidbody2D rb;
    private Animator animator;
    private BallController ball;

    public float hitRange = 1.5f;
    public float hitForce = 10f;

    // === Vari�veis Internas da IA ===
    private float mapCenterlineX;
    private Vector2 targetPosition;
    private float currentSpeed;
    private bool isAttacking = false;
    private float nextActionTime; // Para controlar o tempo de rea��o

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentSpeed = moveSpeed;

        // Encontra a bola na cena
        ball = FindFirstObjectByType<BallController>();
        if (ball == null)
        {
            Debug.LogError("BallController n�o encontrado na cena para o NPC!");
        }

        // Define a coordenada X da linha central
        if (centerlineMarker != null)
        {
            mapCenterlineX = centerlineMarker.position.x;
        }
        else
        {
            Debug.LogError("Centerline Marker n�o atribu�do ao NPC! Usando valor padr�o.");
            mapCenterlineX = (mapLeftBoundX + mapRightBoundX) / 2f;
        }

        // Garante que o NPC comece na sua �rea (na metade direita)
        if (transform.position.x < mapCenterlineX)
        {
            transform.position = new Vector2(mapCenterlineX + defensePositionOffset, transform.position.y);
        }
    }

    void Update()
    {
        // Se a IA est� atacando, paralisa o movimento e pula o resto da l�gica
        if (isAttacking)
        {
            SetAnimatorMovement(2); // Anima��o de ataque
            return;
        }

        // L�gica de IA para decidir movimento e ataque
        if (Time.time >= nextActionTime)
        {
            DecideAction();
            nextActionTime = Time.time + reactionTime;
        }

        // Atualiza a anima��o de movimento
        Vector2 currentVelocity = rb.linearVelocity;
        if (currentVelocity.sqrMagnitude > 0.1f) // Se estiver se movendo significativamente
        {
            SetAnimatorMovement(1); // Anima��o de movimento
        }
        else
        {
            SetAnimatorMovement(0); // Anima��o parado
        }
    }

    void FixedUpdate()
    {
        // Move o Rigidbody para a posi��o alvo
        Vector2 currentPosition = rb.position;
        Vector2 moveDirection = (targetPosition - currentPosition).normalized;
        Vector2 newPosition = currentPosition + moveDirection * currentSpeed * Time.fixedDeltaTime;

        // Limita a movimenta��o do NPC � sua metade direita do campo
        float minX = mapCenterlineX;
        float maxX = mapRightBoundX;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, mapLowerBoundY, mapUpperBoundY);

        rb.MovePosition(newPosition);
    }

    void DecideAction()
    {
        if (ball == null) return;

        // Detecta se a bola est� ao alcance para um ataque
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

        // === L�gica de Decis�o ===
        if (ballInRange)
        {
            // Se a bola est� ao alcance, o NPC tenta atacar
            StartAttack();
        }
        else
        {
            // Se a bola n�o est� ao alcance, o NPC se posiciona
            CalculateMovementTarget();
        }
    }

    void CalculateMovementTarget()
    {
        Vector2 ballPosition = ball.transform.position;
        Vector2 ballVelocity = ball.GetComponent<Rigidbody2D>().linearVelocity;

        // Previs�o da posi��o da bola
        Vector2 predictedBallPosition = ballPosition + ballVelocity * anticipationFactor;

        // Decide a posi��o alvo do NPC
        // O NPC tenta se posicionar na mesma altura Y da bola, mas em sua metade do campo
        targetPosition = new Vector2(
            Mathf.Clamp(predictedBallPosition.x, mapCenterlineX + defensePositionOffset, mapRightBoundX), // Mover para a frente da linha central
            Mathf.Clamp(predictedBallPosition.y, mapLowerBoundY, mapUpperBoundY)
        );

        // Se a bola estiver vindo para a �rea do NPC, ele pode "correr" para interceptar
        if (ballVelocity.x < 0 && ballPosition.x > mapCenterlineX) // Bola vindo da direita para a esquerda e ainda na �rea do NPC
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
        SetAnimatorMovement(2); // Anima��o de ataque

        // L�gica de "K" apertado para a IA
        // Atrasar um pouco o "soltar" do ataque para simular um clique
        Invoke(nameof(PerformHit), attackDecisionBuffer);
    }

    void PerformHit()
    {
        if (ball == null) return;

        // Reverifica se a bola ainda est� ao alcance (pode ter se movido muito r�pido)
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                ball.HitBall(direction, hitForce); // Chama o m�todo HitBall da bola
                break;
            }
        }

        EndAttack();
    }

    void EndAttack()
    {
        isAttacking = false;
        currentSpeed = moveSpeed; // Retorna � velocidade normal
        SetAnimatorMovement(0); // Anima��o parado (ou volta para movimento se a DecideAction chamar)
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

        // Desenhar os limites para visualiza��o no editor (similar ao PlayerController)
        if (centerlineMarker != null)
        {
            Gizmos.color = Color.blue;
            float currentMinX = mapCenterlineX;
            float currentMaxX = mapRightBoundX;

            Vector3 minBounds = new Vector3(currentMinX, mapLowerBoundY, 0);
            Vector3 maxBounds = new Vector3(currentMaxX, mapUpperBoundY, 0);

            Gizmos.DrawWireCube((minBounds + maxBounds) / 2, maxBounds - minBounds);

            // Desenhar a linha central tamb�m no Gizmos para visualiza��o
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(mapCenterlineX, mapLowerBoundY, 0), new Vector3(mapCenterlineX, mapUpperBoundY, 0));
        }
    }
}