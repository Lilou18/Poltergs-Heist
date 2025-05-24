using UnityEngine;
using UnityEngine.Rendering;

public class JukeBox : SoundDetection, IPossessable, IResetObject, IResetInitialState
{
    private PossessionManager possessionManager;
    private bool isPlaying;
    [SerializeField] public AK.Wwise.Event musicLooping;

    Animator jukeboxAnim;

    protected override void Start()
    {
        base.Start();
        objectType = SoundEmittingObject.SoundObject;
        isPlaying = false;
        possessionManager = GetComponent<PossessionManager>();
        jukeboxAnim = this.transform.GetChild(0).GetComponent<Animator>();
    }

    private void PlaySoundOnRepeat()
    {
        audioSource.Play();
        audioSource.loop = true;
        musicLooping.Post(gameObject);
        isPlaying = true;
    }
    public void OnDepossessed()
    {
        // No behaviour expected
    }

    public void OnPossessed()
    {
        if (!isPlaying)
        {
            jukeboxAnim.SetBool("isPlaying", true);
            PlaySoundOnRepeat();
            NotifyNearbyEnemies(this);
        }
    }

    // Stop the sound
    public void ResetObject()
    {
        audioSource.Stop();
        musicLooping.Stop(gameObject);
        isPlaying = false;
        jukeboxAnim.SetBool("isPlaying", false);
    }

    public void ResetInitialState()
    {
        ResetObject();
    }

    //public void StopSound()
    //{
    //    audioSource.Stop();
    //    isPlaying = false;
    //}
}
