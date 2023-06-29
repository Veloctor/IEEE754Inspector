using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IEEE754Inspector;

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

	const int Inline = (int)MethodImplOptions.AggressiveInlining;

	private static Tto ReadAs<Tfrom, Tto>(Tfrom value) => Unsafe.As<Tfrom, Tto>(ref value);

	public static (Tbin sign, Tbin expo, Tbin mantissa) SplitFPBinary<Tfp, Tbin>(Tfp val) where Tfp : unmanaged where Tbin : unmanaged
	{
		unsafe { Debug.Assert(sizeof(Tfp) == sizeof(Tbin)); }
		if (typeof(Tfp) == typeof(float))
			return (sign: ReadAs<int, Tbin>(AsInt32(val) >> 31 & 1),
					  expo: ReadAs<int, Tbin>(AsInt32(val) >> FP32MantBits & FP32ExpoMask >> FP32MantBits),
				 mantissa: ReadAs<int, Tbin>(AsInt32(val) & FP32MantMask));
		if (typeof(Tfp) == typeof(double))
			return (sign: ReadAs<long, Tbin>(AsInt64(val) >> 63 & 1),
					  expo: ReadAs<long, Tbin>(AsInt64(val) >> FP64MantBits & FP64ExpoMask >> FP64MantBits),
				 mantissa: ReadAs<long, Tbin>(AsInt64(val) & FP64MantMask));
		throw new NotSupportedException($"分离浮点类型{typeof(Tfp)}暂未支持");
	}

	[MethodImpl(Inline)]
	public static Tfp SetupFPBinary<Tbin, Tfp>(Tbin sign, Tbin expo, Tbin mant) where Tfp : unmanaged where Tbin : unmanaged
	{
		unsafe { Debug.Assert(sizeof(Tfp) == sizeof(Tbin)); }
		if (typeof(Tfp) == typeof(float)) {
			var fbin = AsInt32(sign) << 31 | AsInt32(expo) << FP32MantBits & FP32ExpoMask | AsInt32(mant) & FP32MantMask;
			return ReadAs<int, Tfp>(fbin);
		}

		if (typeof(Tfp) == typeof(double)) {
			var dbin = AsInt64(sign) << 63 | AsInt64(expo) << FP64MantBits & FP64ExpoMask | AsInt64(mant) & FP64MantMask;
			return ReadAs<long, Tfp>(dbin);
		}

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
		for (int i = 0; i < binStr.Length; i++) {
			tmp <<= 1;
			if (TryParseBinChar(binStr[i], out int val)) {
				tmp |= (long)val;
			}
			else return binStr[i];
		}
		result = Unsafe.As<long, Tbin>(ref tmp);
		return '\0';
	}

	[MethodImpl(Inline)]
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
	public static unsafe string ToBinString<T>(T x, int length = 0) where T : unmanaged
	{
		Trace.Assert(length <= sizeof(T) * 8, $"length{length}不能大于{typeof(T)}的位长度!");

		var sb = new StringBuilder();
		byte* px = (byte*)&x;

		if (length <= 0) {
			length = sizeof(T) * 8;
			while (BitAt(px, length - 1) == 0 && --length > 0) ;
		}

		while (--length >= 0)
			sb.Append((char)(BitAt(px, length) + '0'));

		return sb.ToString();
	}

	[MethodImpl(Inline)]
	static unsafe int BitAt(byte* p, int bitIdx)
	{
		int byteIdx = Math.DivRem(bitIdx, 8, out int rem);
		return p[byteIdx] >> rem & 1;
	}
	[MethodImpl(Inline)] public static int AsInt32<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, int>(ref from);
	[MethodImpl(Inline)] public static long AsInt64<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, long>(ref from);
	[MethodImpl(Inline)] public static uint AsUInt32<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, uint>(ref from);
	[MethodImpl(Inline)] public static ulong AsUInt64<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, ulong>(ref from);
	[MethodImpl(Inline)] public static float AsFP32<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, float>(ref from);
	[MethodImpl(Inline)] public static double AsFP64<Tfrom>(Tfrom from) => Unsafe.As<Tfrom, double>(ref from);
}
