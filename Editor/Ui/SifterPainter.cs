using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Turns a frame into the one coloured line it is shown as.
	/// <para>
	/// Rich text rather than an element per part, because a trace is twenty frames of six parts
	/// each and a hundred and twenty labels per selection is a lot of elements for something that
	/// cannot be clicked separately anyway. The frame as a whole is what gets clicked.
	/// </para>
	/// </summary>
	public static class SifterPainter
	{
		public static string Paint(SifterFrame frame)
		{
			if (!frame.Read)
			{
				return Tinted(Escaped(frame.Raw), SifterPalette.Hex(SifterPalette.Faint));
			}

			var built = new StringBuilder();
			Who(built, frame);
			What(built, frame);
			Whence(built, frame);
			return built.ToString();
		}

		/// <summary>Every frame of a trace, with the ones nobody asked to see taken out.</summary>
		public static List<SifterFrame> Worth(List<SifterFrame> frames)
		{
			var worth = new List<SifterFrame>();
			foreach (var frame in frames)
			{
				Keep(worth, frame);
			}

			return worth;
		}

		/// <summary>
		/// The least of a stack worth showing when it is shut: the frame it happened in, and, when
		/// that one has no file to open, everything down to the first that has.
		/// <para>
		/// The top frame is very often UnityEngine.Debug:Log itself, which has nowhere to go, so a
		/// shut stack that showed only the first line would be a line you cannot click and cannot
		/// learn anything from.
		/// </para>
		/// </summary>
		public static List<SifterFrame> Top(List<SifterFrame> worth)
		{
			var top = new List<SifterFrame>();
			var until = Reachable(worth);
			for (var i = 0; i <= until && i < worth.Count; i++)
			{
				top.Add(worth[i]);
			}

			return top;
		}

		private static int Reachable(List<SifterFrame> worth)
		{
			for (var i = 0; i < worth.Count; i++)
			{
				if (worth[i].Somewhere)
				{
					return i;
				}
			}

			return 0;
		}

		/// <summary>The first frame that is the project's own, which is where a log really came from.</summary>
		public static SifterFrame Source(List<SifterFrame> frames)
		{
			foreach (var frame in frames)
			{
				if (frame.Mine && !SifterHidden.Is(frame))
				{
					return frame;
				}
			}

			return null;
		}

		private static void Keep(List<SifterFrame> worth, SifterFrame frame)
		{
			if (SifterShown.Honour && SifterHidden.Is(frame))
			{
				return;
			}

			if (SifterShown.MineOnly && !frame.Mine)
			{
				return;
			}

			worth.Add(frame);
		}

		private static void Who(StringBuilder built, SifterFrame frame)
		{
			if (SifterShown.Has(SifterPart.Namespace) && frame.Namespace.Length > 0)
			{
				built.Append(Part(frame.Namespace, SifterPart.Namespace)).Append(Faint("."));
			}

			if (SifterShown.Has(SifterPart.Class) && frame.Class.Length > 0)
			{
				built.Append(Part(frame.Class, SifterPart.Class)).Append(Faint("."));
			}
		}

		private static void What(StringBuilder built, SifterFrame frame)
		{
			if (SifterShown.Has(SifterPart.Method))
			{
				built.Append(Part(frame.Method, SifterPart.Method));
			}

			if (!SifterShown.Has(SifterPart.Parameters))
			{
				built.Append(Faint("()"));
				return;
			}

			built.Append(Faint("(")).Append(Part(frame.Parameters, SifterPart.Parameters)).Append(Faint(")"));
		}

		private static void Whence(StringBuilder built, SifterFrame frame)
		{
			if (!frame.Somewhere || !SifterShown.Has(SifterPart.File))
			{
				return;
			}

			built.Append("   ").Append(Part(Path.GetFileName(frame.File), SifterPart.File));
			if (SifterShown.Has(SifterPart.Line))
			{
				built.Append(Faint(":")).Append(Part(frame.Line.ToString(), SifterPart.Line));
			}
		}

		private static string Part(string text, SifterPart part)
		{
			return Tinted(Escaped(text), SifterPalette.Hex(part));
		}

		private static string Faint(string text)
		{
			return Tinted(text, SifterPalette.Hex(SifterPalette.Faint));
		}

		private static string Tinted(string text, string hex)
		{
			return "<color=#" + hex + ">" + text + "</color>";
		}

		/// <summary>
		/// A generic parameter arrives as List&lt;int&gt;, and an angle bracket left alone in rich
		/// text swallows everything up to the next one.
		/// <para>
		/// Wrapped rather than escaped, because UI Toolkit's text understands tags and does not
		/// understand HTML entities: an escaped bracket is drawn as the five letters of &amp;lt;.
		/// </para>
		/// </summary>
		private static string Escaped(string text)
		{
			return text.IndexOf('<') < 0 ? text : "<noparse>" + text + "</noparse>";
		}
	}
}
