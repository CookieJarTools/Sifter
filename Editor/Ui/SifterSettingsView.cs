using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Everything that can be changed, with a sample of what changing it looks like.
	/// <para>
	/// The sample is drawn with the console's own row and the console's own painter, so it cannot
	/// drift away from the real thing. That is the whole reason it is here: nearly every setting in
	/// this tool is about how something reads, and a sentence describing a colour or a row height is
	/// no use at all next to seeing it.
	/// </para>
	/// <para>
	/// It sits along the bottom and folds away. Along the bottom because the settings are what you
	/// came here to change and should be the first thing under your eye, and it folds away because
	/// once you have the colours you want it is only taking up room.
	/// </para>
	/// </summary>
	public sealed class SifterSettingsView : VisualElement
	{
		private static readonly int[] Heights =
			{ SifterShown.ShortRow, SifterShown.CosyRow, SifterShown.TallRow };
		private static readonly int[] Caps = { 1000, 5000, 20000, 100000 };

		private readonly List<SifterRow> rows = new List<SifterRow>();
		private readonly List<SifterChip> parts = new List<SifterChip>();
		private readonly Dictionary<SifterPart, ColorField> swatches = new Dictionary<SifterPart, ColorField>();
		private readonly Dictionary<string, ColorField> kinds = new Dictionary<string, ColorField>();

		private VisualElement shown;
		private VisualElement caret;
		private SifterTrace stack;
		private VisualElement tints;
		private List<string> tinted;
		private SifterChoice dense;
		private SifterChoice beside;
		private SifterChoice marked;
		private SifterChoice deep;
		private SifterChoice brief;
		private SifterChoice height;
		private SifterChoice tint;
		private SifterChoice striped;
		private SifterChoice hover;
		private SifterChoice wrapped;
		private SifterChoice mine;
		private SifterChoice honour;
		private SifterChoice shut;
		private SifterChoice cap;
		private SifterChoice play;
		private SifterChoice restart;

		public SifterSettingsView()
		{
			AddToClassList("sifter__settings");
			Head();

			var scroll = SifterStyle.Classed(new ScrollView(), "sifter__settings-body");
			Window(scroll);
			Rows(scroll);
			Stack(scroll);
			Colours(scroll);
			Keeping(scroll);
			Add(scroll);

			Preview();
			Drawn();
		}

		/// <summary>Raised when the screen is finished with.</summary>
		public event Action Closed = delegate { };

		/// <summary>Raised when something the console draws from changed.</summary>
		public event Action Changed = delegate { };

		/// <summary>Every control put back in step with what is stored, and the sample drawn again.</summary>
		public void Drawn()
		{
			dense.Pick(SifterShown.Compact ? 1 : 0);
			beside.Pick(SifterShown.Beside ? 1 : 0);
			height.Pick(Array.IndexOf(Heights, SifterShown.ChosenRowHeight));
			marked.Pick(SifterShown.CategoryMark);
			deep.Pick(SifterShown.CategoryDepth - 1);
			brief.Pick(SifterShown.ShortMark ? 1 : 0);
			tint.Pick(SifterShown.TintRows ? 1 : 0);
			striped.Pick(SifterShown.Stripes ? 1 : 0);
			hover.Pick(SifterShown.Tooltips ? 1 : 0);
			wrapped.Pick(SifterShown.WrapFrames ? 1 : 0);
			mine.Pick(SifterShown.MineOnly ? 1 : 0);
			honour.Pick(SifterShown.Honour ? 1 : 0);
			shut.Pick(SifterShown.Collapsed ? 1 : 0);
			cap.Pick(Array.IndexOf(Caps, SifterSettings.Capacity));
			play.Pick(SifterSettings.SurvivePlay ? 1 : 0);
			restart.Pick(SifterSettings.SurviveRestart ? 1 : 0);
			Chips();
			Tints();
			Kinds();
			Redraw();
		}

		private void Kinds()
		{
			kinds["Error"].SetValueWithoutNotify(SifterPalette.Error);
			kinds["Warning"].SetValueWithoutNotify(SifterPalette.Warning);
		}

		private void Head()
		{
			var head = SifterStyle.Classed(new VisualElement(), "sifter__settings-head");
			head.Add(SifterStyle.Classed(new Label("Settings"), "sifter__settings-title"));
			head.Add(SifterStyle.Classed(new VisualElement(), "sifter__spacer"));
			var done = SifterStyle.Classed(new Button(() => Closed()) { text = "Done" }, "sifter__primary");
			SifterTips.On(done, "Go back to the logs.");
			head.Add(done);
			Add(head);
		}

		private void Preview()
		{
			var sample = SifterStyle.Classed(new VisualElement(), "sifter__sample");
			var head = SifterStyle.Classed(new Button(Folded) { text = string.Empty }, "sifter__sample-head");
			caret = new VisualElement { pickingMode = PickingMode.Ignore };
			head.Add(SifterStyle.Classed(caret, "sifter__stack-caret"));
			head.Add(SifterStyle.Classed(new Label("Sample"), "sifter__stack-title"));
			head.Add(SifterStyle.Classed(new Label("what these settings look like"), "sifter__stack-count"));
			sample.Add(head);

			shown = SifterStyle.Classed(new VisualElement(), "sifter__sample-body");
			var list = SifterStyle.Classed(new VisualElement(), "sifter__sample-rows");
			foreach (var entry in SifterSample.Rows())
			{
				list.Add(Row(entry));
			}

			shown.Add(list);
			stack = new SifterTrace();
			stack.Changed += Drawn;
			shown.Add(SifterStyle.Classed(stack, "sifter__sample-trace"));
			sample.Add(shown);
			Add(sample);
		}

		private void Folded()
		{
			SifterShown.SampleShut = !SifterShown.SampleShut;
			Drawn();
		}

		private SifterRow Row(SifterEntry entry)
		{
			var row = new SifterRow();
			row.Show(entry);
			rows.Add(row);
			return row;
		}

		private void Window(VisualElement into)
		{
			var box = Section(into, "Window");
			dense = Switch(box, "Compact",
				"Draw everything tighter, for a window docked in a corner. Rows shrink, the message loses its box, and the toolbar loses its padding.",
				on => Set(() => SifterShown.Compact = on));
			beside = Choice(box, "Where A Log Opens",
				"Under the list, or next to it. Docked down the side of the editor there is height and no width, and a stack that wraps every line is hard to read.",
				new[] { "Below", "Beside" }, at => Set(() => SifterShown.Beside = at == 1));
		}

		private void Rows(VisualElement into)
		{
			var box = Section(into, "Rows");
			height = Choice(box, "Row Height",
				"How much room each log gets in the list. Compact mode overrules this while it is on.",
				new[] { "Short", "Cosy", "Tall" }, at => Set(() => SifterShown.RowHeight = Heights[at]));
			tint = Switch(box, "Colour By Kind",
				"Give a warning and an error their own colour, not only the mark down the edge.",
				on => Set(() => SifterShown.TintRows = on));
			striped = Switch(box, "Alternating Row Colours",
				"Shade every other row, so the eye does not slide from one line to the next.",
				on => Set(() => SifterShown.Stripes = on));
			hover = Switch(box, "Show The Message On Hover",
				"Pop the whole message up when you rest on a row. Off by default: it is in the way more often than it is wanted.",
				on => Set(() => SifterShown.Tooltips = on));
			marked = Choice(box, "Show Category",
				"How a row says which category it is in. Its own colour, rather than the message colour, which already means error or warning.",
				new[] { "Off", "Dot", "Name" }, at => Set(() => SifterShown.CategoryMark = at));
			deep = Choice(box, "Category Depth",
				"How many parts of a namespace make a category. One gives CookieJarTools, two gives CookieJarTools.Sifter, which is the level most projects think at.",
				new[] { "1", "2", "3" }, at => Set(() => SifterShown.CategoryDepth = at + 1));
			brief = Switch(box, "Short Category Names",
				"Show only the last part on a row, so CookieJarTools.Sifter reads as Sifter. The whole name is still in the list and in the tip.",
				on => Set(() => SifterShown.ShortMark = on));
		}

		private void Stack(VisualElement into)
		{
			var box = Section(into, "Stack");
			var option = new SifterOption("Parts Of A Frame",
				"Turn off what you do not read. Namespaces and parameters alone are most of the width.");
			foreach (SifterPart part in Enum.GetValues(typeof(SifterPart)))
			{
				option.Control.Add(Part(part));
			}

			option.AddToClassList("sifter__option--wide");
			box.Add(option);

			wrapped = Switch(box, "Wrap Long Frames",
				"Let a frame run onto the next line instead of off the side of the pane.",
				on => Set(() => SifterShown.WrapFrames = on));
			mine = Switch(box, "My Code Only",
				"Fold away every frame that is not under Assets.",
				on => Set(() => SifterShown.MineOnly = on));
			honour = Switch(box, "Honour Hide In Callstack",
				"Drop frames marked with Unity's attribute, which is what a logging helper of your own wants.",
				on => Set(() => SifterShown.Honour = on));
			shut = Switch(box, "Shut By Default",
				"Open a log with its stack closed down to the frame it happened in.",
				on => Set(() => SifterShown.Collapsed = on));
		}

		private SifterChip Part(SifterPart part)
		{
			var chip = new SifterChip(part.ToString(),
				() => Set(() => SifterShown.Set(part, !SifterShown.Has(part))));
			parts.Add(chip);
			return chip;
		}

		private void Colours(VisualElement into)
		{
			var box = Section(into, "Colours");
			Kind(box, "Error", "Errors, exceptions, asserts and compile errors.",
				() => SifterPalette.Error, colour => SifterPalette.Error = colour);
			Kind(box, "Warning", "Warnings and compile warnings.",
				() => SifterPalette.Warning, colour => SifterPalette.Warning = colour);
			foreach (SifterPart part in Enum.GetValues(typeof(SifterPart)))
			{
				Swatch(box, part);
			}

			var reset = SifterStyle.Classed(new Button(Defaults) { text = "Reset The Colours" }, "sifter__ghost");
			reset.AddToClassList("sifter__settings-reset");
			box.Add(reset);

			var cats = Section(into, "Category Colours");
			cats.Add(SifterStyle.Classed(new Label(
					"Each category is given a colour from its own name, so it is the same one tomorrow and on somebody else's machine. Change any of them here."),
				"sifter__section-note"));
			tints = SifterStyle.Classed(new VisualElement(), "sifter__tints");
			cats.Add(tints);
			var forget = SifterStyle.Classed(new Button(Unpicked) { text = "Back To The Worked Out Ones" }, "sifter__ghost");
			forget.AddToClassList("sifter__settings-reset");
			cats.Add(forget);
		}

		/// <summary>One colour standing for a kind, which drives both the mark and the message.</summary>
		private void Kind(VisualElement box, string title, string note, Func<Color> read, Action<Color> write)
		{
			var option = new SifterOption(title, note);
			var field = SifterStyle.Quiet(
				SifterStyle.Classed(new ColorField { value = read(), showAlpha = false }, "sifter__swatch"));
			field.RegisterValueChangedCallback(evt => Set(() => write(evt.newValue)));
			kinds[title] = field;
			option.Control.Add(field);
			box.Add(option);
		}

		/// <summary>
		/// A swatch per category, rebuilt only when the set of categories actually changed.
		/// Rebuilding on every edit would throw away the very field somebody is picking a colour in.
		/// </summary>
		private void Tints()
		{
			var known = SifterCategories.Known;
			if (Same(known))
			{
				return;
			}

			tinted = known;
			tints.Clear();
			foreach (var name in known)
			{
				tints.Add(Tint(name));
			}
		}

		private bool Same(List<string> known)
		{
			if (tinted == null || tinted.Count != known.Count)
			{
				return false;
			}

			for (var i = 0; i < known.Count; i++)
			{
				if (tinted[i] != known[i])
				{
					return false;
				}
			}

			return true;
		}

		private VisualElement Tint(string name)
		{
			var option = new SifterOption(name, string.Empty);
			option.AddToClassList("sifter__option--tight");
			var field = SifterStyle.Quiet(SifterStyle.Classed(
				new ColorField { value = SifterPalette.Category(name), showAlpha = false }, "sifter__swatch"));
			field.RegisterValueChangedCallback(evt => Set(() => SifterPalette.SetCategory(name, evt.newValue)));
			option.Control.Add(field);
			return option;
		}

		private void Unpicked()
		{
			foreach (var name in SifterCategories.Known)
			{
				SifterPalette.ForgetCategory(name);
			}

			Drawn();
			Changed();
		}

		private void Swatch(VisualElement box, SifterPart part)
		{
			var option = new SifterOption(part.ToString(), string.Empty);
			option.AddToClassList("sifter__option--tight");
			var field = SifterStyle.Quiet(SifterStyle.Classed(
				new ColorField { value = SifterPalette.Of(part), showAlpha = false }, "sifter__swatch"));
			field.RegisterValueChangedCallback(evt => Set(() => SifterPalette.Set(part, evt.newValue)));
			swatches[part] = field;
			option.Control.Add(field);
			box.Add(option);
		}

		private void Defaults()
		{
			SifterPalette.Reset();
			foreach (var pair in swatches)
			{
				pair.Value.SetValueWithoutNotify(SifterPalette.Of(pair.Key));
			}

			Drawn();
			Changed();
		}

		private void Keeping(VisualElement into)
		{
			var box = Section(into, "Keeping");
			cap = Choice(box, "Logs Held",
				"How many to hold before the oldest is dropped. All of them are written out and read back on every recompile, so a larger number is a slower reload.",
				new[] { "1,000", "5,000", "20,000", "100,000" }, at => Set(() => SifterSettings.Capacity = Caps[at]));
			play = Switch(box, "Survive Play Mode",
				"Keep what is already here when you press play.",
				on => Set(() => SifterSettings.SurvivePlay = on));
			restart = Switch(box, "Survive Closing The Editor",
				"Keep what is here between sessions. A recompile is always survived either way.",
				on => Set(() => SifterSettings.SurviveRestart = on));
		}

		private void Set(Action change)
		{
			change();
			Drawn();
			Changed();
		}

		private void Chips()
		{
			var all = (SifterPart[])Enum.GetValues(typeof(SifterPart));
			for (var i = 0; i < parts.Count && i < all.Length; i++)
			{
				parts[i].On(SifterShown.Has(all[i]));
			}
		}

		private void Redraw()
		{
			var folded = SifterShown.SampleShut;
			caret.EnableInClassList("sifter__stack-caret--open", !folded);
			shown.style.display = folded ? DisplayStyle.None : DisplayStyle.Flex;
			if (folded)
			{
				return;
			}

			var made = SifterSample.Rows();
			for (var i = 0; i < rows.Count && i < made.Length; i++)
			{
				Draw(rows[i], made[i]);
			}

			stack.Show(SifterSample.Stack());
		}

		private static void Draw(SifterRow row, SifterEntry entry)
		{
			row.style.height = SifterShown.RowHeight;
			row.Show(entry);
		}

		private SifterChoice Switch(VisualElement box, string title, string note, Action<bool> set)
		{
			return Choice(box, title, note, new[] { "Off", "On" }, at => set(at == 1));
		}

		private SifterChoice Choice(VisualElement box, string title, string note, string[] names,
			Action<int> picked)
		{
			var option = new SifterOption(title, note);
			var choice = new SifterChoice(names, picked);
			option.Control.Add(choice);
			box.Add(option);
			return choice;
		}

		private static VisualElement Section(VisualElement into, string title)
		{
			var box = SifterStyle.Classed(new VisualElement(), "sifter__section");
			box.Add(SifterStyle.Classed(new Label(title), "sifter__section-title"));
			into.Add(box);
			return box;
		}
	}
}
