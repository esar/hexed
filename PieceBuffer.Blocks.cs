using System;
using System.Collections.Generic;
using System.IO;


public partial class PieceBuffer
{
	protected interface IBlock
	{
		long Length { get; }
		long Used { get; set; }
		byte this[long index] { get; set; }
		bool CanSaveInPlace { get; }
		bool GetOffsetsRelativeToBlock(IBlock block, out long start, out long end);
		void GetBytes(long start, long length, byte[] dst, long dstOffset);
		void SetBytes(long start, long length, byte[] src, long srcOffset);
		void Write(InternalSavePlan dest, long start, long length);
	}
	
	protected abstract class Block : IBlock
	{
		protected long _Length;
		public long Length { get { return _Length; } }
		protected long _Used;
		public long Used
		{
			get { return _Used; }
			set { _Used = value; }
		}

		public virtual bool CanSaveInPlace
		{
			get { return true; }
		}

		public virtual bool GetOffsetsRelativeToBlock(IBlock block, out long start, out long end)
		{
			start = 0;
			end = _Length;
			if(block == this)
				return true;
			else
				return false;	// Not related to block
		}

		public abstract byte this[long index] { get; set; }
		public abstract void Close();
		public abstract void GetBytes(long start, long length, byte[] dst, long dstOffset);
		public abstract void SetBytes(long start, long length, byte[] src, long srcOffset);
		public abstract void Write(InternalSavePlan dest, long start, long length);
	}
	
	protected class ConstantBlock : Block
	{
		byte[] Constant;
		
		public override byte this[long i]
		{
			get { return Constant[i % Constant.Length]; }
			set { }
		}

		private ConstantBlock(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
			_Length = Int64.MaxValue;
			_Used = _Length;
		}
		
		public static ConstantBlock Create(Dictionary<string,Block> openBlocks, byte[] constant)
		{
			lock(openBlocks)
			{
				Block block;
				// TODO: Need to lookup existing constant blocks
				//if(!openBlocks.TryGetValue("Constant" + constant, out block))
				//{
					block = new ConstantBlock(constant);
				//	openBlocks.Add("Constant" + constant, block);
				//}
				return (ConstantBlock)block;
			}
		}

		public override void Close()
		{
			_Length = 0;
			_Used = 0;
		}		

		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			int offset = (int)(start % Constant.Length);
			for(int i = offset; i < length; ++i)
				dst[dstOffset + i] = Constant[(offset + i) % Constant.Length];
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't SetBytes() on a ConstantBlock");
		}

		public override void Write(InternalSavePlan dest, long start, long length)
		{
System.Console.WriteLine("ConstantBlock.Write");
			byte[] data = new byte[4096];

			int offset = (int)(start % Constant.Length);
			while(length > 0)
			{
				int len = length > 4096 ? 4096 : (int)length;
				for(int i = offset; i < len; ++i)
					data[i] = Constant[(offset + i) % Constant.Length];
			
				dest.Write(data, 0, len);
				length -= len;
				start += len;
			}
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
		private string     FileName;

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
						long oldPos = FS.Position;
						FS.Position = StartAddress;
						//FS.Seek(StartAddress, SeekOrigin.Begin);
						BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
						FS.Position = oldPos;
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
			FileName = filename;
			FS = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			_Length = FS.Length;
			_Used = _Length;
		}
		
		public static FileBlock Create(Dictionary<string,Block> openBlocks, string filename)
		{
			lock(openBlocks)
			{
				Block block;
				if(!openBlocks.TryGetValue("File" + filename, out block))
				{
					block = new FileBlock(filename);
					openBlocks.Add("File" + filename, block);
				}
				return (FileBlock)block;
			}
		}
		
		public override void Close()
		{
			FS.Close();
			FS = null;
			_Length = 0;
			_Used = 0;
		}

		public FileStream GetWriteStream()
		{
			System.Threading.Monitor.Enter(Lock);
			FS.Close();
System.Console.WriteLine("Reopening stream for write: " + FileName);
			FS = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			return FS;
		}

		public void ReleaseWriteStream()
		{
System.Console.WriteLine("Reopening stream read only after write: " + FileName);
			FS.Close();
			FS = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			System.Threading.Monitor.Exit(Lock);
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
						long oldPos = FS.Position;
						FS.Position = StartAddress;
						//FS.Seek(StartAddress, SeekOrigin.Begin);
						BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
						FS.Position = oldPos;
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
		
		public override void Write(InternalSavePlan dest, long start, long length)
		{
System.Console.WriteLine("FileBlock.Write");
			lock(Lock)
			{
				while(length > 0)
				{
					if(start < StartAddress || start >= StartAddress + BufferedLength)
					{
						StartAddress = start;
						long oldPos = FS.Position;
						FS.Position = StartAddress;
						//FS.Seek(StartAddress, SeekOrigin.Begin);
						BufferedLength = (uint)FS.Read(Buffer, 0, (int)MaxLength);
						FS.Position = oldPos;
					}

					if(start < StartAddress || start >= StartAddress + BufferedLength)
						throw new Exception("Failed to read from stream");

					int offset = (int)(start - StartAddress);
					int len = (int)(length > BufferedLength - offset ? BufferedLength - offset : length);
					dest.Write(Buffer, offset, len);
					length -= len;
					start += len;
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing && FS != null)
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

		private MemoryBlock(long size)
		{
			Buffer = new byte[size];
			_Length = size;
			_Used = 0;
		}

		public static MemoryBlock Create(Dictionary<string,Block> openBlocks, long size)
		{
			lock(openBlocks)
			{
				MemoryBlock block = new MemoryBlock(size);
				openBlocks.Add("Memory" + (BlockNum++), block);
				return block;
			}
		}

		public override void Close()
		{
			Buffer = null;
			_Length = 0;
			_Used = 0;
		}

		public override void GetBytes(long start, long length, byte[] dest, long destOffset)
		{
			Array.Copy(Buffer, start, dest, destOffset, length);
		}

		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			Array.Copy(src, srcOffset, Buffer, start, length);
		}

		public override void Write(InternalSavePlan dest, long start, long length)
		{
System.Console.WriteLine("MemoryBlock.Write");
			// cast assumes we'll never have a single memory block over 2GB
			dest.Write(Buffer, (int)start, (int)length);
		}
	}
}

