using System;
using System.IO;
using System.Collections.Generic;



	
	
	
public class Document
{
	public PieceBuffer		Buffer;
	public Record			Structure;
	
	public Document()
	{
	}
	
	public void Open(string filename)
	{
		Buffer = new PieceBuffer(filename);
	}
	
	public void Close()
	{
		
	}
	
	public void ApplyStructureDefinition(string filename)
	{
		StructureDefinitionCompiler compiler = new StructureDefinitionCompiler();
		Structure = compiler.Parse(filename);
		
		ulong pos = 0;
		Structure.ApplyStructure(this, ref pos);
		Structure.Dump();
	}
}
