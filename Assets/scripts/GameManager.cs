using UnityEngine;
using TMPro; // Para usar TextMeshProUGUI (se você estiver usando)
using UnityEngine.SceneManagement; // Para carregar cenas de game over, etc.

public class GameManager : MonoBehaviour
{
    // Vidas dos jogadores
    public int vidaJogador1 = 3; // Jogador da Esquerda (PlayerController)
    public int vidaJogador2 = 3; // Jogador da Direita (NPC Controller)

    // Referências para TextMeshProUGUI para exibir a vida na UI
    [Header("UI Vidas")]
    public TextMeshProUGUI textoVidaJogador1;
    public TextMeshProUGUI textoVidaJogador2;

    // Referência ao script da bola
    [Header("Referências de Cena")]
    public BallController ballController;
    public Transform centerlineMarker; // Para determinar o meio do campo (já estava public)

    [Header("Nomes de Cenas")]
    [SerializeField] private string gameOverSceneName = "GameOverScene"; // Cena de Game Over
    [SerializeField] private string menuPrincipalSceneName = "GUI/Assets/Menu"; // Cena do Menu Principal

    void Start()
    {
        // Se este GameManager deve persistir entre cenas, mantenha.
        // Se você terá um GameManager por cena, remova esta linha.
        // DontDestroyOnLoad(gameObject);

        // Encontra o BallController na cena se não for atribuído via Inspector
        if (ballController == null)
        {
            ballController = FindAnyObjectByType<BallController>();
            if (ballController == null)
            {
                Debug.LogError("BallController não encontrado na cena! O GameManager não poderá detectar gols.");
                return; // Impede que o restante do Start execute sem a bola
            }
        }

        // Encontra o Centerline Marker se não for atribuído via Inspector
        if (centerlineMarker == null)
        {
            GameObject markerObj = GameObject.Find("MapCenterline");
            if (markerObj != null)
            {
                centerlineMarker = markerObj.transform;
            }
            else
            {
                Debug.LogError("Centerline Marker (MapCenterline GameObject) não encontrado! Não será possível determinar o lado do gol.");
            }
        }

        // Assina o evento da bola quando um gol é marcado
        ballController.OnGoalScored += HandleGoalScored;

        AtualizarUI(); // Atualiza a UI com a vida inicial
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Tecla 'M' pressionada. Voltando para o Menu Principal.");
            VoltarParaMenuPrincipal(); // Chama o método para voltar ao menu
        }
    }

    // Método que é chamado quando o evento OnGoalScored é disparado
    private void HandleGoalScored(string goalTag, float ballXPosition)
    {
        // NOVO: Lógica CORRETA de perda de vida
        // Se a bola passou pela 'GoalLeft' (X NEGATIVO em relação ao centro), o JOGADOR 1 perde vida
        // Porque o Jogador 1 defende a ESQUERDA.
        if (centerlineMarker != null && ballXPosition < centerlineMarker.position.x)
        {
            vidaJogador1--; // Gol na área do Jogador 1 (esquerda)
            Debug.Log("Gol do Jogador 2! Jogador 1 perdeu vida. Vida restante: " + vidaJogador1);
        }
        // Se a bola passou pela 'GoalRight' (X POSITIVO em relação ao centro), o JOGADOR 2 perde vida
        // Porque o Jogador 2 defende a DIREITA.
        else if (centerlineMarker != null && ballXPosition > centerlineMarker.position.x)
        {
            vidaJogador2--; // Gol na área do Jogador 2 (direita)
            Debug.Log("Gol do Jogador 1! Jogador 2 perdeu vida. Vida restante: " + vidaJogador2);
        }
        else
        {
            Debug.LogWarning("Gol marcado, mas não foi possível determinar qual jogador perdeu vida (centerlineMarker ou posição X da bola).");
        }

        AtualizarUI();
        VerificarFimDeJogo();
    }

    void AtualizarUI()
    {
        if (textoVidaJogador1 != null)
        {
            textoVidaJogador1.text = "VIDA J1: " + vidaJogador1;
        }
        if (textoVidaJogador2 != null)
        {
            textoVidaJogador2.text = "VIDA J2: " + vidaJogador2;
        }
    }

    void VerificarFimDeJogo()
    {
        if (vidaJogador1 <= 0)
        {
            Debug.Log("Fim de Jogo! Jogador 2 Venceu!");
            ExibirMensagemVitoria("JOGADOR 2 VENCEU!");
        }
        else if (vidaJogador2 <= 0)
        {
            Debug.Log("Fim de Jogo! Jogador 1 Venceu!");
            ExibirMensagemVitoria("JOGADOR 1 VENCEU!");
        }
    }

    public TextMeshProUGUI textoMensagemVitoria;

    void ExibirMensagemVitoria(string mensagem)
    {
        if (textoMensagemVitoria != null)
        {
            textoMensagemVitoria.text = mensagem;
            textoMensagemVitoria.gameObject.SetActive(true);
            Invoke("VoltarParaMenuPrincipal", 3f);
        }
        else
        {
            Debug.LogWarning("Texto de Mensagem de Vitória não atribuído ao GameManager.");
            VoltarParaMenuPrincipal();
        }
    }

    void VoltarParaMenuPrincipal()
    {
        SceneManager.LoadScene(menuPrincipalSceneName);
    }

    void OnDestroy()
    {
        if (ballController != null)
        {
            ballController.OnGoalScored -= HandleGoalScored;
        }
    }

}