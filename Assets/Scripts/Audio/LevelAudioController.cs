using UnityEngine;

public class LevelAudioController : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.SetMusicState(AudioManager.MusicState.Ambient);
    }
}
