using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゴールアイテムを生成する
/// プレイヤーの位置とロビーの位置を取り、最も近いロビー2つを除いた遠い2つの中から
/// </summary>
public class GoaLItemGenerator : MonoBehaviour
{
    [SerializeField] GameObject goalitemPre;    // ゴールアイテム
    [SerializeField] Transform lobby;  // ゴールアイテムを生成するロビー(とりあえず一つ)
    KeyFgmManager keyManager;   // KeyFgmManager


    [SerializeField] float height = 0.6f;   // 道の上に浮かせる高さ
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keyManager = GameObject.Find("KeyFgmManager").GetComponent<KeyFgmManager>();
        Instantiate(goalitemPre, new Vector3(lobby.position.x, lobby.position.y + height, lobby.position.z), Quaternion.identity, transform);
    }

    // Update is called once per frame
    void Update()
    {
        if(keyManager.GetKeyFgmNum() < 0)
        {
            
        }
    }
}
