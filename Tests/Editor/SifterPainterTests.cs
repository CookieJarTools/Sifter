using System;
using System.Collections.Generic;
using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	public sealed class SifterPainterTests
	{
		private readonly Dictionary<SifterPart, bool> was = new Dictionary<SifterPart, bool>();

		private bool wasMine;
		private bool wasHonour;

		/// <summary>
		/// Every part turned on and the folding turned off, whatever this machine happens to have
		/// chosen. These settings live in EditorPrefs, so a test that reads them instead of setting
		/// them passes or fails depending on what somebody last clicked, which is how this one
		/// started failing with nothing to do with it having changed.
		/// </summary>
		[SetUp]
		public void Setup()
		{
			was.Clear();
			foreach (SifterPart part in Enum.GetValues(typeof(SifterPart)))
			{
				Remember(part);
			}

			wasMine = SifterShown.MineOnly;
			wasHonour = SifterShown.Honour;
			SifterShown.MineOnly = false;
			SifterShown.Honour = false;
		}

		[TearDown]
		public void Teardown()
		{
			foreach (var pair in was)
			{
				SifterShown.Set(pair.Key, pair.Value);
			}

			SifterShown.MineOnly = wasMine;
			SifterShown.Honour = wasHonour;
		}

		private void Remember(SifterPart part)
		{
			was[part] = SifterShown.Has(part);
			SifterShown.Set(part, true);
		}

		[Test]
		public void TurningAPartOffTakesItOutOfTheLine()
		{
			var frame = One("CookieJarTools.Thing.Runner:Start (int,string) (at Assets/Runner.cs:7)");

			Assert.That(SifterPainter.Paint(frame), Does.Contain("CookieJarTools.Thing"));

			SifterShown.Set(SifterPart.Namespace, false);
			Assert.That(SifterPainter.Paint(frame), Does.Not.Contain("CookieJarTools.Thing"));
		}

		[Test]
		public void TheFileIsShownByNameRatherThanByPath()
		{
			var frame = One("A.B:C () (at Assets/Deep/Deeper/Runner.cs:7)");
			var painted = SifterPainter.Paint(frame);

			Assert.That(painted, Does.Contain("Runner.cs"));
			Assert.That(painted, Does.Not.Contain("Assets/Deep"));
		}

		/// <summary>
		/// UI Toolkit's text understands tags and not HTML entities, so an escaped bracket is drawn
		/// as the five letters of &amp;lt; rather than as a bracket. A generic parameter has to be
		/// wrapped instead.
		/// </summary>
		[Test]
		public void AGenericParameterIsWrappedRatherThanEscaped()
		{
			var frame = One("A.B:C (System.Action`1<int>) (at Assets/Runner.cs:7)");
			var painted = SifterPainter.Paint(frame);

			Assert.That(painted, Does.Not.Contain("&lt;"), "an entity would be drawn literally");
			Assert.That(painted, Does.Contain("<noparse>"));
		}

		[Test]
		public void AnUnreadLineIsPaintedAsItArrived()
		{
			var frame = One("this line is not a stack frame at all");

			Assert.That(SifterPainter.Paint(frame), Does.Contain("this line is not a stack frame at all"));
		}

		[Test]
		public void MyCodeOnlyFoldsAwayEverythingElse()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "CookieJarTools.Thing.Runner:Start () (at Assets/Runner.cs:7)\n"
				+ "Unity.Pipeline.Thing:Go () (at ./Library/PackageCache/x/Thing.cs:1)\n");

			SifterShown.MineOnly = false;
			Assert.AreEqual(3, SifterPainter.Worth(frames).Count);

			SifterShown.MineOnly = true;
			var worth = SifterPainter.Worth(frames);
			Assert.AreEqual(1, worth.Count);
			Assert.AreEqual("Runner", worth[0].Class);
		}

		[Test]
		public void TheSourceIsTheFirstFrameOfTheProjectsOwn()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "CookieJarTools.Thing.Runner:Start () (at Assets/Runner.cs:7)\n"
				+ "CookieJarTools.Thing.Other:Go () (at Assets/Other.cs:3)\n");

			Assert.AreEqual("Runner", SifterPainter.Source(frames).Class);
		}

		[Test]
		public void ThereIsNoSourceWhenNothingIsTheProjectsOwn()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n");

			Assert.IsNull(SifterPainter.Source(frames));
		}

		/// <summary>
		/// The top frame of a Unity stack is very often UnityEngine.Debug:Log, which has nowhere to
		/// go, so a shut stack showing only the first line would show a line you cannot click.
		/// </summary>
		[Test]
		public void AShutStackReachesDownToTheFirstFrameWithAFile()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "UnityEngine.Debug:LogFormat (string)\n"
				+ "CookieJarTools.Thing.Runner:Start () (at Assets/Runner.cs:7)\n"
				+ "CookieJarTools.Thing.Other:Go () (at Assets/Other.cs:3)\n");

			var top = SifterPainter.Top(frames);

			Assert.AreEqual(3, top.Count);
			Assert.AreEqual("Runner", top[2].Class, "and it stops at the first one you can open");
		}

		[Test]
		public void AShutStackShowsOneFrameWhenThatOneIsAlreadyClickable()
		{
			var frames = SifterFrames.Of("CookieJarTools.Thing.Runner:Start () (at Assets/Runner.cs:7)\n"
				+ "CookieJarTools.Thing.Other:Go () (at Assets/Other.cs:3)\n");

			Assert.AreEqual(1, SifterPainter.Top(frames).Count);
		}

		[Test]
		public void AShutStackWithNowhereToGoStillShowsTheTopFrame()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "UnityEngine.Debug:LogFormat (string)\n");

			var top = SifterPainter.Top(frames);

			Assert.AreEqual(1, top.Count, "one line is better than every line when none of them open");
			Assert.AreEqual("Log", top[0].Method);
		}

		[Test]
		public void AShutStackOfNothingIsNothing()
		{
			Assert.AreEqual(0, SifterPainter.Top(SifterFrames.Of(string.Empty)).Count);
		}

		private static SifterFrame One(string line)
		{
			return SifterFrames.Of(line)[0];
		}
	}
}
