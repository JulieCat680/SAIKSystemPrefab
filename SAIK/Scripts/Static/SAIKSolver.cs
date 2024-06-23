using UnityEngine;

public static class SAIKSolver
{
    public static Quaternion[] SolveArm2Chain(SAIKCoreController controller, int[] humanBones, Transform endPoint)
    {
        return SolveArm2Chain(controller, humanBones, endPoint.position, endPoint.up);
    }

    public static Quaternion[] SolveArm2Chain(SAIKCoreController controller, int[] humanBones, Vector3 endPoint, Vector3 endPointUp)
    {
        SAIKAvatarInterface avatar = controller.Avatar;
        SAIKAvatarInfo avatarInfo = controller.AvatarInfo;

        Matrix4x4 solverFrame = controller.ComputeSolverFrame();
        Matrix4x4 avatarFrame = controller.Avatar.ComputeTransformFrame();
        Matrix4x4 solverToAvatarFrame = avatarFrame * Matrix4x4.Inverse(solverFrame);
        endPoint = solverToAvatarFrame.MultiplyPoint(endPoint);
        endPointUp = solverToAvatarFrame.MultiplyVector(endPointUp);
        
        int upperBone = humanBones[0];
        int lowerBone = humanBones[1];
        int handBone = (humanBones.Length > 2 ? humanBones[2] : -1);

        float avatarScale = avatarInfo.ComputeScaleFactor(avatar.GetEyeHeight());
        Quaternion upperRestRotation = avatarInfo.GetRestRotation(upperBone);
        Quaternion lowerRestRotation = avatarInfo.GetRestRotation(lowerBone);
        Vector3 upperPseudoBone = avatarInfo.GetPseudoBone(upperBone) * avatarScale;
        Vector3 lowerPseudoBone = avatarInfo.GetPseudoBone(lowerBone) * avatarScale;

        if (handBone != -1)
        {
            Quaternion lowerRotation = avatar.GetBoneRotation(lowerBone);
            Quaternion handRotation = avatar.GetBoneRotation(handBone);
            Quaternion handRotationLocal = Quaternion.Inverse(lowerRotation) * handRotation;
            Vector3 handPseudoBone = avatarInfo.GetPseudoBone(handBone) * avatarScale;
            lowerPseudoBone = lowerPseudoBone + handRotationLocal * handPseudoBone;
        }

        Vector3 startPoint = avatar.GetBonePosition(upperBone);
        Vector3 endPointOffset = endPoint - startPoint;
        Vector3 endPointDirection = endPointOffset.normalized;

        float distance = endPointOffset.magnitude;
        float upperLength = upperPseudoBone.magnitude;
        float lowerLength = lowerPseudoBone.magnitude;

        float length = upperLength + lowerLength;

        Vector3 bendAxis = avatarInfo.GetMuscleAxis(lowerBone, 2);
        float bendFlexMinus = avatarInfo.GetMuscleFlexMinus(lowerBone, 2);
        float bendFlexPlus = avatarInfo.GetMuscleFlexPlus(lowerBone, 2);

        float targetBendFlex = bendFlexPlus;
        if (distance < length)
        {
            // Chain bend rotation axis may not be perfectly perpendicular to the upper bone, so we project onto the
            // rotation plane to get the correct planar triangle edge lengths.
            Vector3 upperLocal = Quaternion.Inverse(lowerRestRotation) * upperPseudoBone;
            Vector3 lowerLocal = lowerPseudoBone;

            float upperDot = Vector3.Dot(upperLocal, bendAxis);
            float lowerDot = Vector3.Dot(lowerLocal, bendAxis);
            Vector3 upperProjected = upperLocal - upperDot * bendAxis;
            Vector3 lowerProjected = lowerLocal - lowerDot * bendAxis;

            // Law of cosines to compute the chain bend
            float aSqr = upperProjected.sqrMagnitude;
            float bSqr = lowerProjected.sqrMagnitude;
            float cSqr = distance * distance - upperDot * upperDot;
            float ab = Mathf.Sqrt(aSqr) * Mathf.Sqrt(bSqr);
            float cosC = (aSqr + bSqr - cSqr) / (2 * ab);
            if (cosC > 1.0f)
            {
                targetBendFlex = bendFlexMinus;
            }
            else if (cosC < -1.0f)
            {
                targetBendFlex = bendFlexPlus;
            }
            else
            {
                float restAngle = Vector3.Angle(-upperProjected, lowerProjected);
                float angleAB = Mathf.Acos(cosC) * Mathf.Rad2Deg;
                float muscleAngle = angleAB - restAngle;
                targetBendFlex = Mathf.Clamp(muscleAngle, bendFlexMinus, bendFlexPlus);
            }
        }

        Quaternion lowerTargetRotation = Quaternion.AngleAxis(targetBendFlex, bendAxis);

        Quaternion shoulderRotationWorld = avatar.GetBoneRotation(avatarInfo.GetParentBone(upperBone));
        Quaternion upperArmRestRotationWorld = shoulderRotationWorld * upperRestRotation;
        Quaternion upperArmRestRotationWorldInv = Quaternion.Inverse(upperArmRestRotationWorld);
        Vector3 endpointFrom = Vector3.Normalize(upperPseudoBone + lowerRestRotation * lowerTargetRotation * lowerPseudoBone);
        Vector3 endpointFromUp = lowerRestRotation * bendAxis;
        Vector3 endpointTo = upperArmRestRotationWorldInv * endPointDirection;
        Vector3 endpointToUp = upperArmRestRotationWorldInv * endPointUp;
        Quaternion rotationFrom = Quaternion.LookRotation(endpointFrom, -endpointFromUp);
        Quaternion rotationTo = Quaternion.LookRotation(endpointTo, endpointToUp);

        Quaternion upperTargetRotation = rotationTo * Quaternion.Inverse(rotationFrom);

        return new[] { upperTargetRotation, lowerTargetRotation };
    }
}
