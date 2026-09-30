using UnityEngine;

public class KeyFgmManager : MonoBehaviour
{

    int keyFgmNum;
    bool isGameClear;
    //[SerializeField] GameObject keyFgm;
    //KeyFragment keyFgmObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        isGameClear = false;
        keyFgmNum = GameObject.FindGameObjectsWithTag("KeyFragment").Length;
    }

    // Update is called once per frame
    void Update()
    {
        if (keyFgmNum <= 0)
        {
            isGameClear = true;
        }
        keyFgmNum = GameObject.FindGameObjectsWithTag("KeyFragment").Length;

        Debug.Log("keyFgmNum" + keyFgmNum);
        Debug.Log("isGameEnd" + isGameClear);
    }

    /// <summary>
    /// 鍵のかけらのオブジェクトがなくなったときのクリアフラグ
    /// </summary>
    /// <returns>true:クリア false: ゲーム中</returns>
    public bool IsGameClear(){ return isGameClear;}
    /// <summary>
    /// 鍵のかけらの残り個数を取得
    /// </summary>
    /// <returns></returns>
    public int GetKeyFgmNum() {  return keyFgmNum; }
}
