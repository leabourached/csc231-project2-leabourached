
using UnityEngine;
using UnityEngine.UI;

public class AudioMuteButtons : MonoBehaviour
{
    [Header("Ambience Button")]
    [SerializeField] private Image ambienceIcon;
    [SerializeField] private Sprite ambienceOn;
    [SerializeField] private Sprite ambienceOff;

    [Header("SFX Button")]
    [SerializeField] private Image sfxIcon;
    [SerializeField] private Sprite sfxOn;
    [SerializeField] private Sprite sfxOff;

    private void OnEnable()
    {
        RefreshIcons();
    }

    public void ToggleAmbience()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ToggleAmbience();

        RefreshIcons();
    }

    public void ToggleSFX()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ToggleSFX();

        RefreshIcons();
    }

    private void RefreshIcons()
    {
        if (AudioManager.Instance == null)
            return;

        if (ambienceIcon != null)
        {
            ambienceIcon.sprite =
                AudioManager.Instance.AmbienceMuted
                ? ambienceOff
                : ambienceOn;
        }

        if (sfxIcon != null)
        {
            sfxIcon.sprite =
                AudioManager.Instance.SFXMuted
                ? sfxOff
                : sfxOn;
        }
    }
}
