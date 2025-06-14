// PowerUpSpawner.cs
using UnityEngine;
using System.Collections; // Para usar Coroutines

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject firePowerUpPrefab; // Arraste seu Prefab de Fogo aqui
    [SerializeField] private GameObject icePowerUpPrefab;  // Arraste seu Prefab de Gelo aqui

    [SerializeField] private Vector2[] spawnPositions; // Array de posições onde podem aparecer
    [SerializeField] private float spawnInterval = 15f; // A cada quantos segundos um power-up surge

    private GameObject currentActivePowerUp = null; // Rastreia o power-up ativo para evitar múltiplos

    void Start()
    {
        StartCoroutine(SpawnPowerUpRoutine());
    }

    IEnumerator SpawnPowerUpRoutine()
    {
        while (true) // Loop infinito enquanto o jogo roda
        {
            yield return new WaitForSeconds(spawnInterval); // Espera o intervalo

            // Se não há um power-up ativo OU o power-up ativo está desativado (foi coletado)
            if (currentActivePowerUp == null || !currentActivePowerUp.activeInHierarchy)
            {
                // Se existia um power-up anterior e ele foi desativado, o destruímos para limpar a cena
                if (currentActivePowerUp != null)
                {
                    Destroy(currentActivePowerUp);
                }
                SpawnRandomPowerUp();
            }
        }
    }

    void SpawnRandomPowerUp()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("Nenhuma posição de spawn definida para os power-ups!");
            return;
        }

        int randomPosIndex = Random.Range(0, spawnPositions.Length);
        Vector2 spawnPos = spawnPositions[randomPosIndex];

        // Escolhe aleatoriamente entre fogo e gelo
        GameObject powerUpToSpawn = Random.value < 0.5f ? firePowerUpPrefab : icePowerUpPrefab;

        if (powerUpToSpawn != null)
        {
            currentActivePowerUp = Instantiate(powerUpToSpawn, spawnPos, Quaternion.identity);
            // Opcional: Se quiser que o power-up fique visível imediatamente se foi spawnado
            currentActivePowerUp.SetActive(true);
            Debug.Log($"Power-up {powerUpToSpawn.name} spawnado em {spawnPos}");
        }
        else
        {
            Debug.LogError("Prefab de Power-up não atribuído no Spawner!");
        }
    }
}