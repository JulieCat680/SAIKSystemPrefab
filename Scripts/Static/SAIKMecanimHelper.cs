using UnityEngine;

public static class SAIKMecanimHelper
{
    public static Vector3 ComputeMecanimFlexRotations(SAIKAvatarInfo avatarInfo, int boneIndex, Quaternion targetRotation)
    {
        // Compute the swing-twist decomposition of the targetRotation about each muscle axis.
        bool muscle0 = avatarInfo.CheckMuscleExists(boneIndex, 0);
        bool muscle1 = avatarInfo.CheckMuscleExists(boneIndex, 1);
        bool muscle2 = avatarInfo.CheckMuscleExists(boneIndex, 2);
        Vector3 axis0 = avatarInfo.GetMuscleAxis(boneIndex, 0);
        Vector3 axis1 = avatarInfo.GetMuscleAxis(boneIndex, 1);
        Vector3 axis2 = avatarInfo.GetMuscleAxis(boneIndex, 2);

        if (muscle0 && muscle1 && muscle2)
            return SwingTwistDecomposition(targetRotation, axis0, axis1, axis2);
        else if (muscle0 && muscle1)
            return SwingTwistDecomposition(targetRotation, axis0, axis1, Vector3.Cross(axis0, axis1));
        else if (muscle0 && muscle2)
            return SwingTwistDecomposition(targetRotation, axis0, Vector3.Cross(axis2, axis0), axis2);
        else if (muscle1 && muscle2)
            return SwingTwistDecomposition(targetRotation, Vector3.Cross(axis1, axis2), axis1, axis2);
        else if (muscle0)
            return SwingTwistDecomposition(targetRotation, axis0, Vector3.zero, Vector3.zero);
        else if (muscle1)
            return SwingTwistDecomposition(targetRotation, Vector3.zero, axis1, Vector3.zero);
        else if (muscle2)
            return SwingTwistDecomposition(targetRotation, Vector3.zero, Vector3.zero, axis2);

        return Vector3.zero;
    }

    public static Vector3[] ComputeMecanimFlexRotations(SAIKAvatarInfo avatarInfo, int[] humanBones, Quaternion[] rotations)
    {
        int numElems = Mathf.Min(humanBones.Length, rotations.Length);
        Vector3[] result = new Vector3[numElems];
        for (int i = 0; i < result.Length; ++i)
            result[i] = ComputeMecanimFlexRotations(avatarInfo, humanBones[i], rotations[i]);
        return result;
    }

    public static Vector3 ComputeMecanimFlexParams(SAIKAvatarInfo avatarInfo, int boneIndex, Vector3 rotations)
    {
        Vector3 flexParams = Vector3.zero;
        for (int i = 0; i < 3; ++i)
        {
            if (avatarInfo.CheckMuscleExists(boneIndex, i))
            {
                float param = 0.0f;
                float angle = rotations[i];
                if (i == 0)
                    angle *= avatarInfo.GetTwistDistribution(boneIndex);
                if (angle > 0.0f)
                    param = angle / avatarInfo.GetMuscleFlexPlus(boneIndex, i);
                else if (angle < 0.0f)
                    param = angle / Mathf.Abs(avatarInfo.GetMuscleFlexMinus(boneIndex, i));
                flexParams[i] = param;
            }
        }
        return flexParams;
    }

    public static Vector3[] ComputeMecanimFlexParams(SAIKAvatarInfo avatarInfo, int[] humanBones, Vector3[] rotations)
    {
        int numElems = Mathf.Min(humanBones.Length, rotations.Length);
        Vector3[] result = new Vector3[numElems];
        for (int i = 0; i < result.Length; ++i)
            result[i] = ComputeMecanimFlexParams(avatarInfo, humanBones[i], rotations[i]);

        return result;
    }

    public static Vector3 ComputeMecanimFlexParams(SAIKAvatarInfo avatarInfo, int boneIndex, Quaternion rotation)
    {
        Vector3 flexRotations = ComputeMecanimFlexRotations(avatarInfo, boneIndex, rotation);
        return ComputeMecanimFlexParams(avatarInfo, boneIndex, flexRotations);
    }

    public static Vector3[] ComputeMecanimFlexParams(SAIKAvatarInfo avatarInfo, int[] humanBones, Quaternion[] rotations)
    {
        Vector3[] flexRotations = ComputeMecanimFlexRotations(avatarInfo, humanBones, rotations);
        return ComputeMecanimFlexParams(avatarInfo, humanBones, flexRotations);
    }

    public static bool CheckMecanimFlexLimits(Vector3 flexParams, float limit)
    {
        return Mathf.Abs(flexParams.x) <= limit && Mathf.Abs(flexParams.y) <= limit && Mathf.Abs(flexParams.z) <= limit;
    }

    public static bool CheckMecanimFlexLimits(Vector3[] flexParams, float limit)
    {
        foreach (Vector3 flexVector in flexParams)
            if (!CheckMecanimFlexLimits(flexVector, limit))
                return false;
        return true;
    }

    public static Vector3 SwingTwistDecomposition(Quaternion quat, Vector3 X, Vector3 Y, Vector3 Z)
    {
        Vector3 qaxis = new Vector3(quat.x, quat.y, quat.z);
        float qx = Vector3.Dot(qaxis, X);
        float qy = Vector3.Dot(qaxis, Y);
        float qz = Vector3.Dot(qaxis, Z);
        float qw = quat.w;

        float x = 2.0f * Mathf.Atan(qx / qw) * Mathf.Rad2Deg;
        float y = (Mathf.Atan2((qy * qw + qz * qx), (qx * qx + qw * qw)) * 2) * Mathf.Rad2Deg;
        float z = (Mathf.Atan2((qz * qw - qy * qx), (qx * qx + qw * qw)) * 2) * Mathf.Rad2Deg;
        return new Vector3(x, y, z);
    }
}
