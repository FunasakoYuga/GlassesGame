using UnityEngine;

public class KeyFgmGenerator : MonoBehaviour
{
    [SerializeField] const int keyFgmNum = 40;
    GameObject keyFgm;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keyFgm = GameObject.FindGameObjectWithTag("KeyFragment");
       // for (int i = 0;)
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
