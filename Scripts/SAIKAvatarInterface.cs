using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using System;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAIKAvatarInterface : UdonSharpBehaviour
{
    public Animator Animator { get => m_animator; }
    public VRCPlayerApi Player { get => m_player; }
    private Animator m_animator = null;
    private VRCPlayerApi m_player = null;

    public void Init(VRCPlayerApi player)
    {
        m_player = player;
        m_animator = null;
    }

    public void Init(Animator animator)
    {
        m_player = null;
        m_animator = animator;
    }

    public void Clear()
    {
        m_animator = null;
        m_player = null;
    }

    public bool IsNull()
    {
        return m_animator == null && m_player == null;
    }

    public bool IsPlayer()
    {
        return m_player != null;
    }

    public bool IsVRPlayer()
    {
        return (m_player != null && m_player.IsUserInVR());
    }

    public bool IsDesktopPlayer()
    {
        return (m_player != null && !m_player.IsUserInVR());
    }

    public bool IsLocalPlayer()
    {
        return (m_player != null && m_player.isLocal);
    }

    public bool IsAnimator()
    {
        return m_animator != null;
    }

    public Vector3 GetPositionRoot()
    {
        return (IsAnimator() ? m_animator.transform.position : m_player.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position);
    }

    public Quaternion GetRotationRoot()
    {
        return (IsAnimator() ? m_animator.transform.rotation : m_player.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation);
    }

    public Vector3 GetBonePosition(HumanBodyBones bone)
    {
        return (IsAnimator() ? m_animator.GetBoneTransform(bone).position : m_player.GetBonePosition(bone));
    }

    public Vector3 GetBonePosition(int bone)
    {
        return GetBonePosition((HumanBodyBones)bone);
    }

    public Quaternion GetBoneRotation(HumanBodyBones bone)
    {
        return (IsAnimator() ? m_animator.GetBoneTransform(bone).rotation : m_player.GetBoneRotation(bone));
    }

    public Quaternion GetBoneRotation(int bone)
    {
        return GetBoneRotation((HumanBodyBones)bone);
    }

    public Vector3 GetVelocity()
    {
        return (IsAnimator() ? Vector3.zero : m_player.GetVelocity());
    }

    public void ZeroVelocity()
    {
        if (IsAnimator())
        {
            m_animator.SetFloat("VelocityX", 0.0f);
            m_animator.SetFloat("VelocityY", 0.0f);
            m_animator.SetFloat("VelocityZ", 0.0f);
            m_animator.SetFloat("VelocityMagnitude", 0.0f);
        }
        else
        {
            m_player.SetVelocity(Vector3.zero);
        }
    }

    public void SetVelocityPlayer(Vector3 worldVelocity)
    {
        m_player.SetVelocity(worldVelocity);
    }

    public void SetVelocityAnimator(Vector3 localVelocity)
    {
        m_animator.SetFloat("VelocityX", localVelocity.x);
        m_animator.SetFloat("VelocityY", localVelocity.y);
        m_animator.SetFloat("VelocityZ", localVelocity.z);
        m_animator.SetFloat("VelocityMagnitude", localVelocity.magnitude);
    }

    public bool CheckBoneExists(HumanBodyBones bone)
    {
        return (IsAnimator() ? m_animator.GetBoneTransform(bone) != null : m_player.GetBonePosition(bone) != Vector3.zero);
    }

    public bool CheckBoneExists(int bone)
    {
        return CheckBoneExists((HumanBodyBones)bone);
    }

    public int GetBoneParent(HumanBodyBones bone)
    {
        int parentIndex = HumanTrait.GetParentBone((int)bone);
        while (parentIndex != -1 && !CheckBoneExists((HumanBodyBones)parentIndex))
        {
            parentIndex = HumanTrait.GetParentBone(parentIndex);
        }
        return parentIndex;
    }

    public int GetBoneParent(int bone)
    {
        return GetBoneParent((HumanBodyBones)bone);
    }

    public float GetEyeHeight()
    {
        if (IsAnimator())
            return m_animator.transform.lossyScale.y;
        if (IsPlayer())
            return m_player.GetAvatarEyeHeightAsMeters(); 

        return 1.0f;
    }

    public Matrix4x4 ComputeTransformFrame()
    {
        return Matrix4x4.TRS(GetPositionRoot(), GetRotationRoot(), Vector3.one);
    }
}