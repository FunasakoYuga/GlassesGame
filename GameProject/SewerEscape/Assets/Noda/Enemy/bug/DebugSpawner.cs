using UnityEngine;

public class DebugSpawner : MonoBehaviour
{
    [Header("スポーン設定")]
    [SerializeField] private GameObject prefabToSpawn; // 生成したいプレハブ（プレイヤーや敵など）
    [SerializeField] private KeyCode spawnKey = KeyCode.P; // スポーンさせるキー（初期値: Pキー）

    [Header("スポーン位置")]
    [SerializeField] private Transform spawnPoint; // スポーンさせる場所（空のGameObjectなどを指定）

    void Update()
    {
        // 指定したキーが押されたらスポーン実行
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[DebugSpawner] プレハブがセットされていません！");
            return;
        }

        // スポーン位置と回転の決定
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }

        // オブジェクトを生成
        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        Debug.Log($"[Debug] {spawnedObj.name} をスポーンさせました。");
    }

    // Sceneビュー上でスポーン位置を黄色い球体で可視化
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, 0.5f);
    }
}