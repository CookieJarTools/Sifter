using UnityEditorInternal;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Opens a file where a frame says to.
	/// <para>
	/// The whole point is that this works on any frame, not only the one that called Debug.Log.
	/// The line you want is almost never the line that logged, and having to read a trace, find
	/// the file by hand and scroll to a number is the slowest part of using a console.
	/// </para>
	/// </summary>
	public static class SifterOpen
	{
		public static bool At(string file, int line)
		{
			return !string.IsNullOrEmpty(file) && line > 0
				&& InternalEditorUtility.OpenFileAtLineExternal(file, line, 0);
		}

		public static bool At(SifterFrame frame)
		{
			return frame != null && At(frame.File, frame.Line);
		}

		/// <summary>Where a log itself points, which for a compiler message is all there is.</summary>
		public static bool At(SifterEntry entry)
		{
			if (entry == null)
			{
				return false;
			}

			return At(entry.File, entry.Line) || First(entry);
		}

		private static bool First(SifterEntry entry)
		{
			foreach (var frame in entry.Frames)
			{
				if (frame.Mine && At(frame))
				{
					return true;
				}
			}

			return Any(entry);
		}

		private static bool Any(SifterEntry entry)
		{
			foreach (var frame in entry.Frames)
			{
				if (At(frame))
				{
					return true;
				}
			}

			return false;
		}
	}
}
