using UnityEngine;

/// <summary>
/// 鍵のかけらの個数を監視する
/// クリアチェックをする
/// </summary>
public class KeyFgmManager : MonoBehaviour
{

    int keyFgmCount;
    bool isGameClear;
    //[SerializeField] GameObject keyFgm;
    //KeyFragment keyFgmObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        isGameClear = false;
        keyFgmCount = GameObject.FindGameObjectsWithTag("KeyFragment").Length;
    }

    // Update is called once per frame
    void Update()
    {
        if (keyFgmCount <= 0)
        {
            isGameClear = true;
        }
        keyFgmCount = GameObject.FindGameObjectsWithTag("KeyFragment").Length;

        Debug.Log("keyFgmNum" + keyFgmCount);
        Debug.Log("isGameEnd" + isGameClear);
    }

    /// <summary>
    /// 鍵のかけらのオブジェクトがなくなったときのクリアフラグ
    /// テキスト表示に使って欲しい
    /// </summary>
    /// <returns>true:クリア false: ゲーム中</returns>
    public bool IsGameClear(){ return isGameClear;}
    /// <summary>
    /// 鍵のかけらの残り個数を取得
    /// テキスト表示に使って欲しい
    /// </summary>
    /// <returns></returns>
    public int GetKeyFgmNum() {  return keyFgmCount; }
}
