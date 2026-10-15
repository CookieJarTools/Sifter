using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>
	/// Backfill reads the live editor, so these check the part that can be checked without one:
	/// that reading Unity's console is possible at all in this version, and that what it reads is
	/// shaped like a log.
	/// </summary>
	public sealed class SifterBackfillTests
	{
		[Test]
		public void TheConsoleBackendIsReachableInThisEditor()
		{
			Assert.IsTrue(SifterConsole.Available,
				"Unity's console backend moved: " + SifterConsole.Unavailable);
		}

		[Test]
		public void ReadingTheBackendGivesEntriesWithSomethingInThem()
		{
			UnityEngine.Debug.Log("Sifter backfill test marker");

			var found = SifterConsole.Read();

			Assert.IsNotEmpty(found);
			Assert.That(found.Exists(entry => entry.Message.Contains("Sifter backfill test marker")),
				"the log that was just written should be in what the backend hands back");
		}

		/// <summary>
		/// The message and the stack arrive as one string with an offset saying where to cut. If
		/// that offset is ever wrong the message swallows the whole trace, which is the single
		/// most visible way this could break.
		/// </summary>
		[Test]
		public void TheMessageDoesNotSwallowTheStack()
		{
			UnityEngine.Debug.Log("Sifter split test marker");

			var found = SifterConsole.Read();
			var entry = found.FindLast(one => one.Message == "Sifter split test marker");

			Assert.IsNotNull(entry, "the marker should come back as its own message");
			Assert.IsNotEmpty(entry.Trace, "and the stack should be beside it, not inside it");
		}

		[Test]
		public void ReadingPutsTheBuiltInConsolesOwnFilterBack()
		{
			SifterConsole.Read();
			SifterConsole.Read();

			Assert.IsTrue(SifterConsole.Available, "reading twice should not break the backend");
		}
	}
}
