using System.Reflection;
using System.Text;
using NUnit.Framework;

[TestFixture()]
public class PieceBufferTest
{
	PieceBuffer b;
	
	public PieceBufferTest()
	{
	}

	[SetUp()]
	public void Setup()
	{
		b = new PieceBuffer();
	}
	
	[TearDown()]
	public void Cleanup()
	{
		b = null;
	}
		
	public string GetMarks()
	{
		StringBuilder tmp = new StringBuilder();
		
		PieceBuffer.Mark mark = b.Marks;
		while((mark = mark.Next) != b.Marks)
			tmp.AppendFormat("{{{0},{1}}}", mark.Offset, mark.Position);
		
		return tmp.ToString();		
	}
	
	public string GetPieces()
	{
		StringBuilder tmp = new StringBuilder();;
	
		PieceBuffer.Piece p = b.Pieces;
		while((p = p.Next) != b.Pieces)
			tmp.AppendFormat("{{{0},{1}}}", p.Start, p.End);
		
		return tmp.ToString();
	}
	
	public string GetText()
	{
		PieceBuffer.Mark x = b.CreateMark(0 - b.InsertMark.Position);
		PieceBuffer.Mark y = b.CreateMark(1000000);

		byte[] tmp = new byte[y.Position - x.Position];
		b.GetBytes(x, y, tmp, y.Position - x.Position);
		b.DestroyMark(x);
		b.DestroyMark(y);
		
		return System.Text.ASCIIEncoding.ASCII.GetString(tmp);
	}
	
	[Test]
	public void MarkDestroyInternalMarks()
	{
		// Attempting to destroy internal marks (insert/start/end) should fail
		b.DestroyMark(b.InsertMark);
		b.DestroyMark(b.StartMark);
		b.DestroyMark(b.EndMark);
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
		
		// insert a second character
		b.Insert((byte)'b');
		Assert.AreEqual(GetMarks(), "{0,0}{0,2}{0,2}");
		Assert.AreEqual(GetPieces(), "{0,1}{1,2}");
		Assert.AreEqual(GetText(), "ab");
		
		// seek back one char, insert another
		b.MoveMark(-1);
		b.Insert((byte)'c');
		Assert.AreEqual(GetMarks(), "{0,0}{0,2}{0,3}");
		Assert.AreEqual(GetPieces(), "{0,1}{2,3}{1,2}");
		Assert.AreEqual(GetText(), "acb");
		
		// seek forward one char, insert another
		b.MoveMark(1);
		b.Insert((byte)'d');
		Assert.AreEqual(GetMarks(), "{0,0}{0,4}{0,4}");
		Assert.AreEqual(GetPieces(), "{0,1}{2,3}{1,2}{3,4}");
		Assert.AreEqual(GetText(), "acbd");
		
		// seek back past beginning, insert another
		b.MoveMark(-100);
		b.Insert((byte)'e');
		Assert.AreEqual(GetMarks(), "{0,0}{0,1}{0,5}");
		Assert.AreEqual(GetPieces(), "{4,5}{0,1}{2,3}{1,2}{3,4}");
		Assert.AreEqual(GetText(), "eacbd");
		
		// seek forward past end, insert another
		b.MoveMark(100);
		b.Insert((byte)'f');
		Assert.AreEqual(GetMarks(), "{0,0}{0,6}{0,6}");
		Assert.AreEqual(GetPieces(), "{4,5}{0,1}{2,3}{1,2}{3,4}{5,6}");
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
		b.MoveMark(-18);
		b.Insert("brown fox jumps ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,26}{0,44}");
		Assert.AreEqual(GetPieces(), "{0,10}{28,44}{10,28}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.");
		
		// seek forwards past the end, insert a string
		b.MoveMark(12345);
		b.Insert("<---{end}");
		Assert.AreEqual(GetMarks(), "{0,0}{0,53}{0,53}");
		Assert.AreEqual(GetPieces(), "{0,10}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "the quick brown fox jumps over the lazy dog.<---{end}");
		
		// seek backwards past the beginning, insert a string
		b.MoveMark(-98765);
		b.Insert("{begin}--->");
		Assert.AreEqual(GetMarks(), "{0,0}{0,11}{0,64}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick brown fox jumps over the lazy dog.<---{end}");
		
		// seek forward to piece boundary, insert a string
		b.MoveMark(10);
		b.Insert("and redish ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,32}{0,75}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{64,75}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick and redish brown fox jumps over the lazy dog.<---{end}");
		
		// seek backwards to middle of piece, insert a string
		b.MoveMark(-7);
		b.Insert("slightly ");
		Assert.AreEqual(GetMarks(), "{0,0}{0,34}{0,84}");
		Assert.AreEqual(GetPieces(), "{53,64}{0,10}{64,68}{75,84}{68,75}{28,44}{10,28}{44,53}");
		Assert.AreEqual(GetText(), "{begin}--->the quick and slightly redish brown fox jumps over the lazy dog.<---{end}");

		// seek forwards to middle of piece, insert a string
		b.MoveMark(32);
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
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
		PieceBuffer.Mark m1 = b.CreateMark(-34);
		PieceBuffer.Mark m2 = b.CreateMark(-29);
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
		b.DestroyMark(m1);
		b.DestroyMark(m2);
		m1 = b.CreateMarkAbsolute(0);
		m2 = b.CreateMarkAbsolute(15);
		b.Insert(m1, m2, "the big lazy");
		Assert.AreEqual(GetText(), "the big lazy fox jumps over the lazy dog.");
		
		// replace spanning multiple partial pieces
		b.DestroyMark(m1);
		b.DestroyMark(m2);
		m1 = b.CreateMarkAbsolute(4);
		m2 = b.CreateMarkAbsolute(22);
		b.Insert(m1, m2, "small dog runs");
		Assert.AreEqual(GetText(), "the small dog runs over the lazy dog.");
		
		// replace last piece
		b.DestroyMark(m1);
		b.DestroyMark(m2);
		m1 = b.CreateMarkAbsolute(19);
		m2 = b.CreateMarkAbsolute(100);
		b.Insert(m1, m2, "under the brown fox.");
		Assert.AreEqual(GetText(), "the small dog runs under the brown fox.");
	}

	[Test]
	public void InsertMarkPreservation()
	{
		b.Insert("fox jump dog");
		PieceBuffer.Mark m1 = b.CreateMark(-12);
		PieceBuffer.Mark m2 = b.CreateMark(-9);
		PieceBuffer.Mark m3 = b.CreateMark(-8);
		PieceBuffer.Mark m4 = b.CreateMark(-4);
		PieceBuffer.Mark m5 = b.CreateMark(-3);
		PieceBuffer.Mark m6 = b.CreateMark(0);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{8,8}{9,9}{0,12}{0,12}{0,12}");
		Assert.AreEqual(GetPieces(), "{0,12}");
		Assert.AreEqual(GetText(), "fox jump dog");
		
		b.Insert(m1, "the ");
		PieceBuffer.Mark m7 = b.CreateMark(-4);
		PieceBuffer.Mark m8 = b.CreateMark(-1);
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
		PieceBuffer.Mark m1 = b.CreateMark(0);
		PieceBuffer.Mark m2 = b.CreateMark(-3);
		PieceBuffer.Mark m3 = b.CreateMark(-4);
		PieceBuffer.Mark m4 = b.CreateMark(-8);
		PieceBuffer.Mark m5 = b.CreateMark(-9);
		PieceBuffer.Mark m6 = b.CreateMark(-12);
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{3,3}{4,4}{8,8}{9,9}{0,12}{0,12}{0,12}");
		Assert.AreEqual(GetPieces(), "{0,12}");
		Assert.AreEqual(GetText(), "fox jump dog");
		
		b.Insert(m6, "the ");
		PieceBuffer.Mark m7 = b.CreateMark(-1);
		PieceBuffer.Mark m8 = b.CreateMark(-4);
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
		PieceBuffer.Mark m1 = b.CreateMark(-19);
		PieceBuffer.Mark m2 = b.CreateMark(-16);
		PieceBuffer.Mark m3 = b.CreateMark(-15);
		PieceBuffer.Mark m4 = b.CreateMark(-10);
		PieceBuffer.Mark m5 = b.CreateMark(-9);
		PieceBuffer.Mark m6 = b.CreateMark(-4);
		PieceBuffer.Mark m7 = b.CreateMark(-3);
		PieceBuffer.Mark m8 = b.CreateMark(0);
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");

		b.Insert("the lazy dog");
		Assert.AreEqual(GetText(), "the lazy dog");
	}

	[Test]
	public void RemoveMarkPreservationReversed()
	{
		// add some text and mark the beginning and end of each word
		b.Insert("the quick brown fox");
		PieceBuffer.Mark m1 = b.CreateMark(0);
		PieceBuffer.Mark m2 = b.CreateMark(-3);
		PieceBuffer.Mark m3 = b.CreateMark(-4);
		PieceBuffer.Mark m4 = b.CreateMark(-9);
		PieceBuffer.Mark m5 = b.CreateMark(-10);
		PieceBuffer.Mark m6 = b.CreateMark(-15);
		PieceBuffer.Mark m7 = b.CreateMark(-16);
		PieceBuffer.Mark m8 = b.CreateMark(-19);
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");

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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
		// Undo with no history should silently do nothing
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
		b.Undo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
	}

	[Test]
	public void UndoReplace()
	{
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
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
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
		
		b.Redo();
		Assert.AreEqual(GetMarks(), "{0,0}{0,0}{0,0}");
		Assert.AreEqual(GetPieces(), "");
		Assert.AreEqual(GetText(), "");
	}
}
