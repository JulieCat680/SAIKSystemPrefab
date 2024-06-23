using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
public class SAIKStationFrame : UdonSharpBehaviour
{
    public VRCStation Station { get; private set; }
    public VRCPlayerApi Occupant { get; private set; }
    public Transform ControlFrame { get => Station.stationEnterPlayerLocation ?? this.transform; }

    public bool m_immobilizeView = false;

    private UdonSharpBehaviour m_callbackTarget = null;
    private string m_onStationEntered = null;
    private string m_onStationExited = null;

    public void Init(UdonSharpBehaviour target, string onStationEntered, string onStationExited)
    {
        m_callbackTarget = target;
        m_onStationEntered = onStationEntered;
        m_onStationExited = onStationExited;
        Station = GetComponent<VRCStation>();
    }

    public void UseStation(VRCPlayerApi player)
    {
        if (player.isLocal && !player.IsUserInVR())
            Station.PlayerMobility = (m_immobilizeView ? VRCStation.Mobility.Mobile : VRCStation.Mobility.Immobilize);

        Station.UseStation(player);
    }

    public void EjectFromStation()
    {
        if (Occupant != null && Occupant.IsValid() && Occupant.isLocal)
            Station.ExitStation(Occupant);
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        Occupant = player;

        if (player.isLocal && !player.IsUserInVR())
        {
            // For desktop players, this will lock the horizontal look movement of the player when in the station.
            // This is undocumented behavior and may break sometime down the road. It also spams the logs a lot when you do this...
            Station.PlayerMobility = VRCStation.Mobility.Immobilize;
            if (player != null)
                player.Immobilize(true);
        }

        if (m_callbackTarget != null && m_onStationEntered != null)
            m_callbackTarget.SendCustomEvent(m_onStationEntered);
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        Occupant = null;

        if (m_callbackTarget != null && m_onStationExited != null)
            m_callbackTarget.SendCustomEvent(m_onStationExited);
    }
}

