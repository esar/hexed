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
	public class SavePlan : IAsyncResult
	{
		protected long _TotalLength;
		public long TotalLength
		{
			get { return _TotalLength; }
		}

		protected long _WriteLength;
		public long WriteLength
		{
			get { return _WriteLength; }
		}

		protected long _BlockCount;
		public long BlockCount
		{
			get { return _BlockCount; }
		}

		protected bool _IsInPlace;
		public bool IsInPlace
		{
			get { return _IsInPlace; }
		}

		protected long _LengthWritten;
		public long LengthWritten
		{
			get { return _LengthWritten; }
		}

		protected long _BlocksWritten;
		public long BlocksWritten
		{
			get { return _BlocksWritten; }
		}

		protected bool _ShouldAbort;
		public void Abort()
		{
			_ShouldAbort = true;
		}

		protected bool _Aborted;
		public bool Aborted
		{
			get { return _Aborted; }
		}


		//
		// IAsyncResult
		//
		protected IAsyncResult _AsyncResult;

		protected object _AsyncState;
		public object AsyncState
		{
			get { return _AsyncState; }
		}

		public WaitHandle AsyncWaitHandle
		{
			get 
			{
				if(_AsyncResult != null)
					return _AsyncResult.AsyncWaitHandle;
				else
					return new EventWaitHandle(true, EventResetMode.ManualReset);
			}
		}

		public bool CompletedSynchronously
		{
			get 
			{ 
				if(_AsyncResult != null) 
					return _AsyncResult.CompletedSynchronously; 
				else 
					return true; 
			}
		}

		public bool IsCompleted
		{
			get 
			{ 
				if(_AsyncResult != null)
					return _AsyncResult.IsCompleted;
				else
					return true; 
			}
		}
	}

	protected class AbortSaveException : Exception {}

	protected class InternalSavePlan : SavePlan
	{

		public AsyncCallback AsyncCallback;

		protected Stream _Stream;
		public Stream Stream
		{
			get { return _Stream; }
			set { _Stream = value; }
		}

		public Piece Pieces;
		public List< KeyValuePair<long, Piece> > InPlacePieces;

		public new long TotalLength
		{
			get { return _TotalLength; }
			set { _TotalLength = value; }
		}
		public new long WriteLength
		{
			get { return _WriteLength; }
			set { _WriteLength = value; }
		}
		public new long BlockCount
		{
			get { return _BlockCount; }
			set { _BlockCount = value; }
		}
		public new bool IsInPlace
		{
			get { return _IsInPlace; }
			set { _IsInPlace = value; }
		}
		public new long LengthWritten
		{
			get { return _LengthWritten; }
			set { _LengthWritten = value; }
		}
		public new long BlocksWritten
		{
			get { return _BlocksWritten; }
			set { _BlocksWritten = value; }
		}
		public new bool Aborted
		{
			get { return _Aborted; }
			set { _Aborted = value; }
		}
		
		public IAsyncResult AsyncResult
		{
			get { return _AsyncResult; }
			set { _AsyncResult = value; }
		}

		public new object AsyncState
		{
			get { return _AsyncState; }
			set { _AsyncState = value; }
		}

		public Delegate ExecuteDelegate;

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.Append("SavePlan:\n");
			sb.Append("    InPlace: " + IsInPlace + "\n");
			sb.Append("    TotalLength: " + TotalLength + "\n");
			sb.Append("    WriteLength: " + WriteLength + "\n");
			sb.Append("    Blocks: " + BlockCount + "\n");
			if(IsInPlace)
			{
				foreach(KeyValuePair<long, Piece> kvp in InPlacePieces)
					sb.Append("        " + kvp.Value.Length + "@" + kvp.Key + "\n");
			}
			else
			{
				long offset = 0;
				Piece p = Pieces;
				while((p = p.Next) != Pieces)
				{
					sb.Append("        " + p.Length + "@" + offset + "\n");
					offset += p.Length;
				}
			}

			return sb.ToString();
		}

		public void Write(byte[] buffer, int offset, int length)
		{
			if(_ShouldAbort)
				throw new AbortSaveException();

			_Stream.Write(buffer, offset, length);
			LengthWritten += length;

			//System.Threading.Thread.Sleep(25);
		}

		public InternalSavePlan Clone()
		{
			return (InternalSavePlan)this.MemberwiseClone();
		}
	}



	protected InternalSavePlan BuildInPlaceSavePlan()
	{
		if(OriginalFileBlock == null)
		{
			//System.Console.WriteLine("CanSaveInPlace: no original file block, can't save in-place");
			return null;
		}

		InternalSavePlan plan = new InternalSavePlan();
		plan.InPlacePieces = new List< KeyValuePair<long, Piece> >();
		plan.IsInPlace = true;
		plan.TotalLength = Length;
		plan.WriteLength = 0;
		plan.BlockCount = 0;

		bool isContiguous = true;
		long offset = 0;
		Piece p = Pieces;
		while((p = p.Next) != Pieces)
		{
			if(!p.CanSaveInPlace)
			{
				//System.Console.WriteLine("CanSaveInPlace: piece can't be saved in place");
				return null;
			}

			long start, end = 0;
			if(p.GetOffsetsRelativeToBlock(OriginalFileBlock, out start, out end))
			{
				//System.Console.WriteLine("CanSaveInPlace: orig piece: offset: " + offset + 
				//			 ", start: " + start + 
				//			 ", end: " + end);

				if(start < offset || end != start + p.Length)
				{
					// Piece references an earlier part of original file so
					// we can't do an in-place save as we'll have overwritten
					// it by the time we need it.
					//System.Console.WriteLine("Save: earlier reference, can't in-place save");
					return null;
				}

				if(start != offset)
					isContiguous = false;
			}
			else
			{
				//System.Console.WriteLine("CanSaveInPlace: new piece: offset: " + offset +
				//			 ", length: " + p.Length);
			}

			if(!isContiguous || p.Block != OriginalFileBlock)
			{
				plan.InPlacePieces.Add(new KeyValuePair<long, Piece>(offset, p));
				plan.BlockCount += 1;
				plan.WriteLength += p.Length;
			}

			offset += p.Length;
		}

		return plan;
	}

	public SavePlan BuildSavePlan()
	{
		if(CachedSavePlan == null)
		{
			CachedSavePlan = BuildInPlaceSavePlan();
			if(CachedSavePlan == null)
			{
				CachedSavePlan = new InternalSavePlan();
				CachedSavePlan.IsInPlace = false;
				CachedSavePlan.TotalLength = Length;
				CachedSavePlan.WriteLength = Length;
			}

			CachedSavePlan.Pieces = Pieces;
		}
			
		return CachedSavePlan != null ? CachedSavePlan.Clone() : null;
	}		

	public bool CanSaveInPlace
	{
		get { return BuildSavePlan().IsInPlace == true; }
	}

	protected void ExecuteSavePlan(InternalSavePlan plan, Stream stream)
	{
		plan.Stream = stream;

		if(plan.IsInPlace)
		{
			foreach(KeyValuePair<long, Piece> kvp in plan.InPlacePieces)
			{
				stream.Seek(kvp.Key, SeekOrigin.Begin);
				kvp.Value.Write(plan, 0, kvp.Value.Length);
				plan.BlocksWritten += 1;
			}
		}
		else
		{
			Piece p = plan.Pieces;
			while((p = p.Next) != plan.Pieces)
			{
				p.Write(plan, 0, p.Length);
				plan.BlocksWritten += 1;
			}
		}
	}

	protected delegate void SaveAsDelegate(InternalSavePlan plan, string filename);
	protected void SaveAs(InternalSavePlan plan, string filename)
	{
		// TODO: Protect against overwritting the original file
		//
		bool savedSuccessfully = false;
		FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
		try
		{
			ExecuteSavePlan(plan, fs);
			savedSuccessfully = true;
		}
		finally
		{
			fs.Close();
			if(!savedSuccessfully)
				File.Delete(filename);
		}
	}

	public void SaveAs(string filename)
	{
		lock(Lock)
		{
			InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
			plan.IsInPlace = false;
			SaveAs(plan, filename);
		}
	}

	public void SaveCompleteCallback(IAsyncResult result)
	{
		InternalSavePlan plan = (InternalSavePlan)result.AsyncState;
		plan.AsyncCallback(plan);
	}

	public SavePlan BeginSaveAs(string filename, AsyncCallback callback, object state)
	{
		InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
		plan.IsInPlace = false;
		plan.Aborted = false;
		plan.AsyncCallback = callback;
		plan.AsyncState = state;
		plan.ExecuteDelegate = new SaveAsDelegate(SaveAs);
		plan.AsyncResult = ((SaveAsDelegate)plan.ExecuteDelegate).BeginInvoke(plan, filename, 
		                          new AsyncCallback(SaveCompleteCallback), plan);
		return plan;
	}

	public void EndSaveAs(SavePlan plan)
	{
		InternalSavePlan p = (InternalSavePlan)plan;
		try
		{
			((SaveAsDelegate)p.ExecuteDelegate).EndInvoke(p.AsyncResult);
		}
		catch(AbortSaveException e)
		{
			System.Console.WriteLine("Caught abort save exception: " + e);
		}
	}

	protected delegate void SaveDelegate(InternalSavePlan plan);
	protected void InternalSave(InternalSavePlan plan)
	{
		if(plan.IsInPlace)
		{
			FileStream stream = OriginalFileBlock.GetWriteStream();
			try
			{
				ExecuteSavePlan(plan, stream);
			}
			finally
			{
				OriginalFileBlock.ReleaseWriteStream();
				Reopen();
			}
		}
		else
		{
			// Make sure we have write access to the original file
			OriginalFileBlock.GetWriteStream();

			string origFilename = _FileName;
			string tempFilename = null;
			FileStream stream = null;
			bool savedSuccessfully = false;

			try
			{
				// open temp file
				do
				{
					tempFilename = origFilename + Path.GetRandomFileName();
				}
				while(File.Exists(tempFilename));

				System.Console.WriteLine("using tmp file: " + tempFilename);
				stream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write);

				// write to temp file
				ExecuteSavePlan(plan, stream);
				stream.Close();
				stream = null;
				savedSuccessfully = true;
			}
			finally
			{
				OriginalFileBlock.ReleaseWriteStream();
				
				if(savedSuccessfully)
				{
					InternalMarkCollection oldMarks = _Marks;
					Close();
					File.Delete(origFilename);
					File.Move(tempFilename, origFilename);
					Open(origFilename);
					_Marks = oldMarks;
					_Marks.UpdateAfterReopen(Pieces);
				}
				else
				{
					if(stream != null)
					{
						stream.Close();
						File.Delete(tempFilename);
					}

					Reopen();
				}
			}
		}
	}

	public void Save(bool allowInPlace)
	{
		InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
		if(!allowInPlace)
			plan.IsInPlace = false;
		System.Console.WriteLine(plan.ToString());

		InternalSave(plan);
	}

	public SavePlan BeginSave(bool allowInPlace, AsyncCallback callback, object state)
	{
		InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
		if(!allowInPlace)
			plan.IsInPlace = false;
		plan.Aborted = false;
		plan.AsyncCallback = callback;
		plan.AsyncState = state;
		plan.ExecuteDelegate = new SaveDelegate(InternalSave);
		plan.AsyncResult = ((SaveDelegate)plan.ExecuteDelegate).BeginInvoke(plan, 
		                          new AsyncCallback(SaveCompleteCallback), plan);
		return plan;
	}

	public void EndSave(SavePlan plan)
	{
		InternalSavePlan p = (InternalSavePlan)plan;
		try
		{
			((SaveDelegate)p.ExecuteDelegate).EndInvoke(p.AsyncResult);
		}
		catch(AbortSaveException e)
		{
			System.Console.WriteLine("Caught abort save exception: " + e);
		}
	}
}

