using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using System;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAIKAvatarInfoCache : UdonSharpBehaviour
{
    private Int32[] m_cachedMemory = null;

    public bool IsValid()
    {
        return m_cachedMemory != null
            && m_cachedMemory.Length == SAIKAvatarInfoLayoutConstants.SyncedMemorySize
            && m_cachedMemory[SAIKAvatarInfoFields.measurementsReady] != 0;
    }

    public void StoreToCache(Int32[] memory)
    {
        m_cachedMemory = new Int32[memory.Length];
        Array.Copy(memory, m_cachedMemory, memory.Length);
    }

    public void LoadFromCache(ref Int32[] memory)
    {
        memory = new Int32[m_cachedMemory.Length];
        Array.Copy(m_cachedMemory, memory, m_cachedMemory.Length);
    }

    public override void OnAvatarChanged(VRCPlayerApi player)
    {
        if (player.isLocal)
            m_cachedMemory = null;
    }
}


