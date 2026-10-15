using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// The categories, with a tick each and how many logs are in them.
	/// <para>
	/// A panel of its own rather than a menu, because a menu can show the state of a toggle and
	/// never how much is behind it, and how much is behind it is the thing that tells you which one
	/// is drowning the list.
	/// </para>
	/// </summary>
	public sealed class SifterCategoryList : VisualElement
	{
		private readonly VisualElement body;

		private Dictionary<string, int> counts = new Dictionary<string, int>();

		public SifterCategoryList()
		{
			AddToClassList("sifter__popover");
			var head = SifterStyle.Classed(new VisualElement(), "sifter__popover-head");
			head.Add(SifterStyle.Classed(new Label("Categories"), "sifter__popover-title"));
			head.Add(SifterStyle.Classed(new VisualElement(), "sifter__spacer"));
			head.Add(Small("All", () => Every(true)));
			head.Add(Small("None", () => Every(false)));
			Add(head);

			body = SifterStyle.Classed(new ScrollView(), "sifter__popover-body");
			Add(body);

			var foot = SifterStyle.Classed(new VisualElement(), "sifter__popover-foot");
			foot.Add(Small("Forget The Empty Ones", Tidy));
			Add(foot);
		}

		/// <summary>Raised when what is shown changed, so the list behind can be filtered again.</summary>
		public event Action Changed = delegate { };

		/// <summary>Draws the categories, given how many held logs are in each.</summary>
		public void Show(Dictionary<string, int> many)
		{
			counts = many;
			body.Clear();
			foreach (var name in SifterCategories.Known)
			{
				body.Add(Row(name));
			}
		}

		private VisualElement Row(string name)
		{
			var row = SifterStyle.Classed(new Button(() => Flipped(name)) { text = string.Empty },
				"sifter__popover-row");
			row.Add(Tick(name));
			row.Add(SifterStyle.Clipped(
				SifterStyle.Classed(new Label(SifterCategories.Leaf(name)), "sifter__popover-name")));
			row.Add(SifterStyle.Classed(new Label(Many(name)), "sifter__popover-count"));
			row.style.paddingLeft = 7f + SifterCategories.Depth(name) * 14f;
			row.EnableInClassList("sifter__popover-row--on", SifterCategories.Shown(name));
			row.EnableInClassList("sifter__popover-row--builtin", SifterCategories.Builtin(name));
			SifterTips.On(row, SifterCategories.Builtin(name)
				? "Built in. Always here, whether anything is in it or not."
				: "Found in this project's own logs.");
			return row;
		}

		/// <summary>
		/// The tick is the category's own colour rather than one colour for all of them, so the
		/// list doubles as the key for the marks down the side of the log list.
		/// </summary>
		private static VisualElement Tick(string name)
		{
			var tick = SifterStyle.Classed(new VisualElement(), "sifter__tick");
			var colour = SifterPalette.Category(name);
			tick.style.backgroundColor = SifterCategories.Shown(name)
				? colour
				: new StyleColor(new Color(colour.r, colour.g, colour.b, 0.14f));
			tick.style.borderTopColor = colour;
			tick.style.borderBottomColor = colour;
			tick.style.borderLeftColor = colour;
			tick.style.borderRightColor = colour;
			return tick;
		}

		/// <summary>
		/// How many logs are in a category, counting everything underneath it. A parent whose own
		/// name nothing uses would otherwise read as empty while holding hundreds.
		/// </summary>
		private string Many(string name)
		{
			var held = 0;
			foreach (var pair in counts)
			{
				held += Under(name, pair.Key) ? pair.Value : 0;
			}

			return held.ToString();
		}

		private static bool Under(string parent, string name)
		{
			return name == parent || name.StartsWith(parent + ".", StringComparison.Ordinal);
		}

		private void Flipped(string name)
		{
			SifterCategories.Show(name, !SifterCategories.Shown(name));
			Show(counts);
			Changed();
		}

		private void Every(bool shown)
		{
			SifterCategories.ShowAll(shown);
			Show(counts);
			Changed();
		}

		private void Tidy()
		{
			var alive = new List<string>();
			foreach (var pair in counts)
			{
				Keep(alive, pair.Key, pair.Value);
			}

			SifterCategories.Forget(alive);
			Show(counts);
			Changed();
		}

		private static void Keep(List<string> alive, string name, int held)
		{
			if (held > 0)
			{
				alive.Add(name);
			}
		}

		private static Button Small(string text, Action clicked)
		{
			return SifterStyle.Classed(new Button(clicked) { text = text }, "sifter__popover-action");
		}
	}
}
