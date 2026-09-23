using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private bool isDead = false;

    // isTrigger専用
    private void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    // 衝突判定
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        CheckHit(hit.gameObject);
    }

    // 共通の当たり判定チェック
    private void CheckHit(GameObject target)
    {
        if (isDead) return;

        Debug.Log("当たった相手のオブジェクト名: " + target.name);

        // 相手自身、または親オブジェクトのタグが "Enemy" か判定
        if (target.CompareTag("Enemy") || target.transform.root.CompareTag("Enemy"))
        {
            Die();
        }
    }

    // 死亡判定
    private void Die()
    {
        isDead = true;
        Debug.Log("プレイヤー死亡");
        Destroy(gameObject);
    }
}