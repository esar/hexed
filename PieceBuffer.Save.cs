using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;


public partial class PieceBuffer : IDisposable
{
	public class SavePlan
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
	}

	protected class InternalSavePlan : SavePlan
	{
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

		public void Execute(FileStream stream)
		{
			if(IsInPlace)
			{
				foreach(KeyValuePair<long, Piece> kvp in InPlacePieces)
				{
					stream.Seek(kvp.Key, SeekOrigin.Begin);
					kvp.Value.Write(stream, 0, kvp.Value.Length);
				}
			}
			else
			{
				Piece p = Pieces;
				while((p = p.Next) != Pieces)
					p.Write(stream, 0, p.Length);
			}
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
		if(CachedSavePlan != null)
			return CachedSavePlan;

		CachedSavePlan = BuildInPlaceSavePlan();
		if(CachedSavePlan == null)
		{
			CachedSavePlan = new InternalSavePlan();
			CachedSavePlan.IsInPlace = false;
			CachedSavePlan.TotalLength = Length;
			CachedSavePlan.WriteLength = Length;
		}

		CachedSavePlan.Pieces = Pieces;
			
		return CachedSavePlan;
	}		

	public bool CanSaveInPlace
	{
		get { return BuildSavePlan().IsInPlace == true; }
	}

	public void SaveAs(string filename)
	{
		lock(Lock)
		{
			// TODO: Protect against overwritting the original file
			//
			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
			Piece p = Pieces;
			while((p = p.Next) != Pieces)
				p.Write(fs, 0, p.Length);
			fs.Close();
		}
	}

	public void SaveInPlace()
	{
		lock(Lock)
		{
			InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
			if(plan.IsInPlace == false)
				throw new Exception("Can't save in-place");
			System.Console.WriteLine(plan.ToString());

			FileStream stream = OriginalFileBlock.GetWriteStream();
			plan.Execute(stream);
			OriginalFileBlock.ReleaseWriteStream();

			Reopen();
		}
	}

	public void Save()
	{
		lock(Lock)
		{
			InternalSavePlan plan = (InternalSavePlan)BuildSavePlan();
			bool wasInPlace = plan.IsInPlace;
			plan.IsInPlace = false;
			System.Console.WriteLine(plan.ToString());

			// Make sure we have write access to the original file
			OriginalFileBlock.GetWriteStream();

			// open temp file
			string origFilename = _FileName;
			string tempFilename;
			do
			{
				tempFilename = origFilename + Path.GetRandomFileName();
			}
			while(File.Exists(tempFilename));

			System.Console.WriteLine("using tmp file: " + tempFilename);
			FileStream stream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write);

			// write to temp file
			plan.Execute(stream);
			stream.Close();

			OriginalFileBlock.ReleaseWriteStream();

			InternalMarkCollection oldMarks = _Marks;
			Close();
			File.Delete(origFilename);
			File.Move(tempFilename, origFilename);
			Open(origFilename);
			_Marks = oldMarks;
			_Marks.UpdateAfterReopen(Pieces);

			plan.IsInPlace = wasInPlace;
		}
	}
}

