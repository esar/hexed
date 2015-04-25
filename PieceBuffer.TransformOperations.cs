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
	protected class TransformOperationDataSource : IBlock
	{
		protected Piece StartPiece;
		protected long StartOffset;
		protected Piece EndPiece;
		protected long EndOffset;
		protected long _Length;
		
		public byte this[long index]
		{
			get { return 0; }
			set {}
		}
		
		public long Length { get { return _Length; } }
		public long Used { get { return 0; } set {} }

		public bool CanSaveInPlace
		{
			get 
			{
				Piece p = StartPiece;
				while(true)
				{
					if(!p.CanSaveInPlace)
						return false;
					if(p == EndPiece)
						break;
					p = p.Next;
				}

				return true;
			}
		}		
		
		public TransformOperationDataSource(Piece startPiece, long startOffset, Piece endPiece, long endOffset, long length)
		{
			StartPiece = startPiece;
			StartOffset = startOffset;
			EndPiece = endPiece;
			EndOffset = endOffset;
			_Length = length;
		}
		
		public  bool GetOffsetsRelativeToBlock(IBlock block, out long start, out long end)
		{
			if(block == this)
			{
				start = 0;
				end = _Length;
			}
			
			long s, e;
			if(StartPiece.GetOffsetsRelativeToBlock(block, out s, out e))
			{
				start = s + StartOffset;
				if(EndPiece.GetOffsetsRelativeToBlock(block, out s, out e))
				{
					end = e + EndOffset;
					return true;
				}
			}

			start = StartOffset;
			end = EndOffset;
			return false; // Either start or end piece (or both) isn't related to block
		}

		public void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Piece p = StartPiece;
			
			if(length > _Length)
				throw new ArgumentOutOfRangeException("length", "The requested length is longer than the available data");
			
			start += StartOffset;
			while(start >= p.Length)
			{
				start -= p.Length;
				p = p.Next;
			}
			
			while(length > 0)
			{
				long len = length > p.Length ? p.Length : length;
				p.GetBytes(start, len, dst, dstOffset);
				length -= len;
				dstOffset += len;
				p = p.Next;
			}
		}
		
		public void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
		}
		
		public virtual void Write(InternalSavePlan dest, long start, long length)
		{
			byte[] data = new byte[4096];

			while(length > 0)
			{
				int len = length > 4096 ? 4096 : (int)length;
				GetBytes(start, len, data, 0);
				dest.Write(data, 0, len);
				start += len;
				length -= len;
			}
		}
	}
	
	protected interface ITransformOperation
	{
		bool CanSaveInPlace { get; }
		void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset);
	}
	
	protected class TransformOperationOr : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationOr(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] |= Constant[(start++) % Constant.Length];
		}
	}
	
	protected class TransformOperationAnd : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationAnd(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}
		
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] &= Constant[(start++) % Constant.Length];
		}
	}
	
	protected class TransformOperationXor : ITransformOperation
	{
		byte[] Constant;
		
		public TransformOperationXor(byte[] constant)
		{
			Constant = new byte[constant.Length];
			Array.Copy(constant, Constant, constant.Length);
		}
		
		public bool CanSaveInPlace
		{
			get { return true; }
		}
		
		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= Constant[(start++) % Constant.Length];
		}
	}

	protected class TransformOperationInvert : ITransformOperation
	{
		public bool CanSaveInPlace
		{
			get { return true; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(start, length, dest, destOffset);
			for(int i = (int)destOffset; i < destOffset + length; ++i)
				dest[i] ^= 0xFF;
		}
	}

	protected class TransformOperationReverse : ITransformOperation
	{
		public bool CanSaveInPlace
		{
			get { return false; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			source.GetBytes(source.Length - start - length, length, dest, destOffset);
			Array.Reverse(dest, (int)destOffset, (int)length);
		}
	}
	
	protected class TransformOperationShift : ITransformOperation
	{
		protected int Distance;
		
		public TransformOperationShift(int distance)
		{
			Distance = distance;
		}
		
		public bool CanSaveInPlace
		{
			get { return Distance < 0; }
		}

		public void GetTransformedBytes(IBlock source, long start, long length, byte[] dest, long destOffset)
		{
			int distance = Distance / 8;
			
			// Read into buffer, shifting by whole bytes
			start -= distance;
			if(distance > 0)
			{
				int off = (int)destOffset;
				int len = (int)length;
				
				if(start < 0)
				{
					Array.Clear(dest, off, (int)(0 - start));
					off -= (int)start;
					len += (int)start;
					start = 0;
				}
				
				source.GetBytes(start, len, dest, off);
			}
			else if(distance < 0)
			{
				int len = (int)length;
				if(start + len > source.Length)
					len = (int)(source.Length - start);
				
				source.GetBytes(start, len, dest, destOffset);
				int off = (int)destOffset + len;
				len = (int)length - len;
				
				if(len > 0)
					Array.Clear(dest, (int)off, (int)len);
			}
			
			// Adjust buffer, shifting by partial bytes
			distance = Distance % 8;
			if(distance < 0)
			{
				Console.WriteLine("distance: " + distance);
				Console.WriteLine("dstOff: {0}, len: {1}", destOffset, length);
				distance = 0 - distance;
				for(int i = (int)destOffset; i < (int)(destOffset + length) - 1; ++i)
				{
					Console.WriteLine("Shifting: " + i + ", by: " + distance);
					dest[i] = (byte)((dest[i] << distance) | (dest[i + 1] >> (8 - distance)));
				}
				dest[destOffset + length - 1] <<= distance;
			}
			else if(distance > 0)
			{
				dest[destOffset] >>= distance;
				for(int i = (int)destOffset + 1; i < (int)(destOffset + length); ++i)
					dest[i] = (byte)((dest[i - 1] << (8 - distance)) | (dest[i] >> distance));
			}
		}
	}

	protected class TransformPiece : Piece
	{
		protected ITransformOperation Op;
		
		public override bool CanSaveInPlace
		{
			get { return Block.CanSaveInPlace && Op.CanSaveInPlace; }
		}

		public override byte this[long index]
		{
			get { return 0; }
			set { throw new Exception("Can't set data in TransformPiece"); }
		}
		
		public TransformPiece(List<Piece> allocatedPieces, ITransformOperation op, IBlock source) : 
		       base(allocatedPieces, source, 0, source.Length) 
		{
			Op = op;
		}
		
		public override void SetBytes(long start, long length, byte[] src, long srcOffset)
		{
			throw new Exception("Can't set data in TransformPiece");
		}
		
		public override void GetBytes(long start, long length, byte[] dst, long dstOffset)
		{
			Op.GetTransformedBytes(Block, start, length, dst, dstOffset);
		}
		
		public override void Write(InternalSavePlan dest, long start, long length)
		{
			byte[] data = new byte[4096];

			while(length > 0)
			{
				int len = length > 4096 ? 4096 : (int)length;
				Op.GetTransformedBytes(Block, start, len, data, 0);
				dest.Write(data, 0, len);
				start += len;
				length -= len;
			}
		}
	}
}

