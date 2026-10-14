using UnityEngine;

public class KeyFragment : MonoBehaviour
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
        if(other.gameObject.tag == "Player")
        {
            isExist = false;
            Destroy(gameObject);
        }
    }
}
