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
using System.Collections;
using System.Security.Cryptography;


namespace ChecksumPlugin
{
	public class CrcModel
	{
		public string	Name;
		public int		Width;
		public UInt32	Poly;
		public UInt32	Init;
		public bool		RefIn;
		public bool		RefOut;
		public UInt32	XorOut;
		public UInt32	Check;
		
		public CrcModel() {}
		public CrcModel(string name, int width, UInt32 poly, UInt32 init, bool refIn, bool refOut, UInt32 xorOut, UInt32 check)
		{
			Width = width;
			Name = name;
			Poly = poly;
			Init = init;
			RefIn = refIn;
			RefOut = refOut;
			XorOut = xorOut;
			Check = check;
		}
	}
		
	public class CrcManaged : HashAlgorithm
	{
		protected int Width;
		protected UInt32 Poly;
		protected UInt32 Init;
		protected bool Refin;
		protected bool Refot;
		protected UInt32 Xorot;
		
		protected UInt32[] Table;
		protected UInt32 Widmask;
		protected UInt32 Reg;

		public static readonly  CrcModel[] Models = new CrcModel[] 
		{ 
			new CrcModel("CRC-4/ITU",        4,   3,          0,          true,  true,  0,          7),
			new CrcModel("CRC-5/ITU",        5,   0x15,       0,          true,  true,  0,          7),
			new CrcModel("CRC-5/USB",        5,   5,          0x1F,       true,  true,  0x1F,       0x19),
			new CrcModel("CRC-6/ITU",        6,   3,          0,          true,  true,  0,          6),
			new CrcModel("CRC-7",            7,   9,          0,          false, false, 0,          0x75),
			new CrcModel("CRC-8",            8,   7,          0,          false, false, 0,          0xF4),
			new CrcModel("CRC-8/DALLAS",     8,   0x31,       0,          true,  true,  0,          0xA1),
			new CrcModel("CRC-8/I-CODE",     8,   0x1D,       0xFD,       false, false, 0,          0x7E),
			new CrcModel("CRC-11",           11,  0x385,      0x01A,      false, false, 0,          0x5A3),
			new CrcModel("CRC-15",           15,  0x4599,     0,          false, false, 0,          0x059E),
			new CrcModel("CRC-16/ARC",       16,  0x8005,     0,          true,  true,  0,          0xBB3D),
			new CrcModel("CRC-16/DNP",       16,  0x3D65,     0,          true,  true,  0xFFFF,     0xEA82),
			new CrcModel("CRC-16/I-CODE",    16,  0x1021,     0xFFFF,     false, false, 0xFFFF,     0xD64E),
			new CrcModel("CRC-16/KERMIT",    16,  0x1021,     0,          true,  true,  0,          0x2189),
			new CrcModel("CRC-16/MCRF4XX",   16,  0x1021,     0xFFFF,     true,  true,  0,          0x6F91),
			new CrcModel("CRC-16/MODBUS",    16,  0x8005,     0xFFFF,     true,  true,  0,          0x4B37),
			new CrcModel("CRC-16/R",         16,  0x0589,     0,          false, false, 1,          0x007E),
			new CrcModel("CRC-16/USB",       16,  0x8005,     0xFFFF,     true,  true,  0xFFFF,     0xB4C8),
			new CrcModel("CRC-16/X25",       16,  0x1021,     0xFFFF,     true,  true,  0xFFFF,     0x906E),
			new CrcModel("CRC-16/XKERMIT",   16,  0x8408,     0,          true,  true,  0,          0x0C73),
			new CrcModel("CRC-16/ZMODEM",    16,  0x1021,     0,          false, false, 0,          0x31C3),
			new CrcModel("CRC-24/PGP",       24,  0x864CFB,   0xB704CE,   false, false, 0,          0x21CF02),
			new CrcModel("CRC-24/FLEXRAY-A", 24,  0x5D6DCB,   0xFEDCBA,   false, false, 0,          0x7979BD),
			new CrcModel("CRC-24/FLEXRAY-B", 24,  0x5D6DCB,   0xABCDEF,   false, false, 0,          0x1F23B8),
			new CrcModel("CRC-32",           32,  0x04C11DB7, 0xFFFFFFFF, true,  true,  0xFFFFFFFF, 0xCBF43926),
			new CrcModel("CRC-32/C",         32,  0x1EDC6F41, 0xFFFFFFFF, true,  true,  0xFFFFFFFF, 0xE3069283),
			new CrcModel("CRC-32/JAMCRC",    32,  0x04C11DB7, 0xFFFFFFFF, true,  true,  0x00000000, 0x340BC6D9),
			new CrcModel("CRC-32/POSIX",     32,  0x04C11DB7, 0x00000000, false, false, 0xFFFFFFFF, 0x765E7680),
			new CrcModel("CRC-32/XFER",      32,  0x000000AF, 0x00000000, false, false, 0x00000000, 0xBD0BE338)
		};
		
		
		public CrcManaged(string model)
		{
			int i;
			
			for(i = 0; i < Models.Length; ++i)
				if(Models[i].Name == model)
					break;
			if(i >= Models.Length)
				throw new System.NotImplementedException(String.Format("The '{0}' CRC model has not been implemented", model));
			
			Width = Models[i].Width;
			Poly = Models[i].Poly;
			Init = Models[i].Init;
			Refin = Models[i].RefIn;
			Refot = Models[i].RefOut;
			Xorot = Models[i].XorOut;
			
			if(Width == 32)
				Widmask = 0xFFFFFFFF;
			else
				Widmask = (1U << Width) - 1;

			if(Width >= 8)
			{
				Table = new UInt32[0x100];
				for(i = 0; i < 0x100; ++i)
					Table[i] = TableValue(i);
			}
			
			HashSizeValue = Width;
			Initialize();
		}
		
		public CrcManaged(int width, UInt32 poly, UInt32 init, bool refin, bool refout, UInt32 xorout)
		{
			Width = width;
			Poly = poly;
			Init = init;
			Refin = refin;
			Refot = refout;
			Xorot = xorout;
			
			if(Width > 31)
				Widmask = 0xFFFFFFFF;
			else
				Widmask = (1U << Width) - 1;

			if(width >= 8)
			{
				Table = new UInt32[0xFF];
				for(int i = 0; i < 0xFF; ++i)
					Table[i] = TableValue(i);
			}
			
			HashSizeValue = width;
			Initialize();
		}
		
		
		
		
		
		
		
		
		
		



		// Returns the value v with the bottom b [0,32] bits reflected.
		// Example: reflect(0x3e23L,3) == 0x3e26
		UInt32 reflect(UInt32 v, int b)
		{
			UInt32 t = v;
					
			for(int i = 0; i < b; ++i)
			{
				if((t & 1) != 0)
					v |= 1U << ((b - 1) - i);
				else
					v &= ~(1U << ((b - 1) - i));
				t >>= 1;
			}
			
			return v;
		}


		public override void Initialize()
		{
			Reg = Init;
		}


		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			if(Table == null)
			{
				while(cbSize-- > 0)
				{
					UInt32 uch = array[ibStart++];
					UInt32 topbit = 1U << (Width - 1);

					if(Refin)
						uch = reflect(uch, 8);
					if(Width > 7)
					{
						Reg ^= (uch << (Width - 8));
						for(int i = 0; i < 8; ++i)
						{
							if ((Reg & topbit) != 0)
								Reg = (Reg << 1) ^ Poly;
							else
								Reg <<= 1;
							Reg &= Widmask;							
						}
					}
					else
					{
						UInt32 j;
						int i = 0x80;
						while(i > 0)
						{
							if((uch & i) == 0)
								j = Reg & topbit;
							else
								j = (Reg & topbit) ^ topbit;
							if(j == 0)
								Reg = (Reg << 1) & Widmask;
							else
								Reg = ((Reg << 1) ^ Poly) & Widmask;
							i = i >> 1;
						}
					}
				}
			}
			else
			{
				if(Refin)
				{
					while(cbSize-- > 0)
					{
						byte b = array[ibStart++];
						Reg = Table[(Reg ^ b) & 0xFF] ^ (Reg >> 8);
					}
				}
				else
				{
					while(cbSize-- > 0)
					{
						byte b = array[ibStart++];
						Reg = Table[((Reg >> (Width - 8)) ^ b) & 0xFF] ^ (Reg << 8);
					}
				}
			}
		}


		protected override byte[] HashFinal()
		{			
			int byteWidth = Width / 8;

			if(Width % 8 != 0)
				++byteWidth;
			
			if(Refot && Table == null)
				Reg = Xorot ^ reflect(Reg, Width);
			else
				Reg = Xorot ^ Reg;
			Reg &= Widmask;
			
			byte[] result = new byte[byteWidth];
			for(int i = 0; i < byteWidth; ++i) 
			{
				result[byteWidth - i - 1] = (byte)(Reg & 0xFF);
				Reg >>= 8;
			}
			
			return result;
		}


		UInt32 TableValue(int index)
		{
			UInt32 r;
			UInt32 topbit = 1U << (Width - 1);
			UInt32 inbyte = (UInt32)index;

			if(Refin)
				inbyte = reflect(inbyte, 8);
			r = inbyte << (Width - 8);
			for(int i = 0; i < 8; ++i)
			{
				if ((r & topbit) != 0)
					r = (r << 1) ^ Poly;
				else
					r<<=1;
			}
			if(Refin) 
				r = reflect(r, Width);
			return r & Widmask;
		}

		
	}
	
	

	public class Crc32Managed : CrcManaged
	{
		public Crc32Managed() : base(32, 0x04C11DB7, 0xFFFFFFFF, true, true, 0xFFFFFFFF) {}
	}
	
	
}
