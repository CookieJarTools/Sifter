using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Writes what has been captured to disk before the domain goes away, and reads it back after.
	/// <para>
	/// A recompile is exactly the moment you want to compare what happened before a change with
	/// what happens after, and it is the moment Unity's console throws everything away. The file
	/// sits under Library because it is a cache: losing it costs nothing, it is already ignored by
	/// version control, and nobody has to be told to add anything to a gitignore.
	/// </para>
	/// <para>
	/// The format is binary rather than JSON because this is read on every single domain reload. A
	/// few thousand entries with their stacks is megabytes, and a reload is already slow enough
	/// without parsing that twice.
	/// </para>
	/// </summary>
	public static class SifterSession
	{
		private const int Version = 1;

		/// <summary>
		/// How much of one stack is worth keeping. A runaway recursion produces a trace measured in
		/// megabytes, and one of those in the buffer would make every reload from then on slow.
		/// </summary>
		private const int LongestTrace = 32 * 1024;

		private const string Cut = "\n... the rest of this stack was too long to keep across a reload";

		public static string Kept
		{
			get { return Path.Combine(Project, "Library/CookieJarTools/Sifter/session.bin"); }
		}

		private static string Project
		{
			get { return Path.GetDirectoryName(Application.dataPath); }
		}

		public static void Save(SifterLog log)
		{
			Save(log, Kept);
		}

		public static void Save(SifterLog log, string path)
		{
			try
			{
				Write(log, path);
			}
			catch (Exception e)
			{
				Debug.LogWarning("Sifter could not keep this session's logs: " + e.Message);
			}
		}

		public static void Restore(SifterLog log)
		{
			Restore(log, Kept);
		}

		public static void Restore(SifterLog log, string path)
		{
			if (!File.Exists(path))
			{
				return;
			}

			try
			{
				Read(log, path);
			}
			catch (Exception e)
			{
				Forget(path);
				Debug.LogWarning("Sifter could not read the logs it kept, so it has started again: " + e.Message);
			}
		}

		public static void Forget()
		{
			Forget(Kept);
		}

		public static void Forget(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception)
			{
				// Nothing useful to do about a cache file that will not go. It gets overwritten.
			}
		}

		private static void Write(SifterLog log, string path)
		{
			var all = log.All();
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var writer = new BinaryWriter(File.Create(path), Encoding.UTF8))
			{
				Put(writer, all);
			}
		}

		private static void Put(BinaryWriter writer, List<SifterEntry> all)
		{
			writer.Write(Version);
			writer.Write(all.Count);
			foreach (var entry in all)
			{
				Put(writer, entry);
			}
		}

		private static void Put(BinaryWriter writer, SifterEntry entry)
		{
			writer.Write((byte)entry.Kind);
			writer.Write(entry.Ticks);
			writer.Write(entry.Frame);
			writer.Write(entry.OffMainThread);
			writer.Write(entry.Count);
			writer.Write(entry.Line);
			writer.Write(entry.File);
			writer.Write(entry.Message);
			writer.Write(Short(entry.Trace));
		}

		private static string Short(string trace)
		{
			return trace.Length <= LongestTrace ? trace : trace.Substring(0, LongestTrace) + Cut;
		}

		private static void Read(SifterLog log, string path)
		{
			using (var reader = new BinaryReader(File.OpenRead(path), Encoding.UTF8))
			{
				Take(log, reader);
			}
		}

		private static void Take(SifterLog log, BinaryReader reader)
		{
			if (reader.ReadInt32() != Version)
			{
				return;
			}

			var count = reader.ReadInt32();
			var most = count < 0 || count > SifterSettings.Most ? 0 : count;
			for (var i = 0; i < most; i++)
			{
				log.Add(Take(reader));
			}
		}

		private static SifterEntry Take(BinaryReader reader)
		{
			var kind = (SifterKind)reader.ReadByte();
			var ticks = reader.ReadInt64();
			var frame = reader.ReadInt32();
			var off = reader.ReadBoolean();
			var count = reader.ReadInt32();
			var line = reader.ReadInt32();
			var file = reader.ReadString();
			var message = reader.ReadString();
			return new SifterEntry(kind, message, reader.ReadString(), ticks, file, line, frame, off, count);
		}
	}
}
