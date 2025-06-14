// Use este código para AMBOS FirePowerUp.cs e IcePowerUp.cs
using UnityEngine;

public class FirePowerUp : MonoBehaviour // OU public class IcePowerUp : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"PowerUp: Colisão detectada com {other.gameObject.name} (Tag: {other.tag})");

        // Agora, como ambos Player e NPC têm a tag "Player", a verificação é mais simples:
        if (other.CompareTag("Player"))
        {
            Debug.Log("PowerUp: Colidiu com um jogador válido!");
            BallController ball = FindAnyObjectByType<BallController>();
            if (ball != null)
            {
                Debug.Log("PowerUp: BallController encontrado! Chamando SetBallType.");

                ball.SetBallType(BallController.BallType.FIRE);

                gameObject.SetActive(false); // Desativa o power-up
                Debug.Log("Power-up coletado e desativado!");
            }
            else
            {
                Debug.LogWarning("PowerUp: BallController NÃO encontrado na cena!");
            }
        }
        else
        {
            Debug.Log("PowerUp: Colisão com objeto não-jogador (Ignorado).");
        }
    }
}