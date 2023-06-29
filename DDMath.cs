using System;
using System.Runtime.CompilerServices;

namespace Kerwis.DDouble
{

    public static class DDMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double QuickTwoSum(double a, double b, out double err)
        {
            double s = a + b;
            err = b - (s - a);
            return s;
        }

        /* Computes fl(a-b) and err(a-b).  Assumes |a| >= |b| */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double QuickTwoDiff(double a, double b, out double err)
        {
            double s = a - b;
            err = (a - s) - b;
            return s;
        }

        /* Computes fl(a+b) and err(a+b).  */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TwoSum(double a, double b, out double err)
        {
            double s = a + b;
            double bb = s - a;
            err = (a - (s - bb)) + (b - bb);
            return s;
        }

        /* Computes fl(a-b) and err(a-b).  */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TwoDiff(double a, double b, out double err)
        {
            double s = a - b;
            double bb = s - a;
            err = (a - (s - bb)) - (b + bb);
            return s;
        }
#if !NETCOREAPP
        const double _QD_SPLITTER = 134217729.0;             // = 2^27 + 1
        const double _QD_SPLIT_THRESH = 6.69692879491417e+299; // = 2^996
        static void Split(double a, out double hi, out double lo)
        {
            double temp;
            if (a > _QD_SPLIT_THRESH || a < -_QD_SPLIT_THRESH)
            {
                a *= 3.7252902984619140625e-09;  // 2^-28
                temp = _QD_SPLITTER * a;
                hi = temp - (temp - a);
                lo = a - hi;
                hi *= 268435456.0;          // 2^28
                lo *= 268435456.0;          // 2^28
            }
            else
            {
                temp = _QD_SPLITTER * a;
                hi = temp - (temp - a);
                lo = a - hi;
            }
        }
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TwoProd(double a, double b, out double err)
        {
#if NETCOREAPP
            double p = a * b;
            err = Math.FusedMultiplyAdd(a, b, -p);
            return p;
#else
            double p = a * b;
            Split(a, out double a_hi, out double a_lo);
            Split(b, out double b_hi, out double b_lo);
            err = a_hi * b_hi - p + a_hi * b_lo + a_lo * b_hi + a_lo * b_lo;
            return p;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /* Computes fl(a*a) and err(a*a).  Faster than the above method. */
        public static double TwoSqr(double a, out double err)
        {
#if NETCOREAPP
            double p = a * a;
            err = Math.FusedMultiplyAdd(a, a, -p);
            return p;
#else
            double q = a * a;
            Split(a, out double hi, out double lo);
            err = hi * hi - q + 2.0 * hi * lo + lo * lo;
            return q;
#endif
        }

        public static ddouble Add(double a, double b)
        {
            double s = TwoSum(a, b, out double e);
            return new ddouble(s, e);
        }

        public static ddouble IeeeAdd(in ddouble a, in ddouble b)
        {
            /* This one satisfies IEEE style error bound,
               due to K. Briggs and W. Kahan.                   */
            double s1 = TwoSum(a.hi, b.hi, out double s2);
            double t1 = TwoSum(a.lo, b.lo, out double t2);
            s2 += t1;
            s1 = QuickTwoSum(s1, s2, out s2);
            s2 += t2;
            s1 = QuickTwoSum(s1, s2, out s2);
            return new ddouble(s1, s2);
        }

        public static ddouble SloppyAdd(in ddouble a, in ddouble b)
        {
            /* This is the less accurate version ... obeys Cray-style error bound. */
            double s = TwoSum(a.hi, b.hi, out double e);
            e += a.lo + b.lo;
            s = QuickTwoSum(s, e, out e);
            return new ddouble(s, e);
        }

        /// <summary> double-double = double - double </summary>
        public static ddouble Sub(double a, double b) => new(TwoDiff(a, b, out double e), e);

        /// <summary> double-double * (2.0 ^ exp) </summary>
        public static double Ldexp(double x, int exp) => (exp > -1 && exp < 64) ? x * (1L << exp) : x * Math.Pow(2, exp);

        /// <summary> double-double * (2.0 ^ exp) </summary>
        public static ddouble Ldexp(in ddouble a, int exp) => new(Ldexp(a.hi, exp), Ldexp(a.lo, exp));

        /// <summary> double-double * double, where double is a power of 2. </summary>
        public static ddouble MulPowerOf2(in ddouble a, double b) => new(a.hi * b, a.lo * b);

        /// <summary> double-double = double * double </summary>
        public static ddouble Mul(double a, double b) => new(TwoProd(a, b, out double e), e);

        public static ddouble Div(double a, double b)
        {
            double q1, q2;
            double p1;
            double s;

            q1 = a / b;

            /* Compute  a - q1 * b */
            p1 = TwoProd(q1, b, out double p2);
            s = TwoDiff(a, p1, out double e);
            e -= p2;

            /* get next approximation */
            q2 = (s + e) / b;

            s = QuickTwoSum(q1, q2, out e);

            return new ddouble(s, e);
        }

        public static ddouble SloppyDiv(in ddouble a, in ddouble b)
        {
            double s1;
            double q1, q2;
            ddouble r;

            q1 = a.hi / b.hi;  /* approximate quotient */

            /* compute  this - q1 * dd */
            r = b * q1;
            s1 = TwoDiff(a.hi, r.hi, out double s2);
            s2 -= r.lo;
            s2 += a.lo;

            /* get next approximation */
            q2 = (s1 + s2) / b.hi;

            /* renormalize */
            r.hi = QuickTwoSum(q1, q2, out r.lo);
            return r;
        }

        public static ddouble AccurateDiv(in ddouble a, in ddouble b)
        {
            double q1, q2, q3;
            ddouble r;

            q1 = a.hi / b.hi;  /* approximate quotient */

            r = a - q1 * b;

            q2 = r.hi / b.hi;
            r -= (q2 * b);

            q3 = r.hi / b.hi;

            q1 = QuickTwoSum(q1, q2, out q2);
            r = new ddouble(q1, q2) + q3;
            return r;
        }

        public static ddouble Sqr(in ddouble a)
        {
            double p1 = TwoSqr(a.hi, out double p2);
            p2 += 2.0 * a.hi * a.lo;
            p2 += a.lo * a.lo;
            double s1 = QuickTwoSum(p1, p2, out double s2);
            return new ddouble(s1, s2);
        }

        public static ddouble Sqr(double a)
        {
            double p1 = TwoSqr(a, out double p2);
            return new ddouble(p1, p2);
        }
        /// <summary> Computes the n-th power of a double-double number. </summary>
        /// <exception cref="ArgumentException">0^0 causes an error.</exception>
        public static ddouble Npwr(in ddouble a, int n)
        {

            if (n == 0)
            {
                if (a.IsZero) throw new ArgumentException("ddouble Npwr: x=0, n=0.");
                else return 1.0;
            }
            ddouble r = a;
            ddouble s = 1.0;
            int N = Math.Abs(n);

            if (N > 1)
            {
                /* Use binary exponentiation */
                while (N > 0)
                {
                    if (N % 2 == 1)
                    {
                        s *= r;
                    }
                    N /= 2;
                    if (N > 0)
                        r = Sqr(r);
                }
            }
            else
            {
                s = r;
            }
            /* Compute the reciprocal if n is negative. */
            if (n < 0) return (1.0 / s);

            return s;
        }

        public static ddouble Abs(in ddouble a) => (a.hi < 0.0) ? -a : a;

        public static ddouble Pow(in ddouble a, int n) => Npwr(a, n);

        public static ddouble Pow(in ddouble a, in ddouble b) => Exp(b * Log(a));

        public static ddouble Floor(in ddouble a)
        {
            double hi = Math.Floor(a.hi);
            double lo = 0d;

            if (hi == a.hi)
            {
                /* High word is integer already.  Round the low word. */
                lo = Math.Floor(a.lo);
                hi = QuickTwoSum(hi, lo, out lo);
            }

            return new ddouble(hi, lo);
        }

        public static ddouble Ceil(in ddouble a)
        {
            double hi = Math.Ceiling(a.hi);
            double lo = 0.0;

            if (hi == a.hi)
            {
                /* High word is integer already.  Round the low word. */
                lo = Math.Ceiling(a.lo);
                hi = QuickTwoSum(hi, lo, out lo);
            }

            return new ddouble(hi, lo);
        }

        const int n_inv_fact = 15;
        static readonly double[,] inv_fact = new double[n_inv_fact, 2]
        {
            { 1.66666666666666657e-01,  9.25185853854297066e-18},
            { 4.16666666666666644e-02,  2.31296463463574266e-18},
            { 8.33333333333333322e-03,  1.15648231731787138e-19},
            { 1.38888888888888894e-03, -5.30054395437357706e-20},
            { 1.98412698412698413e-04,  1.72095582934207053e-22},
            { 2.48015873015873016e-05,  2.15119478667758816e-23},
            { 2.75573192239858925e-06, -1.85839327404647208e-22},
            { 2.75573192239858883e-07,  2.37677146222502973e-23},
            { 2.50521083854417202e-08, -1.44881407093591197e-24},
            { 2.08767569878681002e-09, -1.20734505911325997e-25},
            { 1.60590438368216133e-10,  1.25852945887520981e-26},
            { 1.14707455977297245e-11,  2.06555127528307454e-28},
            { 7.64716373181981641e-13,  7.03872877733453001e-30},
            { 4.77947733238738525e-14,  4.39920548583408126e-31},
            { 2.81145725434552060e-15,  1.65088427308614326e-31}
        };
        /* Exponential.  Computes exp(x) in double-double precision. */
        public static ddouble Exp(in ddouble a)
        {
            /* Strategy:  We first reduce the size of x by noting that

                    exp(kr + m * log(2)) = 2^m * exp(r)^k

               where m and k are integers.  By choosing m appropriately
               we can make |kr| <= log(2) / 2 = 0.347.  Then exp(r) is
               evaluated using the familiar Taylor series.  Reducing the
               argument substantially speeds up the convergence.       */

            const double k = 512.0;
            const double inv_k = 1.0 / k;

            if (a.hi <= -709.0) return 0.0;

            if (a.hi >= 709.0) return ddouble.PositiveInfinity;

            if (a.IsZero) return 1.0;

            if (a.IsOne) return ddouble.E;

            double m = Math.Floor(a.hi / ddouble.Log2.hi + 0.5);
            ddouble r = MulPowerOf2(a - ddouble.Log2 * m, inv_k);
            ddouble s, t, p;

            p = Sqr(r);
            s = r + MulPowerOf2(p, 0.5);
            p *= r;
            t = p * new ddouble(inv_fact[0, 0], inv_fact[0, 1]);
            int i = 0;
            do
            {
                s += t;
                p *= r;
                ++i;
                t = p * new ddouble(inv_fact[i, 0], inv_fact[i, 1]);
            } while (Math.Abs((double)t) > inv_k * ddouble.Epsilon && i < 5);

            s += t;

            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s = MulPowerOf2(s, 2.0) + Sqr(s);
            s += 1.0;

            return Ldexp(s, (int)m);
        }

        public static ddouble Log(in ddouble a)
        {
            /* Strategy.  The Taylor series for log converges much more
               slowly than that of exp, due to the lack of the factorial
               term in the denominator.  Hence this routine instead tries
               to determine the root of the function

                   f(x) = exp(x) - a

               using Newton iteration.  The iteration is given by

                   x' = x - f(x)/f'(x)
                      = x - (1 - a * exp(-x))
                      = x + a * exp(-x) - 1.

               Only one iteration is needed, since Newton's iteration
               approximately doubles the number of digits per iteration. */

            if (a.IsOne) return 0.0;

            if (a.hi <= 0.0)
            {
                throw new ArgumentException("Non-positive argument.");
            }

            ddouble x = Math.Log(a.hi);   /* Initial approximation */

            x = x + a * Exp(-x) - 1.0;
            return x;
        }

        public static ddouble Log10(in ddouble a) => Log(a) / ddouble.Log10;
    }
}