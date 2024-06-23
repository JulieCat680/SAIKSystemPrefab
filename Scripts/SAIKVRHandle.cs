using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[RequireComponent(typeof(VRC_Pickup))]
public class SAIKVRHandle : UdonSharpBehaviour
{
    public VRC_Pickup Pickup { get; private set; }
    public Collider Collider { get; private set; }
    
    private UdonSharpBehaviour m_callbackTarget = null;
    private string m_onPickup = null;
    private string m_onDrop = null;

    public void Init(UdonSharpBehaviour target, string onPickup, string onDrop)
    {
        m_callbackTarget = target;
        m_onPickup = onPickup;
        m_onDrop = onDrop;
        Pickup = GetComponent<VRC_Pickup>();
        Collider = GetComponent<Collider>();
    }

    public void SetActive()
    {
        Pickup.pickupable = true;
        Collider.enabled = true;
    }

    public void SetInactive()
    {
        Pickup.pickupable = false;
        Collider.enabled = false;
    }

    public override void OnPickup()
    {
        if (m_callbackTarget != null)
            m_callbackTarget.SendCustomEvent(m_onPickup);
    }

    public override void OnDrop()
    {
        if (m_callbackTarget != null)
            m_callbackTarget.SendCustomEvent(m_onDrop);
    }
}
