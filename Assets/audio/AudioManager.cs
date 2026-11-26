
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Background Audio")]
    [SerializeField] private AudioSource backgroundAudio;

    [Header("Audio Trimming")]
    [SerializeField] private float startTime = 0f;
    [SerializeField] private float endTime = 0f;

    [System.Serializable]
    public class SoundEffect
    {
        public AudioClip clip;
        public float startTime = 0f;
        public float endTime = 0f;

        [Range(0f, 1f)]
        public float volume = 0.5f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [Header("Movement Sounds")]
    public SoundEffect footsteps = new SoundEffect();
    public SoundEffect jump = new SoundEffect();
    public SoundEffect landing = new SoundEffect();

    [Header("Attack Sounds")]
    public SoundEffect staffSwing = new SoundEffect();
    public SoundEffect lanternShine = new SoundEffect();

    [Header("Collectible Sounds")]
    public SoundEffect pyrkaCollect = new SoundEffect();

    [Header("Shrine Sounds")]
    public SoundEffect shrineUnlock = new SoundEffect();

    [Header("Player Health Sounds")]
    public SoundEffect hurt = new SoundEffect();
    public SoundEffect death = new SoundEffect();

    [Header("Monster Sounds")]
    public SoundEffect monster1 = new SoundEffect();
    public SoundEffect monster2 = new SoundEffect();

    [Header("Victory Sound")]
    public SoundEffect victory = new SoundEffect();

    public bool SFXMuted { get; private set; }
    public bool AmbienceMuted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (backgroundAudio == null)
            backgroundAudio = GetComponent<AudioSource>();

        backgroundAudio.playOnAwake = false;
        backgroundAudio.loop = false;

        AmbienceMuted =
            PlayerPrefs.GetInt("AmbienceMuted", 0) == 1;

        SFXMuted =
            PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        backgroundAudio.mute = AmbienceMuted;
    }

    private void Start()
    {
        PlayBackground();
    }

    private void Update()
    {
        if (backgroundAudio.clip == null)
            return;

        float stopTime = endTime > startTime
            ? Mathf.Min(endTime, backgroundAudio.clip.length)
            : backgroundAudio.clip.length;

        if (!backgroundAudio.isPlaying ||
            backgroundAudio.time >= stopTime)
        {
            PlayBackground();
        }
    }

    private void PlayBackground()
    {
        if (backgroundAudio.clip == null)
            return;

        backgroundAudio.time = Mathf.Clamp(
            startTime, 0f,
            Mathf.Max(0f, backgroundAudio.clip.length - 0.01f)
        );

        backgroundAudio.Play();
    }

    // MOVEMENT

    public void PlayFootsteps()
    {
        PlaySound(footsteps);
    }

    public void PlayJump()
    {
        PlaySound(jump);
    }

    public void PlayLanding()
    {
        PlaySound(landing);
    }

    // COMBAT

    public void PlayStaffSwing()
    {
        PlaySound(staffSwing);
    }

    public void PlayLanternShine()
    {
        PlaySound(lanternShine);
    }

    // COLLECTIBLES AND SHRINES

    public void PlayPyrkaCollect()
    {
        PlaySound(pyrkaCollect);
    }

    public void PlayShrineUnlock()
    {
        PlaySound(shrineUnlock);
    }

    // HEALTH

    public void PlayHurt()
    {
        PlaySound(hurt);
    }

    public void PlayDeath()
    {
        PlaySound(death);
    }

    // MONSTERS

    public void PlayMonster1()
    {
        PlaySound(monster1);
    }

    public void PlayMonster2()
    {
        PlaySound(monster2);
    }

    // VICTORY

    public void PlayVictory()
    {
        PlaySound(victory);
    }

    // SOUND PLAYBACK

    public void PlaySound(SoundEffect sound)
    {
        if (sound == null || sound.clip == null || SFXMuted)
            return;

        StartCoroutine(PlayTrimmedSound(sound));
    }

    private IEnumerator PlayTrimmedSound(SoundEffect sound)
    {
        GameObject obj = new GameObject("SoundEffect");
        obj.transform.SetParent(transform);

        AudioSource source = obj.AddComponent<AudioSource>();

        source.clip = sound.clip;
        source.volume = sound.volume;
        source.pitch = Mathf.Clamp(sound.pitch, 0.1f, 3f);
        source.spatialBlend = 0f;
        source.loop = false;
        source.playOnAwake = false;
        source.mute = SFXMuted;

        float start = Mathf.Clamp(
            sound.startTime, 0f,
            Mathf.Max(0f, sound.clip.length - 0.01f)
        );

        float end = sound.endTime > start
            ? Mathf.Min(sound.endTime, sound.clip.length)
            : sound.clip.length;

        source.time = start;
        source.Play();

        float duration = (end - start) / source.pitch;

        yield return new WaitForSecondsRealtime(duration);

        source.Stop();
        Destroy(obj);
    }

    // AUDIO SETTINGS

    public void ToggleAmbience()
    {
        SetAmbienceMuted(!AmbienceMuted);
    }

    public void ToggleSFX()
    {
        SetSFXMuted(!SFXMuted);
    }

    public void SetAmbienceMuted(bool muted)
    {
        AmbienceMuted = muted;
        backgroundAudio.mute = muted;

        PlayerPrefs.SetInt("AmbienceMuted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFXMuted(bool muted)
    {
        SFXMuted = muted;

        foreach (AudioSource source in
                 GetComponentsInChildren<AudioSource>())
        {
            if (source != backgroundAudio)
                source.mute = muted;
        }

        PlayerPrefs.SetInt("SFXMuted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetAmbienceVolume(float volume)
    {
        backgroundAudio.volume = Mathf.Clamp01(volume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
