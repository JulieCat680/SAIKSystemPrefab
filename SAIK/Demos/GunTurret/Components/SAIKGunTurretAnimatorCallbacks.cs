using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAIKGunTurretAnimatorCallbacks : UdonSharpBehaviour
{
    private UdonSharpBehaviour m_owner = null;
    private string m_callback = null;

    public void Init(UdonSharpBehaviour owner, string callback)
    {
        m_owner = owner;
        m_callback = callback;
    }

    public void _PlayGunFire()
    {
        if (m_owner != null && !string.IsNullOrWhiteSpace(m_callback))
            m_owner.SendCustomEvent(m_callback);
    }
}
