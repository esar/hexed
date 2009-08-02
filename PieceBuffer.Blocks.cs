using System;
using System.Collections.Generic;
using System.IO;


public partial class PieceBuffer
{
	public interface IBlock
	{
		long Length { get; }
		long Used { get; set; }
		byte this[long index] { get; set; }
		void GetBytes(long start, long length, byte[] dst, long dstOffset);
		void SetBytes(long start, long length, byte[] src, long srcOffset);
	}
	
	protected abstract class Block : IBlock
	{
		protected static Dictionary<string, Block> OpenBlocks = new Dictionary<string,Block>(); 

		protected long _Length;
		public long Length { get { return _Length; } }
		protected long _Used;
		public long Used
		{
			get { return _Used; }
			set { _Used = value; }
		}

		public abstract byte this[long index] { get; set; }
		public abstract void GetBytes(long start, long length, byte[] dst, long dstOffset);
		public abstract void SetBytes(long start, long length, byte[] src, long srcOffset);
	}
	
	protected class ConstantBlock : Block
	{
		byte Constant;
		
		public override byte this[long i]
		{
			get { return Constant; }
			set { }
		}

		private ConstantBlock(byte constant)
		{
			Constant = constant;
			_Length = Int64.MaxValue;
			_Used = _Length;
		}
		
		public static Block Create(byte constant)
		{
			lock(OpenBlocks)
			{
				Block block;
				if(!OpenBlocks.TryGetValue("Constant" + constant, out block))
				{
					block = new ConstantBlock(constant);
					OpenBlocks.Add("Constant" + constant, block);
				}
				return block;
			}
		}
		
		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			for(int i = 0; i < length; ++i)
				dst[dstOffset + i] = Constant;
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't SetBytes() on a ConstantBlock");
		}
	}

	protected class FileBlock : Block, IDisposable
	{
		private FileStream FS;
		private byte[]     Buffer = new byte[4096];
		private uint       BufferedLength = 0;
		private uint       MaxLength = 4096;
		private long       StartAddress = 0;
		private object     Lock = new object();

		public override byte this[long i]
		{
			get
			{
				lock(Lock)
				{
					if(i < StartAddress || i >= StartAddress + BufferedLength)
					{
						StartAddress = i - MaxLength / 2;
						if(StartAddress < 0)
							StartAddress = 0;
						FS.Seek(StartAddress, SeekOrigin.Begin);
						BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
					}

					if(i < StartAddress || i >= StartAddress + BufferedLength)
						return 0;
					else
						return Buffer[i - StartAddress];
				}
			}
			set	{ }
		}

		private FileBlock(string filename)
		{
			FS = new FileStream(filename, FileMode.Open, FileAccess.Read);
			_Length = FS.Length;
			_Used = _Length;
		}
		
		public static Block Create(string filename)
		{
			lock(OpenBlocks)
			{
				Block block;
				if(!OpenBlocks.TryGetValue("File" + filename, out block))
				{
					block = new FileBlock(filename);
					OpenBlocks.Add("File" + filename, block);
				}
				return block;
			}
		}
		
		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			lock(Lock)
			{
				while(length > 0)
				{
					if(start < StartAddress || start >= StartAddress + BufferedLength)
					{
						StartAddress = start;
						FS.Seek(StartAddress, SeekOrigin.Begin);
						BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
					}

					if(start < StartAddress || start >= StartAddress + BufferedLength)
						throw new Exception("Failed to read from stream");

					long offset = start - StartAddress;
					long len = length > BufferedLength - offset ? BufferedLength - offset : length;
					Array.Copy(Buffer, offset, dst, dstOffset, len);
					dstOffset += len;
					length -= len;
					start += len;
				}
			}
		}

		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't SetBytes() on a FileBlock");
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
				FS.Close();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);			
		}
	}

	protected class MemoryBlock : Block
	{
		private static int BlockNum = 0;
		public byte[] Buffer;

		public override byte this[long index]
		{
			get { return Buffer[index]; }
			set { Buffer[index] = value; }
		}

		public MemoryBlock(long size)
		{
			Buffer = new byte[size];
			_Length = size;
			_Used = 0;
			
			OpenBlocks.Add("Memory" + (BlockNum++), this);
		}

		public override void GetBytes(long start, long length, byte[] dest, long destOffset)
		{
			Array.Copy(Buffer, start, dest, destOffset, length);
		}

		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			Array.Copy(src, srcOffset, Buffer, start, length);
		}
	}
}

