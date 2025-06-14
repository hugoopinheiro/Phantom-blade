// Use este c�digo para AMBOS FirePowerUp.cs e IcePowerUp.cs
using UnityEngine;

public class FirePowerUp : MonoBehaviour // OU public class IcePowerUp : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"PowerUp: Colis�o detectada com {other.gameObject.name} (Tag: {other.tag})");

        // Agora, como ambos Player e NPC t�m a tag "Player", a verifica��o � mais simples:
        if (other.CompareTag("Player"))
        {
            Debug.Log("PowerUp: Colidiu com um jogador v�lido!");
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
                Debug.LogWarning("PowerUp: BallController N�O encontrado na cena!");
            }
        }
        else
        {
            Debug.Log("PowerUp: Colis�o com objeto n�o-jogador (Ignorado).");
        }
    }
}