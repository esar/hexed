/*
	This file is part of HexEd

	Copyright (C) 2008-2015  Stephen Robinson <hacks@esar.org.uk>

	HexEd is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License version 2 as 
	published by the Free Software Foundation.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

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

	protected void DebugDumpHistory(HistoryTree history, InternalHistoryItem item, int indent)
	{
		const string padding = "                                                                                ";
		string pad = padding.Substring(0, indent * 4);

		while(item != null)
		{
			if(item == history.Current)
				Console.Write("=>" + pad.Substring(2));
			else
				Console.Write(pad);

			if(item.GroupHistory != null)
			{
				Console.Write("Group: " + item.Operation + "\n");

				InternalHistoryItem i = item.GroupHistory.Current;
				if(i != null)
				{
					while(i.InternalParent != null)
						i = i.InternalParent;
					DebugDumpHistory(item.GroupHistory, i, indent + 1);
				}
			}
			else
			{
				Console.Write(item.Operation + ": ");
				if(item.Head != null && item.Tail != null)
				{
					Console.Write("Head: " + (item.Head.Start == Int64.MaxValue ? -1 : item.Head.Start));
					Console.Write(" => " + (item.Head.End == Int64.MaxValue ? -1 : item.Head.End));
					Console.Write(", Tail: " + (item.Tail.Start == Int64.MaxValue ? -1 : item.Tail.Start));
					Console.Write(" => " + (item.Tail.End == Int64.MaxValue ? -1 : item.Tail.End));
					Console.Write(", RHead: " + (item.Head.Prev.Start == Int64.MaxValue ? -1 : item.Head.Prev.Start));
					Console.Write(" => " + (item.Head.Prev.End == Int64.MaxValue ? -1 : item.Head.Prev.End));
					Console.Write(", RTail: " + (item.Tail.Next.Start == Int64.MaxValue ? -1 : item.Tail.Next.Start));
					Console.Write(" => " + (item.Tail.Next.End == Int64.MaxValue ? -1 : item.Tail.Next.End));
				}
				Console.Write("\n");

				if(item.Head != null && item.Tail != null)
				{
					//DebugDumpPieceText(pad, item.Head, item.Tail, false);
					//DebugDumpPieceText(pad, item.Head.Prev, item.Tail.Next, false);
				}
			}
				
			DebugDumpHistory(history, item.InternalFirstChild, indent + 1);
			item = item.InternalNextSibling;
		}
	}

	protected void DebugDumpHistory(string msg)
	{
		return;

		Console.WriteLine("\n" + msg + "\n========\n");

		InternalHistoryItem i = _History.Current;
		while(i.InternalParent != null)
			i = i.InternalParent;
		DebugDumpHistory(_History, i, 1); 
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

