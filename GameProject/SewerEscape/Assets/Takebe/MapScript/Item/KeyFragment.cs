using UnityEngine;

public class KeyFragment : MonoBehaviour
{
    // 存在するか
    bool isExist = true;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      // audioSource = GameObject.Find("AudioSouce").GetComponentInChildren<AudioSource>();    // 子のAudioSourceを取得
      // audioSource = GetComponentInChildren<AudioSource>();    // 子のAudioSourceを取得
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // 当たったら消える
        if(other.gameObject.tag == "Player")
        {
            isExist = false;
            audioSource.PlayOneShot(clip);
        //    audioSource.transform.parent = null;    // Keyから切り離す
            Destroy(audioSource.gameObject, 3); // 子(audioSource)も1秒後に消す

            Destroy(gameObject);    // 自身を消す
        }
    }
}
