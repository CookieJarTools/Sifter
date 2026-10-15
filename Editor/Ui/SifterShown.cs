using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// What a trace shows, which is two separate questions worth keeping separate.
	/// <para>
	/// Which parts of a frame you see: namespace, class, method, parameters, file and line, each on
	/// its own. Turning off namespaces and parameters alone takes most traces from two lines to one.
	/// And which frames you see at all: your own code, or everything.
	/// </para>
	/// </summary>
	public static class SifterShown
	{
		/// <summary>No mark, a coloured dot, or the category's name in its own colour.</summary>
		public const int NoMark = 0;

		public const int DotMark = 1;

		public const int NamedMark = 2;

		public const int ShortRow = 22;
		public const int CosyRow = 26;
		public const int TallRow = 32;

		private const string PartKey = "CookieJarTools.Sifter.Show.";
		private const string MineKey = "CookieJarTools.Sifter.MineOnly";
		private const string HideKey = "CookieJarTools.Sifter.Honour";
		private const string RowKey = "CookieJarTools.Sifter.RowHeight";
		private const string ShutKey = "CookieJarTools.Sifter.Collapsed";
		private const string HoverKey = "CookieJarTools.Sifter.Tooltips";
		private const string TintKey = "CookieJarTools.Sifter.TintRows";
		private const string SampleKey = "CookieJarTools.Sifter.SampleShut";
		private const string MarkKey = "CookieJarTools.Sifter.CategoryMark";
		private const string DenseKey = "CookieJarTools.Sifter.Compact";
		private const string BesideKey = "CookieJarTools.Sifter.Beside";
		private const string DepthKey = "CookieJarTools.Sifter.CategoryDepth";
		private const string ShortKey = "CookieJarTools.Sifter.ShortCategory";
		private const string StripeKey = "CookieJarTools.Sifter.Stripes";
		private const string WrapKey = "CookieJarTools.Sifter.WrapFrames";

		public static int Version { get; private set; }

		/// <summary>
		/// Goes up when the shape of a category changes, which is the one setting that makes every
		/// category already worked out wrong. Everything else only changes how one is drawn.
		/// </summary>
		public static int Shaped { get; private set; }

		/// <summary>
		/// How many parts of a namespace make up a category. One gives CookieJarTools, two gives
		/// CookieJarTools.Sifter, which in a project with more than one thing in it is the level
		/// somebody actually thinks at.
		/// </summary>
		public static int CategoryDepth
		{
			get { return Mathf.Clamp(EditorPrefs.GetInt(DepthKey, 2), 1, 3); }
			set
			{
				EditorPrefs.SetInt(DepthKey, Mathf.Clamp(value, 1, 3));
				Shaped++;
				Version++;
			}
		}

		/// <summary>
		/// Whether every other row is a shade lighter. In a dense list of near identical lines it is
		/// what stops the eye sliding from the message on one row to the place on the next.
		/// </summary>
		public static bool Stripes
		{
			get { return EditorPrefs.GetBool(StripeKey, true); }
			set { Kept(StripeKey, value); }
		}

		/// <summary>
		/// Whether a long frame wraps onto the next line instead of running off the side. On by
		/// default: reading a stack should not mean scrolling sideways and losing your place in it.
		/// </summary>
		public static bool WrapFrames
		{
			get { return EditorPrefs.GetBool(WrapKey, true); }
			set { Kept(WrapKey, value); }
		}

		/// <summary>Whether the mark on a row drops the parents and shows only the last part.</summary>
		public static bool ShortMark
		{
			get { return EditorPrefs.GetBool(ShortKey, true); }
			set { Kept(ShortKey, value); }
		}

		/// <summary>Fold away everything that is not under Assets.</summary>
		public static bool MineOnly
		{
			get { return EditorPrefs.GetBool(MineKey, false); }
			set { Kept(MineKey, value); }
		}

		/// <summary>Drop frames whose method is marked HideInCallstack.</summary>
		public static bool Honour
		{
			get { return EditorPrefs.GetBool(HideKey, true); }
			set { Kept(HideKey, value); }
		}

		/// <summary>
		/// Whether the stack is shut down to the frame it happened in. On to begin with: a stack of
		/// twenty frames is worth having and is not worth looking at every time.
		/// </summary>
		public static bool Collapsed
		{
			get { return EditorPrefs.GetBool(ShutKey, true); }
			set { Kept(ShutKey, value); }
		}

		/// <summary>
		/// Whether resting on a row pops its whole message up. Off to begin with, because a tooltip
		/// that appears over the list every time the pointer crosses it is in the way far more often
		/// than it is wanted.
		/// </summary>
		public static bool Tooltips
		{
			get { return EditorPrefs.GetBool(HoverKey, false); }
			set { Kept(HoverKey, value); }
		}

		/// <summary>
		/// Whether a row's text takes the colour of what it is.
		/// <para>
		/// The mark down the left edge says the same thing, but a two pixel bar is not what the eye
		/// lands on when it is scanning a hundred lines. An option rather than a rule, because a
		/// list where a third of the lines are red is its own kind of hard to read.
		/// </para>
		/// </summary>
		public static bool TintRows
		{
			get { return EditorPrefs.GetBool(TintKey, true); }
			set { Kept(TintKey, value); }
		}

		/// <summary>Whether the sample along the bottom of the settings screen is folded away.</summary>
		public static bool SampleShut
		{
			get { return EditorPrefs.GetBool(SampleKey, false); }
			set { Kept(SampleKey, value); }
		}

		/// <summary>
		/// Everything drawn tighter, for a window docked in a corner. It takes the padding out of
		/// the toolbar, the rows and the stack, drops the box around the message and hides the
		/// timestamp in the header, where the list already says it.
		/// </summary>
		public static bool Compact
		{
			get { return EditorPrefs.GetBool(DenseKey, false); }
			set { Kept(DenseKey, value); }
		}

		/// <summary>
		/// Whether the selected log opens beside the list rather than under it. Docked down the side
		/// of the editor there is height and no width; docked along the bottom it is the other way
		/// round, and a stack that has to wrap every line is hard to read.
		/// </summary>
		public static bool Beside
		{
			get { return EditorPrefs.GetBool(BesideKey, false); }
			set { Kept(BesideKey, value); }
		}

		/// <summary>How a row says which category it belongs to.</summary>
		public static int CategoryMark
		{
			get { return Mathf.Clamp(EditorPrefs.GetInt(MarkKey, DotMark), NoMark, NamedMark); }
			set
			{
				EditorPrefs.SetInt(MarkKey, Mathf.Clamp(value, NoMark, NamedMark));
				Version++;
			}
		}

		/// <summary>
		/// How tall a row is. Compact overrules whatever was chosen, because the whole point of it
		/// is to get more on screen, and it puts the choice back untouched when it is turned off.
		/// </summary>
		public static int RowHeight
		{
			get { return Compact ? ShortRow : Settled(EditorPrefs.GetInt(RowKey, CosyRow)); }
			set { EditorPrefs.SetInt(RowKey, Settled(value)); Version++; }
		}

		public static bool Has(SifterPart part)
		{
			return EditorPrefs.GetBool(PartKey + part, Usually(part));
		}

		public static void Set(SifterPart part, bool shown)
		{
			EditorPrefs.SetBool(PartKey + part, shown);
			Version++;
		}

		private static void Kept(string key, bool value)
		{
			EditorPrefs.SetBool(key, value);
			Version++;
		}

		/// <summary>
		/// One of the three heights the menu offers. A height saved by an older version is not one
		/// of them, and a row size nothing in the menu agrees with cannot be changed back.
		/// </summary>
		private static int Settled(int height)
		{
			if (height == ShortRow || height == TallRow)
			{
				return height;
			}

			return CosyRow;
		}

		/// <summary>What the row height menu should show, which is the choice and not the override.</summary>
		public static int ChosenRowHeight
		{
			get { return Settled(EditorPrefs.GetInt(RowKey, CosyRow)); }
		}

		/// <summary>
		/// Namespaces are off to begin with. They are the longest part of a frame and the least
		/// often the thing you are looking for, and with them on most traces wrap.
		/// </summary>
		private static bool Usually(SifterPart part)
		{
			return part != SifterPart.Namespace;
		}
	}
}
