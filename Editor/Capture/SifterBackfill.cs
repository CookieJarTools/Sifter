using System.Collections.Generic;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Takes whatever Unity's console is holding that Sifter is not.
	/// <para>
	/// Two things make this necessary. The first time Sifter is installed the window would
	/// otherwise be empty until something logged. And a compile error that was still outstanding
	/// when the editor was last closed is announced by nothing at all on the way back in, because
	/// the compile it came from happened in a different session.
	/// </para>
	/// <para>
	/// Everything Unity is holding was very likely also read back from disk a moment ago, so this
	/// counts what is already held and takes only the surplus. Counting rather than just checking
	/// matters: the same line logged five times is five entries, and four of them would be thrown
	/// away by a plain seen-it-before test.
	/// </para>
	/// </summary>
	public static class SifterBackfill
	{
		/// <summary>A character no message can contain, so joining fields cannot make two keys collide.</summary>
		private const char Apart = (char)1;

		public static int Run(SifterLog log)
		{
			if (!SifterConsole.Available)
			{
				return 0;
			}

			var already = Counted(log.All());
			var added = 0;
			foreach (var entry in SifterConsole.Read())
			{
				if (!Spare(already, entry))
				{
					continue;
				}

				log.Add(entry);
				added++;
			}

			return added;
		}

		/// <summary>
		/// Whether this one is surplus to what is already held, using up a match when it finds one
		/// so that a line logged twice is only matched twice.
		/// </summary>
		private static bool Spare(Dictionary<string, int> already, SifterEntry entry)
		{
			var key = Key(entry);
			int left;
			if (!already.TryGetValue(key, out left) || left < 1)
			{
				return true;
			}

			already[key] = left - 1;
			return false;
		}

		private static Dictionary<string, int> Counted(List<SifterEntry> all)
		{
			var counted = new Dictionary<string, int>(all.Count);
			foreach (var entry in all)
			{
				Bump(counted, Key(entry));
			}

			return counted;
		}

		private static void Bump(Dictionary<string, int> counted, string key)
		{
			int had;
			counted[key] = counted.TryGetValue(key, out had) ? had + 1 : 1;
		}

		/// <summary>
		/// What makes two entries the same one.
		/// <para>
		/// Deliberately not the time, because the backend keeps a time to the second while the live
		/// stream keeps one to the tick, so the two never agree. Deliberately not the stack either,
		/// since the two sources disagree about its last frame. And the file counts only for a
		/// compiler message, because that is the one kind with a file on both sides. The live stream
		/// gives no file at all for an ordinary log, and including it there made every restored log
		/// look new, which doubled the whole buffer on the first reload.
		/// </para>
		/// </summary>
		private static string Key(SifterEntry entry)
		{
			var where = SifterKinds.IsCompiler(entry.Kind)
				? entry.File.Replace('\\', '/') + Apart + entry.Line
				: string.Empty;
			return (int)entry.Kind + Apart + where + Apart + entry.Message;
		}
	}
}
