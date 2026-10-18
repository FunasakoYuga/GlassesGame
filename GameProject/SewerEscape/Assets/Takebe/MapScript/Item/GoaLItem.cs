using UnityEngine;

public class GoaLItem : MonoBehaviour
{
    // 存在するか
    bool isExist = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            isExist = false;
            Debug.Log("クリア！");
        //    Destroy(gameObject);
        }
    }

    /// <summary>
    /// アイテムが存在しているかどうかを返す
    /// </summary>
    /// <param name="isExist"></param>
    /// <returns>true:存在している　false:存在していない</returns>
    public bool IsExist()
    {
        return isExist;
    }
}
