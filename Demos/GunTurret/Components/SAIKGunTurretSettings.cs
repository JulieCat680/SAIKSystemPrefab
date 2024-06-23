using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAIKGunTurretSettings : UdonSharpBehaviour
{
    public Slider m_hapticStrengthSlider = null;

    public float m_hapticStrength = 1.0f;

    private void Start()
    {
        if (m_hapticStrengthSlider != null)
            m_hapticStrengthSlider.SetValueWithoutNotify(m_hapticStrength);
    }

    public void HapticStrengthSliderChanged()
    {
        float oldValue = m_hapticStrength;
        m_hapticStrength = m_hapticStrengthSlider.value;

        if (Mathf.FloorToInt(m_hapticStrength / 0.1f) != Mathf.FloorToInt(oldValue / 0.1f))
        {
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.25f, m_hapticStrength, 200.0f);
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, m_hapticStrength, 200.0f);
        }
    }
}
