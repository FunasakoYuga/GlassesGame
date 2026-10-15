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

    // ゴールアイテムを生成したか
    bool isGoalGenerate;

    [SerializeField] float height = 0.6f;   // 道の上に浮かせる高さ

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keyManager = GameObject.Find("KeyFgmManager").GetComponent<KeyFgmManager>();
        isGoalGenerate = false;
    }

    // Update is called once per frame
    void Update()
    {
        // ゴールアイテムを生成してたらここで終わる
        if (isGoalGenerate) return;

        if (keyManager.IsClearReach())
        {
            PlaceGoal();
            isGoalGenerate = true;
        }
    }

    // ゴールアイテムを生成
    void PlaceGoal()
    {
        Instantiate(goalitemPre, new Vector3(lobby.position.x, lobby.position.y + height + (goalitemPre.transform.localScale.y * 0.1f), lobby.position.z), Quaternion.Euler(-90f, 0f, 0f), transform);
    }
}
