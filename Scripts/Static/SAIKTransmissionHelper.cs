using UnityEngine;

public static class SAIKTransmissionHelper
{
    public static bool ComputeTransmissionEquation(Quaternion q, Vector3 targetV, out Quaternion transmitQ, out Vector3 transmitV)
    {
        // Test the trivial case where we can already generate the target bitfield pattern.
        transmitQ = q;
        transmitV = q * targetV;
        if (TestTransmission(transmitQ, transmitV, targetV))
        {
            return true;
        }

        // If not, then we need to compute a different vector/rotation pair that will generate the target bitfield pattern.
        // Let's start by inverse multiplying the target vector with the inverted rotation.
        double[] r = InverseMultiplyDoublePrecisionPartial(Quaternion.Inverse(q), targetV);
        double rx = (r[0] + r[1] + r[2]);
        double ry = (r[3] + r[4] + r[5]);
        double rz = (r[6] + r[7] + r[8]);

        // Truncate to float since that's what we're limited to.
        float fx = (float)rx;
        float fy = (float)ry;
        float fz = (float)rz;

        // Error when converted back to floating point.
        double ex = (rx - fx);
        double ey = (ry - fy);
        double ez = (rz - fz);

        // No particular reason for these specific coefficients besides that they produce good results.
        double[] coeffs = new double[7];
        coeffs[0] = 0.5;
        coeffs[1] = 1.0;
        coeffs[2] = 2.0;
        coeffs[3] = 4.0;
        coeffs[4] = 8;
        coeffs[5] = 16;
        coeffs[6] = 32;

        // It will take a few tries to find a good rotation, but this should get us one in under 9 tries.
        for (int i = 0; i < coeffs.Length; ++i)
            for (int j = 0; j < coeffs.Length; ++j)
                for (int k = 0; k < coeffs.Length; ++k)
                {
                    // Recompute the inverted rotation matrix again this time accounting for error.
                    double rInv00 = (r[0] - ex * coeffs[i]) / targetV.x;
                    double rInv10 = (r[1] - ex * coeffs[j]) / targetV.y;
                    double rInv20 = (r[2] - ex * coeffs[k]) / targetV.z;
                    double rInv01 = (r[3] - ey * coeffs[i]) / targetV.x;
                    double rInv11 = (r[4] - ey * coeffs[j]) / targetV.y;
                    double rInv21 = (r[5] - ey * coeffs[k]) / targetV.z;
                    double rInv02 = (r[6] - ez * coeffs[i]) / targetV.x;
                    double rInv12 = (r[7] - ez * coeffs[j]) / targetV.y;
                    double rInv22 = (r[8] - ez * coeffs[k]) / targetV.z;

                    // Invert again to get back to the forward transform matrix.
                    double rInvDet = (rInv00 * rInv11 * rInv22) + (rInv01 * rInv12 * rInv20) + (rInv02 * rInv10 * rInv21) - (rInv02 * rInv11 * rInv20) - (rInv01 * rInv10 * rInv22) - (rInv00 * rInv12 * rInv21);
                    double r00 = +(rInv11 * rInv22 - rInv12 * rInv21) / rInvDet;
                    double r01 = -(rInv10 * rInv22 - rInv12 * rInv20) / rInvDet;
                    double r02 = +(rInv10 * rInv21 - rInv11 * rInv20) / rInvDet;
                    double r10 = -(rInv01 * rInv22 - rInv02 * rInv21) / rInvDet;
                    double r11 = +(rInv00 * rInv22 - rInv02 * rInv20) / rInvDet;
                    double r12 = -(rInv00 * rInv21 - rInv01 * rInv20) / rInvDet;
                    double r20 = +(rInv01 * rInv12 - rInv02 * rInv11) / rInvDet;
                    double r21 = -(rInv00 * rInv12 - rInv02 * rInv10) / rInvDet;
                    double r22 = +(rInv00 * rInv11 - rInv01 * rInv10) / rInvDet;

                    // A rotation matrix can yield up to 4 distinct rotation quaternions.
                    // Lets try them all and see if we can find one that can produce the result we want.
                    double trace0 = r00 + r11 + r22;
                    if (trace0 > -0.95)
                    {
                        double r0 = System.Math.Sqrt(1.0 + trace0);
                        float x0 = (float)((r21 - r12) / (2 * r0));
                        float y0 = (float)((r02 - r20) / (2 * r0));
                        float z0 = (float)((r10 - r01) / (2 * r0));
                        float w0 = (float)(0.5 * r0);
                        transmitQ = Quaternion.Normalize(Quaternion.Inverse(Quaternion.Normalize(new Quaternion(x0, y0, z0, w0))));
                        transmitV = InverseMultiplyDoublePrecision(Quaternion.Inverse(transmitQ), targetV);
                        if (TestTransmission(transmitQ, transmitV, targetV))
                        {
                            return true;
                        }
                    }

                    double trace1 = r00 - r11 - r22;
                    if (trace1 > -0.95)
                    {
                        double r1 = System.Math.Sqrt(1.0 + trace1);
                        float x1 = (float)(0.5 * r1);
                        float y1 = (float)((r01 + r10) / (2 * r1));
                        float z1 = (float)((r20 + r02) / (2 * r1));
                        float w1 = (float)((r21 - r12) / (2 * r1));
                        transmitQ = Quaternion.Normalize(Quaternion.Inverse(Quaternion.Normalize(new Quaternion(x1, y1, z1, w1))));
                        transmitV = InverseMultiplyDoublePrecision(Quaternion.Inverse(transmitQ), targetV);
                        if (TestTransmission(transmitQ, transmitV, targetV))
                        {
                            return true;
                        }
                    }

                    double trace2 = r11 - r00 - r22;
                    if (trace2 > -0.95)
                    {
                        double r2 = System.Math.Sqrt(1.0 + trace2);
                        float x2 = (float)((r10 + r01) / (2 * r2));
                        float y2 = (float)(0.5 * r2);
                        float z2 = (float)((r21 + r12) / (2 * r2));
                        float w2 = (float)((r02 - r20) / (2 * r2));
                        transmitQ = Quaternion.Normalize(Quaternion.Inverse(Quaternion.Normalize(new Quaternion(x2, y2, z2, w2))));
                        transmitV = InverseMultiplyDoublePrecision(Quaternion.Inverse(transmitQ), targetV);
                        if (TestTransmission(transmitQ, transmitV, targetV))
                        {
                            return true;
                        }
                    }

                    double trace3 = r22 - r00 - r11;
                    if (trace3 > -0.95)
                    {
                        double r3 = System.Math.Sqrt(1.0 + trace3);
                        float x3 = (float)((r20 + r02) / (2 * r3));
                        float y3 = (float)((r21 + r12) / (2 * r3));
                        float z3 = (float)(0.5 * r3);
                        float w3 = (float)((r10 - r01) / (2 * r3));
                        transmitQ = Quaternion.Normalize(Quaternion.Inverse(Quaternion.Normalize(new Quaternion(x3, y3, z3, w3))));
                        transmitV = InverseMultiplyDoublePrecision(Quaternion.Inverse(transmitQ), targetV);
                        if (TestTransmission(transmitQ, transmitV, targetV))
                        {
                            return true;
                        }
                    }
                }

        transmitQ = q;
        transmitV = targetV;
        return false;
    }

    public static Vector3 InverseMultiplyDoublePrecision(Quaternion q, Vector3 vec)
    {
        double[] v = InverseMultiplyDoublePrecisionPartial(q, vec);
        return new Vector3(
            (float)(v[0] + v[1] + v[2]),
            (float)(v[3] + v[4] + v[5]),
            (float)(v[6] + v[7] + v[8]));
    }

    public static double[] InverseMultiplyDoublePrecisionPartial(Quaternion q, Vector3 vec)
    {
        // Double precision inverse quaternion multiplication to best preserve our packed bits.
        // Since the quaternion isn't guaranteed to be normalized at double-precision, we can't
        // use the trivial conjugate to compute the correct inverse of the rotation. We also
        // can't re-normalize it without modifying the actual (skewed) transform it represents,
        // so instead we'll use Cramer's Rule matrix inversion to get the actual inverse at full
        // precision.

        // Convert the quaternion into a rotation matrix. We apply the float truncation here since
        // that's what unity does for the forward multiplication of quaternions with vectors.
        double xx = (float)(2.0 * q.x * q.x);
        double yy = (float)(2.0 * q.y * q.y);
        double zz = (float)(2.0 * q.z * q.z);
        double xy = (float)(2.0 * q.x * q.y);
        double xz = (float)(2.0 * q.x * q.z);
        double yz = (float)(2.0 * q.y * q.z);
        double wx = (float)(2.0 * q.w * q.x);
        double wy = (float)(2.0 * q.w * q.y);
        double wz = (float)(2.0 * q.w * q.z);

        // Matrix representation of the quaternion
        double m00 = (1.0 - (yy + zz));
        double m01 = (xy - wz);
        double m02 = (xz + wy);
        double m10 = (xy + wz);
        double m11 = (1.0 - (xx + zz));
        double m12 = (yz - wx);
        double m20 = (xz - wy);
        double m21 = (yz + wx);
        double m22 = (1.0 - (xx + yy));

        // Cramer's Rule matrix inversion
        double mDet = (m00 * m11 * m22) + (m01 * m12 * m20) + (m02 * m10 * m21) - (m02 * m11 * m20) - (m01 * m10 * m22) - (m00 * m12 * m21);
        double mInv00 = +(m11 * m22 - m12 * m21) / mDet;
        double mInv01 = -(m10 * m22 - m12 * m20) / mDet;
        double mInv02 = +(m10 * m21 - m11 * m20) / mDet;
        double mInv10 = -(m01 * m22 - m02 * m21) / mDet;
        double mInv11 = +(m00 * m22 - m02 * m20) / mDet;
        double mInv12 = -(m00 * m21 - m01 * m20) / mDet;
        double mInv20 = +(m01 * m12 - m02 * m11) / mDet;
        double mInv21 = -(m00 * m12 - m02 * m10) / mDet;
        double mInv22 = +(m00 * m11 - m01 * m10) / mDet;

        // Matrix-vector multiplication to compute partial coordinates of the bitfield vector in inverted quaternion space.
        double[] result = new double[9];
        result[0] = vec.x * mInv00;
        result[1] = vec.y * mInv10;
        result[2] = vec.z * mInv20;
        result[3] = vec.x * mInv01;
        result[4] = vec.y * mInv11;
        result[5] = vec.z * mInv21;
        result[6] = vec.x * mInv02;
        result[7] = vec.y * mInv12;
        result[8] = vec.z * mInv22;

        return result;
    }

    public static bool TestTransmission(Quaternion transmitQ, Vector3 transmitV, Vector3 targetV)
    {
        Vector3 resultV = Quaternion.Inverse(transmitQ) * transmitV;
        return resultV.x == targetV.x && resultV.y == targetV.y && resultV.z == targetV.z;
    }

#if false
    public static bool ComputeTransmissionEquation3c(Quaternion q, Vector3 vec, out Quaternion newQ, out Vector3 newVec)
    {
        newQ = q;

        // Integrate error into the input
        float mixer = 0.722f; // Determined by brute force to be decent
        Quaternion qi = Quaternion.Inverse(q);

        Vector3 modVec = new Vector3(vec.x, vec.y, vec.z);
        Vector3 roundTrip = qi * (q * vec);
        float bestError = (roundTrip - vec).sqrMagnitude;
        for (int i = 0; i < 5; i++)
        {
            Vector3 testVec = new Vector3(modVec.x + ((vec.x - roundTrip.x) * mixer), modVec.y + ((vec.y - roundTrip.y) * mixer), modVec.z + ((vec.z - roundTrip.z) * mixer));
            if (testVec.Equals(modVec))
            {
                break;
            }
            Vector3 testOut = q * testVec;
            roundTrip = qi * testOut;
            float error = (roundTrip - vec).sqrMagnitude;
            if (error <= bestError)
            {
                bestError = error;
                modVec = testVec;
                if (error == 0)
                {
                    newVec = testOut;
                    return true;
                }
            }
            else
            {
                break;
            }
        }
        newVec = q * modVec;

        // Bit mix the rotation
        float qi_x = qi.x;
        float qi_y = qi.y;
        float qi_z = qi.z;
        float qi_w = qi.w;
        float xqi = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(qi_x) + 1);
        float yqi = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(qi_y) + 1);
        float zqi = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(qi_z) + 1);
        float wqi = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(qi_w) + 1);

        for (float wqo = -1; wqo <= 1; wqo++)
        {
            float nw = Mathf.LerpUnclamped(qi_w, wqi, wqo);
            for (float zqo = -1; zqo <= 1; zqo++)
            {
                float nz = Mathf.LerpUnclamped(qi_z, zqi, zqo);
                for (float yqo = -1; yqo <= 1; yqo++)
                {
                    float ny = Mathf.LerpUnclamped(qi_y, yqi, yqo);
                    for (float xqo = -1; xqo <= 1; xqo++)
                    {
                        float nx = Mathf.LerpUnclamped(qi_x, xqi, xqo);
                        Quaternion testQi = new Quaternion(nx, ny, nz, nw).normalized;
                        roundTrip = testQi * newVec;
                        float error = (roundTrip - vec).sqrMagnitude;
                        if (error < bestError)
                        {
                            bestError = error;
                            newQ = Quaternion.Inverse(testQi);
                            if (error == 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

#endif
}
