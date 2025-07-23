using UnityEngine;

public class AudioController
{
    private AudioSource musicSource;

    public AudioController(AudioSource source)
    {
        musicSource = source;
        musicSource.loop = true;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip != clip)
            musicSource.clip = clip;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Stop();
    }
}
