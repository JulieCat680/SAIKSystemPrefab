using UnityEngine;

public static class SAIKDefaultAnimatorInterface
{
    public static Vector3 PackC0(Vector3[] flexValues)
    {
        return SAIKChannelPacker.BothArms2Chain9933(flexValues, new[] { Vector3.zero, Vector3.zero }) / (1 << 4);
    }

    public static Vector3 PackC1(Vector3[] flexValues)
    {
        return SAIKChannelPacker.BothArms2Chain9933(new[] { Vector3.zero, Vector3.zero }, flexValues) / (1 << 6);
    }

    public static Vector3 PackC2(Vector3[] leftFlex, Vector3[] rightFlex)
    {
        return SAIKChannelPacker.BothArms2Chain9933(leftFlex, rightFlex) / (1 << 8);
    }
}
