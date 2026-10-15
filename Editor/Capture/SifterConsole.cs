using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Unity's own console backend, reached by reflection.
	/// <para>
	/// It is the only place a compile error lives, and the only thing that still remembers what
	/// happened before Sifter was loaded. It is also undocumented and moves between versions, so
	/// every member is resolved once here and a failure leaves Unavailable set with a reason
	/// rather than throwing somewhere inconvenient later.
	/// </para>
	/// <para>
	/// What it is deliberately not used for is the live stream. Reading it honours whatever is
	/// typed into the built-in console's own search box and its three level toggles, so a poll
	/// would quietly stop seeing most logs the moment somebody typed in there. Read turns both off
	/// and puts them back, which is safe to do once at load but not on a timer.
	/// </para>
	/// </summary>
	public static class SifterConsole
	{
		private const BindingFlags Anywhere =
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private const BindingFlags OnTheEntry =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private const int ErrorBit = 1 << 0;
		private const int AssertBit = 1 << 1;
		private const int FatalBit = 1 << 4;
		private const int AssetImportErrorBit = 1 << 6;
		private const int AssetImportWarningBit = 1 << 7;
		private const int ScriptingErrorBit = 1 << 8;
		private const int ScriptingWarningBit = 1 << 9;
		private const int ScriptCompileErrorBit = 1 << 11;
		private const int ScriptCompileWarningBit = 1 << 12;
		private const int StickyErrorBit = 1 << 13;
		private const int ScriptingExceptionBit = 1 << 17;
		private const int GraphCompileErrorBit = 1 << 20;
		private const int ScriptingAssertionBit = 1 << 21;

		private const int ShowLogs = 1 << 7;
		private const int ShowWarnings = 1 << 8;
		private const int ShowErrors = 1 << 9;

		private static Type shape;
		private static MethodInfo begin;
		private static MethodInfo end;
		private static MethodInfo fetch;
		private static MethodInfo stamp;
		private static MethodInfo readFilter;
		private static MethodInfo writeFilter;
		private static PropertyInfo flags;
		private static FieldInfo message;
		private static FieldInfo file;
		private static FieldInfo line;
		private static FieldInfo mode;
		private static FieldInfo traceAt;

		static SifterConsole()
		{
			try
			{
				Resolve();
				Available = true;
			}
			catch (Exception e)
			{
				Unavailable = e.Message;
			}
		}

		/// <summary>Whether the backend was found. When it was not, compile errors are all Sifter misses.</summary>
		public static bool Available { get; private set; }

		/// <summary>Why it was not found, for the window to say out loud rather than going quiet.</summary>
		public static string Unavailable { get; private set; }

		/// <summary>
		/// Everything Unity's console is holding right now, whatever the built-in console happens
		/// to be filtered to.
		/// </summary>
		public static List<SifterEntry> Read()
		{
			var found = new List<SifterEntry>();
			if (!Available)
			{
				return found;
			}

			try
			{
				Unfiltered(found);
			}
			catch (Exception e)
			{
				Available = false;
				Unavailable = e.Message;
			}

			return found;
		}

		private static void Unfiltered(List<SifterEntry> found)
		{
			var typed = (string)readFilter.Invoke(null, null);
			var was = (int)flags.GetValue(null);
			try
			{
				writeFilter.Invoke(null, new object[] { string.Empty });
				flags.SetValue(null, was | ShowLogs | ShowWarnings | ShowErrors);
				Gather(found);
			}
			finally
			{
				flags.SetValue(null, was);
				writeFilter.Invoke(null, new object[] { typed ?? string.Empty });
			}
		}

		private static void Gather(List<SifterEntry> found)
		{
			var count = (int)begin.Invoke(null, null);
			try
			{
				Take(found, count);
			}
			finally
			{
				end.Invoke(null, null);
			}
		}

		private static void Take(List<SifterEntry> found, int count)
		{
			var row = Activator.CreateInstance(shape);
			for (var i = 0; i < count; i++)
			{
				found.Add(One(row, i));
			}
		}

		private static SifterEntry One(object row, int index)
		{
			fetch.Invoke(null, new object[] { index, row });
			var text = (string)message.GetValue(row) ?? string.Empty;
			var at = (int)traceAt.GetValue(row);
			var split = at > 0 && at <= text.Length ? at : text.Length;
			return new SifterEntry(Kind((int)mode.GetValue(row)), text.Substring(0, split).TrimEnd('\n'),
				text.Substring(split), When(index), (string)file.GetValue(row), (int)line.GetValue(row));
		}

		/// <summary>
		/// The backend describes an entry as a bitmask with more than twenty bits in it, several of
		/// which are set at once. The order these are tested in is the whole of the mapping: an
		/// exception also carries the error bit, and a compile error carries it too.
		/// </summary>
		private static SifterKind Kind(int bits)
		{
			if ((bits & (ScriptCompileErrorBit | GraphCompileErrorBit)) != 0)
			{
				return SifterKind.CompileError;
			}

			if ((bits & ScriptCompileWarningBit) != 0)
			{
				return SifterKind.CompileWarning;
			}

			if ((bits & ScriptingExceptionBit) != 0)
			{
				return SifterKind.Exception;
			}

			return Plain(bits);
		}

		private static SifterKind Plain(int bits)
		{
			if ((bits & (AssertBit | ScriptingAssertionBit)) != 0)
			{
				return SifterKind.Assert;
			}

			if ((bits & (ScriptingWarningBit | AssetImportWarningBit)) != 0)
			{
				return SifterKind.Warning;
			}

			var bad = ScriptingErrorBit | ErrorBit | FatalBit | AssetImportErrorBit | StickyErrorBit;
			return (bits & bad) != 0 ? SifterKind.Error : SifterKind.Log;
		}

		private static long When(int index)
		{
			var args = new object[] { index, string.Empty };
			var got = (bool)stamp.Invoke(null, args);
			return got ? Parsed((string)args[1]) : DateTime.UtcNow.Ticks;
		}

		/// <summary>
		/// The backend hands back the whole entry with the time in front of it, and only the time:
		/// there is no date anywhere in it. Today is assumed, and a time that has not happened yet
		/// is taken to be yesterday's, which is what an editor left open overnight produces.
		/// </summary>
		private static long Parsed(string stamped)
		{
			if (stamped == null || stamped.Length < 10 || stamped[0] != '[')
			{
				return DateTime.UtcNow.Ticks;
			}

			TimeSpan clock;
			var read = TimeSpan.TryParseExact(stamped.Substring(1, 8), @"hh\:mm\:ss",
				CultureInfo.InvariantCulture, out clock);
			if (!read)
			{
				return DateTime.UtcNow.Ticks;
			}

			var when = DateTime.Now.Date + clock;
			return (when > DateTime.Now ? when.AddDays(-1) : when).ToUniversalTime().Ticks;
		}

		private static void Resolve()
		{
			var entries = Need("UnityEditor.LogEntries");
			shape = Need("UnityEditor.LogEntry");
			begin = Method(entries, "StartGettingEntries");
			end = Method(entries, "EndGettingEntries");
			fetch = Method(entries, "GetEntryInternal");
			stamp = Method(entries, "GetEntryTimestampInternal");
			readFilter = Method(entries, "GetFilteringText");
			writeFilter = Method(entries, "SetFilteringText");
			flags = Property(entries, "consoleFlags");
			message = Field(shape, "message");
			file = Field(shape, "file");
			line = Field(shape, "line");
			mode = Field(shape, "mode");
			traceAt = Field(shape, "callstackTextStartUTF16");
		}

		private static Type Need(string name)
		{
			var found = Type.GetType(name + ",UnityEditor");
			if (found == null)
			{
				throw new MissingMemberException(name + " is not in this version of the editor.");
			}

			return found;
		}

		private static MethodInfo Method(Type owner, string name)
		{
			var found = owner.GetMethod(name, Anywhere);
			if (found == null)
			{
				throw new MissingMemberException(owner.Name + "." + name + " is gone.");
			}

			return found;
		}

		private static PropertyInfo Property(Type owner, string name)
		{
			var found = owner.GetProperty(name, Anywhere);
			if (found == null)
			{
				throw new MissingMemberException(owner.Name + "." + name + " is gone.");
			}

			return found;
		}

		private static FieldInfo Field(Type owner, string name)
		{
			var found = owner.GetField(name, OnTheEntry);
			if (found == null)
			{
				throw new MissingMemberException(owner.Name + "." + name + " is gone.");
			}

			return found;
		}
	}
}
