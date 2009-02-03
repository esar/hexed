using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;



public class MetaDataItemChangedEventArgs : EventArgs
{
	public string Key;
	public object Value;
	
	public MetaDataItemChangedEventArgs(string key, object val)
	{
		Key = key;
		Value = val;
	}
}

public delegate void MetaDataItemChangedEventHandler(object sender, MetaDataItemChangedEventArgs e);
	
public class MetaDataCollection : Dictionary<string, object>
{
	public event MetaDataItemChangedEventHandler ItemChanged;
	
	public new object this[string key]
	{
		get { return base[key]; }
		set 
		{ 
			base[key] = value; 
			if(ItemChanged != null) 
				ItemChanged(this, new MetaDataItemChangedEventArgs(key, value)); 
		}
	}
	
	public new void Add(string key, object val)
	{
		base.Add(key, val);
		if(ItemChanged != null)
			ItemChanged(this, new MetaDataItemChangedEventArgs(key, val));
	}
}
	
public class Document : PieceBuffer
{
	protected MetaDataCollection _MetaData = new MetaDataCollection();
	public MetaDataCollection MetaData
	{
		get { return _MetaData; }
	}
	
	protected Dictionary<string, object> _PluginState = new Dictionary<string,object>();
	public Dictionary<string, object> PluginState
	{
		get { return _PluginState; }
	}
	
	public Document()
	{
	}
	
	public Document(string filename) : base(filename)
	{
	}
	
	public void ApplyStructureDefinition(string filename)
	{
		StructureDefinitionCompiler compiler = new StructureDefinitionCompiler();
		Record structure = compiler.Parse(filename);
		
		if(structure != null)
		{
			long pos = 0;
			structure.ApplyStructure(this, ref pos, true);
			structure.Dump();
		}
		
		if(MetaData.ContainsKey("Structure"))
			MetaData["Structure"] = structure;
		else
			MetaData.Add("Structure", structure);
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
			x = (ulong)(this[byteOffset] >> ((8 - bitOffset) - len));
			x &= (ulong)((1 << len) - 1);
			len = 0;
		}
		else if(len > 8 && bitOffset != 0)
		{
			x |= (ulong)(this[byteOffset++] & ((1 << (8 - bitOffset)) - 1));
			len -= 8 - bitOffset;
		}
		
		// get full bytes
		while(len >= 8)
		{
			x <<= 8;
			x |= this[byteOffset++];
			len -= 8;
		}
		
		// get last part byte
		if(len > 0)
		{
			x <<= len;
			x |= (ulong)(this[byteOffset] >> (8 - len));
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
}
