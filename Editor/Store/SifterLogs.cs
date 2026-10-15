namespace CookieJarTools.Sifter.Editor
{
	/// <summary>The one log the editor captures into. Everything else takes a log as an argument.</summary>
	public static class SifterLogs
	{
		private static readonly SifterLog Held = new SifterLog(SifterSettings.Capacity);

		public static SifterLog Current
		{
			get { return Held; }
		}
	}
}
