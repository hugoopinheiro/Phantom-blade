using UnityEngine;
using TMPro; // Para usar TextMeshProUGUI (se voc� estiver usando)
using UnityEngine.SceneManagement; // Para carregar cenas de game over, etc.

public class GameManager : MonoBehaviour
{
    // Vidas dos jogadores
    public int vidaJogador1 = 3; // Jogador da Esquerda (PlayerController)
    public int vidaJogador2 = 3; // Jogador da Direita (NPC Controller)

    // Refer�ncias para TextMeshProUGUI para exibir a vida na UI
    [Header("UI Vidas")]
    public TextMeshProUGUI textoVidaJogador1;
    public TextMeshProUGUI textoVidaJogador2;

    // Refer�ncia ao script da bola
    [Header("Refer�ncias de Cena")]
    public BallController ballController;
    public Transform centerlineMarker; // Para determinar o meio do campo (j� estava public)

    [Header("Nomes de Cenas")]
    [SerializeField] private string gameOverSceneName = "GameOverScene"; // Cena de Game Over
    [SerializeField] private string menuPrincipalSceneName = "GUI/Assets/Menu"; // Cena do Menu Principal

    void Start()
    {
        // Se este GameManager deve persistir entre cenas, mantenha.
        // Se voc� ter� um GameManager por cena, remova esta linha.
        // DontDestroyOnLoad(gameObject);

        // Encontra o BallController na cena se n�o for atribu�do via Inspector
        if (ballController == null)
        {
            ballController = FindAnyObjectByType<BallController>();
            if (ballController == null)
            {
                Debug.LogError("BallController n�o encontrado na cena! O GameManager n�o poder� detectar gols.");
                return; // Impede que o restante do Start execute sem a bola
            }
        }

        // Encontra o Centerline Marker se n�o for atribu�do via Inspector
        if (centerlineMarker == null)
        {
            GameObject markerObj = GameObject.Find("MapCenterline");
            if (markerObj != null)
            {
                centerlineMarker = markerObj.transform;
            }
            else
            {
                Debug.LogError("Centerline Marker (MapCenterline GameObject) n�o encontrado! N�o ser� poss�vel determinar o lado do gol.");
            }
        }

        // Assina o evento da bola quando um gol � marcado
        ballController.OnGoalScored += HandleGoalScored;

        AtualizarUI(); // Atualiza a UI com a vida inicial
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Tecla 'M' pressionada. Voltando para o Menu Principal.");
            VoltarParaMenuPrincipal(); // Chama o m�todo para voltar ao menu
        }
    }

    // M�todo que � chamado quando o evento OnGoalScored � disparado
    private void HandleGoalScored(string goalTag, float ballXPosition)
    {
        // NOVO: L�gica CORRETA de perda de vida
        // Se a bola passou pela 'GoalLeft' (X NEGATIVO em rela��o ao centro), o JOGADOR 1 perde vida
        // Porque o Jogador 1 defende a ESQUERDA.
        if (centerlineMarker != null && ballXPosition < centerlineMarker.position.x)
        {
            vidaJogador1--; // Gol na �rea do Jogador 1 (esquerda)
            Debug.Log("Gol do Jogador 2! Jogador 1 perdeu vida. Vida restante: " + vidaJogador1);
        }
        // Se a bola passou pela 'GoalRight' (X POSITIVO em rela��o ao centro), o JOGADOR 2 perde vida
        // Porque o Jogador 2 defende a DIREITA.
        else if (centerlineMarker != null && ballXPosition > centerlineMarker.position.x)
        {
            vidaJogador2--; // Gol na �rea do Jogador 2 (direita)
            Debug.Log("Gol do Jogador 1! Jogador 2 perdeu vida. Vida restante: " + vidaJogador2);
        }
        else
        {
            Debug.LogWarning("Gol marcado, mas n�o foi poss�vel determinar qual jogador perdeu vida (centerlineMarker ou posi��o X da bola).");
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
            Debug.LogWarning("Texto de Mensagem de Vit�ria n�o atribu�do ao GameManager.");
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