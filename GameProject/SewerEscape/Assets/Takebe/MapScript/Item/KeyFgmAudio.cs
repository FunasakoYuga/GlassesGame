using UnityEngine;

public class KeyFgmAudio : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip clip;

    GameObject keyFgmObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
        keyFgmObj = GameObject.FindWithTag("KeyFragment");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
