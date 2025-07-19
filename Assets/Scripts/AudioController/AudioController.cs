using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{

    public AudioSource backgroundMusicSource;
    public AudioSource effectMusicSource;

    public AudioClip Instance;

  


    // Start is called before the first frame update
    void Start()
    {
        backgroundMusicSource.Play();    
        backgroundMusicSource.loop = true;  
    }

  
}
