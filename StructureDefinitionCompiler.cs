/*
 * Created by SharpDevelop.
 * User: stephen
 * Date: 29/08/2008
 * Time: 00:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Windows.Forms;
using System.Drawing;



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
				{
					if(r.Name == index)
					{
						// If it's an array of duplicate structures, return the last one
						// as that's what the script probably wants.
						if(r.ArrayElements != null)
							return r.ArrayElements[r.ArrayElements.Count - 1];
						else
							return r;
					}
				}
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
		
			foreach(Record r in Records)
			{
				if(r.Name == record.Name)
				{
					if(r.ArrayElements == null)
					{
						Record copy = new Record();
						copy.Name = r.Name;
						copy.Position = r.Position;
						copy.Length = r.Length;
						copy._Children = r._Children;
						r._Children = new RecordCollection(r);
						r.ArrayLength = 2;
						r.ArrayElements = new List<Record>();
						r.ArrayElements.Add(copy);
						r.ArrayElements.Add(record);
					}
					else
					{
						r.ArrayLength += 1;
						r.ArrayElements.Add(record);
					}
					
					return;
				}
			}
			
			Records.Add(record);
		}
	}

    public class Record
    {
		public Record Parent;
    	public RecordCollection _Children;
    	public List<Record> ArrayElements;
    	public Document Document;
    	public string Name;
    	public ulong Position = 0;
    	public ulong Length = 0;
    	public ulong ArrayLength = 1;
    	
    	public Color BackColor = Color.Transparent;
    	
    	public Record()
    	{
			_Children = new RecordCollection(this);
    	}
    	public Record(string name, ulong pos, ulong length, uint arrayLength)
    	{
			_Children = new RecordCollection(this);
		
    		Name = name;
    		Position = pos;
    		Length = length;
    		ArrayLength = arrayLength;
    	}
    	
		public virtual void ApplyStructure(Document doc, ref ulong pos)
		{
			Document = doc;
			pos += Length * ArrayLength;
		}

		public virtual void SetValue(string s)
		{
		}
	
		public override string ToString()
		{
			return "[unknown]";
		}
		
		public void Dump()
		{
			Console.WriteLine(Name);
			Console.WriteLine("    ArrayLength: " + ArrayLength);
			Console.WriteLine("    Length: " + Length);
			Console.WriteLine("    Pos: " + Position);
			Console.WriteLine("----");
			for(int i = 0; i < _Children.Count; ++i)
				_Children[i].Dump();
		}
    }
    
    public class CharRecord : Record
    {
    	public CharRecord(string name, 
    	                  ulong pos, 
    	                  ulong length, 
    	                  uint arrayLength) : base(name, pos, length, arrayLength) {}
    	public static implicit operator char(CharRecord r)
    	{
    		return (char)r.Document[(long)r.Position / 8];
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
    		for(ulong i = 0; i < (Length * ArrayLength) / 8 && i < 32; ++i)
    			str.Append(AsciiChar[Document[(long)(Position/8 + i)]]);
    		str.Append("\"");
    		return str.ToString();
    	}
    }
    
    public class IntRecord : Record
    {
    	public IntRecord(string name, 
    	                  ulong pos, 
    	                  ulong length,
    	                  uint arrayLength) : base(name, pos, length, arrayLength) {}
    	public static implicit operator int(IntRecord r)
    	{
    		int x = 0;
    		for(ulong i = 0; i < r.Length / 8; ++i)
    			x |= (int)r.Document[(long)(r.Position/8 + i)] << (int)(i * 8);
    		return x;
    	}

    	public static implicit operator long(IntRecord r)
    	{
    		long x = 0;
    		for(ulong i = 0; i < r.Length / 8; ++i)
    			x |= (long)r.Document[(long)(r.Position/8 + i)] << (int)(i * 8);
    		return x;
    	}
    	
    	public override string ToString()
    	{
    		return ((long)this).ToString();
    	}
    }
    
    public class UintRecord : Record
    {
    	public UintRecord(string name, 
    	                  ulong pos, 
    	                  ulong length,
    	                  uint arrayLength) : base(name, pos, length, arrayLength) {}
    	public static implicit operator uint(UintRecord r)
    	{
			return (uint)r.Document.GetInteger((long)r.Position, (int)r.Length, Endian.Little);
    	}

    	public static implicit operator ulong(UintRecord r)
    	{
			return r.Document.GetInteger((long)r.Position, (int)r.Length, Endian.Little);
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
		
			PieceBuffer.Mark a = Document.CreateMarkAbsolute((long)(Position/8));
			PieceBuffer.Mark b = Document.CreateMarkAbsolute((long)((Position+Length)/8));
			Document.Insert(a, b, data, (long)Length/8);
			Document.DestroyMark(a);
			Document.DestroyMark(b);
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
			const string padding = "                                                                                        ";
			string pad = padding.Substring(0, depth*4);
			
			if(node.Type == ParseNodeType.RecordDefinition)
			{
				foreach(ParseNode n in node.Children)
					if(n.Type == ParseNodeType.RecordDefinition)
						GenCode(output, n, depth);

				output.Append(pad + "public class " + KnownTypes[node.TypeName].RecordType + " : Record\n");
				output.Append(pad + "{\n");
				foreach(ParseNode n in node.Children)
				{
					if(n.Type == ParseNodeType.Record || n.Type == ParseNodeType.RecordDefinition)
						output.Append(pad + "    public " + KnownTypes[n.TypeName].RecordType + " " + n.Name + "{ get { return (" + KnownTypes[n.TypeName].RecordType + ")_Children[\"" + n.Name + "\"]; } }\n");
				}
				output.Append(pad + "    public " + node.TypeName + "() : base() {} \n");
//				output.Append(pad + "    public " + node.TypeName + "(string name, ulong pos) : base(name, pos) { } \n");
				output.Append(pad + "    public " + node.TypeName + "(string name, ulong pos, ulong length, uint arrayLength) : base(name, pos, length, arrayLength) { } \n");
				output.Append(pad + "    public override void ApplyStructure(Document doc, ref ulong pos)\n");
				output.Append(pad + "    {\n");
				output.Append(pad + "        Record r;\n");
				output.Append(pad + "        ulong oldPos;\n");
				foreach(ParseNode n in node.Children)
				{
					if(n.Type == ParseNodeType.CSharp)
						output.Append(n.Text);
					else if(n.Type == ParseNodeType.Record || n.Type == ParseNodeType.RecordDefinition)
					{
//						string lengthArgs;
//						if(KnownTypes[n.TypeName].Length == 0)
//							lengthArgs = "";
//						else
//							lengthArgs = ", " + KnownTypes[n.TypeName].Length;

						if(n.ArrayLength != null)
							output.Append(pad + "        r = new " + KnownTypes[n.TypeName].RecordType + "(\"" + n.Name + "\", pos, " + KnownTypes[n.TypeName].Length + ", (uint)" + n.ArrayLength + ");\n");
						else
							output.Append(pad + "        r = new " + KnownTypes[n.TypeName].RecordType + "(\"" + n.Name + "\", pos, " + KnownTypes[n.TypeName].Length + ", 1);\n");
						output.Append(pad + "        oldPos = pos;\n");
						output.Append(pad + "        r.ApplyStructure(doc, ref pos);\n");
						output.Append(pad + "        Length += pos - oldPos;\n");
						output.Append(pad + "        _Children.Add(r);\n");
					}
				}
				output.Append(pad + "    }\n");
				output.Append(pad + "}\n");
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
				MessageBox.Show("ERROR: " + cr.Errors[0].ToString(),
								"Error compiling script", MessageBoxButtons.OK, 
								MessageBoxIcon.Error );
				return null;
			}
			
			System.Reflection.Assembly a = cr.CompiledAssembly;
			return (Record)a.CreateInstance(name);
		}
	}

