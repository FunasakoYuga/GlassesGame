using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private string gameScene = "GameScene";
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(gameScene);
    }
}
