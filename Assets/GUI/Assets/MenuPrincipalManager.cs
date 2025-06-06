using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    // [SerializeField] torna esta variável visível e editável no Inspetor do Unity.
    // 'nomeDaCenaDoJogo' será onde você digitará o nome da cena a ser carregada.
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

        // No Editor do Unity, Application.Quit() não encerra o aplicativo.
        // Ele funciona apenas em builds. Para testar no Editor, você verá a mensagem no console.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}