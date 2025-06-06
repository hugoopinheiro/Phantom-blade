using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private bool isLeftPlayer = true;

    // **NOVO: Referência para o GameObject da linha central**
    [SerializeField] private Transform centerlineMarker;

    // Os limites do mapa (bordas externas) ainda podem ser fixos ou de outro objeto
    [SerializeField] private float mapRightBoundX = 19f;
    [SerializeField] private float mapLeftBoundX = 0f;
    [SerializeField] private float mapUpperBoundY = 10f;
    [SerializeField] private float mapLowerBoundY = 0f;

    // Variável interna para a coordenada X da linha central
    private float mapCenterlineX;

    private BallController currentBall;

    [SerializeField] private float maxMoveDistance = 5f; // Distância máxima do ponto inicial
    private Vector2 initialPosition;

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

        currentBall = FindFirstObjectByType<BallController>();
        if (currentBall == null)
        {
            Debug.LogError("BallController não encontrado na cena! Certifique-se de que a bola tem o script BallController.");
        }

        // **Defina a coordenada X da linha central a partir do GameObject**
        if (centerlineMarker != null)
        {
            mapCenterlineX = centerlineMarker.position.x;
        }
        else
        {
            Debug.LogError("Centerline Marker não foi atribuído no Inspector do PlayerController! Usando valor padrão.");
            mapCenterlineX = (mapLeftBoundX + mapRightBoundX) / 2f; // Valor padrão
        }

        // Opcional: Garante que o jogador comece na sua área. 
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
        Vector2 newPosition = _playerRigidBody2d.position + _playerDirection * _playerSpeed * Time.fixedDeltaTime;

        float currentMinX;
        float currentMaxX;

        if (isLeftPlayer)
        {
            currentMinX = mapLeftBoundX;
            currentMaxX = mapCenterlineX; // Limite é a linha central
        }
        else
        {
            currentMinX = mapCenterlineX; // Começa na linha central
            currentMaxX = mapRightBoundX;
        }

        newPosition.x = Mathf.Clamp(newPosition.x, currentMinX, currentMaxX);
        newPosition.y = Mathf.Clamp(newPosition.y, mapLowerBoundY, mapUpperBoundY);

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


            // Mas precisamos ter certeza que a bola está ao alcance para ser "atingida"
            if (currentBall != null)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, hitRange, LayerMask.GetMask("Ball")); // Opcional: Filtrar por Layer "Ball" para otimizar
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Ball"))
                    {
                        // A bola está ao alcance e tem a tag "Ball"
                        // Calcular a direção da bola a partir do jogador
                        Vector2 direction = (hit.transform.position - transform.position).normalized;

                        // Chamar o método HitBall da bola para ela se mover
                        currentBall.HitBall(direction, hitForce); // Usa o hitForce do PlayerController
                        break; // Se encontrou e atingiu a bola, pode sair do loop
                    }
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            _isAttack = false;
            _playerSpeed = _playerInitialSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint.position, hitRange);
        }

        // Desenhar os limites para visualização no editor
        // Certifique-se de que mapCenterlineX foi definido (ou use centerlineMarker.position.x diretamente)
        if (centerlineMarker != null)
        {
            Gizmos.color = Color.blue;
            float currentMinX;
            float currentMaxX;
            float currentCenterlineX = centerlineMarker.position.x; // Use a posição atual do marker

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

            // Desenhar a linha central também no Gizmos para visualização
            Gizmos.color = Color.red; // Cor diferente para a linha central
            Gizmos.DrawLine(new Vector3(currentCenterlineX, mapLowerBoundY, 0), new Vector3(currentCenterlineX, mapUpperBoundY, 0));
        }
    }
    public void ExitGame() 
    {
        if (Input.GetKeyDown(KeyCode.M))
            {
               
            }
    }
}

