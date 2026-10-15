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
    MeshFilter meshFilter;
    // ゴールアイテムを生成したか
    bool isGoalGenerate;

    [SerializeField] float height;   // 道の上に浮かせる高さ

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keyManager = GameObject.Find("KeyFgmManager").GetComponent<KeyFgmManager>();
        meshFilter = goalitemPre.GetComponent<MeshFilter>();
        isGoalGenerate = false;

        // 生成位置の調整
        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;   // メッシュ本来のサイズ
        Vector3 goalMeshSize = Vector3.Scale(meshSize, goalitemPre.transform.lossyScale);
        height = goalMeshSize.z / 2f;
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
        Instantiate(goalitemPre, new Vector3(lobby.position.x, lobby.position.y + height, lobby.position.z), Quaternion.Euler(-90f, 0f, 0f), transform);
    }
}
