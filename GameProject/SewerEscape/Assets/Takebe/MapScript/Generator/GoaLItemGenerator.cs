using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゴールアイテムを生成する
/// </summary>
public class GoaLItemGenerator : MonoBehaviour
{
    [SerializeField] GameObject goalitemPre;    // ゴールアイテム
    KeyFgmManager keyManager;   // KeyFgmManager
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keyManager = GameObject.Find("KeyFgmManager").GetComponent<KeyFgmManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(keyManager.GetKeyFgmNum() < 0)
        {

        }
    }
}
