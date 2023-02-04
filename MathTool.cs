﻿using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace IEEE754Calculator
{
    public enum ParseResult
    {
        成功,
        字符串过长,
        有非法字符
    }
    internal static class MathTool
    {
        public static (int sign, int expo, int mantissa) SplitBinary32Bits(int floatBits) => 
            (sign: floatBits >> 31, 
            expo: (floatBits & int.MaxValue) >> 23, 
            mantissa: floatBits & 0b111_11111_11111_11111_11111);

        public static int SetupBinary32Bits(int signBit, int exponentBit, int mantissaBit)
        {
            exponentBit &= byte.MaxValue;
            mantissaBit &= 0b111_11111_11111_11111_11111;
            int allBits = (signBit << 31) | (exponentBit << 23) | (mantissaBit);
            return allBits;
        }

        public static unsafe Tto As<Tfrom, Tto>(Tfrom from) where Tfrom : unmanaged where Tto : unmanaged => *(Tto*)&from;
        public static unsafe int AsInt<Tfrom>(Tfrom from) where Tfrom : unmanaged => *(int*)&from;
        public static unsafe float AsFloat<Tfrom>(Tfrom from) where Tfrom : unmanaged => *(float*)&from;

        public static ParseResult TryParseBin(string binStr, out int result, int lengthLimit)
        {
            result = 0;
            if (binStr.Length > lengthLimit)
                return ParseResult.字符串过长;
            int bin = 0;
            for (int i = 0; i < binStr.Length; i++)
            {
                bin <<= 1;
                if (TryParseBinChar(binStr[i], out int val))
                    bin |= val;
                else
                    return ParseResult.有非法字符;
            }
            result = bin;
            return ParseResult.成功;
        }
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
        /// <exception cref="ArgumentOutOfRangeException">如果length大于T类型的bit数量, 为防止内存越界读取, 会</exception>
        public static unsafe string ToBinString<T>(T x, int length = 0) where T : unmanaged
        {
            var sb = new StringBuilder();
            byte* px = (byte*)&x;

            if (length <= 0)
            {
                length = sizeof(T) * 8;
                while (BitAt(px, length - 1) == 0 && --length >= 0) ;
            }
            else if (length > sizeof(T) * 8)
            {
                throw new ArgumentOutOfRangeException($"length长度不能大于{typeof(T)}的长度!");
            }

            while (--length >= 0)
                sb.Append((char)(BitAt(px, length) + '0'));

            return sb.ToString();
        }

        static unsafe int BitAt(byte* p, int bitIdx)
        {
            int byteIdx = Math.DivRem(bitIdx, 8, out int rem);
            return (p[byteIdx] >> rem) & 1;
        }
    }
}