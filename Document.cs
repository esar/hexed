using System;
using System.IO;
using System.Collections.Generic;



	
	
	
public class Document
{
	private PieceBuffer		_Buffer;
	private Record			_Structure;
	
	public PieceBuffer Buffer
	{
		get { return _Buffer; }
	}
	
	public Record Structure
	{
		get { return _Structure; }
	}
	
	public Document()
	{
	}
	
	public void Open(string filename)
	{
		_Buffer = new PieceBuffer(filename);
		_Buffer.Changed += OnBufferChanged;
	}
	
	public void Close()
	{
		
	}
	
	public void ApplyStructureDefinition(string filename)
	{
		StructureDefinitionCompiler compiler = new StructureDefinitionCompiler();
		_Structure = compiler.Parse(filename);
		
		ulong pos = 0;
		_Structure.ApplyStructure(this, ref pos);
		_Structure.Dump();
	}
	
	public ulong GetInteger(long offset, int length, Endian endian)
	{
		ulong x = 0;
		long byteOffset = offset / 8;
		int bitOffset = (int)(offset % 8);
		int len = length;
		
		// get first part byte
		if(len < 8)
		{
			x = (ulong)(Buffer[byteOffset] >> ((8 - bitOffset) - len));
			x &= (ulong)((1 << len) - 1);
			len = 0;
		}
		else if(len > 8 && bitOffset != 0)
		{
			x |= (ulong)(Buffer[byteOffset++] & ((1 << (8 - bitOffset)) - 1));
			len -= 8 - bitOffset;
		}
		
		// get full bytes
		while(len >= 8)
		{
			x <<= 8;
			x |= Buffer[byteOffset++];
			len -= 8;
		}
		
		// get last part byte
		if(len > 0)
		{
			x <<= len;
			x |= (ulong)(Buffer[byteOffset] >> (8 - len));
		}
		
		if(endian == Endian.Little)
		{
			ulong y = 0;
			while(length >= 8)
			{
				y <<= 8;
				y |= x & 0xFF;
				x >>= 8;
				length -= 8;
			}
			
			if(length > 0)
			{
				y <<= length;
				y |= x & (ulong)((1 << length) - 1);
			}
			x = y;
		}
		
		return x;
	}

	protected void OnBufferChanged(object sender, PieceBuffer.BufferChangedEventArgs e)
	{
		if(_Structure != null)
		{
//			foreach(Record r in _Structure._Children)
//			{
//			}
		}
	}
}
