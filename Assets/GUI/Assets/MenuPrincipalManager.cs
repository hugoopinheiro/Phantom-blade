using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    // [SerializeField] torna esta vari�vel vis�vel e edit�vel no Inspetor do Unity.
    // 'nomeDaCenaDoJogo' ser� onde voc� digitar� o nome da cena a ser carregada.
    [SerializeField] private string nomeDaCenaDoJogo;
    void Start()
    {
        Debug.Log("Menu Principal Carregado");
    }
    public void jogar()
    {
        // Carrega a cena cujo nome foi definido no Inspetor do Unity.
        SceneManager.LoadScene(nomeDaCenaDoJogo);
    }

    public void sair()
    {
        Debug.Log("Sair do Jogo");
        Application.Quit();

        // No Editor do Unity, Application.Quit() n�o encerra o aplicativo.
        // Ele funciona apenas em builds. Para testar no Editor, voc� ver� a mensagem no console.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}