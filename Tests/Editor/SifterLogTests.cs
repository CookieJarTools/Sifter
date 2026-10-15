using System.Threading;
using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	public sealed class SifterLogTests
	{
		[Test]
		public void ANewLogIsEmpty()
		{
			var log = new SifterLog(8);

			Assert.AreEqual(0, log.Count);
			Assert.IsNull(log.At(0));
		}

		[Test]
		public void EntriesComeBackOldestFirst()
		{
			var log = new SifterLog(8);
			log.Add(SifterMade.Log("one"));
			log.Add(SifterMade.Log("two"));

			Assert.AreEqual("one", log.At(0).Message);
			Assert.AreEqual("two", log.At(1).Message);
		}

		[Test]
		public void AFullLogDropsTheOldest()
		{
			var log = new SifterLog(3);
			for (var i = 0; i < 5; i++)
			{
				log.Add(SifterMade.Log(i.ToString()));
			}

			Assert.AreEqual(3, log.Count);
			Assert.AreEqual("2", log.At(0).Message);
			Assert.AreEqual("4", log.At(2).Message);
		}

		[Test]
		public void TheCountsFollowWhatIsHeld()
		{
			var log = new SifterLog(8);
			log.Add(SifterMade.Log("a"));
			log.Add(SifterMade.Warning("b"));
			log.Add(SifterMade.Error("c"));
			log.Add(SifterMade.Entry(SifterKind.Exception, "d"));
			log.Add(SifterMade.Entry(SifterKind.CompileWarning, "e"));

			Assert.AreEqual(2, log.Errors, "an exception counts against the red number");
			Assert.AreEqual(2, log.Warnings, "a compile warning counts against the yellow one");
			Assert.AreEqual(1, log.Logs);
		}

		[Test]
		public void TheCountsComeBackDownAsEntriesAreDropped()
		{
			var log = new SifterLog(2);
			log.Add(SifterMade.Error("gone"));
			log.Add(SifterMade.Error("also gone"));
			log.Add(SifterMade.Log("here"));
			log.Add(SifterMade.Log("here too"));

			Assert.AreEqual(0, log.Errors);
			Assert.AreEqual(2, log.Logs);
		}

		[Test]
		public void ClearingEmptiesTheCountsToo()
		{
			var log = new SifterLog(8);
			log.Add(SifterMade.Error("a"));
			log.Clear();

			Assert.AreEqual(0, log.Count);
			Assert.AreEqual(0, log.Errors);
		}

		[Test]
		public void ShrinkingKeepsTheNewest()
		{
			var log = new SifterLog(8);
			for (var i = 0; i < 6; i++)
			{
				log.Add(SifterMade.Log(i.ToString()));
			}

			log.Resize(2);

			Assert.AreEqual(2, log.Count);
			Assert.AreEqual("4", log.At(0).Message);
			Assert.AreEqual("5", log.At(1).Message);
		}

		[Test]
		public void GrowingKeepsEverything()
		{
			var log = new SifterLog(2);
			log.Add(SifterMade.Log("a"));
			log.Add(SifterMade.Log("b"));

			log.Resize(16);

			Assert.AreEqual(2, log.Count);
			Assert.AreEqual(16, log.Capacity);
			Assert.AreEqual("a", log.At(0).Message);
		}

		/// <summary>
		/// The one thing adding must not do is raise the event, because a handler is going to touch
		/// a window and the add may be on a worker thread.
		/// </summary>
		[Test]
		public void AddingDoesNotRaiseChangedUntilItSettles()
		{
			var log = new SifterLog(8);
			var raised = 0;
			log.Changed += () => raised++;

			log.Add(SifterMade.Log("a"));
			log.Add(SifterMade.Log("b"));
			Assert.AreEqual(0, raised, "nothing should be raised from inside an add");

			log.Settle();
			Assert.AreEqual(1, raised, "two adds in one tick are one repaint");

			log.Settle();
			Assert.AreEqual(1, raised, "a quiet tick raises nothing");
		}

		[Test]
		public void AddingFromManyThreadsLosesNothing()
		{
			var log = new SifterLog(4096);
			var threads = new Thread[8];
			for (var i = 0; i < threads.Length; i++)
			{
				threads[i] = Busy(log, i);
			}

			foreach (var thread in threads)
			{
				thread.Join();
			}

			Assert.AreEqual(8 * 100, log.Count);
		}

		[Test]
		public void SortingPutsThemBackInTheOrderTheyHappened()
		{
			var log = new SifterLog(8);
			log.Add(SifterMade.At(SifterKind.Log, "late", 300));
			log.Add(SifterMade.At(SifterKind.Log, "early", 100));
			log.Add(SifterMade.At(SifterKind.Log, "middle", 200));

			log.Sort();

			Assert.AreEqual("early", log.At(0).Message);
			Assert.AreEqual("middle", log.At(1).Message);
			Assert.AreEqual("late", log.At(2).Message);
		}

		[Test]
		public void SortingLeavesTheCountsAlone()
		{
			var log = new SifterLog(8);
			log.Add(SifterMade.At(SifterKind.Error, "b", 200));
			log.Add(SifterMade.At(SifterKind.Log, "a", 100));

			log.Sort();

			Assert.AreEqual(2, log.Count);
			Assert.AreEqual(1, log.Errors);
			Assert.AreEqual(1, log.Logs);
		}

		private static Thread Busy(SifterLog log, int which)
		{
			var thread = new Thread(() => Hundred(log, which));
			thread.Start();
			return thread;
		}

		private static void Hundred(SifterLog log, int which)
		{
			for (var i = 0; i < 100; i++)
			{
				log.Add(SifterMade.Log(which + ":" + i));
			}
		}
	}
}
