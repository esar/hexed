using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;


[TestFixture()]
public class PieceBufferTest
{
	PieceBuffer b;
	List<PieceBuffer.BufferChangedEventArgs> Changes;
	
	public PieceBufferTest()
	{
	}

	[SetUp()]
	public void Setup()
	{
		Changes = new List<PieceBuffer.BufferChangedEventArgs>();
		b = new PieceBuffer();
		b.Changed += OnBufferChanged;
	}
	
	[TearDown()]
	public void Cleanup()
	{
		b = null;
		Changes = null;
	}
		
	protected void OnBufferChanged(object sender, PieceBuffer.BufferChangedEventArgs e)
	{
		Changes.Add(e);
	}

	public string GetChanges()
	{
		StringBuilder tmp = new StringBuilder();
		
		foreach(PieceBuffer.BufferChangedEventArgs change in Changes)
			tmp.AppendFormat("{{{0},{1}}}", change.StartOffset, change.EndOffset);
		Changes.Clear();
		
		return tmp.ToString();
	}
	
	public string GetMarks()
	{
		return b.Marks.ToString();
	}
	
	public string GetPieces()
	{
		return b.DebugGetPieces();
	}
	
	public string GetText()
	{
		byte[] tmp = new byte[b.Marks.End.Position - b.Marks.Start.Position];
		b.GetBytes(b.Marks.Start, b.Marks.End, tmp, b.Marks.End.Position - b.Marks.Start.Position);
		return System.Text.ASCIIEncoding.ASCII.GetString(tmp);
	}
	
	[Test]
	public void MarkDestroyInternalMarks()
	{
		// Attempting to destroy internal marks (insert/start/end) should fail
		b.Marks.Remove(b.Marks.Insert);
		b.Marks.Remove(b.Marks.Start);
		b.Marks.Remove(b.Marks.End);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
	}
	
	[Test]
	public void InsertSingleChar()
	{
		// insert a character
		b.Insert((byte)'a');
		Assert.AreEqual(GetMarks(), "{0,0}{0,1}{0,1}");
		Assert.AreEqual(GetPieces(), "{0,1}");
		Assert.AreEqual(GetText(), "a");
		Assert.AreEqual(GetChanges(), "{0,0}");
		
		// insert a second character
		b.Insert((byte)'b');
		Assert.AreEqual(GetMarks(), "{0,0}{0,2}{0,2}");
		Assert.AreEqual(GetPieces(), "{0,1}{1,2}");
		Assert.AreEqual(GetText(), "ab");
		Assert.AreEqual(GetChanges(), "{1,1}");
		
		// seek back one char, insert another
		b.Marks.Insert.Position -= 1;
		b.Insert((byte)'c');
		Assert.AreEqual(GetMarks(), "{0,0}{0,2}{0,3}");
		Assert.AreEqual(GetPieces(), "{0,1}{2,3}{1,2}");
		Assert.AreEqual(GetText(), "acb");
		Assert.AreEqual(GetChanges(), "{1,1}");
		
		// seek forward one char, insert another
		b.Marks.Insert.Position += 1;
		b.Insert((byte)'d');
		Assert.AreEqual(GetMarks(), "{0,0}{0,4}{0,4}");
		Assert.AreEqual(GetPieces(), "{0,1}{2,3}{1,2}{3,4}");
		Assert.AreEqual(GetText(), "acbd");
		Assert.AreEqual(GetChanges(), "{3,3}");
		
		// seek back past beginning, insert another
		b.Marks.Insert.Position -= 100;
		b.Insert((byte)'e');
		Assert.AreEqual(GetMarks(), "{0,0}{0,1}{0,5}");
		Assert.AreEqual(GetPieces(), "{4,5}{0,1}{2,3}{1,2}{3,4}");
		Assert.AreEqual(GetText(), "eacbd");
		Assert.AreEqual(GetChanges(), "{0,0}");
		
		// seek forward past end, insert another
		b.Marks.Insert.Position += 100;
		b.Insert((byte)'f');
		Assert.AreEqual(GetMarks(), "{0,0}{0,6}{0,6}");
		Assert.AreEqual(GetPieces(), "{4,5}{0,1}{2,3}{1,2}{3,4}{5,6}");
		Assert.AreEqual(GetText(), "eacbdf");
		Assert.AreEqual(GetChanges(), "{5,5}");
	}

	[Test]
	public void InsertString()
	{
		// insert a string
		b.Insert("the quick ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,10}");
		Assert.AreEqual(GetText(), "the quick ");
		
		// insert a second string
		b.Insert("over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,28}{0,28}");
		Assert.AreEqual(GetPieces(), "{0,10}{10,28}");
		Assert.AreEqual(GetText(), "the quick over the lazy dog.");
		
		// seek backwards to piece boundary, insert another string
		b.Marks.Insert.Position -= 18;
		b.Insert("brown fox jumps ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,26}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,10}{28,44}{10,28}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// seek forwards past the end, insert a string
		b.Marks.Insert.Position += 12345;
		b.Insert("<---{end}");
		Assert.AreEqual(GetMarks(), "{0,0}{0,53}{0,53}");
		Assert.AreEqual(GetPieces(), "{0,10}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.<---{end}");
		
		// seek backwards past the beginning, insert a string
		b.Marks.Insert.Position -= 98765;
		b.Insert("{begin}--->");
		Assert.AreEqual(GetMarks(), "{0,0}{0,11}{0,64}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick brown fox jumps over the lazy dog.<---{end}");
		
		// seek forward to piece boundary, insert a string
		b.Marks.Insert.Position += 10;
		b.Insert("and redish ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,32}{0,75}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{64,75}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick and redish brown fox jumps over the lazy dog.<---{end}");
		
		// seek backwards to middle of piece, insert a string
		b.Marks.Insert.Position -= 7;
		b.Insert("slightly ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,34}{0,84}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{64,68}{75,84}{68,75}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick and slightly redish brown fox jumps over the lazy dog.<---{end}");

		// seek forwards to middle of piece, insert a string
		b.Marks.Insert.Position += 32;
		b.Insert("big ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,70}{0,88}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{64,68}{75,84}{68,75}{28,44}{10,19}{84,88}{19,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick and slightly redish brown fox jumps over the big lazy dog.<---{end}");
	}	

	[Test]
	public void Remove()
	{
		b.Insert("the quick ");
		b.Insert("brown ");
		b.Insert("fox jumps ");
		b.Insert("over the ");
		b.Insert("lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,10}{10,16}{16,26}{26,35}{35,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");

		// remove a whole piece from the middle		
		b.Remove(10, 16);
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,38}");
		Assert.AreEqual(GetPieces(), "{0,10}{16,26}{26,35}{35,44}");
		Assert.AreEqual(GetText(), "the quick fox jumps over the lazy dog.");
		
		// remove a whole piece at the end
		b.Remove(29, 38);
		Assert.AreEqual(GetMarks(), "{0,0}{0,29}{0,29}");
		Assert.AreEqual(GetPieces(), "{0,10}{16,26}{26,35}");
		Assert.AreEqual(GetText(), "the quick fox jumps over the ");
		
		// remove a whole piece at the beginning
		b.Remove(0, 10);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,19}");
		Assert.AreEqual(GetPieces(), "{16,26}{26,35}");
		Assert.AreEqual(GetText(), "fox jumps over the ");
		
		// remove part spanning two pieces
		b.Remove(4, 14);
		Assert.AreEqual(GetMarks(), "{0,0}{0,4}{0,9}");
		Assert.AreEqual(GetPieces(), "{16,20}{30,35}");
		Assert.AreEqual(GetText(), "fox  the ");

		// remove middle of one piece
		b.Remove(1,2);
		Assert.AreEqual(GetMarks(), "{0,0}{0,1}{0,8}");
		Assert.AreEqual(GetPieces(), "{16,17}{18,20}{30,35}");
		Assert.AreEqual(GetText(), "fx  the ");
		
		// remove left end of one piece
		b.Remove(3,4);
		Assert.AreEqual(GetMarks(), "{0,0}{0,3}{0,7}");
		Assert.AreEqual(GetPieces(), "{16,17}{18,20}{31,35}");
		Assert.AreEqual(GetText(), "fx the ");
		
		// remove right end of one piece
		b.Remove(2, 3);
		Assert.AreEqual(GetMarks(), "{0,0}{0,2}{0,6}");
		Assert.AreEqual(GetPieces(), "{16,17}{18,19}{31,35}");
		Assert.AreEqual(GetText(), "fxthe ");
		
		// remove everything
		b.Remove(0,100);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
	}
	
	[Test]
	public void InsertReplace()
	{
		// insert some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// creates marks around the third word (brown) and replace it
		PieceBuffer.Mark m1 = b.Marks.Add(b.Marks.Insert.Position - 34);
		PieceBuffer.Mark m2 = b.Marks.Add(b.Marks.Insert.Position - 29);
		b.Insert(m1, m2, "red");
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,13}{0,13}{0,42}");
		Assert.AreEqual(GetPieces(), "{0,10}{44,47}{15,44}");
		Assert.AreEqual(GetText(), "the quick red fox jumps over the lazy dog.");

		// existing marks should surround new word, replace again
		b.Insert(m1, m2, "green");
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,15}{0,15}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,10}{47,52}{15,44}");
		Assert.AreEqual(GetText(), "the quick green fox jumps over the lazy dog.");
		
		// replace first piece
		b.Marks.Remove(m1);
		b.Marks.Remove(m2);
		m1 = b.Marks.Add(0);
		m2 = b.Marks.Add(15);
		b.Insert(m1, m2, "the big lazy");
		Assert.AreEqual(GetText(), "the big lazy fox jumps over the lazy dog.");
		
		// replace spanning multiple partial pieces
		b.Marks.Remove(m1);
		b.Marks.Remove(m2);
		m1 = b.Marks.Add(4);
		m2 = b.Marks.Add(22);
		b.Insert(m1, m2, "small dog runs");
		Assert.AreEqual(GetText(), "the small dog runs over the lazy dog.");
		
		// replace last piece
		b.Marks.Remove(m1);
		b.Marks.Remove(m2);
		m1 = b.Marks.Add(19);
		m2 = b.Marks.Add(100);
		b.Insert(m1, m2, "under the brown fox.");
		Assert.AreEqual(GetText(), "the small dog runs under the brown fox.");
	}

	[Test]
	public void InsertMarkPreservation()
	{
		b.Insert("fox jump dog");
		PieceBuffer.Mark m1 = b.Marks.Add(b.Marks.Insert.Position - 12);
		PieceBuffer.Mark m2 = b.Marks.Add(b.Marks.Insert.Position - 9);
		PieceBuffer.Mark m3 = b.Marks.Add(b.Marks.Insert.Position - 8);
		PieceBuffer.Mark m4 = b.Marks.Add(b.Marks.Insert.Position - 4);
		PieceBuffer.Mark m5 = b.Marks.Add(b.Marks.Insert.Position - 3);
		PieceBuffer.Mark m6 = b.Marks.Add(b.Marks.Insert.Position);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{8,8}{9,9}{0,12}{0,12}{0,12}");
		Assert.AreEqual(GetPieces(), "{0,12}");
		Assert.AreEqual(GetText(), "fox jump dog");
		
		b.Insert(m1, "the ");
		PieceBuffer.Mark m7 = b.Marks.Add(b.Marks.Insert.Position - 4);
		PieceBuffer.Mark m8 = b.Marks.Add(b.Marks.Insert.Position - 1);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{3,7}{4,8}{8,12}{9,13}{0,16}{0,16}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,12}");
		Assert.AreEqual(GetText(), "the fox jump dog");

		b.Insert(m4, "s over the");
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{3,7}{4,8}{0,22}{0,22}{1,23}{0,26}{0,26}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,8}{16,26}{8,12}");
		Assert.AreEqual(GetText(), "the fox jumps over the dog");

		b.Insert(m6, ".");
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{3,7}{4,8}{0,22}{1,23}{0,27}{0,27}{0,27}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,8}{16,26}{8,12}{26,27}");
		Assert.AreEqual(GetText(), "the fox jumps over the dog.");
	}

	[Test]
	public void InsertMarkPreservationReversed()
	{
		b.Insert("fox jump dog");
		PieceBuffer.Mark m1 = b.Marks.Add(b.Marks.Insert.Position);
		PieceBuffer.Mark m2 = b.Marks.Add(b.Marks.Insert.Position - 3);
		PieceBuffer.Mark m3 = b.Marks.Add(b.Marks.Insert.Position - 4);
		PieceBuffer.Mark m4 = b.Marks.Add(b.Marks.Insert.Position - 8);
		PieceBuffer.Mark m5 = b.Marks.Add(b.Marks.Insert.Position - 9);
		PieceBuffer.Mark m6 = b.Marks.Add(b.Marks.Insert.Position - 12);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{8,8}{9,9}{0,12}{0,12}{0,12}");
		Assert.AreEqual(GetPieces(), "{0,12}");
		Assert.AreEqual(GetText(), "fox jump dog");
		
		b.Insert(m6, "the ");
		PieceBuffer.Mark m7 = b.Marks.Add(b.Marks.Insert.Position - 1);
		PieceBuffer.Mark m8 = b.Marks.Add(b.Marks.Insert.Position - 4);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{3,7}{4,8}{8,12}{9,13}{0,16}{0,16}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,12}");
		Assert.AreEqual(GetText(), "the fox jump dog");

		b.Insert(m3, "s over the");
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{3,7}{4,8}{0,22}{0,22}{1,23}{0,26}{0,26}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,8}{16,26}{8,12}");
		Assert.AreEqual(GetText(), "the fox jumps over the dog");

		b.Insert(m1, ".");
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{3,7}{4,8}{0,22}{1,23}{0,27}{0,27}{0,27}");
		Assert.AreEqual(GetPieces(), "{12,16}{0,8}{16,26}{8,12}{26,27}");
		Assert.AreEqual(GetText(), "the fox jumps over the dog.");
	}

	[Test]
	public void RemoveMarkPreservation()
	{
		// add some text and mark the beginning and end of each word
		b.Insert("the quick brown fox");
		PieceBuffer.Mark m1 = b.Marks.Add(b.Marks.Insert.Position - 19);
		PieceBuffer.Mark m2 = b.Marks.Add(b.Marks.Insert.Position - 16);
		PieceBuffer.Mark m3 = b.Marks.Add(b.Marks.Insert.Position - 15);
		PieceBuffer.Mark m4 = b.Marks.Add(b.Marks.Insert.Position - 10);
		PieceBuffer.Mark m5 = b.Marks.Add(b.Marks.Insert.Position - 9);
		PieceBuffer.Mark m6 = b.Marks.Add(b.Marks.Insert.Position - 4);
		PieceBuffer.Mark m7 = b.Marks.Add(b.Marks.Insert.Position - 3);
		PieceBuffer.Mark m8 = b.Marks.Add(b.Marks.Insert.Position);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{9,9}{10,10}{15,15}{16,16}{0,19}{0,19}{0,19}");
		Assert.AreEqual(GetPieces(), "{0,19}");
		Assert.AreEqual(GetText(), "the quick brown fox");
	
		// remove from the start of the second word to the end of the third
		b.Remove(m3, m6);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{0,4}{0,4}{0,4}{1,5}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,4}{15,19}");
		Assert.AreEqual(GetText(), "the  fox");

		// remove the same again, nothing should change
		b.Remove(m3, m6);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{0,4}{0,4}{0,4}{1,5}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,4}{15,19}");
		Assert.AreEqual(GetText(), "the  fox");

		// remove from beginning to mark that used to be end of third word
		b.Remove(m1, m6);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{1,1}{0,4}{0,4}");
		Assert.AreEqual(GetPieces(), "{15,19}");
		Assert.AreEqual(GetText(), " fox");
		
		// remove everything
		b.Remove(m1, m8);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);

		b.Insert("the lazy dog");
		Assert.AreEqual(GetText(), "the lazy dog");
	}

	[Test]
	public void RemoveMarkPreservationReversed()
	{
		// add some text and mark the beginning and end of each word
		b.Insert("the quick brown fox");
		PieceBuffer.Mark m1 = b.Marks.Add(b.Marks.Insert.Position);
		PieceBuffer.Mark m2 = b.Marks.Add(b.Marks.Insert.Position - 3);
		PieceBuffer.Mark m3 = b.Marks.Add(b.Marks.Insert.Position - 4);
		PieceBuffer.Mark m4 = b.Marks.Add(b.Marks.Insert.Position - 9);
		PieceBuffer.Mark m5 = b.Marks.Add(b.Marks.Insert.Position - 10);
		PieceBuffer.Mark m6 = b.Marks.Add(b.Marks.Insert.Position - 15);
		PieceBuffer.Mark m7 = b.Marks.Add(b.Marks.Insert.Position - 16);
		PieceBuffer.Mark m8 = b.Marks.Add(b.Marks.Insert.Position - 19);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{9,9}{10,10}{15,15}{16,16}{0,19}{0,19}{0,19}");
		Assert.AreEqual(GetPieces(), "{0,19}");
		Assert.AreEqual(GetText(), "the quick brown fox");
	
		// remove from the start of the second word to the end of the third
		b.Remove(m3, m6);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{0,4}{0,4}{0,4}{1,5}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,4}{15,19}");
		Assert.AreEqual(GetText(), "the  fox");

		// remove the same again, nothing should change
		b.Remove(m3, m6);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{0,4}{0,4}{0,4}{0,4}{0,4}{1,5}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,4}{15,19}");
		Assert.AreEqual(GetText(), "the  fox");

		// remove from beginning to mark that used to be end of third word
		b.Remove(m8, m3);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{1,1}{0,4}{0,4}");
		Assert.AreEqual(GetPieces(), "{15,19}");
		Assert.AreEqual(GetText(), " fox");
		
		// remove everything
		b.Remove(m1, m8);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);

		b.Insert("the lazy dog");
		Assert.AreEqual(GetText(), "the lazy dog");
	}

	[Test]
	public void InsertHuge()
	{
		byte[] data = new byte[32768];

		// two whole blocks				
		b.Insert(data, 8192);
		Assert.AreEqual(GetMarks(), "{0,0}{0,8192}{0,8192}");
		Assert.AreEqual(GetPieces(), "{0,4096}{0,4096}");
	
		// partial block, then two whole blocks
		b.Insert(data, 10);
		b.Insert(data, 8192);
		Assert.AreEqual(GetMarks(), "{0,0}{0,16394}{0,16394}");
		Assert.AreEqual(GetPieces(), "{0,4096}{0,4096}{0,10}{10,4096}{0,4096}{0,10}");
	}

	[Test]
	public void Copy()
	{
		// Add some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");

		// Copy the word 'brown'
		PieceBuffer.Mark srcStart = b.Marks.Add(10);
		PieceBuffer.Mark srcEnd = b.Marks.Add(16);
		PieceBuffer.Mark dstStart = b.Marks.Add(40);
		PieceBuffer.Mark dstEnd = b.Marks.Add(40);
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetMarks(), "{0,0}{10,10}{16,16}{0,46}{0,46}{0,46}{0,50}");
		Assert.AreEqual(GetPieces(), "{0,40}{10,16}{40,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.");
		
		// Copy the whole buffer (including previous copy)
		srcStart.Position = 0;
		srcEnd.Position = 1000;
		dstStart.Position = 1000;
		dstEnd.Position = 1000;
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,100}{0,100}{0,100}{0,100}{0,100}");
		Assert.AreEqual(GetPieces(), "{0,40}{10,16}{40,44}{0,40}{10,16}{40,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.the quick brown fox jumps over the lazy brown dog.");
		
		// Copy last 'lazy' over first 'quick'
		srcStart.Position = 85;
		srcEnd.Position = 89;
		dstStart.Position = 4;
		dstEnd.Position = 9;
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetText(), "the lazy brown fox jumps over the lazy brown dog.the quick brown fox jumps over the lazy brown dog.");
	}
	
	[Test]
	public void Move()
	{
		// Add some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");

		// Move 'brown'
		PieceBuffer.Mark srcStart = b.Marks.Add(10);
		PieceBuffer.Mark srcEnd = b.Marks.Add(16);
		PieceBuffer.Mark dstStart = b.Marks.Add(40);
		PieceBuffer.Mark dstEnd = b.Marks.Add(40);
		b.Move(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}{0,10}{0,40}{0,40}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,10}{16,40}{10,16}{40,44}");
		Assert.AreEqual(GetText(), "the quick fox jumps over the lazy brown dog.");
	}
	
	[Test]
	public void ClipboardCopy()
	{
		// Add some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// Create a clipboard range covering the word 'fox'
		PieceBuffer.Mark m1 = b.Marks.Add(16);
		PieceBuffer.Mark m2 = b.Marks.Add(19);
		PieceBuffer.ClipboardRange foxRange = b.ClipboardCopy(m1, m2);
		
		// Create a 2nd clipboard range covering the word 'dog'
		m1.Position = 40;
		m2.Position = 43;
		PieceBuffer.ClipboardRange dogRange = b.ClipboardCopy(m1, m2);
		
		// Paste the 'dog' clipboard range over the word 'fox'
		m1.Position = 16;
		m2.Position = 19;
		b.ClipboardPaste(m1, m2, dogRange);
		Assert.AreEqual(GetText(), "the quick brown dog jumps over the lazy dog.");
		
		// Paste the 'fox' clipboard range (that doesn't exist in the buffer anymore) over the word 'dog' 
		m1.Position = 40;
		m2.Position = 43;
		b.ClipboardPaste(m1, m2, foxRange);
		Assert.AreEqual(GetText(), "the quick brown dog jumps over the lazy fox.");
	}

	[Test]
	public void ClipboardCut()
	{
		// Add some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// Create a clipboard range covering 'quick brown fox'
		PieceBuffer.Mark m1 = b.Marks.Add(4);
		PieceBuffer.Mark m2 = b.Marks.Add(19);
		PieceBuffer.ClipboardRange foxRange = b.ClipboardCut(m1, m2);
		Assert.AreEqual(GetText(), "the  jumps over the lazy dog.");
		
		// Create a 2nd clipboard range covering 'lazy dog'
		m1.Position = 20;
		m2.Position = 28;
		PieceBuffer.ClipboardRange dogRange = b.ClipboardCut(m1, m2);
		Assert.AreEqual(GetText(), "the  jumps over the .");
		
		// Paste the 'lazy dog' clipboard range where 'quick brown fox' used to be
		m1.Position = 4;
		m2.Position = 4;
		b.ClipboardPaste(m1, m2, dogRange);
		Assert.AreEqual(GetText(), "the lazy dog jumps over the .");
		
		// Paste the 'quick brown fox' clipboard range where 'lazy dog' used to be
		m1.Position = 28;
		m2.Position = 28;
		b.ClipboardPaste(m1, m2, foxRange);
		Assert.AreEqual(GetText(), "the lazy dog jumps over the quick brown fox.");
	}
	
	[Test]
	public void UndoInsert()
	{
		// Add some text
		b.Insert("this ");
		b.Insert("is ");
		b.Insert("a ");
		b.Insert("test ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,15}{0,15}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}{10,15}");
		Assert.AreEqual(GetText(), "this is a test ");
		
		// First undo should remove "test "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}");
		Assert.AreEqual(GetText(), "this is a ");
		
		// Second undo should remove "a "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}");
		Assert.AreEqual(GetText(), "this is ");
		
		// Third undo should remove "is "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,5}{0,5}");
		Assert.AreEqual(GetPieces(), "{0,5}");
		Assert.AreEqual(GetText(), "this ");
		
		// Fourth undo should return us to an empty buffer
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		// Undo with no history should silently do nothing
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
	}
	
	[Test]
	public void UndoInsertMidPiece()
	{
		// Insert some text
		b.Insert("this is a test");
		Assert.AreEqual(GetMarks(), "{0,0}{0,14}{0,14}");
		Assert.AreEqual(GetPieces(), "{0,14}");
		Assert.AreEqual(GetText(), "this is a test");
		
		// Insert some more text in the middle of the previous piece
		b.Marks.Insert.Position -= 4;
		b.Insert("small ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,16}{0,20}");
		Assert.AreEqual(GetPieces(), "{0,10}{14,20}{10,14}");
		Assert.AreEqual(GetText(), "this is a small test");
		
		// Undo should return the original piece
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,14}{0,14}");
		Assert.AreEqual(GetPieces(), "{0,14}");
		Assert.AreEqual(GetText(), "this is a test");
		
		// Another undo should return us to an empty buffer
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
	}

	[Test]
	public void UndoRemove()
	{
		// Insert some text
		b.Insert("this is a test");
		Assert.AreEqual(GetMarks(), "{0,0}{0,14}{0,14}");
		Assert.AreEqual(GetPieces(), "{0,14}");
		Assert.AreEqual(GetText(), "this is a test");
		
		// Remove the last word ("test")
		b.Remove(10, 14);
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,10}");
		Assert.AreEqual(GetText(), "this is a ");
		
		// Remove the last word ("a ")
		b.Remove(8, 10);
		Assert.AreEqual(GetMarks(), "{0,0}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,8}");
		Assert.AreEqual(GetText(), "this is ");
		
		// Remove the last word ("is ");
		b.Remove(5, 8);
		Assert.AreEqual(GetMarks(), "{0,0}{0,5}{0,5}");
		Assert.AreEqual(GetPieces(), "{0,5}");
		Assert.AreEqual(GetText(), "this ");
		
		// Remove the remaining text ("this ")
		b.Remove(0, 5);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		// First undo should put back "this "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,5}{0,5}");
		Assert.AreEqual(GetPieces(), "{0,5}");
		Assert.AreEqual(GetText(), "this ");
		
		// Second undo should put back "is "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,8}");
		Assert.AreEqual(GetText(), "this is ");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,10}");
		Assert.AreEqual(GetText(), "this is a ");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,14}{0,14}");
		Assert.AreEqual(GetPieces(), "{0,14}");
		Assert.AreEqual(GetText(), "this is a test");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
	}

	[Test]
	public void UndoReplace()
	{
		Assert.Fail("Test needs writing");
	}
	
	[Test]
	public void UndoCopy()
	{
		// Add some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");

		// Copy the word 'brown'
		PieceBuffer.Mark srcStart = b.Marks.Add(10);
		PieceBuffer.Mark srcEnd = b.Marks.Add(16);
		PieceBuffer.Mark dstStart = b.Marks.Add(40);
		PieceBuffer.Mark dstEnd = b.Marks.Add(40);
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetMarks(), "{0,0}{10,10}{16,16}{0,46}{0,46}{0,46}{0,50}");
		Assert.AreEqual(GetPieces(), "{0,40}{10,16}{40,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.");
		
		// Copy the whole buffer (including previous copy)
		srcStart.Position = 0;
		srcEnd.Position = 1000;
		dstStart.Position = 1000;
		dstEnd.Position = 1000;
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,100}{0,100}{0,100}{0,100}{0,100}");
		Assert.AreEqual(GetPieces(), "{0,40}{10,16}{40,44}{0,40}{10,16}{40,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.the quick brown fox jumps over the lazy brown dog.");
		
		// Copy last 'lazy' over first 'quick'
		srcStart.Position = 85;
		srcEnd.Position = 89;
		dstStart.Position = 4;
		dstEnd.Position = 9;
		b.Copy(dstStart, dstEnd, srcStart, srcEnd);
		Assert.AreEqual(GetText(), "the lazy brown fox jumps over the lazy brown dog.the quick brown fox jumps over the lazy brown dog.");

		b.Undo();
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.the quick brown fox jumps over the lazy brown dog.");
		
		b.Undo();
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy brown dog.");

		b.Undo();
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		b.Undo();
		Assert.AreEqual(GetText(), String.Empty);
	}
	
	[Test]
	public void UndoMove()
	{
		Assert.Fail("Test needs writing");
	}
	
	[Test]
	public void RedoInsert()
	{
		// Add some text
		b.Insert("this ");
		b.Insert("is ");
		b.Insert("a ");
		b.Insert("test ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,15}{0,15}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}{10,15}");
		Assert.AreEqual(GetText(), "this is a test ");
		
		// Undo insert of "test "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}");
		Assert.AreEqual(GetText(), "this is a ");
		
		// Undo insert of "a "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}");
		Assert.AreEqual(GetText(), "this is ");
		
		// Undo insert of "is "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,5}{0,5}");
		Assert.AreEqual(GetPieces(), "{0,5}");
		Assert.AreEqual(GetText(), "this ");
		
		// Undo insert of "this "
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		// Redo insert of "this "
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,5}{0,5}");
		Assert.AreEqual(GetPieces(), "{0,5}");
		Assert.AreEqual(GetText(), "this ");
		
		// Redo insert of "is "
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,8}{0,8}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}");
		Assert.AreEqual(GetText(), "this is ");
		
		// Redo insert of "a "
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,10}{0,10}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}");
		Assert.AreEqual(GetText(), "this is a ");
		
		// Redo insert of "test "
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,15}{0,15}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}{10,15}");
		Assert.AreEqual(GetText(), "this is a test ");
		
		// Try and redo with nothing in forward history
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,15}{0,15}");
		Assert.AreEqual(GetPieces(), "{0,5}{5,8}{8,10}{10,15}");
		Assert.AreEqual(GetText(), "this is a test ");
	}
	
	[Test]
	public void RedoRemove()
	{
		// Insert some text
		b.Insert("the quick brown fox jumps over the lazy dog.");
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// Remove a word from the start of the buffer
		b.Remove(0, 4);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,40}");
		Assert.AreEqual(GetPieces(), "{4,44}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy dog.");
		
		// Remove a word from the end of the buffer
		b.Remove(35, 40);
		Assert.AreEqual(GetMarks(), "{0,0}{0,35}{0,35}");
		Assert.AreEqual(GetPieces(), "{4,39}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy");
		
		// Remove a word from the middle
		b.Remove(12, 16);
		Assert.AreEqual(GetMarks(), "{0,0}{0,12}{0,31}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,39}");
		Assert.AreEqual(GetText(), "quick brown jumps over the lazy");
		
		// Remove another word from the middle
		b.Remove(18, 23);
		Assert.AreEqual(GetMarks(), "{0,0}{0,18}{0,26}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,26}{31,39}");
		Assert.AreEqual(GetText(), "quick brown jumps the lazy");
		
		// Remove the remainder (three pieces at once)
		b.Remove(0, 100);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,26}{0,26}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,26}{31,39}");
		Assert.AreEqual(GetText(), "quick brown jumps the lazy");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,31}{0,31}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,39}");
		Assert.AreEqual(GetText(), "quick brown jumps over the lazy");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,35}{0,35}");
		Assert.AreEqual(GetPieces(), "{4,39}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,40}{0,40}");
		Assert.AreEqual(GetPieces(), "{4,44}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy dog.");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,44}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,44}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,40}{0,40}");
		Assert.AreEqual(GetPieces(), "{4,44}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy dog.");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,35}{0,35}");
		Assert.AreEqual(GetPieces(), "{4,39}");
		Assert.AreEqual(GetText(), "quick brown fox jumps over the lazy");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,31}{0,31}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,39}");
		Assert.AreEqual(GetText(), "quick brown jumps over the lazy");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,26}{0,26}");
		Assert.AreEqual(GetPieces(), "{4,16}{20,26}{31,39}");
		Assert.AreEqual(GetText(), "quick brown jumps the lazy");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), String.Empty);
		Assert.AreEqual(GetText(), String.Empty);
	}
	
	[Test]
	public void RedoReplace()
	{
		Assert.Fail("Test needs writing");
	}
	
	[Test]
	public void RedoCopy()
	{
		Assert.Fail("Test needs writing");
	}
	
	[Test]
	public void RedoMove()
	{
		Assert.Fail("Test needs writing");
	}
}
