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
        public static string IntToBinString(int x, int length) => IntToBinString((uint)x, length);
        public static string IntToBinString(uint x, int length)
        {
            var sb = new StringBuilder();
            while (length--> 0)
            {
                sb.Insert(0, (char)((x & 1) + '0'));
                x >>= 1;
            }
            return sb.ToString();
        }
    }
}