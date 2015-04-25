using System;
using System.Collections.Generic;

class PatternMatchBMH
{
	int[] Jump = new int[0x100];
	byte[] Pattern;
	byte[] PartialText;
	int PartialTextSize;
	
	public PatternMatchBMH()
	{
	}
	
	public void Initialize(byte[] pattern, bool caseSensitive)
	{
		Pattern = pattern;

		PartialText = new byte[Pattern.Length];
		PartialTextSize = 0;
		
		int i;
		for(i = 0; i < 0x100; ++i)
			Jump[i] = Pattern.Length;

		for(i = pattern.Length - 1; i >= 0; --i)
		{
			byte b = Pattern[i];
			int shift = Pattern.Length - i - 1;
			if(shift > 0 && shift < Jump[b])
				Jump[b] = shift;
		}
	}
	
	private int CompareByteArrays(byte[] array1, int offset1, byte[] array2, int offset2, int length)
	{
		for(int i = 0; i < length; ++i)
		{
			int result = array1[offset1 + i] - array2[offset2 + i];
			if(result != 0)
				return result;
		}
		
		return 0;
	}
	
	public IEnumerable<int> SearchBlock(byte[] block, int offset, int count)
	{
		int i = offset;
		int len = Pattern.Length;
		int end = offset + count;

		// If we're continuing a search that started in a previous block, we'll have
		// a few chars left over which might be part of the string we're searching for.
		// So we need to do the comparissons in two parts until we're working entirely
		// in the new block, then we can continue as normal.
		if(PartialTextSize > 0)
		{
			// Wind the pointer back to where it would be if all the data was in this packet
			// It's ok to do this as we're only looking at the last byte which is definately 
			// in this new packet
			i -= PartialTextSize - 1;

			// This while loop breaks when we get to the end of the block _or_ when we're
			// working entirely in the new block and can use the main loop below
			while((i + len) < end && i < offset)
			{
				// Get the last character and convert it to lower case
				byte b = block[i + len - 1];
				// TODO: Case insensitive
				//if(c <= 'Z' && c >= 'A')
				//	c |= 32;
				
				// Comparison has to be done in two parts, first part in state->partialText
				// second part in the packet
				
				if(b == Pattern[len - 1] && 
				   CompareByteArrays(PartialText, PartialTextSize - (offset - i), Pattern, 0, offset - i) == 0  &&
				   CompareByteArrays(block, offset - i, Pattern, offset - i, len - (offset - i)) == 0)
				{
					i += len;
					offset = i;
					// Found pattern
					yield return offset - Pattern.Length;
				}
				else
				{
					// If no match, skip ahead x chars, as specified by the bad char jump table
					i += Jump[b];
				}
			}

			PartialTextSize = 0;
		}

		// Main boyer-moore-horspool string search loop
		while((i + len) < end)
		{
			// Get last character of potential match and convert to lower case
			byte b = block[i + len - 1];
			// TODO: Case insensitive
			//if(c <= 'Z' && c >= 'A')
			//	c |= 32;

			// If it matches, check the whole string for a match
			if(b == Pattern[len - 1] && CompareByteArrays(block, i, Pattern, 0, len) == 0)
			{
				i += len;
				offset = i;
				// Found pattern
				yield return offset - Pattern.Length;
			}
			else
			{
				// If no match, skip ahead x chars, as specified by the bad char jump table
				i += Jump[b];
			}
		}

		offset = i;
		
		// finished searching in this packet, but do we need those last few chars next time?
		if(end - offset > 0)
		{
			if(PartialText.Length < PartialTextSize + (end - offset))
			{
				Console.WriteLine("BMH Partial Text Resize: " + (PartialTextSize + (end - offset)));
				Array.Resize(ref PartialText, (PartialTextSize + (end - offset)));
			}
			
			Array.Copy(block, offset, PartialText, PartialTextSize, end - offset);
			PartialTextSize += end - offset;
		}
	}
	
	public IEnumerable<int> SearchFinalBlock(byte[] block, int offset, int count)
	{
		return SearchBlock(block, offset, count);
	}
}
