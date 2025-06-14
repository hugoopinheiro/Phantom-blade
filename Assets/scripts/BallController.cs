using UnityEngine;
using System;
using System.Runtime.CompilerServices; // Necessário para 'Action'

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    // NOVO: Evento para notificar o GameManager quando um gol é marcado
    // O string que passamos será a tag do objeto que a bola colidiu ("Goal")
    // e o float será a posição X da bola no momento do gol, para saber o lado.
    public event Action<string, float> OnGoalScored; // Evento com tag e posição X
    public enum BallType
    {
        NORMAL,
        FIRE,
        ICE
    }
    public BallType currentBallType = BallType.NORMAL;

    public float initialSpeed = 0f; // A bola começa parada, só se move com o ataque
    private Rigidbody2D rb;
    private Vector2 startPosition; // Posição inicial da bola (onde ela spawna)

    [Header("Configurações da Bola")]
    [SerializeField] private Vector3 normalBallScale = new Vector3(1f, 1f, 1f); // Escala normal da bola
    [SerializeField] private Vector3 fireBallScale = new Vector3(0.7f, 0.7f, 1f); // Escala para bola de fogo (menor)
    [SerializeField] private Vector3 iceBallScale = new Vector3(0.7f, 0.7f, 1f);  // Escala para bola de gelo (menor)

    private Transform ballTransform;

    [SerializeField] private Sprite normalBallSprite;
    [SerializeField] private Sprite fireBallSprite;
    [SerializeField] private Sprite iceBallSprite;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Transform centerlineMarker;
    [SerializeField] private float resetLaunchSpeed = 7f; // Velocidade com que a bola é lançada após um reset
    [SerializeField] private float resetDelay = 1.0f; // Tempo de espera antes de relançar a bola após o reset

    public int baseDamage = 1;
    public int currentDamage;

    [SerializeField] private float powerUpDuration = 10f;
    private float powerUpTimer = 0f;


    private bool isBallInPlay = false;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ballTransform = transform;

        if (spriteRenderer == null)
        {
            Debug.Log("BallController precisa de um SpriteRenderer ");
        }
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        rb.linearVelocity = Vector2.zero; // Garante que a bola não se movi no início
        rb.angularVelocity = 0f; // Zera a rotação também, por segurança
        currentDamage = baseDamage; // Inicializa o dano atual com o dano base
        UpdateBallSprite();

        if (centerlineMarker == null)
        {
            GameObject markerObj = GameObject.Find("MapCenterline");
            if (markerObj != null)
            {
                centerlineMarker = markerObj.transform;
            }
            else
            {
                Debug.LogError("Centerline Marker (MapCenterline GameObject) não encontrado na cena! A bola não saberá para onde voltar.");
            }
        }
    }
    private void Update()
    {
        // NOVO: Lógica do timer do power-up
        if (currentBallType != BallType.NORMAL)
        {
            powerUpTimer -= Time.deltaTime;
            if (powerUpTimer <= 0)
            {
                SetBallType(BallType.NORMAL); // Retorna ao normal quando o tempo acaba
            }
        }
    }
    public void SetBallType(BallType newType)
    {
        currentBallType = newType;
        currentDamage = baseDamage; // Reseta o dano para o base antes de aplicar o novo
        powerUpTimer = powerUpDuration; // Reinicia o timer do power-up

        switch (currentBallType)
        {
            case BallType.NORMAL:
                currentDamage = baseDamage;
                ballTransform.localScale = normalBallScale; // Volta para escala normal
                break;
            case BallType.FIRE:
                currentDamage = 2;
                ballTransform.localScale = fireBallScale; // Aplica escala da bola de fogo
                break;
            case BallType.ICE:
                currentDamage = baseDamage;
                ballTransform.localScale = iceBallScale; // Aplica escala da bola de gelo
                break;
        }
        UpdateBallSprite(); // Atualiza a aparência da bola
        Debug.Log($"Tipo da bola alterado para: {currentBallType}. Dano atual: {currentDamage}");
    }

    // NOVO: Método para atualizar a sprite da bola
    private void UpdateBallSprite()
    {
        if (spriteRenderer == null) return;

        switch (currentBallType)
        {
            case BallType.NORMAL:
                spriteRenderer.sprite = normalBallSprite;
                break;
            case BallType.FIRE:
                spriteRenderer.sprite = fireBallSprite;
                break;
            case BallType.ICE:
                spriteRenderer.sprite = iceBallSprite;
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            float playerVelocityY = collision.rigidbody.linearVelocity.y;
            Vector2 newVelocity = rb.linearVelocity;
            newVelocity.y += playerVelocityY * 0.5f;
            rb.linearVelocity = newVelocity.normalized * rb.linearVelocity.magnitude;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
        {
            Debug.Log("GOOOOL! Bola indo para o centro. Gol na " + other.name); // 'other.name' para saber qual gol

            // Dispara o evento, passando a tag e a posição X da bola no momento do gol
            // Isso permite que o GameManager saiba qual jogador perdeu vida.
            OnGoalScored?.Invoke(other.tag, transform.position.x);

            ResetBallToCenter();
        }
    }

    void ResetBallToCenter()
    {
        if (centerlineMarker == null)
        {
            Debug.LogError("Não foi possível resetar a bola ao centro: Centerline Marker não definido!");
            transform.position = startPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            isBallInPlay = false;
            return;
        }

        // Move a bola para a posição X da linha central, e mantém o Y na posição inicial (ou um Y fixo)
        transform.position = new Vector2(centerlineMarker.position.x, startPosition.y);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        isBallInPlay = false;

        Invoke(nameof(LaunchBallFromCenter), resetDelay);
        Debug.Log("Bola resetada para o centro, aguardando relançamento...");
    }

    void LaunchBallFromCenter()
    {
        float xDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f; // 'UnityEngine.Random' para evitar conflito com System.Random
        float yDirection = UnityEngine.Random.Range(-0.5f, 0.5f);

        Vector2 direction = new Vector2(xDirection, yDirection).normalized;
        rb.linearVelocity = direction * resetLaunchSpeed;
        isBallInPlay = true;
        Debug.Log($"Bola relançada do centro com velocidade: {rb.linearVelocity}");
    }

    public void HitBall(Vector2 direction, float force)
    {
        rb.linearVelocity = direction.normalized * force;
        isBallInPlay = true;
        Debug.Log($"Bola atacada com velocidade: {rb.linearVelocity}");
    }
}