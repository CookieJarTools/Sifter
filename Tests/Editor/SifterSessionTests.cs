using System.IO;
using CookieJarTools.Sifter.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CookieJarTools.Sifter.Tests
{
	public sealed class SifterSessionTests
	{
		private string path;

		[SetUp]
		public void Setup()
		{
			path = Path.Combine(Path.GetTempPath(), "sifter-test-" + Path.GetRandomFileName() + ".bin");
		}

		[TearDown]
		public void Teardown()
		{
			SifterSession.Forget(path);
		}

		[Test]
		public void ARoundTripKeepsEveryField()
		{
			var saved = new SifterLog(8);
			saved.Add(new SifterEntry(SifterKind.Exception, "boom", "at Thing.Go()", 1234567,
				"Assets/Thing.cs", 42, 99, true, 3));
			SifterSession.Save(saved, path);

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			var entry = read.At(0);
			Assert.AreEqual(SifterKind.Exception, entry.Kind);
			Assert.AreEqual("boom", entry.Message);
			Assert.AreEqual("at Thing.Go()", entry.Trace);
			Assert.AreEqual(1234567, entry.Ticks);
			Assert.AreEqual("Assets/Thing.cs", entry.File);
			Assert.AreEqual(42, entry.Line);
			Assert.AreEqual(99, entry.Frame);
			Assert.IsTrue(entry.OffMainThread);
			Assert.AreEqual(3, entry.Count);
		}

		[Test]
		public void TheOrderIsKept()
		{
			var saved = new SifterLog(8);
			saved.Add(SifterMade.Log("first"));
			saved.Add(SifterMade.Log("second"));
			SifterSession.Save(saved, path);

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.AreEqual("first", read.At(0).Message);
			Assert.AreEqual("second", read.At(1).Message);
		}

		[Test]
		public void RestoringFromNothingIsQuiet()
		{
			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.AreEqual(0, read.Count);
		}

		/// <summary>
		/// This file is a cache written on the way out of a domain, so it can be half written if
		/// the editor went down. It must cost a warning and a fresh start, never an exception on
		/// every reload from then on.
		/// </summary>
		[Test]
		public void ARuinedFileIsThrownAwayRatherThanThrown()
		{
			File.WriteAllBytes(path, new byte[] { 1, 0, 0, 0, 90, 0, 0, 0, 7, 7, 7 });
			LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Sifter could not read"));

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.AreEqual(0, read.Count);
			Assert.IsFalse(File.Exists(path), "a file that cannot be read should not be read again");
		}

		[Test]
		public void AFileFromAnotherVersionIsIgnored()
		{
			using (var writer = new BinaryWriter(File.Create(path)))
			{
				writer.Write(9999);
				writer.Write(1);
			}

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.AreEqual(0, read.Count);
		}

		/// <summary>
		/// A runaway recursion produces a stack measured in megabytes, and one of those kept in the
		/// buffer would make every reload from then on slow.
		/// </summary>
		[Test]
		public void AnEnormousStackIsCutShort()
		{
			var saved = new SifterLog(8);
			saved.Add(new SifterEntry(SifterKind.Exception, "deep", new string('x', 200000), 1));
			SifterSession.Save(saved, path);

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.Less(read.At(0).Trace.Length, 40000);
			Assert.IsTrue(read.At(0).Trace.EndsWith("across a reload"));
		}

		[Test]
		public void SavingOverAnOlderFileReplacesIt()
		{
			var first = new SifterLog(8);
			first.Add(SifterMade.Log("old"));
			SifterSession.Save(first, path);

			var second = new SifterLog(8);
			second.Add(SifterMade.Log("new"));
			SifterSession.Save(second, path);

			var read = new SifterLog(8);
			SifterSession.Restore(read, path);

			Assert.AreEqual(1, read.Count);
			Assert.AreEqual("new", read.At(0).Message);
		}
	}
}
