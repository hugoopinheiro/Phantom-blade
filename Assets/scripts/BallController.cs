using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    public float initialSpeed = 0f; // A bola começa parada, só se move com o ataque
    private Rigidbody2D rb;
    private Vector2 startPosition; // Posição inicial da bola (onde ela spawna)

    // **NOVO: Referência para a linha central do mapa (para onde a bola deve voltar)**
    [SerializeField] private Transform centerlineMarker;
    [SerializeField] private float resetLaunchSpeed = 7f; // Velocidade com que a bola é lançada após um reset
    [SerializeField] private float resetDelay = 1.0f; // Tempo de espera antes de relançar a bola após o reset

    private bool isBallInPlay = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        rb.linearVelocity = Vector2.zero; // Garante que a bola não se mova no início
        rb.angularVelocity = 0f; // Zera a rotação também, por segurança

        // Encontrar o Centerline Marker se não for atribuído no Inspector
        // Isso é útil para garantir que ele seja encontrado, mas é melhor atribuir no Inspector
        if (centerlineMarker == null)
        {
            GameObject markerObj = GameObject.Find("MapCenterline"); // Assumindo que você nomeou seu GameObject da linha central assim
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

    // OnCollisionEnter2D é para objetos que *não são triggers* (como jogadores ou bordas superiores/inferiores que rebatem)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se a bola colidir com um jogador, ajusta a direção baseada no movimento do jogador
        if (collision.gameObject.CompareTag("Player"))
        {
            float playerVelocityY = collision.rigidbody.linearVelocity.y;

            // Adiciona um efeito baseado na velocidade do jogador
            Vector2 newVelocity = rb.linearVelocity;
            newVelocity.y += playerVelocityY * 0.5f;

            rb.linearVelocity = newVelocity.normalized * rb.linearVelocity.magnitude;
        }
        // Se você tiver paredes *superiores/inferiores* que *rebatem* e não são "Gols",
        // elas teriam um Collider2D **sem** Is Trigger e poderiam ter uma Tag "Wall" aqui.
        // else if (collision.gameObject.CompareTag("Wall"))
        // {
        //     Debug.Log("Bola bateu em uma parede rebatível (não um gol).");
        //     // O Unity já lida com o rebatimento automaticamente
        // }
    }

    // OnTriggerEnter2D é para objetos que *são triggers* (como as áreas de "gol" ou as paredes laterais que você quer resetar)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Se o objeto que a bola atravessou tiver a Tag "Goal", reseta a bola para o centro
        // Lembre-se de que WallRight e WallLeft devem ter a Tag "Goal" e o Collider2D marcado como "Is Trigger".
        if (other.CompareTag("Goal"))
        {
            Debug.Log("GOOOOL! Bola indo para o centro.");
            ResetBallToCenter(); // Chama o novo método para resetar ao centro
        }
    }

    // **Este método ResetBall() antigo pode ser removido, a menos que você precise dele para outra coisa.**
    // void ResetBall()
    // {
    //     transform.position = startPosition;
    //     rb.linearVelocity = Vector2.zero;
    //     isBallInPlay = false;
    // }

    // **NOVO: Método para resetar a bola para a linha central**
    void ResetBallToCenter()
    {
        if (centerlineMarker == null)
        {
            Debug.LogError("Não foi possível resetar a bola ao centro: Centerline Marker não definido!");
            // Se o marker não for encontrado, volta para a startPosition (posição inicial do editor)
            transform.position = startPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            isBallInPlay = false;
            return;
        }

        // Move a bola para a posição X da linha central, e mantém o Y na posição inicial (ou um Y fixo, ex: 5f)
        transform.position = new Vector2(centerlineMarker.position.x, startPosition.y);
        rb.linearVelocity = Vector2.zero; // Para a bola imediatamente
        rb.angularVelocity = 0f;
        isBallInPlay = false; // Bola não está mais em jogo até o próximo lançamento

        // Relança a bola após um pequeno atraso
        Invoke(nameof(LaunchBallFromCenter), resetDelay);
        Debug.Log("Bola resetada para o centro, aguardando relançamento...");
    }

    // **NOVO: Método para relançar a bola do centro**
    void LaunchBallFromCenter()
    {
        // Escolhe uma direção aleatória para X (esquerda ou direita)
        float xDirection = Random.value < 0.5f ? -1f : 1f;
        // Escolhe uma direção Y aleatória (um pouco para cima ou para baixo)
        float yDirection = Random.Range(-0.5f, 0.5f);

        Vector2 direction = new Vector2(xDirection, yDirection).normalized;
        rb.linearVelocity = direction * resetLaunchSpeed;
        isBallInPlay = true;
        Debug.Log($"Bola relançada do centro com velocidade: {rb.linearVelocity}");
    }

    // Método público para o PlayerController chamar quando atacar a bola (permaneceu o mesmo)
    public void HitBall(Vector2 direction, float force)
    {
        rb.linearVelocity = direction.normalized * force;
        isBallInPlay = true; // A bola está agora em jogo
        Debug.Log($"Bola atacada com velocidade: {rb.linearVelocity}");
    }
}