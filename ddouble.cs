using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kerwis.DDouble;

using static DDMath;

#pragma warning disable IDE1006, CS8981 // 命名样式, 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
public struct ddouble
{
	public double hi, lo;

	public ddouble(double hi, double lo) { this.hi = hi; this.lo = lo; }
	public ddouble(double h) { hi = h; lo = 0.0; }

	public readonly bool IsZero => hi == 0.0;
	public readonly bool IsOne => hi == 1.0 && lo == 0.0;
	public readonly bool IsPositive => hi > 0.0;
	public readonly bool IsNegative => hi < 0.0;
	public readonly bool IsNaN => double.IsNaN(hi) || double.IsNaN(lo);
#if NETCOREAPP
	public readonly bool IsFinite => double.IsFinite(hi);
#else
	  public bool IsFinite { get => !(double.IsNaN(hi) || double.IsInfinity(hi)); }
#endif
	public readonly bool IsInfinty => double.IsInfinity(hi);

	public static implicit operator ddouble(double a) => new(a);

	public static explicit operator double(in ddouble a) => a.hi;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator +(in ddouble a, double b)
	{
		double s1 = TwoSum(a.hi, b, out double s2);
		s2 += a.lo;
		s1 = QuickTwoSum(s1, s2, out double s3);
		return new ddouble(s1, s3);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator +(in ddouble a, in ddouble b) => SloppyAdd(a, b);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator +(double a, in ddouble b) => b + a;
	public static ddouble operator -(in ddouble a, double b)
	{
		double s1 = TwoDiff(a.hi, b, out double s2);
		s2 += a.lo;
		s1 = QuickTwoSum(s1, s2, out s2);
		return new ddouble(s1, s2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator -(in ddouble a, in ddouble b)
	{
		double s = TwoDiff(a.hi, b.hi, out double e);
		e += a.lo;
		e -= b.lo;
		s = QuickTwoSum(s, e, out e);
		return new ddouble(s, e);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator -(double a, in ddouble b)
	{
		double s1 = TwoDiff(a, b.hi, out double s2);
		s2 -= b.lo;
		s1 = QuickTwoSum(s1, s2, out s2);
		return new ddouble(s1, s2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator -(in ddouble a) => new(-a.hi, -a.lo);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator *(in ddouble a, double b)
	{
		double p1 = TwoProd(a.hi, b, out double p2);
		p2 += a.lo * b;
		p1 = QuickTwoSum(p1, p2, out p2);
		return new ddouble(p1, p2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator *(in ddouble a, in ddouble b)
	{
		double p1 = TwoProd(a.hi, b.hi, out double p2);
		p2 += a.hi * b.lo + a.lo * b.hi;
		p1 = QuickTwoSum(p1, p2, out p2);
		return new ddouble(p1, p2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator *(double a, in ddouble b) => b * a;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ddouble operator /(in ddouble a, in ddouble b)
	{
		return SloppyDiv(a, b);
	}
	public static ddouble operator ^(in ddouble a, int n) => Npwr(a, n);

	public static bool operator ==(in ddouble a, double b) => a.hi == b && a.lo == 0d;
	public static bool operator ==(double a, in ddouble b) => a == b.hi && 0d == b.lo;
	public static bool operator ==(in ddouble a, in ddouble b) => a.hi == b.hi && a.lo == b.lo;
	public static bool operator >(in ddouble a, double b) => a.hi > b || (a.hi == b && a.lo > 0.0);
	public static bool operator >(in ddouble a, in ddouble b) => a.hi > b.hi || (a.hi == b.hi && a.lo > b.lo);
	public static bool operator >(double a, in ddouble b) => a > b.hi || (a == b.hi && b.lo < 0.0);
	public static bool operator <(in ddouble a, double b) => a.hi < b || (a.hi == b && a.lo < 0.0);
	public static bool operator <(in ddouble a, in ddouble b) => a.hi < b.hi || (a.hi == b.hi && a.lo < b.lo);
	public static bool operator <(double a, in ddouble b) => a < b.hi || (a == b.hi && b.lo > 0.0);
	public static bool operator >=(in ddouble a, double b) => a.hi > b || (a.hi == b && a.lo >= 0.0);
	public static bool operator >=(in ddouble a, in ddouble b) => a.hi > b.hi || (a.hi == b.hi && a.lo >= b.lo);
	public static bool operator >=(double a, in ddouble b) => b <= a;
	public static bool operator <=(in ddouble a, double b) => a.hi < b || (a.hi == b && a.lo <= 0.0);
	public static bool operator <=(in ddouble a, in ddouble b) => a.hi < b.hi || (a.hi == b.hi && a.lo <= b.lo);
	public static bool operator <=(double a, in ddouble b) => b >= a;
	public static bool operator !=(in ddouble a, double b) => a.hi != b || a.lo != 0.0;
	public static bool operator !=(in ddouble a, in ddouble b) => a.hi != b.hi || a.lo != b.lo;
	public static bool operator !=(double a, in ddouble b) => a != b.hi || b.lo != 0.0;

	public static readonly ddouble Pi = new(3.141592653589793116e+00, 1.224646799147353207e-16);
	public static readonly ddouble TwoPi = new(6.283185307179586232e+00, 2.449293598294706414e-16);
	public static readonly ddouble Pi2 = new(1.570796326794896558e+00, 6.123233995736766036e-17);
	public static readonly ddouble Pi4 = new(7.853981633974482790e-01, 3.061616997868383018e-17);
	public static readonly ddouble E = new(2.718281828459045091e+00, 1.445646891729250158e-16);
	public static readonly ddouble Log2 = new(6.931471805599452862e-01, 2.319046813846299558e-17);
	public static readonly ddouble Log10 = new(2.302585092994045901e+00, -2.170756223382249351e-16);
	public static readonly ddouble NaN = new(double.NaN, double.NaN);
	public static readonly ddouble PositiveInfinity = new(double.PositiveInfinity, double.PositiveInfinity);
	public static readonly double Epsilon = 4.93038065763132e-32;  // 2^-104
	public static readonly double MinNormalized = 2.0041683600089728e-292;  // = 2^(-1022 + 53)
	public static readonly ddouble MaxValue = new(1.79769313486231570815e+308, 9.97920154767359795037e+291);
	public static readonly ddouble SafeMax = new(1.7976931080746007281e+308, 9.97920154767359795037e+291);
	public static readonly int NDigits = 31;
	private static void round_string(Span<char> s, int precision, ref int offset)
	{
		/* Input string must be all digits or errors will occur. */
		int i;
		int D = precision;

		/* Round, handle carry */
		if (s[D - 1] >= '5') {
			s[D - 2]++;

			i = D - 2;
			while (i > 0 && s[i] > '9') {
				s[i] -= (char)10;
				s[--i]++;
			}
		}

		/* If first digit is 10, shift everything. */
		if (s[0] > '9') {
			// e++; // don't modify exponent here
			for (i = precision; i >= 2; i--) s[i] = s[i - 1];
			s[0] = '1';
			s[1] = '0';

			offset++; // now offset needs to be increased by one
			precision++;
		}

		s[precision] = '\0'; // add terminator for array
	}
	private readonly void to_digits(Span<char> s, out int expn, int precision)
	{
		int D = precision + 1;  /* number of digits to compute */

		ddouble r = Abs(this);
		int e;  /* exponent */
		int i, d;

		if (hi == 0.0) {
			/* this == 0.0 */
			expn = 0;
			for (i = 0; i < precision; i++) s[i] = '0';
			return;
		}

		/* First determine the (approximate) exponent. */
		e = (int)Math.Floor(Math.Log10(Math.Abs(hi)));

		if (e < -300) {
			r *= new ddouble(10.0) ^ 300;
			r /= new ddouble(10.0) ^ (e + 300);
		}
		else if (e > 300) {
			r = Ldexp(r, -53);
			r /= new ddouble(10.0) ^ e;
			r = Ldexp(r, 53);
		}
		else {
			r /= new ddouble(10.0) ^ e;
		}

		/* Fix exponent if we are off by one */
		if (r >= 10.0) {
			r /= 10.0;
			e++;
		}
		else if (r < 1.0) {
			r *= 10.0;
			e--;
		}

		if (r >= 10.0 || r < 1.0) {
			throw new Exception("(ddouble::to_digits): can't compute exponent.");
		}

		/* Extract the digits */
		for (i = 0; i < D; i++) {
			d = (int)r.hi;
			r -= d;
			r *= 10.0;

			s[i] = (char)(d + '0');
		}

		/* Fix out of range digits. */
		for (i = D - 1; i > 0; i--) {
			if (s[i] < '0') {
				s[i - 1]--;
				s[i] += (char)10;
			}
			else if (s[i] > '9') {
				s[i - 1]++;
				s[i] -= (char)10;
			}
		}

		if (s[0] <= '0') {
			throw new Exception("(ddouble::to_digits): non-positive leading digit.");
		}

		/* Round, handle carry */
		if (s[D - 1] >= '5') {
			s[D - 2]++;

			i = D - 2;
			while (i > 0 && s[i] > '9') {
				s[i] -= (char)10;
				s[--i]++;
			}
		}

		/* If first digit is 10, shift everything. */
		if (s[0] > '9') {
			e++;
			for (i = precision; i >= 2; i--) s[i] = s[i - 1];
			s[0] = '1';
			s[1] = '0';
		}

		s[precision] = (char)0;
		expn = e;
	}
	private static void append_expn(StringBuilder sb, int expn)
	{
		int k;

		sb.Append(expn < 0 ? '-' : '+');
		expn = Math.Abs(expn);

		if (expn >= 100) {
			k = expn / 100;
			sb.Append((char)('0' + k));
			expn -= 100 * k;
		}

		k = expn / 10;
		sb.Append((char)('0' + k));
		expn -= 10 * k;

		sb.Append((char)('0' + expn));
	}
	public static bool TryParse(in string s, out ddouble a)
	{
		a = NaN;
		int p = 0;
		char ch;
		int sign = 0;
		int point = -1;
		int nd = 0;
		int e = 0;
		bool done = false;
		ddouble r = 0.0;

		/* Skip any leading spaces */
		while (s[p] == ' ') p++;

		while (!done && p < s.Length) {
			ch = s[p];
			if (ch >= '0' && ch <= '9') {
				int d = ch - '0';
				r *= 10.0;
				r += d;
				nd++;
			}
			else {
				switch (ch) {
					case '.':
						if (point >= 0)
							return false;
						point = nd;
						break;

					case '-':
					case '+':
						if (sign != 0 || nd > 0)
							return false;
						sign = (ch == '-') ? -1 : 1;
						break;

					case 'E':
					case 'e':
						//nread = sscanf_s(p + 1, "%d", &e);
						int eindex = p + 1;
						int length = 0;
						while (eindex < s.Length && s[eindex] >= '0' && s[eindex] <= '9') {
							length++;
							eindex++;
						}
#if NETCOREAPP
						bool esuccess = int.TryParse(s.AsSpan(p + 1, length), out e);
#else
								 bool esuccess = int.TryParse(s.Substring(p + 1, length), out e);
#endif
						done = true;
						if (!esuccess)
							return false;
						break;

					default:
						return false;
				}
			}

			p++;
		}

		if (point >= 0) {
			e -= nd - point;
		}

		if (e != 0) {
			r *= new ddouble(10.0) ^ e;
		}

		a = (sign == -1) ? -r : r;
		return true;
	}
	public override readonly string ToString() => ToString();
	public readonly string ToString(int precision = 5, bool fillZero = true, bool fixedPoint = false, bool showPositive = false, bool uppercase = false)
	{
		StringBuilder s = new();
		int i;
		int e = 0;

		if (IsNaN) {
			s.Clear();
			s.Append(uppercase ? "NAN" : "nan");
		}


		if (IsNegative)
			s.Append('-');
		else if (showPositive)
			s.Append('+');

		if (IsInfinty) {
			s.Append(uppercase ? "INF" : "inf");
		}
		else if (IsZero) {
			/* Zero case */
			s.Append('0');
			if (fillZero && precision > 0) {
				s.Append('.');
				s.Append('0', precision);
			}
		}
		else {
			/* Non-zero case */
			var tmpabs = Abs(this);
			var tmplog10 = Log10(tmpabs);
			var tmpfloor = Floor(tmplog10);
			int off = fixedPoint ? (1 + (int)tmpfloor) : 1;
			int d = precision + off;

			int d_with_extra = d;
			if (fixedPoint)
				d_with_extra = Math.Max(60, d); // longer than the max accuracy for DD

			// highly special case - fixed mode, precision is zero, abs(*this) < 1.0
			// without this trap a number like 0.9 printed fixed with 0 precision prints as 0
			// should be rounded to 1.
			if (fixedPoint && (precision == 0) && (Abs(this) < 1.0)) {
				if (Abs(this) >= 0.5)
					s.Append('1');
				else
					s.Append('0');

				return s.ToString();
			}

			// handle near zero to working precision (but not exactly zero)
			if (fixedPoint && d <= 0) {
				s.Append('0');
				if (precision > 0) {
					s.Append('.');
					s.Append('0', precision);
				}
			}
			else { // default

				Span<char> t; //  = new char[d+1];
				int j;
				int dsize;
				if (fixedPoint) {
					dsize = d_with_extra;
					t = new char[d_with_extra + 1];
					to_digits(t, out e, d_with_extra);
				}
				else {
					dsize = d;
					t = new char[d + 1];
					to_digits(t, out e, d);
				}

				if (fixedPoint) {
					// fix the string if it's been computed incorrectly
					// round here in the decimal string if required
					round_string(t, d + 1, ref off);

					if (off > 0) {
						for (i = 0; i < off; i++) s.Append(t[i]);
						if (precision > 0) {
							s.Append('.');
							for (j = 0; j < precision; j++, i++) s.Append(t[i]);
						}
					}
					else {
						s.Append("0.");
						if (off < 0)
							s.Append('0', -off);
						for (i = 0; i < d; i++) s.Append(t[i]);
					}
				}
				else {
					s.Append(t[0]);
					if (precision > 0) s.Append('.');

					for (i = 1; i <= Math.Min(precision, dsize); i++)
						s.Append(t[i]);
				}
			}
		}

		// trap for improper offset with large values
		// without this trap, output of values of the for 10^j - 1 fail for j > 28
		// and are output with the point in the wrong place, leading to a dramatically off value
		if (fixedPoint && (precision > 0)) {
			// make sure that the value isn't dramatically larger
			double from_string = double.Parse(s.ToString());

			// if this ratio is large, then we've got problems
			if (Math.Abs(from_string / hi) > 3.0) {
				// loop on the string, find the point, move it up one
				// don't act on the first character
				for (i = 1; i < s.Length; i++) {
					if (s[i] == '.') {
						s[i] = s[i - 1];
						s[i - 1] = '.';
						break;
					}
				}

				from_string = double.Parse(s.ToString());
				// if this ratio is large, then the string has not been fixed
				if (Math.Abs(from_string / hi) > 3.0) {
					throw new ArithmeticException("Re-rounding unsuccessful in large number fixed point trap.");
				}
			}
		}

		if (!fillZero) {//我写的屎山，因为懒得翻原本的部分哪些是补0的，所以直接在加e之前把尾部的0字符删掉
			string tmp = s.ToString().TrimEnd('0').TrimEnd('.');
			s.Clear().Append(tmp);
		}

		if (!fixedPoint && !IsInfinty) {
			/* Fill in exponent part */
			s.Append(uppercase ? 'E' : 'e');
			append_expn(s, e);
		}

		return s.ToString();
	}

	public override readonly bool Equals(object obj) => obj is ddouble ddouble && hi == ddouble.hi && lo == ddouble.lo;

	public override readonly int GetHashCode() => HashCode.Combine(hi, lo);
}
#pragma warning restore IDE1006, CS8981 // 命名样式, 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
