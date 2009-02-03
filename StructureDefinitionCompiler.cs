/*
 * Created by SharpDevelop.
 * User: stephen
 * Date: 29/08/2008
 * Time: 00:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Windows.Forms;
using System.Drawing;


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

	public class RecordCollection
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
	}

    public abstract class Record : ICloneable, IEnumerable
    {
		public Record Parent;
    	public RecordCollection _Children;
    	public List<Record> ArrayElements;
    	public Document Document;
    	public string Name;
    	public long Position = -1;
    	public long Length = 0;
    	public long ArrayLength = 1;
		public bool VariableLength = false;
    	
    	public Color BackColor = Color.Transparent;
    	
    	public Record()
    	{
			_Children = new RecordCollection(this);
    	}
    	public Record(string name, long pos, long length, int arrayLength)
    	{
			_Children = new RecordCollection(this);
		
    		Name = name;
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
					throw new ArgumentOutOfRangeException();
			
				if(ArrayElements != null)
					return (CharRecord)ArrayElements[(int)index];
			
				CharRecord newRecord = new CharRecord(Name, Position + (Length * index), Length, 1);
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
					throw new ArgumentOutOfRangeException();
			
				if(ArrayElements != null)
					return (IntRecord)ArrayElements[(int)index];
			
				IntRecord newRecord = new IntRecord(Name, Position + (Length * index), Length, 1);
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
					throw new ArgumentOutOfRangeException();
			
				if(ArrayElements != null)
					return (UintRecord)ArrayElements[(int)index];
			
				UintRecord newRecord = new UintRecord(Name, Position + (Length * index), Length, 1);
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



	class StructureDefinitionCompiler
	{
		enum TokenType
		{
			Unknown,
			Number,
			Name,
			Type,
			SemiColon,
			OpenBrace,
			CloseBrace,
			Array,
			EOF
		}
		
		enum ParseNodeType
		{
			Unknown,
			RecordDefinition,
			Record,
			CSharp
		}
		class Token
		{
			public TokenType Type;
			public int Position;
			public int Length;
			
			public Token()
			{
			}
			
			public Token(TokenType type, int pos, int len)
			{
				Type = type;
				Position = pos;
				Length = len;
			}
		}
		
		class ParseNode
		{	
			public ParseNodeType Type;
			public string TypeName;
			public string Name;
			public string ArrayLength;
			public string Text;
			public List<ParseNode> Children = new List<ParseNode>();
			
			public ParseNode(ParseNodeType type)
			{
				Type = type;
			}
			
			public ParseNode(ParseNodeType type, string typeName, string name, string text)
			{
				Type = type;
				TypeName = typeName;
				Name = name;
				Text = text;
			}
		}
		
		class KnownType
		{
			public string RecordType;
			public string NativeType;
			public ulong Length;
			
			public KnownType(string recordType, string nativeType, ulong length)
			{
				RecordType = recordType;
				NativeType = nativeType;
				Length = length;
			}
		}
		
		Dictionary<string, KnownType> KnownTypes = new Dictionary<string, KnownType>();
		
		Token Lex(string input, ref int pos, int length)
		{
			for(; pos < length; ++pos)
			{
				if(input[pos] == ';')
				{
					++pos;
					return new Token(TokenType.SemiColon, pos, 1);
				}
				else if(input[pos] == '{')
				{
					++pos;
					return new Token(TokenType.OpenBrace, pos, 1);
				}
				else if(input[pos] == '}')
				{
					++pos;
					return new Token(TokenType.CloseBrace, pos, 1);
				}
				else if(input[pos] == '[')
				{
					++pos;
					Token t = new Token();
					t.Type = TokenType.Array;
					t.Position = pos;
					t.Length = 0;
					while(input[pos] != ']')
					{
						t.Length += 1;
						++pos;
					}
					++pos;
					return t;
				}
				else if((input[pos] >= 'a' && input[pos] <= 'z') ||
				        (input[pos] >= 'A' && input[pos] <= 'Z'))
				{
					Token t = new Token();
					t.Position = pos;
					t.Length = 0;
					while((input[pos] >= 'a' && input[pos] <= 'z') ||
				          (input[pos] >= 'A' && input[pos] <= 'Z') ||
				          (input[pos] >= '0' && input[pos] <= '9') ||
				          input[pos] == '.')
					{
						++t.Length;
						++pos;
					}
					
					if(KnownTypes.ContainsKey(input.Substring(t.Position, t.Length)))
						t.Type = TokenType.Type;
					else
						t.Type = TokenType.Name;
					
					return t;
				}
				else if(input[pos] >= '0' && input[pos] <= '9')
				{
					Token t = new Token();
					t.Position = pos;
					t.Length = 0;
					t.Type = TokenType.Number;
					while((input[pos] >= '0' && input[pos] <= '9') ||
				          input[pos] == 'x')
					{
						++t.Length;
						++pos;
					}
					return t;
				}
				else if(input[pos] != ' ' && input[pos] != '\t' && input[pos] != '\r' && input[pos] != '\n')
				{
					++pos;
					return new Token(TokenType.Unknown, pos, 1);
				}
			}
			
			return new Token(TokenType.EOF, pos, 1);
		}
		
		void DumpParseTree(ParseNode n, int depth)
		{
			const string padding = "                                                                ";
			string pad = padding.Substring(0, depth*4);
			Console.WriteLine(pad + "Node Type: " + n.Type);
			if(n.Type == ParseNodeType.RecordDefinition || n.Type == ParseNodeType.Record)
			{
				Console.WriteLine(pad + "Type: " + n.TypeName);
				Console.WriteLine(pad + "Name: " + n.Name);
				if(n.ArrayLength != null)
					Console.WriteLine(pad + "Array: " + n.ArrayLength);
			}
			else if(n.Type == ParseNodeType.CSharp)
				Console.WriteLine(pad + "Text: " + n.Text);
			
			foreach(ParseNode cn in n.Children)
				DumpParseTree(cn, depth + 1);
		}

	void GenCode(StringBuilder output, ParseNode node, int depth)
	{
		string pad = String.Empty.PadLeft(depth * 4, ' ');
		
		if(node.Type == ParseNodeType.RecordDefinition)
		{
			foreach(ParseNode n in node.Children)
				if(n.Type == ParseNodeType.RecordDefinition)
					GenCode(output, n, depth);
			
			// Declare record's class
			output.Append(String.Format("{0}public class {1} : Record\n{{\n", pad, KnownTypes[node.TypeName].RecordType));
			
			// Declare record's members
			foreach(ParseNode n in node.Children)
				if(n.Type == ParseNodeType.Record || n.Type == ParseNodeType.RecordDefinition)
					output.Append(String.Format("{0}    public readonly {1} {2} = new {1}(\"{2}\", -1, {3}, 1);\n", pad, KnownTypes[n.TypeName].RecordType, n.Name, KnownTypes[n.TypeName].Length));
			
			// Declare array accessor
			output.Append(String.Format(@"
{0}    public {1} this[long index]
{0}    {{
{0}        get
{0}        {{
{0}            if(ArrayLength <= 1) return this;
{0}            if(index < 0 || index >= ArrayLength) throw new System.ArgumentOutOfRangeException();
{0}            if(ArrayElements != null)
{0}                return ({1})ArrayElements[(int)index];
{0}            {1} newRecord = new {1}(Name, -1, 0, 1);
{0}            long pos = Position + (Length * index);
{0}            newRecord.ApplyStructure(Document, ref pos, true);
{0}            newRecord.Parent = this;
{0}            return newRecord;
{0}        }}
{0}    }}
{0}    public override Record GetArrayElement(long index) {{ return this[index]; }}
", pad, node.TypeName));

			// Declare constructors
			output.Append(String.Format(@"
{0}    public {1}() : base() {{}}
{0}    public {1}(string name, long pos, long length, int arrayLength) : base(name, pos, length, arrayLength) {{}}
", pad, node.TypeName));

			// Declare application function
			output.Append(String.Format(@"
{0}    public override void ApplyStructure(Document doc, ref long pos, bool first)
{0}    {{
{0}        if(first)
{0}        {{
{0}            Document = doc;
{0}            Position = pos;
", pad));

			foreach(ParseNode n in node.Children)
			{
				if(n.Type == ParseNodeType.Record || n.Type == ParseNodeType.RecordDefinition)
				{
					int len;
					bool isVariableLength = false;
					if(n.ArrayLength != null)
						isVariableLength = !int.TryParse(n.ArrayLength, out len);
					output.Append(String.Format(@"
{0}            if({1}.Position == -1) _Children.Add({1});
{0}            for(int count = 0; count < {2}; ++count)
{0}            {{
{0}                if({1}.VariableLength) // First time can't be variable length and will always goto the else case
{0}                {{
{0}                    if({1}.ArrayElements == null)
{0}                    {{
{0}                        Record old = (Record){1}.Clone();
{0}                        {1}._Children = new RecordCollection({1});
{0}                        {1}.ArrayElements = new System.Collections.Generic.List<Record>();
{0}                        {1}.ArrayElements.Add(old);
{0}                        {1}.ArrayLength = 1;
{0}                    }}
{0}                    Record r = new {4}({1}.Name, -1, 0, 1);
{0}                    r.ApplyStructure(doc, ref pos, true);
{0}                    r.Parent = this;
{0}                    {1}.ArrayElements.Add(r);
{0}                    {1}.ArrayLength += 1;
{0}                }}
{0}                else
{0}                {{
{0}                    {1}.ApplyStructure(doc, ref pos, count == 0);
{0}                    if({1}.VariableLength == false)
{0}                    {{
{0}                        {1}.ArrayLength += ({2}) - 1;
{0}                        Length += ({2}) * {1}.Length;
{0}                        VariableLength = VariableLength || {3};
{0}                        pos += (({2}) - 1) * {1}.Length;
{0}                        break;
{0}                    }}
{0}                }}
{0}                if({3} || {1}.VariableLength) VariableLength = true;
{0}                Length += {1}.Length;
{0}            }}
", pad, n.Name, n.ArrayLength != null ? n.ArrayLength : "1", isVariableLength ? "true" : "false", KnownTypes[n.TypeName].RecordType));
				}
				else if(n.Type == ParseNodeType.CSharp)
					output.Append(n.Text);
			}
			
			output.Append(String.Format(@"
{0}        }}
{0}        else
{0}        {{
", pad));
			
			foreach(ParseNode n in node.Children)
			{
				if(n.Type == ParseNodeType.Record || n.Type == ParseNodeType.RecordDefinition)
				{
					output.Append(String.Format(@"
{0}                    {1}.ApplyStructure(doc, ref pos, false);
", pad, n.Name));
				}
				else if(n.Type == ParseNodeType.CSharp)
					output.Append(n.Text);
			}
			
			output.Append(String.Format(@"
{0}        }}
{0}    }}
{0}}}
", pad));
		}
		else if(node.Type == ParseNodeType.Unknown)
		{
			foreach(ParseNode n in node.Children)
			{
				if(n.Type == ParseNodeType.CSharp)
					output.Append(n.Text);
				else
					GenCode(output, n, depth + 1);
			}
		}
	}
		
		
		public Record Parse(string filename)
		{
			for(uint i = 0; i <= 64; ++i)
				KnownTypes.Add("int" + i, new KnownType("IntRecord", "int", i));
			for(uint i = 0; i <= 64; ++i)
				KnownTypes.Add("uint" + i, new KnownType("UintRecord", "uint", i));
			KnownTypes.Add("char", new KnownType("CharRecord", "char", 8));
			KnownTypes.Add("int", new KnownType("IntRecord", "int", 32));
			KnownTypes.Add("uint", new KnownType("IntRecord", "uint", 32));
			KnownTypes.Add("long", new KnownType("IntRecord", "int", 64));
			KnownTypes.Add("ulong", new KnownType("IntRecord", "uint", 32));

			StreamReader reader = new StreamReader(filename);
			string input = reader.ReadToEnd();
			reader.Close();

			
			ParseNode node = new ParseNode(ParseNodeType.Unknown);
			Parse(input, 0, input.Length, node);
			
//			DumpParseTree(node, 0);		
			
			if(node.Children.Count > 0)
			{
				StringBuilder output = new StringBuilder();
				GenCode(output, node, 0);
				ParseNode mainNode = null;
				foreach(ParseNode n in node.Children)
				{
					if(n.Type == ParseNodeType.RecordDefinition)
					{
						mainNode = n;
						break;
					}
				}
				return Compile(mainNode.TypeName, output.ToString());
			}
			
			return null;
		}
		
		void Parse(string input, int pos, int length, ParseNode parentNode)
		{
			ParseNode node;
			
//			Console.WriteLine("======== START ========");
//			Console.WriteLine(input.Substring(pos, length));
//			Console.WriteLine("-----------------------");
			
			while(pos < length)
			{
				int rpos = input.IndexOf("record", pos);
				if(rpos >= 0)
				{
					node = new ParseNode(ParseNodeType.CSharp);
					node.Text = input.Substring(pos, rpos - pos);
					parentNode.Children.Add(node);
					
					pos = rpos + 6;
					Token t = Lex(input, ref pos, length);
					if(t.Type == TokenType.Name)
					{
//						Console.WriteLine("NAME");
						
						node = new ParseNode(ParseNodeType.RecordDefinition);
						
						node.TypeName = input.Substring(t.Position, t.Length);
						KnownTypes.Add(node.TypeName, new KnownType(node.TypeName, node.TypeName, 0));
						
						t = Lex(input, ref pos, length);
						if(t.Type == TokenType.OpenBrace)
						{
//							Console.WriteLine("OBRACE");
							
							int depth = 1;
							int bpos = pos;
							
							while(depth > 0)
							{
								bpos = input.IndexOfAny(new char[] {'{', '}'}, bpos);
								if(bpos < 0)
									throw new Exception("Parse error: missing '}'");
								else if(input[bpos] == '{')
									++depth;
								else if(input[bpos] == '}')
									--depth;
								else
									throw new Exception("Parse error: missing '}'");
								
								++bpos;
							}
							
							Parse(input.Substring(pos, bpos - pos - 1), 0, bpos - pos - 1, node);
							pos = bpos;

							ParseName(input, ref pos, length, node);
							
							parentNode.Children.Add(node);
						}
						else
							throw new Exception("Parse error: expected '{'");
					}
					else if(t.Type == TokenType.Type)
					{
//						Console.WriteLine("TYPE");
						
						node = new ParseNode(ParseNodeType.Record);
						node.TypeName = input.Substring(t.Position, t.Length);
						ParseName(input, ref pos, length, node);
						parentNode.Children.Add(node);
					}					
				}
				else
				{
					node = new ParseNode(ParseNodeType.CSharp);
					node.Text = input.Substring(pos, length - pos);
					parentNode.Children.Add(node);
					break;
				}
			}	
			
//			Console.WriteLine("======== END ========");
		}

		void ParseName(string input, ref int pos, int length, ParseNode node)
		{
			Token t = Lex(input, ref pos, length);
			if(t.Type == TokenType.Name || t.Type == TokenType.Type)
			{
//				Console.WriteLine("NAME");
				
				node.Name = input.Substring(t.Position, t.Length);
				t = Lex(input, ref pos, length);
				if(t.Type == TokenType.Array)
				{
//					Console.WriteLine("OARRAY");
					
					node.ArrayLength = input.Substring(t.Position, t.Length);
					t = Lex(input, ref pos, length);
				}
				
				if(t.Type != TokenType.SemiColon)
					throw new Exception("Parse error: expected ';'");
			}
			else
				throw new Exception("Parse error: expected a name");
		}		
		
		
		public Record Compile(string name, string code)
		{
			CSharpCodeProvider c = new CSharpCodeProvider();
			CompilerParameters cp = new CompilerParameters();
			
			cp.ReferencedAssemblies.Add("System.dll");
			cp.ReferencedAssemblies.Add("System.Xml.dll");
			cp.ReferencedAssemblies.Add("System.Data.dll");
			cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
			cp.ReferencedAssemblies.Add("System.Drawing.dll");
			cp.ReferencedAssemblies.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);
			
			cp.CompilerOptions = "/t:library";
			cp.GenerateInMemory = true;
			
			Console.Write(code);
			CompilerResults cr = c.CompileAssemblyFromSource(cp, code);
			if( cr.Errors.Count > 0 )
			{
				StringBuilder msg = new StringBuilder();
				foreach(CompilerError err in cr.Errors)
					msg.Append(err.ToString() + "\r\n\r\n");
				MessageBox.Show("ERROR: " + msg.ToString(), //cr.Errors[0].ToString(),
								"Error compiling script", MessageBoxButtons.OK, 
								MessageBoxIcon.Error );
				return null;
			}
			
			System.Reflection.Assembly a = cr.CompiledAssembly;
			return (Record)a.CreateInstance(name);
		}
	}

