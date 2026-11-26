
using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource footsteps;
    [SerializeField] private Animator anim;

    [Header("Audio Trimming")]
    [SerializeField] private float startTime = 0f;
    [SerializeField] private float endTime = 0f;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (footsteps == null)
            footsteps = GetComponent<AudioSource>();

        footsteps.playOnAwake = false;
        footsteps.loop = false;
    }

    private void Update()
    {
        if (footsteps.clip == null)
            return;

        bool muted =
            AudioManager.Instance != null &&
            AudioManager.Instance.SFXMuted;

        bool isRunning =
            anim.GetCurrentAnimatorStateInfo(0)
                .IsName("Base Layer.fern_run") &&
            anim.GetBool("IsGrounded") &&
            Time.timeScale > 0f &&
            !muted;

        if (!isRunning)
        {
            if (footsteps.isPlaying)
                footsteps.Stop();

            return;
        }

        float stopTime = endTime > startTime
            ? Mathf.Min(endTime, footsteps.clip.length)
            : footsteps.clip.length;

        if (!footsteps.isPlaying ||
            footsteps.time >= stopTime)
        {
            footsteps.time = Mathf.Clamp(
                startTime,
                0f,
                Mathf.Max(
                    0f,
                    footsteps.clip.length - 0.01f
                )
            );

            footsteps.Play();
        }
    }

    private void OnDisable()
    {
        if (footsteps != null)
            footsteps.Stop();
    }
}
