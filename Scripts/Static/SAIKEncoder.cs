using UnityEngine;

public static class SAIKEncoder
{
    #region Encoding9933

    public static Vector3 Encode8FlexParams9933(float[] flexValues)
    {
        float f0 = (0.5f + Encode9933_x000(flexValues[0]) + Encode9933_0x00(flexValues[1], out int highBits0) + Encode9933_00hh(flexValues[6])) / (1ul << highBits0);
        float f1 = (0.5f + Encode9933_x000(flexValues[2]) + Encode9933_0x00(flexValues[3], out int highBits1) + Encode9933_00hh(flexValues[7])) / (1ul << highBits1);
        float f2 = (0.5f + Encode9933_x000(flexValues[4]) + Encode9933_0x00(flexValues[5], out int highBits2) + Encode9933_00l0(flexValues[6]) + Encode9933_000l(flexValues[7])) / (1ul << highBits2);

        return new Vector3(f0, f1, f2);
    }

    public static float[] Decode8FlexParams9933(Vector3 vector3)
    {
        float[] result = new float[8];
        result[0] = Decode9933_x000(vector3[0]);
        result[1] = Decode9933_0x00(vector3[0]);
        result[2] = Decode9933_x000(vector3[1]);
        result[3] = Decode9933_0x00(vector3[1]);
        result[4] = Decode9933_x000(vector3[2]);
        result[5] = Decode9933_0x00(vector3[2]);
        result[6] = Decode9933_00hh_00l0(vector3[0], vector3[2]);
        result[7] = Decode9933_00hh_000l(vector3[1], vector3[2]);

        return result;
    }

    public static float[] RoundTripEncode8FlexParams9933(float[] flexParams)
    {
        Vector3 encoded = Encode8FlexParams9933(flexParams);
        return Decode8FlexParams9933(encoded);
    }

    private static float Encode9933_x000(float x)
    {
        // All 9 bits of the 1st value are stored in the top of the mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(x * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        return (float)clamped / (1 << 10);
    }

    private static float Encode9933_0x00(float x, out int highBitsValue)
    {
        // The low 8 bits of the 2nd value are stored in the middle of the mantissa.
        // The high 1 bit will be stored in the exponent.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = Mathf.RoundToInt(x * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        int lowBitsValue = clamped & 0b011111111;
        highBitsValue = (clamped & 0b100000000) >> 8;
        return (float)lowBitsValue / (1 << 18);
    }

    private static float Encode9933_00hh(float xh)
    {
        // The high 6 bits of the 3rd value are stored in the bottom of the X/Y mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(xh * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        int highBitsValue = (clamped & 0b111111000) >> 3;
        return (float)highBitsValue / (1 << 24);
    }

    private static float Encode9933_00l0(float xl)
    {
        // The low 3 bits of the 3rd value are stored in the bottom of the Z mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(xl * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        int lowBitsValue = (clamped & 0b000000111);
        return (float)lowBitsValue / (1 << 21);
    }

    private static float Encode9933_000l(float xl)
    {
        // The low 3 bits of the 3rd value are stored in the bottom of the Z mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(xl * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        int lowBitsValue = (clamped & 0b000000111);
        return (float)lowBitsValue / (1 << 24);
    }

    private static float Decode9933_x000(float x)
    {
        float v = x;
        while (v < 0.5f)
            v *= 2.0f;

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result = 0;
        result += v;
        result -= 0.5f * _2pow24rcp;
        result += (1 << 0);
        result -= (1 << 0) * _2pow24rcp;
        result += (1 << 1);
        result -= (1 << 1) * _2pow24rcp;
        result += (1 << 2);
        result -= (1 << 2) * _2pow24rcp;
        result += (1 << 3);
        result -= (1 << 3) * _2pow24rcp;
        result += (1 << 4);
        result -= (1 << 4) * _2pow24rcp;
        result += (1 << 5);
        result -= (1 << 5) * _2pow24rcp;
        result += (1 << 6);
        result -= (1 << 6) * _2pow24rcp;
        result += (1 << 7);
        result -= (1 << 7) * _2pow24rcp;
        result += (1 << 8);
        result -= (1 << 8) * _2pow24rcp;
        result += (1 << 9);
        result -= (1 << 9) * _2pow24rcp;
        result += (1 << 10);
        result -= (1 << 10) * _2pow24rcp;
        result += (1 << 11);
        result -= (1 << 11) * _2pow24rcp;
        result += (1 << 12);
        result -= (1 << 12) * _2pow24rcp;
        result -= (1 << 13);
        result += 0.5f;
        result *= 2.0f;

        return result * 2.0f - 1.0f;
    }

    private static float Decode9933_0x00(float x)
    {
        float v = x;
        int highBitsValue = 0;
        while (v < 0.5f)
        {
            v *= 2.0f;
            highBitsValue = (highBitsValue + 1) & 0x1;
        }

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result = 0;
        result -= v;
        result += 0.5f * _2pow24rcp;
        result -= (1 << 0);
        result += (1 << 0) * _2pow24rcp;
        result -= (1 << 1);
        result += (1 << 1) * _2pow24rcp;
        result -= (1 << 2);
        result += (1 << 2) * _2pow24rcp;
        result -= (1 << 3);
        result += (1 << 3) * _2pow24rcp;
        result -= (1 << 4);
        result += (1 << 4) * _2pow24rcp;
        result -= (1 << 5);
        result += (1 << 5) * _2pow24rcp;
        result -= (1 << 6);
        result += (1 << 6) * _2pow24rcp;
        result -= (1 << 7);
        result += (1 << 7) * _2pow24rcp;
        result -= (1 << 8);
        result += (1 << 8) * _2pow24rcp;
        result -= (1 << 9);
        result += (1 << 9) * _2pow24rcp;
        result -= (1 << 10);
        result += (1 << 10) * _2pow24rcp;
        result -= (1 << 11);
        result += (1 << 11) * _2pow24rcp;
        result -= (1 << 12);
        result += (1 << 12) * _2pow24rcp;
        result += (1 << 13);
        result -= 1.0f;
        result += v;
        result += 0.5f;

        result -= 0.5f * _2pow24rcp;
        result += (1 << 0);
        result -= (1 << 0) * _2pow24rcp;
        result += (1 << 1);
        result -= (1 << 1) * _2pow24rcp;
        result += (1 << 2);
        result -= (1 << 2) * _2pow24rcp;
        result += (1 << 3);
        result -= (1 << 3) * _2pow24rcp;
        result += (1 << 4);
        result -= (1 << 4) * _2pow24rcp;
        result -= (1 << 5);
        result += 0.5f;
        result += (float)highBitsValue / (1 << 10);
        result *= (1 << 9);

        return result * 2.0f - 1.0f;
    }

    private static float Decode9933_00hh_00l0(float xh, float xl)
    {
        float vh = xh;
        float vl = xl;
        while (vh < 0.5f)
            vh *= 2.0f;
        while (vl < 0.5f)
            vl *= 2.0f;

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result1 = 0;
        result1 -= vh;
        result1 += 0.5f * _2pow24rcp;
        result1 -= (1 << 0);
        result1 += (1 << 0) * _2pow24rcp;
        result1 -= (1 << 1);
        result1 += (1 << 1) * _2pow24rcp;
        result1 -= (1 << 2);
        result1 += (1 << 2) * _2pow24rcp;
        result1 -= (1 << 3);
        result1 += (1 << 3) * _2pow24rcp;
        result1 -= (1 << 4);
        result1 += (1 << 4) * _2pow24rcp;
        result1 += (1 << 5);
        result1 -= 1.0f;
        result1 += vh;
        result1 *= (1 << 18);

        float result2 = 0;
        result2 -= vl;
        result2 += 0.5f * _2pow24rcp;
        result2 -= (1 << 0);
        result2 += (1 << 0) * _2pow24rcp;
        result2 -= (1 << 1);
        result2 += (1 << 1) * _2pow24rcp;
        result2 -= (1 << 2);
        result2 += (1 << 2) * _2pow24rcp;
        result2 -= (1 << 3);
        result2 += (1 << 3) * _2pow24rcp;
        result2 -= (1 << 4);
        result2 += (1 << 4) * _2pow24rcp;
        result2 += (1 << 5);
        result2 -= 1.0f;
        result2 += vl;
        result2 += 0.5f;

        result2 -= 0.5f * _2pow24rcp;
        result2 += (1 << 0);
        result2 -= (1 << 0) * _2pow24rcp;
        result2 += (1 << 1);
        result2 -= (1 << 1) * _2pow24rcp;
        result2 -= (1 << 2);
        result2 += 0.5f;
        result2 *= (1 << 12);

        return (result1 + result2) * 2.0f - 1.0f;
    }

    private static float Decode9933_00hh_000l(float xh, float xl)
    {
        float vh = xh;
        float vl = xl;
        while (vh < 0.5f)
            vh *= 2.0f;
        while (vl < 0.5f)
            vl *= 2.0f;

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result1 = 0;
        result1 -= vh;
        result1 += 0.5f * _2pow24rcp;
        result1 -= (1 << 0);
        result1 += (1 << 0) * _2pow24rcp;
        result1 -= (1 << 1);
        result1 += (1 << 1) * _2pow24rcp;
        result1 -= (1 << 2);
        result1 += (1 << 2) * _2pow24rcp;
        result1 -= (1 << 3);
        result1 += (1 << 3) * _2pow24rcp;
        result1 -= (1 << 4);
        result1 += (1 << 4) * _2pow24rcp;
        result1 += (1 << 5);
        result1 -= 1.0f;
        result1 += vh;
        result1 *= (1 << 18);

        float result2 = 0;
        result2 -= vl;
        result2 += 0.5f * _2pow24rcp;
        result2 -= (1 << 0);
        result2 += (1 << 0) * _2pow24rcp;
        result2 -= (1 << 1);
        result2 += (1 << 1) * _2pow24rcp;
        result2 += (1 << 2);
        result2 -= 1.0f;
        result2 += vl;
        result2 *= (1 << 15);

        return (result1 + result2) * 2.0f - 1.0f;
    }

    #endregion

    #region Encoding996

    public static Vector3 Encode9FlexParams996(float[] flexValues)
    {
        float f0 = (0.5f + Encode996_x00(flexValues[0]) + Encode996_0x0(flexValues[1]) + Encode996_00x(flexValues[6], out int highBits0)) / (1ul << highBits0);
        float f1 = (0.5f + Encode996_x00(flexValues[2]) + Encode996_0x0(flexValues[3]) + Encode996_00x(flexValues[7], out int highBits1)) / (1ul << highBits1);
        float f2 = (0.5f + Encode996_x00(flexValues[4]) + Encode996_0x0(flexValues[5]) + Encode996_00x(flexValues[8], out int highBits2)) / (1ul << highBits2);
        return new Vector3(f0, f1, f2);
    }

    public static float[] Decode9FlexParams996(Vector3 vector3)
    {
        float[] result = new float[9];
        result[0] = Decode996_x00(vector3[0]);
        result[1] = Decode996_0x0(vector3[0]);
        result[2] = Decode996_x00(vector3[1]);
        result[3] = Decode996_0x0(vector3[1]);
        result[4] = Decode996_x00(vector3[2]);
        result[5] = Decode996_0x0(vector3[2]);

        result[6] = Decode996_00x(vector3[0]);
        result[7] = Decode996_00x(vector3[1]);
        result[8] = Decode996_00x(vector3[2]);
        return result;
    }

    public static float[] RoundTripEncode9FlexParams996(float[] flexParams)
    {
        Vector3 encoded = Encode9FlexParams996(flexParams);
        return Decode9FlexParams996(encoded);
    }


    private static float Encode996_x00(float x)
    {
        // All 9 bits of the 1st value are stored in the top of the mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(x * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        return (float)clamped / (1 << 10);
    }

    private static float Encode996_0x0(float x)
    {
        // All 9 bits of the 2nd value are stored in the middle of the mantissa.
        int quantizeScale = (1 << 9);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = (int)Mathf.RoundToInt(x * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        return (float)clamped / (1 << 19);
    }

    private static float Encode996_00x(float x, out int highBitsValue)
    {
        // The low 5 bits of the 3rd value are stored in the bottom of the mantissa.
        // The high 1 bit will be stored in the exponent.
        int quantizeScale = (1 << 6);
        int quantizeScaleHalf = quantizeScale / 2;
        int quantized = Mathf.RoundToInt(x * quantizeScaleHalf) + quantizeScaleHalf;
        int clamped = Mathf.Clamp(quantized, 1, quantizeScale - 1);
        int lowBitsValue = clamped & 0b011111;
        highBitsValue = (clamped & 0b100000) >> 5;
        return (float)lowBitsValue / (1 << 24);
    }

    private static float Decode996_x00(float x)
    {
        float v = x;
        while (v < 0.5f)
            v *= 2.0f;

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result = 0;
        result += v;
        result -= 0.5f * _2pow24rcp;
        result += (1 << 0);
        result -= (1 << 0) * _2pow24rcp;
        result += (1 << 1);
        result -= (1 << 1) * _2pow24rcp;
        result += (1 << 2);
        result -= (1 << 2) * _2pow24rcp;
        result += (1 << 3);
        result -= (1 << 3) * _2pow24rcp;
        result += (1 << 4);
        result -= (1 << 4) * _2pow24rcp;
        result += (1 << 5);
        result -= (1 << 5) * _2pow24rcp;
        result += (1 << 6);
        result -= (1 << 6) * _2pow24rcp;
        result += (1 << 7);
        result -= (1 << 7) * _2pow24rcp;
        result += (1 << 8);
        result -= (1 << 8) * _2pow24rcp;
        result += (1 << 9);
        result -= (1 << 9) * _2pow24rcp;
        result += (1 << 10);
        result -= (1 << 10) * _2pow24rcp;
        result += (1 << 11);
        result -= (1 << 11) * _2pow24rcp;
        result += (1 << 12);
        result -= (1 << 12) * _2pow24rcp;
        result -= (1 << 13);
        result += 0.5f;
        result *= 2.0f;

        return result * 2.0f - 1.0f;
    }

    private static float Decode996_0x0(float x)
    {
        float v = x;
        while (v < 0.5f)
            v *= 2.0f;

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result = 0;
        result -= v;
        result += 0.5f * _2pow24rcp;
        result -= (1 << 0);
        result += (1 << 0) * _2pow24rcp;
        result -= (1 << 1);
        result += (1 << 1) * _2pow24rcp;
        result -= (1 << 2);
        result += (1 << 2) * _2pow24rcp;
        result -= (1 << 3);
        result += (1 << 3) * _2pow24rcp;
        result -= (1 << 4);
        result += (1 << 4) * _2pow24rcp;
        result -= (1 << 5);
        result += (1 << 5) * _2pow24rcp;
        result -= (1 << 6);
        result += (1 << 6) * _2pow24rcp;
        result -= (1 << 7);
        result += (1 << 7) * _2pow24rcp;
        result -= (1 << 8);
        result += (1 << 8) * _2pow24rcp;
        result -= (1 << 9);
        result += (1 << 9) * _2pow24rcp;
        result -= (1 << 10);
        result += (1 << 10) * _2pow24rcp;
        result -= (1 << 11);
        result += (1 << 11) * _2pow24rcp;
        result -= (1 << 12);
        result += (1 << 12) * _2pow24rcp;
        result += (1 << 13);
        result -= 1.0f;
        result += v;
        result += 0.5f;

        result -= 0.5f * _2pow24rcp;
        result += (1 << 0);
        result -= (1 << 0) * _2pow24rcp;
        result += (1 << 1);
        result -= (1 << 1) * _2pow24rcp;
        result += (1 << 2);
        result -= (1 << 2) * _2pow24rcp;
        result += (1 << 3);
        result -= (1 << 3) * _2pow24rcp;
        result -= (1 << 4);
        result += 0.5f;
        result *= (1 << 10);

        return result * 2.0f - 1.0f;
    }

    private static float Decode996_00x(float x)
    {
        float v = x;
        int highBitsValue = 0;
        while (v < 0.5f)
        {
            v *= 2.0f;
            highBitsValue = (highBitsValue + 1) & 0x1;
        }

        const float _2pow24rcp = 0.000000059604644775390625f;
        float result = 0;
        result -= v;
        result += 0.5f * _2pow24rcp;
        result -= (1 << 0);
        result += (1 << 0) * _2pow24rcp;
        result -= (1 << 1);
        result += (1 << 1) * _2pow24rcp;
        result -= (1 << 2);
        result += (1 << 2) * _2pow24rcp;
        result -= (1 << 3);
        result += (1 << 3) * _2pow24rcp;
        result += (1 << 4);
        result -= 1.0f;
        result += v;
        result += (float)highBitsValue / (1 << 19);
        result *= (1 << 18);

        return result * 2.0f - 1.0f;
    }
    #endregion
}

/*
 * Distinct 24-bit Encoding Ranges:
										- 1.0______________________________
============================= 1.00000000 ======================================
0.00392156839________________			- 0.99999994_______________________
============================= 0.00390625 ======================================
0.0000153186265234375________			- 0.003906249765625________________
========================= 0.0000152587890625 ==================================
0.000000059838384857177734375			- 0.00001525878814697265625________
===================== 0.000000059604644775390625 ==============================
2.3374369084835052490234375e-10			- 0.0000000596046411991119384765625
================= 0.00000000023283064365386962890625 ==========================
9.130612923763692378997802734375e-13	- 2.32830629684031009674072265625e-10
================= 9.094947017729282379150390625e-13 ===========================
3.5666456733451923355460166931152e-15	- 9.0949464720324613153934478759766e-13
*/
