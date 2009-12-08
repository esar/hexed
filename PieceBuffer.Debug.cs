using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;


public partial class PieceBuffer : IDisposable
{
	protected void DebugDumpPieceText(string label, Piece head, Piece tail, bool between)
	{
		Console.Write(label);
		Piece p = head;
		int c = 0;
		
		if(between)
			p = p.Next;
		
		while((!between || p == tail) && c < 10)
		{
			if(p == Pieces)
				Console.Write("* ");
			else if(p.Start == Int64.MaxValue || p.End == Int64.MaxValue)
				Console.Write("E ");
			else
			{
				byte[] tmp = new byte[p.End - p.Start];
				p.GetBytes(0, p.End - p.Start, tmp, 0);
						
				Console.Write("\"" + System.Text.ASCIIEncoding.ASCII.GetString(tmp) + "\" ");
			}
			
			if(!between && p == tail)
				break;
			p = p.Next;
			++c;
		}
		Console.Write("\n");
	}

	protected void DebugDumpHistory(InternalHistoryItem item, int indent)
	{
		const string padding = "                                                                                ";
		string pad = padding.Substring(0, indent * 4);

		while(item != null)
		{
			if(item == History)
				Console.Write("=>" + pad.Substring(2));
			else
				Console.Write(pad);

			if(item.Parent != null)
			{
				Console.Write("Head: " + (item.Head.Start == Int64.MaxValue ? -1 : item.Head.Start));
				Console.Write(" => " + (item.Head.End == Int64.MaxValue ? -1 : item.Head.End));
				Console.Write(", Tail: " + (item.Tail.Start == Int64.MaxValue ? -1 : item.Tail.Start));
				Console.Write(" => " + (item.Tail.End == Int64.MaxValue ? -1 : item.Tail.End));
				Console.Write(", RHead: " + (item.Head.Prev.Start == Int64.MaxValue ? -1 : item.Head.Prev.Start));
				Console.Write(" => " + (item.Head.Prev.End == Int64.MaxValue ? -1 : item.Head.Prev.End));
				Console.Write(", RTail: " + (item.Tail.Next.Start == Int64.MaxValue ? -1 : item.Tail.Next.Start));
				Console.Write(" => " + (item.Tail.Next.End == Int64.MaxValue ? -1 : item.Tail.Next.End));
				Console.Write("\n");

				DebugDumpPieceText(pad, item.Head, item.Tail, false);
				DebugDumpPieceText(pad, item.Head.Prev, item.Tail.Next, false);				
			}
			else
				Console.Write("HistoryHead\n");
				
			DebugDumpHistory(item.InternalFirstChild, indent + 1);
			item = item.InternalNextSibling;
		}
	}
	protected void DebugDumpHistory(string msg)
	{
		/*
		Console.WriteLine("\n" + msg + "\n========\n");

		InternalHistoryItem i = _History;
		while(i.InternalParent != null)
			i = i.InternalParent;
		DebugDumpHistory(i, 1); */
	}
	
	public string DebugGetPieces()
	{
		System.Text.StringBuilder tmp = new System.Text.StringBuilder();

		Piece p = Pieces;
		while((p = p.Next) != Pieces)
			tmp.AppendFormat("{{{0},{1}}}", p.Start, p.End);

		return tmp.ToString();
	}
}

