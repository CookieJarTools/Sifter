using System;
using System.Collections.Generic;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One line of output, as it was captured.
	/// <para>
	/// The stack is kept as the text it arrived as rather than broken into frames here. Parsing is
	/// the expensive part and most entries are never opened, so it is left until something asks to
	/// show one.
	/// </para>
	/// </summary>
	public sealed class SifterEntry
	{
		private List<SifterFrame> frames;
		private string category;
		private int shaped = -1;

		public SifterEntry(SifterKind kind, string message, string trace, long ticks,
			string file = null, int line = 0, int frame = -1, bool offMainThread = false, int count = 1)
		{
			Kind = kind;
			Message = message ?? string.Empty;
			Trace = trace ?? string.Empty;
			Ticks = ticks;
			File = file ?? string.Empty;
			Line = line;
			Frame = frame;
			OffMainThread = offMainThread;
			Count = count < 1 ? 1 : count;
		}

		public SifterKind Kind { get; }

		/// <summary>The message on its own, with the stack taken off the end of it.</summary>
		public string Message { get; }

		/// <summary>The stack as text. Empty for anything the compiler reported.</summary>
		public string Trace { get; }

		/// <summary>When it happened, in UTC ticks, so that sorting does not depend on a time zone.</summary>
		public long Ticks { get; }

		/// <summary>
		/// Where the compiler said the problem was. Empty for an ordinary log, which says where it
		/// came from in its stack instead.
		/// </summary>
		public string File { get; }

		public int Line { get; }

		/// <summary>The frame this happened on, or -1 when the game was not playing.</summary>
		public int Frame { get; }

		public bool OffMainThread { get; }

		/// <summary>How many identical entries this one stands for.</summary>
		public int Count { get; private set; }

		/// <summary>
		/// The stack taken apart, worked out the first time something asks and kept after that.
		/// Most entries are never opened, so doing this when one arrives would be work thrown away.
		/// </summary>
		public List<SifterFrame> Frames
		{
			get { return frames ?? (frames = SifterFrames.Of(Trace)); }
		}

		/// <summary>
		/// Which category this belongs to, worked out the first time something asks. Nothing in the
		/// game declares it, so it costs a look at the message and at most one look at the stack.
		/// </summary>
		public string Category
		{
			get
			{
				if (category == null || shaped != SifterShown.Shaped)
				{
					shaped = SifterShown.Shaped;
					category = SifterCategory.Named(this);
				}

				return category;
			}
		}

		public DateTime When
		{
			get { return new DateTime(Ticks, DateTimeKind.Utc).ToLocalTime(); }
		}

		public void Repeated()
		{
			Count++;
		}
	}
}
