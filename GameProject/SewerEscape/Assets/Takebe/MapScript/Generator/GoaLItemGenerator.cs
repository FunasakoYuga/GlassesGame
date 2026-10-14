using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゴールアイテムを生成する
/// プレイヤーの位置とロビーの位置を取り、最も近いロビー2つを除いた遠い2つの中から
/// </summary>
public class GoaLItemGenerator : MonoBehaviour
{
    [SerializeField] GameObject goalitemPre;    // ゴールアイテム
    [SerializeField] float height = 0.6f;   // 道の上に浮かせる高さ
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
            //Instantiate(goalitemPre,)
        }
    }
}
