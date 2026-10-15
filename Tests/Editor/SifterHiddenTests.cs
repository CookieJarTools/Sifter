using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>
	/// Unity strips a frame marked HideInCallstack when it draws its own console, and leaves it in
	/// the text every other way of reading a log hands over. Measured in 6000.4, so this is work
	/// Sifter has to do rather than something it gets for free, and it is the difference between a
	/// trace starting at your logging helper and starting where the problem is.
	/// </summary>
	public sealed class SifterHiddenTests
	{
		[Test]
		public void AMarkedMethodIsFoundThroughItsFrame()
		{
			var frame = Frame("CookieJarTools.Sifter.Tests.SifterHideProbe:Wrapper (string) "
				+ "(at Assets/CookieJarTools/Sifter/Tests/Editor/SifterHideProbe.cs:10)");

			Assert.IsTrue(SifterHidden.Is(frame));
		}

		[Test]
		public void AnUnmarkedMethodIsNot()
		{
			var frame = Frame("CookieJarTools.Sifter.Tests.SifterHideProbe:Plain (string) "
				+ "(at Assets/CookieJarTools/Sifter/Tests/Editor/SifterHideProbe.cs:16)");

			Assert.IsFalse(SifterHidden.Is(frame));
		}

		[Test]
		public void ATypeThatIsNowhereIsNotHidden()
		{
			Assert.IsFalse(SifterHidden.Is(Frame("Nothing.At.All:Ever () (at Assets/x.cs:1)")));
		}

		[Test]
		public void ALineThatWasNotReadIsNotHidden()
		{
			Assert.IsFalse(SifterHidden.Is(Frame("this line is not a stack frame at all")));
			Assert.IsFalse(SifterHidden.Is(null));
		}

		/// <summary>Honouring the attribute is what takes a marked wrapper out of a real trace.</summary>
		[Test]
		public void HonouringItTakesTheWrapperOutOfTheTrace()
		{
			var was = SifterShown.Honour;
			var mine = SifterShown.MineOnly;
			SifterShown.MineOnly = false;
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "CookieJarTools.Sifter.Tests.SifterHideProbe:Wrapper (string) (at Assets/x.cs:10)\n"
				+ "CookieJarTools.Sifter.Tests.SifterHideProbe:Plain (string) (at Assets/x.cs:16)\n");

			SifterShown.Honour = false;
			Assert.AreEqual(3, SifterPainter.Worth(frames).Count);

			SifterShown.Honour = true;
			Assert.AreEqual(2, SifterPainter.Worth(frames).Count);

			SifterShown.Honour = was;
			SifterShown.MineOnly = mine;
		}

		private static SifterFrame Frame(string line)
		{
			return SifterFrames.Of(line)[0];
		}
	}
}
