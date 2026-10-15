using System;
using CookieJarTools.Sifter.Editor;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>Short ways of making an entry, so a test reads as what it is about.</summary>
	public static class SifterMade
	{
		private static long ticking = new DateTime(2026, 1, 1).Ticks;

		public static SifterEntry Log(string message)
		{
			return Entry(SifterKind.Log, message);
		}

		public static SifterEntry Warning(string message)
		{
			return Entry(SifterKind.Warning, message);
		}

		public static SifterEntry Error(string message)
		{
			return Entry(SifterKind.Error, message);
		}

		public static SifterEntry Entry(SifterKind kind, string message)
		{
			ticking += TimeSpan.TicksPerSecond;
			return new SifterEntry(kind, message, "at " + message + "()", ticking);
		}

		public static SifterEntry At(SifterKind kind, string message, long ticks)
		{
			return new SifterEntry(kind, message, string.Empty, ticks);
		}
	}
}
