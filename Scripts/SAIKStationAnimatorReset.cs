using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
class SAIKStationAnimatorReset : UdonSharpBehaviour
{
    private VRCPlayerApi m_occupant = null;

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (player == m_occupant)
            SendCustomEventDelayedFrames(nameof(ExitStation), 1);
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player == m_occupant)
            m_occupant = null;
    }

    public void Apply(VRCPlayerApi player)
    {
        if (!player.isLocal)
            return;
        m_occupant = player;
        SendCustomEventDelayedFrames(nameof(EnterStation), 1);
    }

    public void EnterStation()
    {
        transform.position = m_occupant.GetPosition();
        transform.rotation = m_occupant.GetRotation();
        GetComponent<VRCStation>().UseStation(m_occupant);
    }

    public void ExitStation()
    {
        GetComponent<VRCStation>().ExitStation(m_occupant);
    }
}
