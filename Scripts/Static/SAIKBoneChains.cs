using UnityEngine;

public static class SAIKBoneChains
{
    public static int[] LeftArmUpperLower()
    {
        int[] bones = new int[2];
        bones[0] = (int)HumanBodyBones.LeftUpperArm;
        bones[1] = (int)HumanBodyBones.LeftLowerArm;
        return bones;
    }

    public static int[] LeftArmUpperLowerHand()
    {
        int[] bones = new int[3];
        bones[0] = (int)HumanBodyBones.LeftUpperArm;
        bones[1] = (int)HumanBodyBones.LeftLowerArm;
        bones[2] = (int)HumanBodyBones.LeftHand;
        return bones;
    }

    public static int[] RightArmUpperLower()
    {
        int[] bones = new int[2];
        bones[0] = (int)HumanBodyBones.RightUpperArm;
        bones[1] = (int)HumanBodyBones.RightLowerArm;
        return bones;
    }

    public static int[] RightArmUpperLowerHand()
    {
        int[] bones = new int[3];
        bones[0] = (int)HumanBodyBones.RightUpperArm;
        bones[1] = (int)HumanBodyBones.RightLowerArm;
        bones[2] = (int)HumanBodyBones.RightHand;
        return bones;
    }
}