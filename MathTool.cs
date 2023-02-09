using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IEEE754Calculator
{
    internal static class MathTool
    {
        public const int FP32ExpoBits = 8;
        public const int FP32MantBits = 23;
        public const int FP32ExpoMask = 0b0__11111111__000_00000_00000_00000_00000;
        public const int FP32MantMask = 0b0__00000000__111_11111_11111_11111_11111;

        public const int FP64ExpoBits = 11;
        public const int FP64MantBits = 52;
        public const long FP64ExpoMask = 0b0__11111111111__00_00000_00000_00000_00000_00000_00000_00000_00000_00000_00000;
        public const long FP64MantMask = 0b0__00000000000__11_11111_11111_11111_11111_11111_11111_11111_11111_11111_11111;

        public static (Tbin sign, Tbin expo, Tbin mantissa) SplitFPBinary<Tfp, Tbin>(Tfp val) where Tfp : unmanaged where Tbin : unmanaged
        {
            unsafe { Debug.Assert(sizeof(Tfp) == sizeof(Tbin)); }
            if (typeof(Tfp) == typeof(float))
                return (sign: As<int, Tbin>(AsInt32(val) >> 31 & 1),
                        expo: As<int, Tbin>(AsInt32(val) >> FP32MantBits & FP32ExpoMask >> FP32MantBits),
                    mantissa: As<int, Tbin>(AsInt32(val) & FP32MantMask));
            if (typeof(Tfp) == typeof(double))
                return (sign: As<long, Tbin>(AsInt64(val) >> 63 & 1),
                        expo: As<long, Tbin>(AsInt64(val) >> FP64MantBits & FP64ExpoMask >> FP64MantBits),
                    mantissa: As<long, Tbin>(AsInt64(val) & FP64MantMask));
            throw new NotSupportedException($"分离浮点类型{typeof(Tfp)}暂未支持");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Tfp SetupFPBinary<Tbin, Tfp>(Tbin sign, Tbin expo, Tbin mant) where Tfp : unmanaged where Tbin : unmanaged
        {
            unsafe { Debug.Assert(sizeof(Tfp) == sizeof(Tbin)); }
            if (typeof(Tfp) == typeof(float))
                return As<int, Tfp>(AsInt32(sign) << 31 | AsInt32(expo) << FP32MantBits & FP32ExpoMask | AsInt32(mant) & FP32MantMask);
            if (typeof(Tfp) == typeof(double))
                return As<long, Tfp>(AsInt64(sign) << 63 | AsInt64(expo) << FP64MantBits & FP64ExpoMask | AsInt64(mant) & FP64MantMask);
            throw new NotSupportedException($"拼装浮点类型{typeof(Tfp)}暂未支持");
        }

        /// <summary>
        /// 尝试把二进制字符串转换为二进制变量.
        /// </summary>
        /// <param name="result">如果转换失败, 返回default(Tbin)</param>
        /// <returns>如果转换成功则返回'\0', 如果有'0'或'1'以外的字符则返回对应的字符值</returns>
        public static char TryParseBin<Tbin>(string binStr, out Tbin result) where Tbin : unmanaged
        {
            unsafe { Debug.Assert(sizeof(Tbin) <= 8, "暂不支持转换为大于64位的类型"); }
            result = default;
            long tmp = 0;
            for (int i = 0; i < binStr.Length; i++)
            {
                tmp <<= 1;
                if (TryParseBinChar(binStr[i], out int val))
                {
                    tmp |= (long)val;
                }
                else return binStr[i];
            }
            result = As<long, Tbin>(tmp);
            return'\0';
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseBinChar(char c, out int result)
        {
            result = c - '0';
            return result == (result & 1);
        }
        /// <summary>
        /// 把任意值类型转换为二进制字符串
        /// </summary>
        /// <typeparam name="T">要转换的类型.</typeparam>
        /// <param name="x">要转换的值</param>
        /// <param name="length">从最低位起, 要的bit数量. 如果不填, 则会根据有效位自动确定. 若小于有效位数, 则会截断.</param>
        /// <returns>转换后的字符串.</returns>
        /// <exception cref="ArgumentOutOfRangeException">如果length大于T类型的bit数量, 为防止内存越界读取, 会丢这个东西</exception>
        public static unsafe string ToBinString<T>(T x, int length = 0) where T : unmanaged
        {
            Trace.Assert(length <= sizeof(T) * 8, $"length{length}不能大于{typeof(T)}的位长度!");

            var sb = new StringBuilder();
            byte* px = (byte*)&x;

            if (length <= 0)
            {
                length = sizeof(T) * 8;
                while (BitAt(px, length - 1) == 0 && --length > 0) ;
            }

            while (--length >= 0)
                sb.Append((char)(BitAt(px, length) + '0'));

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe int BitAt(byte* p, int bitIdx)
        {
            int byteIdx = Math.DivRem(bitIdx, 8, out int rem);
            return (p[byteIdx] >> rem) & 1;
        }

        public static float BitIncrement(float binary)
        {
            int bits = AsInt32(binary);
            if ((bits & FP32ExpoMask) >= FP32ExpoMask)
            {
                // NaN returns NaN
                // -Infinity returns float.MinValue
                // +Infinity returns +Infinity
                return (bits == unchecked((int)0xFF800000)) ? float.MinValue : binary;
            }
            if (bits == unchecked((int)0x80000000))
                return float.Epsilon;// -0.0 returns float.Epsilon
            bits += (bits < 0) ? -1 : +1;
            return AsFP32(bits);
        }

        public static float BitDecrement(float binary)
        {
            int bits = AsInt32(binary);
            if ((bits & FP32ExpoMask) >= FP32ExpoMask)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns float.MaxValue
                return (bits == FP32ExpoMask) ? float.MaxValue : binary;
            }
            if (bits == 0)
                return -float.Epsilon;// +0.0 returns -float.Epsilon
            bits += (bits < 0) ? +1 : -1;
            return AsFP32(bits);
        }
        public static double BitDecrement(double x)
        {
            long bits = AsInt64(x);
            if (((bits >> 32) & 0x7FF00000) >= 0x7FF00000)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns double.MaxValue
                return (bits == 0x7FF00000_00000000) ? double.MaxValue : x;
            }
            if (bits == 0x00000000_00000000)
                return -double.Epsilon;// +0.0 returns -double.Epsilon
            bits += (bits < 0) ? +1 : -1;
            return AsFP64(bits);
        }

        public static double BitIncrement(double x)
        {
            long bits = AsInt64(x);

            if (((bits >> 32) & 0x7FF00000) >= 0x7FF00000)
            {
                // NaN returns NaN
                // -Infinity returns double.MinValue
                // +Infinity returns +Infinity
                return (bits == unchecked((long)(0xFFF00000_00000000))) ? double.MinValue : x;
            }

            if (bits == unchecked((long)0x80000000_00000000))
                return double.Epsilon;// -0.0 returns double.Epsilon
            bits += (bits < 0) ? -1 : +1;
            return AsFP64(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Tto As<Tfrom, Tto>(Tfrom from) where Tfrom : unmanaged where Tto : unmanaged
        {
            Debug.Assert(sizeof(Tfrom) >= sizeof(Tto), $"from的类型{typeof(Tfrom)}不能小于{typeof(Tto)}类型的位长度{sizeof(Tto) * 8}!");
            return *(Tto*)&from;
        }
        [MethodImpl(256)] public static int AsInt32<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, int>(from);
        [MethodImpl(256)] public static long AsInt64<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, long>(from);
        [MethodImpl(256)] public static uint AsUInt32<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, uint>(from);
        [MethodImpl(256)] public static ulong AsUInt64<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, ulong>(from);
        [MethodImpl(256)] public static float AsFP32<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, float>(from);
        [MethodImpl(256)] public static double AsFP64<Tfrom>(Tfrom from) where Tfrom : unmanaged => As<Tfrom, double>(from);
    }
}
