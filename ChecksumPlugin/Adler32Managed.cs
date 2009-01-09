using System;
using System.Security.Cryptography;

namespace ChecksumPlugin
{
	class Adler32Managed : HashAlgorithm
	{
		private const UInt32	BASE = 65521;
		private const int		MAX = 6552;
		protected UInt32		A;
		protected UInt32		B;
		
		
		public Adler32Managed()
		{
			HashSizeValue = 32;
			Initialize();
		}
		
		public override void Initialize()
		{
			A = 1;
			B = 0;
		}
		
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			while(cbSize > 0)
			{
				int len = cbSize > MAX ? MAX : cbSize;
				cbSize -= len;
				while(len-- > 0)
				{
					A += array[ibStart++];
					B += A;
				}
				
				A %= BASE;
				B %= BASE;
			}
		}
		
		protected override byte[] HashFinal()
		{
			byte[] result = new byte[4];
			
			result[0] = (byte)(B >> 8);
			result[1] = (byte)(B & 0xFF);
			result[2] = (byte)(A >> 8);
			result[3] = (byte)(A & 0xFF);
			
			return result;
		}
	}
}
