/*
	This file is part of HexEd

	Copyright (C) 2008-2015  Stephen Robinson <hacks@esar.org.uk>

	HexEd is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License version 2 as 
	published by the Free Software Foundation.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Security.Cryptography;

namespace ChecksumPlugin
{
	class Adler32Managed : HashAlgorithm
	{
		private const UInt32 BASE = 65521;
		private const int    MAX = 6552;
		protected UInt32     A;
		protected UInt32     B;
		
		
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
