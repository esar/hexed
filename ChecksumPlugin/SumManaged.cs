using System;
using System.Collections;
using System.Security.Cryptography;


namespace ChecksumPlugin
{
	public class SumModel
	{
		public string	Name;
		public int		Width;
		public bool		BigEndian;
		
		public SumModel() {}
		public SumModel(string name, int width, bool bigEndian)
		{
			Name = name;
			Width = width;
			BigEndian = bigEndian;
		}
	}
		
	public class SumManaged : HashAlgorithm
	{
		protected int Width;
		protected bool BigEndian;
		protected UInt32 Reg;

		public static readonly  SumModel[] Models = new SumModel[] 
		{ 
			new SumModel("SUM-8",     8,  true),
			new SumModel("SUM-16/BE", 16, true),
			new SumModel("SUM-16/LE", 16, false),
			new SumModel("SUM-32/BE", 32, true),
			new SumModel("SUM-32/LE", 32, false)
		};
		
		
		public SumManaged(string model)
		{
			int i;
			
			for(i = 0; i < Models.Length; ++i)
				if(Models[i].Name == model)
					break;
			if(i >= Models.Length)
				throw new System.NotImplementedException(String.Format("The '{0}' SUM model has not been implemented", model));
			
			Width = Models[i].Width;
			BigEndian = Models[i].BigEndian;
			
			HashSizeValue = Width;
			Initialize();
		}
		
		public SumManaged(int width, bool bigEndian)
		{
			Width = width;
			BigEndian = bigEndian;
			
			HashSizeValue = width;
			Initialize();
		}

		public override void Initialize()
		{
			Reg = 0;
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;
			while(ibStart < end)
				Reg += array[ibStart++];
		}


		protected override byte[] HashFinal()
		{			
			byte[] result = new byte[Width / 8];
			for(int i = 0; i < Width / 8; ++i) 
			{
				result[(Width / 8) - i - 1] = (byte)(Reg & 0xFF);
				Reg >>= 8;
			}
			
			return result;
		}
	}
}
