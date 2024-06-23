using UnityEngine;

public static class SAIKChannelPacker
{
    public static Vector3 RestPose()
    {
        return new Vector3(0.0f, 512.0f, 0.0f);
    }

    public static Vector3 CalibrationPose(int index)
    {
        return new Vector3(0.0f, 256.0f + index, 0.0f);
    }

    public static Vector3 BothArms2Chain9933(Vector3[] leftFlex, Vector3[] rightFlex)
    {
        Vector3 leftFlexValuesUpper = leftFlex[0];
        Vector3 leftFlexValuesLower = leftFlex[1];
        Vector3 rightFlexValuesUpper = rightFlex[0];
        Vector3 rightFlexValuesLower = rightFlex[1];
        float[] channelParameters = new float[8];

        // Whole bit sequence values
        channelParameters[0] = leftFlexValuesUpper.y;
        channelParameters[1] = leftFlexValuesUpper.z;
        channelParameters[2] = rightFlexValuesUpper.y;
        channelParameters[3] = rightFlexValuesUpper.z;
        channelParameters[4] = leftFlexValuesUpper.x;
        channelParameters[5] = rightFlexValuesUpper.x;

        // Split bit sequence values
        channelParameters[6] = leftFlexValuesLower.z;
        channelParameters[7] = rightFlexValuesLower.z;

        return SAIKEncoder.Encode8FlexParams9933(channelParameters);
    }

    public static Vector3 Arm2Chain996(Vector3[] flexValues)
    {
        Vector3 flexValuesShoulder = Vector3.zero;
        Vector3 flexValuesUpper = flexValues[0];
        Vector3 flexValuesLower = flexValues[1];
        Vector3 flexValuesWrist = Vector3.zero;
        float[] channelParameters = new float[9];

        // High precision 9-bit values
        channelParameters[0] = flexValuesUpper.y;
        channelParameters[1] = flexValuesUpper.z;
        channelParameters[2] = flexValuesUpper.x;
        channelParameters[3] = flexValuesLower.z;
        channelParameters[4] = flexValuesLower.x;
        channelParameters[5] = flexValuesWrist.y;

        // Low precision 6-bit values
        channelParameters[6] = flexValuesShoulder.y;
        channelParameters[7] = flexValuesShoulder.z;
        channelParameters[8] = flexValuesWrist.z;

        return SAIKEncoder.Encode9FlexParams996(channelParameters);
    }
}
