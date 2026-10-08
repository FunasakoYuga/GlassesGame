using UnityEngine;

public class KeyFgmGenerator : MonoBehaviour
{
    [SerializeField] const int keyFgmNum = 40;
    [SerializeField] GameObject keyFgmPre;  // プレハブをインスペクタで設定
    int count = 40; // 設置する数
    float interval = 2.0f;  // 設置間隔
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(i * interval, 0, 0);
            Instantiate(keyFgmPre, pos, Quaternion.identity);
        }

       // for (int i = 0;)
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
