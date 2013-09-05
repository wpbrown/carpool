/* Carpool
 * Copyright 2013, Will Brown
 * See LICENSE for more information.
 */

using System;
using System.Linq;

namespace Carpool
{
	public static class MathEx
	{
		public static int Gcd(int a, int b)
		{
			if (a == 0 || b == 0)
				throw new ArgumentOutOfRangeException(); 

			a = Math.Abs(a);
			b = Math.Abs(b); 

			if (a == b)
				return a;
			if (a > b && a % b == 0) 
				return b;
			if (b > a && b % a == 0) 
				return a;

			int gcd = 1;
			while (b != 0)
			{
				gcd = b;
				b = a % b;
				a = gcd;
			}

			return gcd;
		}

		public static int Lcm(int a, int b)
		{
			a = Math.Abs(a);
			b = Math.Abs(b);

			a = a / Gcd(a, b);
			return a * b;
		}

		public static int RangeLcm(int a, int b)
		{
			int count = b - a + 1;
			if (count < 2)
				throw new ArgumentOutOfRangeException();

			return Enumerable.Range(a, count).Aggregate(1, (l,v) => Lcm(l, v));
		}
	}
}

