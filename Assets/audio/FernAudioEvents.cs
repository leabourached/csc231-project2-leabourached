
using UnityEngine;

public class FernAudioEvents : MonoBehaviour
{
    public void StaffSwingSound()
    {
        AudioManager.Instance?.PlayStaffSwing();
    }

    public void LanternShineSound()
    {
        AudioManager.Instance?.PlayLanternShine();
    }
}
