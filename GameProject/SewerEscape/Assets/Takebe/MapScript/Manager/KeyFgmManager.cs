using UnityEngine;

/// <summary>
/// 鍵のかけらの個数を監視する
/// クリアチェックをする
/// </summary>
public class KeyFgmManager : MonoBehaviour
{
    [SerializeField] int totalKeyFgmNum = 209; // マップ全体に配置する数
    int keyFgmCount;
    [SerializeField] bool isClearReach;
    //[SerializeField] GameObject keyFgm;
    //KeyFragment keyFgmObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
    //    isClearReach = false;
        keyFgmCount = totalKeyFgmNum;
    }

    // Update is called once per frame
    void Update()
    {
        if (keyFgmCount <= 0)
        {
            isClearReach = true;
        }
        // シーン内の指定オブジェクトの数を取得
        keyFgmCount = GameObject.FindGameObjectsWithTag("KeyFragment").Length;

    //    Debug.Log("keyFgmNum" + keyFgmCount);
    //    Debug.Log("isGameEnd" + isGameClear);
    }

    /// <summary>
    /// 鍵のかけらのオブジェクトがなくなったときのクリア可能状態フラグ
    /// テキスト表示に使って欲しい
    /// </summary>
    /// <returns>true:クリアリーチ false: ゲーム中</returns>
    public bool IsClearReach(){ return isClearReach;}
    /// <summary>
    /// 鍵のかけらの残り個数を取得
    /// テキスト表示に使って欲しい
    /// </summary>
    /// <returns></returns>
    public int GetKeyFgmNum() {  return keyFgmCount; }

    /// <summary>
    /// キーアイテムの全体の数を取得
    /// </summary>
    /// <returns>キーアイテムの全体の数</returns>
    public int GetTotalKeyFgmNum() { return totalKeyFgmNum; }
}
