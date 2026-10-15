using System;
using System.Collections.Generic;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Everything captured so far, oldest first, with the oldest dropped once it is full.
	/// <para>
	/// Logs arrive on whatever thread called Debug.Log, so adding takes a lock. What adding
	/// deliberately does not do is raise Changed: a handler is going to touch a window, and
	/// touching UI off the main thread brings the editor down. It sets a flag, and Settle raises
	/// the event on the next editor tick. That also means a frame that logs four thousand times
	/// causes one repaint rather than four thousand.
	/// </para>
	/// <para>
	/// This is an ordinary object rather than a pile of statics so a test can make one of its own
	/// without throwing away the logs the editor is actually holding. SifterLogs.Current is the one
	/// the editor captures into.
	/// </para>
	/// </summary>
	public sealed class SifterLog
	{
		private readonly object guard = new object();

		private SifterEntry[] ring;
		private int next;
		private int held;
		private int errors;
		private int warnings;
		private int logs;
		private bool dirty;

		public SifterLog(int capacity)
		{
			ring = new SifterEntry[capacity < 1 ? 1 : capacity];
		}

		/// <summary>Raised on the main thread after one or more entries have arrived.</summary>
		public event Action Changed;

		public int Count
		{
			get { lock (guard) { return held; } }
		}

		public int Capacity
		{
			get { lock (guard) { return ring.Length; } }
		}

		public int Errors
		{
			get { lock (guard) { return errors; } }
		}

		public int Warnings
		{
			get { lock (guard) { return warnings; } }
		}

		public int Logs
		{
			get { lock (guard) { return logs; } }
		}

		/// <summary>The entry at a position, counting from the oldest, or null if there is none.</summary>
		public SifterEntry At(int index)
		{
			lock (guard)
			{
				return index < 0 || index >= held ? null : ring[Spot(index)];
			}
		}

		public void Add(SifterEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			lock (guard)
			{
				Store(entry);
			}
		}

		public void Clear()
		{
			lock (guard)
			{
				Wipe(ring.Length);
				dirty = true;
			}
		}

		/// <summary>Changes how many are held, keeping the newest that still fit.</summary>
		public void Resize(int capacity)
		{
			var kept = All();
			lock (guard)
			{
				Wipe(capacity);
				Refill(kept);
				dirty = true;
			}
		}

		/// <summary>
		/// Raises Changed if anything has arrived since the last time. Called once an editor tick,
		/// on the main thread, which is the whole point of it being separate from Add.
		/// </summary>
		public void Settle()
		{
			bool waiting;
			lock (guard)
			{
				waiting = dirty;
				dirty = false;
			}

			if (waiting && Changed != null)
			{
				Changed();
			}
		}

		/// <summary>Everything held, oldest first, as a copy that is safe to walk.</summary>
		public List<SifterEntry> All()
		{
			lock (guard)
			{
				return Copied();
			}
		}

		/// <summary>
		/// Puts everything back in the order it happened. Used once after startup, because what is
		/// read back from disk and what is picked up from Unity's own console arrive in two runs
		/// and interleave in time.
		/// </summary>
		public void Sort()
		{
			var kept = All();
			kept.Sort(Earlier);
			lock (guard)
			{
				Wipe(ring.Length);
				Refill(kept);
			}
		}

		private static int Earlier(SifterEntry left, SifterEntry right)
		{
			return left.Ticks.CompareTo(right.Ticks);
		}

		private void Wipe(int capacity)
		{
			ring = new SifterEntry[capacity < 1 ? 1 : capacity];
			next = 0;
			held = 0;
			errors = 0;
			warnings = 0;
			logs = 0;
		}

		private void Refill(List<SifterEntry> kept)
		{
			var from = kept.Count > ring.Length ? kept.Count - ring.Length : 0;
			for (var i = from; i < kept.Count; i++)
			{
				Store(kept[i]);
			}
		}

		private List<SifterEntry> Copied()
		{
			var all = new List<SifterEntry>(held);
			for (var i = 0; i < held; i++)
			{
				all.Add(ring[Spot(i)]);
			}

			return all;
		}

		private void Store(SifterEntry entry)
		{
			if (held == ring.Length)
			{
				Tally(ring[next], -1);
			}

			ring[next] = entry;
			next = (next + 1) % ring.Length;
			held = held < ring.Length ? held + 1 : ring.Length;
			Tally(entry, 1);
			dirty = true;
		}

		private int Spot(int index)
		{
			return (next - held + index + ring.Length * 2) % ring.Length;
		}

		/// <summary>
		/// Moves the three toolbar counts by one, up for an entry arriving and down for one being
		/// dropped. Counting on the way in is the only way these stay cheap: a buffer of five
		/// thousand recounted on every repaint would be felt.
		/// </summary>
		private void Tally(SifterEntry entry, int by)
		{
			if (SifterKinds.IsError(entry.Kind))
			{
				errors += by;
				return;
			}

			if (SifterKinds.IsWarning(entry.Kind))
			{
				warnings += by;
				return;
			}

			logs += by;
		}
	}
}
