using UnityEngine;
using UdonSharp;
using System;
using CONSTANT = SAIKAvatarInfoLayoutConstants;
using FIELD = SAIKAvatarInfoFields;

[DisallowMultipleComponent]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SAIKAvatarInfo : UdonSharpBehaviour
{
    [UdonSynced]
    private Int32[] m_syncedMemory = new Int32[0];

    // Unsynced
    private bool m_updatedFlag = false;
    private int m_measurementFlag = -1;

    private const int BoneCount = CONSTANT.BoneCount;

    public bool IsReady()
    {
        return m_syncedMemory.Length > 0 && GetRawBool(FIELD.measurementsReady);
    }

    public bool IsMeasuring()
    {
        return !IsReady() && m_measurementFlag != -1;
    }

    public bool IsUpdated()
    {
        return m_updatedFlag;
    }

    public void CommitUpdate()
    {
        m_updatedFlag = false;
    }

    public override void OnDeserialization()
    {
        m_updatedFlag = true;
    }

    public bool CheckBoneExists(int bone)
    {
        return GetRawBool(BONE(bone) + FIELD.boneExists);
    }

    public Quaternion GetRestRotation(int bone)
    {
        return GetRawQuaternion(BONE(bone) + FIELD.restRotation);
    }

    public int GetParentBone(int bone)
    {
        return GetRawInt(BONE(bone) + FIELD.parentBone);
    }

    public Vector3 GetPseudoBone(int bone)
    {
        return GetRawVector3(BONE(bone) + FIELD.pseudoBone);
    }

    public Vector3 GetPseudoAxis(int bone)
    {
        return GetRawVector3(BONE(bone) + FIELD.pseudoAxis);
    }

    public float GetPseudoLength(int bone)
    {
        return GetRawFloat(BONE(bone) + FIELD.pseudoLength);
    }

    public float GetTwistDistribution(int bone)
    {
        return GetRawFloat(BONE(bone) + FIELD.twistDistribution);
    }

    public bool CheckMuscleExists(int bone, int muscle)
    {
        return CheckBoneExists(bone) && HumanTrait.MuscleFromBone(bone, muscle) != -1;
    }

    public Vector3 GetMuscleAxis(int bone, int muscle)
    {
        return GetRawVector3(BONE(bone) + MUSCLE(muscle) + FIELD.axis);
    }

    public float GetMuscleFlexPlus(int bone, int muscle)
    {
        return GetRawFloat(BONE(bone) + MUSCLE(muscle) + FIELD.flexPlus);
    }

    public float GetMuscleFlexMinus(int bone, int muscle)
    {
        return GetRawFloat(BONE(bone) + MUSCLE(muscle) + FIELD.flexMinus);
    }

    public float ComputeScaleFactor(float eyeHeight)
    {
        return eyeHeight / GetRawFloat(FIELD.measuredEyeHeight);
    }

    public int GetTrackingType()
    {
        return GetRawInt(FIELD.trackingType);
    }

    public void ResetData()
    {
        m_syncedMemory = new Int32[0];
        m_updatedFlag = false;
        m_measurementFlag = -1;
    }
    
    public int MeasureAvatar(SAIKAvatarInterface avatar)
    {
        if (IsReady())
            return -1;

        if (m_measurementFlag == -1)
        {
            SetupData();
            if (!RecordBoneHierarchy(avatar))
                return -1;

            return ++m_measurementFlag;
        }
        else if (m_measurementFlag == 0)
        {
            // Wait for tracking to be disabled.
            return ++m_measurementFlag;
        }
        else if (m_measurementFlag == 1)
        {
            RecordXFormsRest(avatar);
            return ++m_measurementFlag;
        }
        else if (m_measurementFlag < 10)
        {
            int passIndex = m_measurementFlag - 2;
            bool[] passBones = new bool[BoneCount];
            for (int i = 0; i < passBones.Length; ++i)
                passBones[i] = true;

            if (passIndex == 0 || passIndex == 1 || passIndex == 6 || passIndex == 7)
            {
                passBones[(int)HumanBodyBones.LeftLowerArm] = false;
                passBones[(int)HumanBodyBones.RightLowerArm] = false;
                passBones[(int)HumanBodyBones.LeftLowerLeg] = false;
                passBones[(int)HumanBodyBones.RightLowerLeg] = false;
                if (passIndex == 6 || passIndex == 7)
                {
                    for (int i = 0; i < passBones.Length; ++i)
                        passBones[i] = !passBones[i];
                }
            }

            int dofIndex = (passIndex >> 1) % 3;
            int flexValue = ((passIndex & 1) == 0 ? 1 : -1);
            RecordXFormsFlex(avatar, dofIndex, flexValue, passBones);
            return ++m_measurementFlag;
        }
        else if (m_measurementFlag == 10)
        {
            RecordTrackingType(avatar);
            ++m_measurementFlag;
        }

        m_syncedMemory[FIELD.measurementsReady] = 1;
        m_updatedFlag = true;

        return -1;
    }

    private void SetupData()
    {
        m_syncedMemory = new Int32[CONSTANT.SyncedMemorySize];
        m_syncedMemory[FIELD.measurementsReady] = 0;
        for (int i = 0, boneOffs = FIELD.boneInfo; i < BoneCount; ++i, boneOffs += CONSTANT.BoneInfoStride)
        {
            m_syncedMemory[boneOffs + FIELD.boneExists] = 0;
            m_syncedMemory[boneOffs + FIELD.twistDistribution] = BitConverter.SingleToInt32Bits(1.0f);
            m_syncedMemory[boneOffs + FIELD.secondaryTwistBone] = -1;
            m_syncedMemory[boneOffs + FIELD.parentBone] = -1;
        }

        m_syncedMemory[BONE(HumanBodyBones.LeftUpperArm) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.LeftLowerArm;
        m_syncedMemory[BONE(HumanBodyBones.LeftLowerArm) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.LeftHand;
        m_syncedMemory[BONE(HumanBodyBones.RightUpperArm) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.RightLowerArm;
        m_syncedMemory[BONE(HumanBodyBones.RightLowerArm) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.RightHand;
        m_syncedMemory[BONE(HumanBodyBones.LeftUpperLeg) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.LeftLowerLeg;
        m_syncedMemory[BONE(HumanBodyBones.LeftLowerLeg) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.LeftFoot;
        m_syncedMemory[BONE(HumanBodyBones.RightUpperLeg) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.RightLowerLeg;
        m_syncedMemory[BONE(HumanBodyBones.RightLowerLeg) + FIELD.secondaryTwistBone] = (int)HumanBodyBones.RightFoot;
    }

    private bool RecordBoneHierarchy(SAIKAvatarInterface avatar)
    {
        bool hasAllRequired = true;
        for (int i = 0, boneOffs = FIELD.boneInfo; i < BoneCount; ++i, boneOffs += CONSTANT.BoneInfoStride)
        {
            bool boneExists = avatar.CheckBoneExists(i);
            if (boneExists)
            {
                int parentBone = avatar.GetBoneParent(i);
                if (boneExists && parentBone != -1)
                {
                    m_syncedMemory[boneOffs + FIELD.parentBone] = parentBone;
                    m_syncedMemory[BONE(parentBone) + FIELD.numChildren]++;
                }
                m_syncedMemory[boneOffs + FIELD.boneExists] = 1;
            }
            else if (HumanTrait.RequiredBone(i))
            {
                hasAllRequired = false;
            }
        }
        m_syncedMemory[FIELD.measuredEyeHeight] = BitConverter.SingleToInt32Bits(avatar.GetEyeHeight());
        return hasAllRequired;
    }

    private void RecordXFormsRest(SAIKAvatarInterface avatar)
    {
        for (int i = 0, boneOffs = FIELD.boneInfo; i < BoneCount; ++i, boneOffs += CONSTANT.BoneInfoStride)
        {
            if (GetRawBool(boneOffs + FIELD.boneExists))
            {
                int parentBone = GetRawInt(boneOffs + FIELD.parentBone);
                bool hasParent = parentBone != -1;

                Vector3 worldPositionParent = (hasParent ? avatar.GetBonePosition(parentBone) : avatar.GetPositionRoot());
                Quaternion worldRotationParent = (hasParent ? avatar.GetBoneRotation(parentBone) : avatar.GetRotationRoot());
                Vector3 worldPosition = avatar.GetBonePosition(i);
                Quaternion worldRotation = avatar.GetBoneRotation(i);
                Quaternion worldRotationParentInv = Quaternion.Inverse(worldRotationParent);
                Quaternion localRotation = worldRotationParentInv * worldRotation;
                Vector3 offsetFromParent = worldRotationParentInv * (worldPosition - worldPositionParent);

                
                m_syncedMemory[boneOffs + FIELD.restRotation + 0] = BitConverter.SingleToInt32Bits(localRotation[0]);
                m_syncedMemory[boneOffs + FIELD.restRotation + 1] = BitConverter.SingleToInt32Bits(localRotation[1]);
                m_syncedMemory[boneOffs + FIELD.restRotation + 2] = BitConverter.SingleToInt32Bits(localRotation[2]);
                m_syncedMemory[boneOffs + FIELD.restRotation + 3] = BitConverter.SingleToInt32Bits(localRotation[3]);
                m_syncedMemory[boneOffs + FIELD.offsetFromParent + 0] = BitConverter.SingleToInt32Bits(offsetFromParent[0]);
                m_syncedMemory[boneOffs + FIELD.offsetFromParent + 1] = BitConverter.SingleToInt32Bits(offsetFromParent[1]);
                m_syncedMemory[boneOffs + FIELD.offsetFromParent + 2] = BitConverter.SingleToInt32Bits(offsetFromParent[2]);

                if (hasParent)
                {
                    int parentEIDX = BONE(parentBone);
                    Vector3 pseudoBoneSum = GetRawVector3(parentEIDX + FIELD.pseudoBone) + offsetFromParent;
                    m_syncedMemory[parentEIDX + FIELD.pseudoBone + 0] = BitConverter.SingleToInt32Bits(pseudoBoneSum[0]);
                    m_syncedMemory[parentEIDX + FIELD.pseudoBone + 1] = BitConverter.SingleToInt32Bits(pseudoBoneSum[1]);
                    m_syncedMemory[parentEIDX + FIELD.pseudoBone + 2] = BitConverter.SingleToInt32Bits(pseudoBoneSum[2]);
                }
            }
        }

        for (int i = 0, boneOffs = FIELD.boneInfo; i < BoneCount; ++i, boneOffs += CONSTANT.BoneInfoStride)
        {
            int parentBone = GetRawInt(boneOffs + FIELD.parentBone);
            int numChildren = GetRawInt(boneOffs + FIELD.numChildren);

            Vector3 pseudoAxis = Vector3.up;
            float pseudoLength = 0.1f;
            if (numChildren > 0)
            {
                Vector3 pseudoBoneSum = GetRawVector3(boneOffs + FIELD.pseudoBone);
                float pseudoBoneSumLength = pseudoBoneSum.magnitude;
                pseudoAxis = pseudoBoneSum / pseudoBoneSumLength;
                pseudoLength = pseudoBoneSumLength / numChildren;
            }
            else if (parentBone != -1)
            {
                pseudoAxis = Vector3.up;
                pseudoLength = GetRawFloat(BONE(parentBone) + FIELD.pseudoLength) / 2;
            }

            Vector3 pseudoBone = pseudoAxis * pseudoLength;
            m_syncedMemory[boneOffs + FIELD.pseudoBone + 0] = BitConverter.SingleToInt32Bits(pseudoBone[0]);
            m_syncedMemory[boneOffs + FIELD.pseudoBone + 1] = BitConverter.SingleToInt32Bits(pseudoBone[1]);
            m_syncedMemory[boneOffs + FIELD.pseudoBone + 2] = BitConverter.SingleToInt32Bits(pseudoBone[2]);
            m_syncedMemory[boneOffs + FIELD.pseudoAxis + 0] = BitConverter.SingleToInt32Bits(pseudoAxis[0]);
            m_syncedMemory[boneOffs + FIELD.pseudoAxis + 1] = BitConverter.SingleToInt32Bits(pseudoAxis[1]);
            m_syncedMemory[boneOffs + FIELD.pseudoAxis + 2] = BitConverter.SingleToInt32Bits(pseudoAxis[2]);
            m_syncedMemory[boneOffs + FIELD.pseudoLength] = BitConverter.SingleToInt32Bits(pseudoLength);
        }
    }

    private void RecordXFormsFlex(SAIKAvatarInterface avatar, int dofIndex, float flexValue, bool[] boneFlags)
    {
        int muscleOffs = MUSCLE(dofIndex);
        for (int i = 0, boneOffs = FIELD.boneInfo; i < BoneCount; ++i, boneOffs += CONSTANT.BoneInfoStride)
        {
            if (boneFlags[i] && GetRawBool(boneOffs + FIELD.boneExists) && HumanTrait.MuscleFromBone(i, dofIndex) != -1)
            {
                int parentBone = m_syncedMemory[boneOffs + FIELD.parentBone];
                bool hasParent = parentBone != -1;

                Quaternion worldRotationParent = (hasParent ? avatar.GetBoneRotation(parentBone) : avatar.GetRotationRoot());
                Quaternion worldRotationParentInv = Quaternion.Inverse(worldRotationParent);
                Quaternion rotationFlexed = worldRotationParentInv * avatar.GetBoneRotation(i);
                Quaternion rotationAtRest = GetRawQuaternion(boneOffs + FIELD.restRotation);
                Quaternion flexRotation = Quaternion.Inverse(rotationAtRest) * rotationFlexed;
                GetFlexAngleAxis(flexValue, flexRotation, out float angle, out Vector3 axis);

                if (flexValue > 0)
                {
                    int secondaryBone = GetRawInt(boneOffs + FIELD.secondaryTwistBone);
                    if (dofIndex == 0 && secondaryBone != -1)
                    {
                        Quaternion secondaryRotationFlexed = Quaternion.Inverse(avatar.GetBoneRotation(i)) * avatar.GetBoneRotation(secondaryBone);
                        Quaternion secondaryRotationAtRest = GetRawQuaternion(BONE(secondaryBone) + FIELD.restRotation);
                        Quaternion secondaryFlexRotation = Quaternion.Inverse(secondaryRotationAtRest) * secondaryRotationFlexed;
                        GetFlexAngleAxis(flexValue, secondaryFlexRotation, out float secondaryAngle, out Vector3 secondaryAxis);
                        m_syncedMemory[boneOffs + FIELD.twistDistribution] = BitConverter.SingleToInt32Bits(angle / (angle + secondaryAngle));
                    }
                }

                if (flexValue >= 0)
                    m_syncedMemory[boneOffs + muscleOffs + FIELD.flexPlus] = BitConverter.SingleToInt32Bits(angle);
                else
                    m_syncedMemory[boneOffs + muscleOffs + FIELD.flexMinus] = BitConverter.SingleToInt32Bits(angle);

                if (GetRawVector3(boneOffs + muscleOffs + FIELD.axis) == Vector3.zero)
                {
                    m_syncedMemory[boneOffs + muscleOffs + FIELD.axis + 0] = BitConverter.SingleToInt32Bits(axis[0]);
                    m_syncedMemory[boneOffs + muscleOffs + FIELD.axis + 1] = BitConverter.SingleToInt32Bits(axis[1]);
                    m_syncedMemory[boneOffs + muscleOffs + FIELD.axis + 2] = BitConverter.SingleToInt32Bits(axis[2]);
                }

                if (i == (int)HumanBodyBones.LeftUpperArm)
                {
                    Vector3 pos = avatar.GetBonePosition(i);
                    Vector3 pseudoBone = GetPseudoBone(i);
                    Vector3 recordedAxis = GetRawVector3(boneOffs + muscleOffs + FIELD.axis);
                    Quaternion restRotation = GetRawQuaternion(boneOffs + FIELD.restRotation);

                    Debug.DrawLine(pos, pos + worldRotationParent * rotationFlexed * pseudoBone, Color.green);
                    Debug.DrawLine(pos, pos + worldRotationParent * restRotation * pseudoBone, Color.grey);
                    Debug.DrawLine(pos, pos + worldRotationParent * restRotation * axis, Color.blue);
                    Debug.DrawLine(pos, pos + worldRotationParent * restRotation * recordedAxis, Color.red);
                }
            }
        }
    }

    private void RecordTrackingType(SAIKAvatarInterface avatar)
    {
        int signalBone = (int)HumanBodyBones.Head;
        Quaternion signalRotationRest = this.GetRestRotation(signalBone);
        Quaternion signalRotationParent = avatar.GetBoneRotation(avatar.GetBoneParent(signalBone));
        Quaternion signalRotationWorld = avatar.GetBoneRotation(signalBone);
        Quaternion signalRotationLocal = Quaternion.Inverse(signalRotationParent) * signalRotationWorld;
        Quaternion signalRotationFlex = Quaternion.Inverse(signalRotationRest) * signalRotationLocal;
        Vector3 signalFlex = SAIKMecanimHelper.ComputeMecanimFlexParams(this, signalBone, signalRotationFlex);

        m_syncedMemory[FIELD.trackingType] = Mathf.RoundToInt(signalFlex.z * 10);
    }

    private void GetFlexAngleAxis(float flexValue, Quaternion flexRotation, out float angle, out Vector3 axis)
    {
        float thetaOver2 = Mathf.Acos(flexRotation.w);
        float sinThetaOver2 = Mathf.Sin(thetaOver2);
        if (thetaOver2 == 0.0f)
        {
            angle = 0.0f;
            axis = Vector3.zero;
            return;
        }

        angle = Mathf.Sign(flexValue) * (thetaOver2 * 2 * Mathf.Rad2Deg);
        axis = Mathf.Sign(flexValue) * Vector3.Normalize(new Vector3(flexRotation.x, flexRotation.y, flexRotation.z) / sinThetaOver2);
    }

    public bool TryLoadFromCache(SAIKAvatarInfoCache cache)
    {
        if (IsMeasuring())
            return false;
        if (cache == null || !cache.IsValid())
            return false;

        cache.LoadFromCache(ref m_syncedMemory);
        m_updatedFlag = true;
        m_measurementFlag = -1;
        return true;
    }

    public bool TrySaveToCache(SAIKAvatarInfoCache cache)
    {
        if (!IsReady())
            return false;
        if (cache == null)
            return false;

        cache.StoreToCache(m_syncedMemory);
        return true;
    }

    #region Data Layout Accessors

    private static int BONE(int bone)
    {
        return FIELD.boneInfo + bone * CONSTANT.BoneInfoStride;
    }

    private static int BONE(HumanBodyBones bone)
    {
        return FIELD.boneInfo + (int)bone * CONSTANT.BoneInfoStride;
    }

    private static int MUSCLE(int muscle)
    {
        return FIELD.muscles + muscle * CONSTANT.MuscleInfoStride;
    }

    private bool GetRawBool(int offset)
    {
        return m_syncedMemory[offset] != 0;
    }

    private int GetRawInt(int offset)
    {
        return m_syncedMemory[offset];
    }

    private float GetRawFloat(int offset)
    {
        return BitConverter.Int32BitsToSingle(m_syncedMemory[offset]);
    }

    private Vector3 GetRawVector3(int offset)
    {
        return new Vector3(
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 0]),
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 1]),
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 2]));
    }

    private Quaternion GetRawQuaternion(int offset)
    {
        return new Quaternion(
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 0]),
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 1]),
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 2]),
            BitConverter.Int32BitsToSingle(m_syncedMemory[offset + 3]));
    }

    #endregion
}

public static class SAIKAvatarInfoLayoutConstants
{
    public const int BoneCount = 55;
    public const int BoneInfoStride = 34;
    public const int MuscleInfoStride = 5;
    public const int SyncedMemorySize = 3 + BoneCount * BoneInfoStride;
}

public static class SAIKAvatarInfoFields
{
    // SAIKAvatarInfo
    public const int measurementsReady = 0;     // bool
    public const int measuredEyeHeight = 1;     // float
    public const int trackingType = 2;          // int
    public const int boneInfo = 3;              // BoneInfo[]

    // BoneInfo
    public const int boneExists = 0;            // bool
    public const int restRotation = 1;          // Quaternion

    public const int twistDistribution = 5;     // float
    public const int secondaryTwistBone = 6;    // int

    public const int parentBone = 7;            // int
    public const int offsetFromParent = 8;      // Vector3

    public const int numChildren = 11;          // int

    public const int pseudoBone = 12;           // Vector3
    public const int pseudoAxis = 15;           // Vector3
    public const int pseudoLength = 18;         // float

    public const int muscles = 19;              // MuscleInfo[]

    // MuscleInfo
    public const int axis = 0;                  // Vector3
    public const int flexPlus = 3;              // float
    public const int flexMinus = 4;             // float
}