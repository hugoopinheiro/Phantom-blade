using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    public float initialSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 startPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        LaunchBall();
    }

    private void LaunchBall()
    {
        // Lança a bola em uma direção aleatória (direita ou esquerda)
        float xDirection = Random.value < 0.5f ? -1f : 1f;
        float yDirection = Random.Range(-0.5f, 0.5f);

        Vector2 direction = new Vector2(xDirection, yDirection).normalized;
        rb.linearVelocity = direction * initialSpeed;
    }

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

        // Você pode adicionar outros comportamentos aqui, como som de colisão, efeitos etc.
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
        {
            ResetBall(); // Aqui você pode resetar a bola e/ou contar ponto
        }
    }

    void ResetBall()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(LaunchBall), 1f);
    }
}
