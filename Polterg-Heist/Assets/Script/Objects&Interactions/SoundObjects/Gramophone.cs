using UnityEngine;
public class Gramophone : SoundDetection, IPossessable, IResetObject, IResetInitialState
{
    // Extends SoundDetection for the gramophone object.
    // When possessed by the player, plays looping music and notifies nearby NPCs.
    // The sound stops when ResetObject or ResetInitialState is called.

    [SerializeField] public AK.Wwise.Event musicLooping;    // Wwise looping music event played when active

    private bool isPlaying;                                 // True while the gramophone is actively playing music


    Animator gramophoneAnim;

    protected void Start()
    {
        objectType = SoundEmittingObject.SoundObject;
        isPlaying = false;
        gramophoneAnim = this.transform.GetChild(0).GetComponent<Animator>();
    }

    // Starts playing music and notifies nearby NPCs when possessed.
    // Does nothing if already playing.
    public void OnPossessed()
    {
        if (!isPlaying)
        {
            gramophoneAnim.SetBool("isPlaying", true);
            musicLooping.Post(gameObject);
            isPlaying = true;

            NotifyNearbyEnemies();
        }
    }

    public void OnDepossessed()
    {
        // No behavior on depossession — music continues until explicitly stopped by an NPC
    }

    // Stops the music and resets the animation.
    // Called by NPCInvestigationController after the investigation ends.
    public void ResetObject()
    {
        musicLooping.Stop(gameObject);
        isPlaying = false;
        gramophoneAnim.SetBool("isPlaying", false);
    }

    // Full reset and delegates to ResetObject.
    public void ResetInitialState()
    {
        ResetObject();
    }
}
