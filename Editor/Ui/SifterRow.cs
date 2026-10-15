using System.IO;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One log in the list, written as one line in columns: when, what it belongs to, what it said,
	/// and where it came from.
	/// <para>
	/// One line rather than two. Two lines put the time under the message, which left the gap inside
	/// a row and the gap between rows almost the same size, and a list where you cannot tell which
	/// second line belongs to which first line is worse than one that says less. In a monospaced
	/// font the time and the place line up down the list and can be read as columns.
	/// </para>
	/// <para>
	/// A category is marked with its own colour rather than by colouring the message, because the
	/// message colour already means something: red is an error and amber is a warning, and that is
	/// the one thing nobody should have to look twice at.
	/// </para>
	/// </summary>
	public sealed class SifterRow : VisualElement
	{
		private readonly VisualElement badge;
		private readonly Label clock;
		private readonly Label mark;
		private readonly Label title;
		private readonly Label count;
		private readonly Label source;

		private string said = string.Empty;
		private string belongs = string.Empty;

		public SifterRow()
		{
			AddToClassList("sifter__row");
			SifterTips.On(this, () => SifterShown.Tooltips ? said : null);
			badge = SifterStyle.Classed(new VisualElement(), "sifter__badge");
			Add(badge);
			clock = SifterStyle.Classed(new Label(), "sifter__row-clock");
			Add(clock);
			mark = SifterStyle.Clipped(SifterStyle.Classed(new Label(), "sifter__cat"));
			SifterTips.On(mark, () => belongs);
			Add(mark);
			title = SifterStyle.Clipped(SifterStyle.Classed(new Label(), "sifter__row-title"));
			Add(title);
			count = SifterStyle.Classed(new Label(), "sifter__pill");
			Add(count);
			source = SifterStyle.Clipped(SifterStyle.Classed(new Label(), "sifter__row-source"));
			Add(source);
		}

		public void Show(SifterEntry entry)
		{
			clock.text = entry.When.ToString("HH:mm:ss");
			title.text = Headline(entry.Message);
			source.text = Where(entry);
			count.text = entry.Count.ToString();
			count.style.display = entry.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
			said = entry.Message;
			Tint(entry.Kind);
			Belongs(entry.Category);
		}

		/// <summary>Which way round the stripe goes, which only the list knows.</summary>
		public void Odd(bool odd)
		{
			EnableInClassList("sifter__row--odd", odd && SifterShown.Stripes);
		}

		/// <summary>At the narrow end a row keeps the one thing it is for.</summary>
		public void Narrow(bool narrow)
		{
			clock.style.display = narrow ? DisplayStyle.None : DisplayStyle.Flex;
			source.style.display = narrow ? DisplayStyle.None : DisplayStyle.Flex;
		}

		/// <summary>
		/// The mark down the edge and, if it is wanted, the message itself. Set here rather than in
		/// the stylesheet because both colours can be changed, and a stylesheet cannot read a
		/// setting.
		/// </summary>
		private void Tint(SifterKind kind)
		{
			var bad = SifterKinds.IsError(kind);
			var uneasy = SifterKinds.IsWarning(kind);
			badge.style.backgroundColor = Edge(bad, uneasy);
			title.style.color = SifterShown.TintRows ? Edge(bad, uneasy) : StyleKeyword.Null;
		}

		private static StyleColor Edge(bool bad, bool uneasy)
		{
			if (bad)
			{
				return SifterPalette.Error;
			}

			return uneasy ? SifterPalette.Warning : new StyleColor(StyleKeyword.Null);
		}

		private void Belongs(string category)
		{
			var how = SifterShown.CategoryMark;
			mark.style.display = how == SifterShown.NoMark ? DisplayStyle.None : DisplayStyle.Flex;
			if (how == SifterShown.NoMark)
			{
				return;
			}

			var colour = SifterPalette.Category(category);
			var named = how == SifterShown.NamedMark;
			mark.text = named ? Shortened(category) : string.Empty;
			mark.EnableInClassList("sifter__cat--named", named);
			mark.style.color = colour;
			mark.style.backgroundColor = named ? SifterPalette.Behind(colour) : colour;
			belongs = category;
		}

		/// <summary>
		/// A category is a path, and on a row the parents are the same on every line around it. The
		/// whole of it is still in the list and in the tip.
		/// </summary>
		private static string Shortened(string category)
		{
			return SifterShown.ShortMark ? SifterCategories.Leaf(category) : category;
		}

		private static string Headline(string message)
		{
			var end = message.IndexOf('\n');
			return end < 0 ? message : message.Substring(0, end);
		}

		private static string Where(SifterEntry entry)
		{
			var from = From(entry);
			return entry.Frame < 0 ? from : (from + "  f" + entry.Frame).TrimStart();
		}

		private static string From(SifterEntry entry)
		{
			if (SifterKinds.IsCompiler(entry.Kind))
			{
				return entry.File.Length == 0 ? string.Empty : Path.GetFileName(entry.File);
			}

			var frame = SifterPainter.Source(entry.Frames);
			return frame == null ? string.Empty : frame.Class + "." + frame.Method;
		}
	}
}
