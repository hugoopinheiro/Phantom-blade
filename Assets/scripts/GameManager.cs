using UnityEngine;
using TMPro; // Para usar TextMeshProUGUI
using UnityEngine.SceneManagement; // Para carregar cenas de game over, etc.

public class GameManager : MonoBehaviour
{
    // Vidas dos jogadores
    public int vidaJogador1 = 5; // Jogador da Esquerda (PlayerController)
    public int vidaJogador2 = 5; // Jogador da Direita (NPC Controller)

    // Referências para TextMeshProUGUI para exibir a vida na UI
    [Header("UI Vidas")]
    public TextMeshProUGUI textoVidaJogador1;
    public TextMeshProUGUI textoVidaJogador2;

    // Referência ao script da bola
    [Header("Referências de Cena")]
    public BallController ballController;
    public Transform centerlineMarker; // Para determinar o meio do campo

    // NOVO: Referências aos PlayerControllers para aplicar o efeito de lentidão
    [Header("Referências dos Jogadores")]
    [SerializeField] private PlayerController player1Controller; // Arraste o Player 1 aqui no Inspector
    [SerializeField] private NpcController player2Controller; // Arraste o Player 2 aqui no Inspector

    // NOVO: Duração do efeito de lentidão para a bola de gelo
    [Header("Configurações de Power-up")]
    [SerializeField] private float slowEffectDuration = 5f; // Duração em segundos do efeito de lentidão

    [Header("Nomes de Cenas")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private string menuPrincipalSceneName = "GUI/Assets/Menu";

    // NOVO: Referência para o texto de mensagem de vitória (se ainda não estiver público)
    public TextMeshProUGUI textoMensagemVitoria;


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
                return;
            }
        }

        // NOVO: Tenta encontrar os PlayerControllers se não forem atribuídos
        if (player1Controller == null)
        {
            // Assumindo que Player1 tem a tag "Player1" ou um componente PlayerController
            // Melhor usar FindObjectsOfType ou Tags se eles não forem filhos diretos deste GameManager
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
            Debug.LogError("Um ou ambos os PlayerControllers não foram encontrados. Certifique-se de que estão atribuídos ou que suas tags/nomes estão corretos.");
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
            VoltarParaMenuPrincipal();
        }
    }

    // Método que é chamado quando o evento OnGoalScored é disparado
    private void HandleGoalScored(string goalTag, float ballXPosition)
    {
        // NOVO: Obtenha o dano atual da bola do BallController
        int damageToDeal = ballController.currentDamage;
        // NOVO: Verifique o tipo da bola para aplicar efeitos adicionais
        BallController.BallType currentBallType = ballController.currentBallType;

        // Determine qual jogador perdeu vida e aplique os efeitos
        if (centerlineMarker != null && ballXPosition < centerlineMarker.position.x)
        {
            // Gol na área do Jogador 1 (esquerda)
            vidaJogador1 -= damageToDeal; // Usa o dano dinâmico da bola
            Debug.Log($"Gol do Jogador 2! Jogador 1 perdeu {damageToDeal} de vida. Vida restante: {vidaJogador1}");

            // NOVO: Aplica o efeito de lentidão se a bola for de gelo
            if (currentBallType == BallController.BallType.ICE && player1Controller != null)
            {
                player1Controller.ApplySlowEffect(slowEffectDuration);
                Debug.Log("Jogador 1 foi atingido por uma bola de gelo e está mais lento!");
            }
        }
        else if (centerlineMarker != null && ballXPosition > centerlineMarker.position.x)
        {
            // Gol na área do Jogador 2 (direita)
            vidaJogador2 -= damageToDeal; // Usa o dano dinâmico da bola
            Debug.Log($"Gol do Jogador 1! Jogador 2 perdeu {damageToDeal} de vida. Vida restante: {vidaJogador2}");

            // NOVO: Aplica o efeito de lentidão se a bola for de gelo
            if (currentBallType == BallController.BallType.ICE && player2Controller != null)
            {
                player2Controller.ApplySlowEffect(slowEffectDuration);
                Debug.Log("Jogador 2 foi atingido por uma bola de gelo e está mais lento!");
            }
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