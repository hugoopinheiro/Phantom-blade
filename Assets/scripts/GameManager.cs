using UnityEngine;
using TMPro; // Para usar TextMeshProUGUI
using UnityEngine.SceneManagement; // Para carregar cenas de game over, etc.

public class GameManager : MonoBehaviour
{
    // Vidas dos jogadores
    public int vidaJogador1 = 5; // Jogador da Esquerda (PlayerController)
    public int vidaJogador2 = 5; // Jogador da Direita (NPC Controller)

    // Refer�ncias para TextMeshProUGUI para exibir a vida na UI
    [Header("UI Vidas")]
    public TextMeshProUGUI textoVidaJogador1;
    public TextMeshProUGUI textoVidaJogador2;

    // Refer�ncia ao script da bola
    [Header("Refer�ncias de Cena")]
    public BallController ballController;
    public Transform centerlineMarker; // Para determinar o meio do campo

    // NOVO: Refer�ncias aos PlayerControllers para aplicar o efeito de lentid�o
    [Header("Refer�ncias dos Jogadores")]
    [SerializeField] private PlayerController player1Controller; // Arraste o Player 1 aqui no Inspector
    [SerializeField] private NpcController player2Controller; // Arraste o Player 2 aqui no Inspector

    // NOVO: Dura��o do efeito de lentid�o para a bola de gelo
    [Header("Configura��es de Power-up")]
    [SerializeField] private float slowEffectDuration = 5f; // Dura��o em segundos do efeito de lentid�o

    [Header("Nomes de Cenas")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private string menuPrincipalSceneName = "GUI/Assets/Menu";

    // NOVO: Refer�ncia para o texto de mensagem de vit�ria (se ainda n�o estiver p�blico)
    public TextMeshProUGUI textoMensagemVitoria;


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
                return;
            }
        }

        // NOVO: Tenta encontrar os PlayerControllers se n�o forem atribu�dos
        if (player1Controller == null)
        {
            // Assumindo que Player1 tem a tag "Player1" ou um componente PlayerController
            // Melhor usar FindObjectsOfType ou Tags se eles n�o forem filhos diretos deste GameManager
            GameObject p1Obj = GameObject.FindWithTag("Player1"); // Ou use o nome do GameObject do Player 1
            if (p1Obj != null) player1Controller = p1Obj.GetComponent<PlayerController>();
        }
        if (player2Controller == null)
        {
            GameObject p2Obj = GameObject.FindWithTag("Player2"); // Ou use o nome do GameObject do Player 2 (NPC)
            if (p2Obj != null) player2Controller = p2Obj.GetComponent<NpcController>();
        }

        if (player1Controller == null || player2Controller == null)
        {
            Debug.LogError("Um ou ambos os PlayerControllers n�o foram encontrados. Certifique-se de que est�o atribu�dos ou que suas tags/nomes est�o corretos.");
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
            VoltarParaMenuPrincipal();
        }
    }

    // M�todo que � chamado quando o evento OnGoalScored � disparado
    private void HandleGoalScored(string goalTag, float ballXPosition)
    {
        // NOVO: Obtenha o dano atual da bola do BallController
        int damageToDeal = ballController.currentDamage;
        // NOVO: Verifique o tipo da bola para aplicar efeitos adicionais
        BallController.BallType currentBallType = ballController.currentBallType;

        // Determine qual jogador perdeu vida e aplique os efeitos
        if (centerlineMarker != null && ballXPosition < centerlineMarker.position.x)
        {
            // Gol na �rea do Jogador 1 (esquerda)
            vidaJogador1 -= damageToDeal; // Usa o dano din�mico da bola
            Debug.Log($"Gol do Jogador 2! Jogador 1 perdeu {damageToDeal} de vida. Vida restante: {vidaJogador1}");

            // NOVO: Aplica o efeito de lentid�o se a bola for de gelo
            if (currentBallType == BallController.BallType.ICE && player1Controller != null)
            {
                player1Controller.ApplySlowEffect(slowEffectDuration);
                Debug.Log("Jogador 1 foi atingido por uma bola de gelo e est� mais lento!");
            }
        }
        else if (centerlineMarker != null && ballXPosition > centerlineMarker.position.x)
        {
            // Gol na �rea do Jogador 2 (direita)
            vidaJogador2 -= damageToDeal; // Usa o dano din�mico da bola
            Debug.Log($"Gol do Jogador 1! Jogador 2 perdeu {damageToDeal} de vida. Vida restante: {vidaJogador2}");

            // NOVO: Aplica o efeito de lentid�o se a bola for de gelo
            if (currentBallType == BallController.BallType.ICE && player2Controller != null)
            {
                player2Controller.ApplySlowEffect(slowEffectDuration);
                Debug.Log("Jogador 2 foi atingido por uma bola de gelo e est� mais lento!");
            }
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