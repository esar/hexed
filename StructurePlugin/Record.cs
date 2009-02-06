using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;


namespace StructurePlugin
{
	public class RecordEnumerator : IEnumerator
	{
		Record Record;
		long _Current = 0;
		
		public RecordEnumerator(Record record)
		{
			Record = record;
		}
		
		public object Current
		{
			get { return Record.GetArrayElement(_Current); }
		}
		
		public void Reset()
		{
			_Current = 0;
		}
		
		public bool MoveNext()
		{
			if(_Current + 1 >= Record.Count)
				return false;

			_Current += 1;		
			return true;
		}
	}

	public class RecordCollection : IEnumerable
	{
		Record _Owner;
		List<Record> Records = new List<Record>();
		
		public int Count
		{
			get { return Records.Count; }
		}
		
		public Record this[string index]
		{
			get
			{
				foreach(Record r in Records)
					if(r.Name == index)
						return r;
				return null;
			}
		}
		
		public Record this[int index]
		{
			get
			{
				return Records[index];
			}
		}
	
		public RecordCollection(Record owner)
		{
			_Owner = owner;
		}
	
		public void Add(Record record)
		{
			record.Parent = _Owner;
			Records.Add(record);
		}
		
		public IEnumerator GetEnumerator()
		{
			return Records.GetEnumerator();
		}
	}

    public abstract class Record : ICloneable, IEnumerable
    {
		public Record Parent;
		public long Index;
    	public RecordCollection _Children;
    	public List<Record> ArrayElements;
    	public Document Document;
    	public long Position = -1;
    	public long Length = 0;
    	public long ArrayLength = 1;
		public bool VariableLength = false;		
    	public Color BackColor = Color.Transparent;
    	
    	protected string _Name;
		public string Name 
		{ 
			get 
			{
				string name;
				if(Parent != null && Parent.ArrayLength > 1)
					name = String.Format("{0}[{1}]", _Name, Index);
				else
					name = _Name;
				if(ArrayLength > 1)
					name = String.Format("{0}[{1}]", name, ArrayLength);
				return name; 
			} 
		}
		public string BaseName { get { return _Name; } }
		
		public virtual string Type { get { return this.GetType().Name; } }
		public virtual string StringValue { get { return this.ToString(); } set {} }
		
    	public Record()
    	{
			_Children = new RecordCollection(this);
    	}
    	public Record(string name, long pos, long length, int arrayLength)
    	{
			_Children = new RecordCollection(this);
		
    		_Name = name;
    		Position = pos;
    		Length = length;
    		ArrayLength = arrayLength;
    	}
    	
		public long Count { get { return ArrayLength; } }
	
		public abstract Record GetArrayElement(long index);
	
		public virtual void ApplyStructure(Document doc, ref long pos, bool first)
		{
			if(first)
			{
				Position = pos;
				Document = doc;
			}
		
			pos += Length;
		}

		public virtual void SetValue(string s)
		{
		}
	
		public override string ToString()
		{
			return "[unknown]";
		}
	
		public object Clone()
		{
			return this.MemberwiseClone();
		}
		
		public void Dump()
		{
			Console.WriteLine(Name);
			Console.WriteLine("    ArrayLength: " + ArrayLength);
			Console.WriteLine("    Length: " + Length);
			Console.WriteLine("    ArrayElements: " + (ArrayElements != null));
			Console.WriteLine("    VariableLength: " + VariableLength);
			Console.WriteLine("    Pos: " + Position);
			Console.WriteLine("----");
			for(int i = 0; i < _Children.Count; ++i)
				_Children[i].Dump();
		}
	
		public IEnumerator GetEnumerator()
		{
			return new RecordEnumerator(this);
		}
    }
    
    public class CharRecord : Record
    {
		public override string Type { get { return "char"; } }
		
    	public CharRecord(string name, 
    	                  long pos, 
    	                  long length, 
    	                  int arrayLength) : base(name, pos, length, arrayLength) {}
	
    	public static implicit operator char(CharRecord r)
    	{
    		return (char)r.Document[(long)r.Position / 8];
    	}

		public CharRecord this[long index]
		{
			get
			{
				if(ArrayLength <= 1)
					return this;
				if(index < 0 || index >= ArrayLength)
					throw new ArgumentOutOfRangeException("index", "The index must be less than the length of the array");
			
				if(ArrayElements != null)
					return (CharRecord)ArrayElements[(int)index];
			
				CharRecord newRecord = new CharRecord(_Name, Position + (Length * index), Length, 1);
				newRecord.Index = index;
				newRecord.Document = Document;
				newRecord.Parent = this;
				return newRecord;
			}
		}
		public override Record GetArrayElement(long index)
		{
			return this[index];
		}
	
    	// TODO: Find a better home for this and the copy in HexView
 		static char[]		AsciiChar = {	'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '!', '\"', '#', '$', '%', '&', '\'',
										'(', ')', '*', '+', ',', '-', '.', '/',
										'0', '1', '2', '3', '4', '5', '6', '7',
										'8', '9', ':', ';', '<', '=', '>', '?',
										'@', 'A', 'B', 'C', 'D', 'E', 'F', 'G',
										'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
										'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W',
										'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
										'`', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
										'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
										'p', 'q', 'r', 's', 't', 'u', 'v', 'w',
										'x', 'y', 'z', '{', '|', '}', '~', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.',
										'.', '.', '.', '.', '.', '.', '.', '.' };   	
    	public override string ToString()
    	{
			
    		
    		StringBuilder str = new StringBuilder();
    		str.Append("\"");
    		for(long i = 0; i < (Length * ArrayLength) / 8 && i < 32; ++i)
    			str.Append(AsciiChar[Document[(long)(Position/8 + i)]]);
    		str.Append("\"");
    		return str.ToString();
    	}
    }
    
    public class IntRecord : Record
    {
		public override string Type { get { return String.Format("int{0}", Length); } }
		
    	public IntRecord(string name, 
    	                  long pos, 
    	                  long length,
    	                  int arrayLength) : base(name, pos, length, arrayLength) {}
    	public static implicit operator int(IntRecord r)
    	{
    		int x = 0;
    		for(long i = 0; i < r.Length / 8; ++i)
    			x |= (int)r.Document[(long)(r.Position/8 + i)] << (int)(i * 8);
    		return x;
    	}

    	public static implicit operator long(IntRecord r)
    	{
    		long x = 0;
    		for(long i = 0; i < r.Length / 8; ++i)
    			x |= (long)r.Document[(long)(r.Position/8 + i)] << (int)(i * 8);
    		return x;
    	}

		public IntRecord this[long index]
		{
			get
			{
				if(ArrayLength <= 1)
					return this;
				if(index < 0 || index >= ArrayLength)
					throw new ArgumentOutOfRangeException("index", "The index must be less than the length of the array");
			
				if(ArrayElements != null)
					return (IntRecord)ArrayElements[(int)index];
			
				IntRecord newRecord = new IntRecord(_Name, Position + (Length * index), Length, 1);
				newRecord.Index = index;
				newRecord.Document = Document;
				newRecord.Parent = this;
				return newRecord;
			}
		}
		public override Record GetArrayElement(long index)
		{
			return this[index];
		}
	
    	public override string ToString()
    	{
    		return ((long)this).ToString();
    	}
    }
    
    public class UintRecord : Record
    {
		public override string Type { get { return String.Format("uint{0}", Length); } }
		
    	public UintRecord(string name, 
    	                  long pos, 
    	                  long length,
    	                  int arrayLength) : base(name, pos, length, arrayLength) {}
    	public static implicit operator uint(UintRecord r)
    	{
			return (uint)r.Document.GetInteger((long)r.Position, (int)r.Length, Endian.Little);
    	}

    	public static implicit operator ulong(UintRecord r)
    	{
			return r.Document.GetInteger((long)r.Position, (int)r.Length, Endian.Little);
    	}

		public UintRecord this[long index]
		{
			get
			{
				if(ArrayLength <= 1)
					return this;
				if(index < 0 || index >= ArrayLength)
					throw new ArgumentOutOfRangeException("index", "The index must be less than the length of the array");
			
				if(ArrayElements != null)
					return (UintRecord)ArrayElements[(int)index];
			
				UintRecord newRecord = new UintRecord(_Name, Position + (Length * index), Length, 1);
				newRecord.Index = index;
				newRecord.Document = Document;
				newRecord.Parent = this;
				return newRecord;
			}
		}
		public override Record GetArrayElement(long index)
		{
			return this[index];
		}
	
    	public override string ToString()
    	{
    		return ((ulong)this).ToString();
    	}

	
		public override void SetValue(string s)
		{
			ulong x = Convert.ToUInt64(s);
			byte[] data = new byte[this.Length / 8];
			for(uint i = 0; i < Length/8; ++i)
			{
				data[i] = (byte)(x & 0xFF);
				x >>= 8;
			}
		
			PieceBuffer.Mark a = Document.Marks.Add((long)(Position/8));
			PieceBuffer.Mark b = Document.Marks.Add((long)((Position+Length)/8));
			Document.Insert(a, b, data, (long)Length/8);
			Document.Marks.Remove(a);
			Document.Marks.Remove(b);
		}
	}
}
