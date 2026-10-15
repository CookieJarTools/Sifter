using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// The console itself: the list on top, the selected log's stack underneath.
	/// <para>
	/// It draws from SifterLogs.Current and captures nothing of its own, so closing it loses nothing
	/// and opening it later shows everything that happened while it was shut.
	/// </para>
	/// </summary>
	public sealed class SifterWindow : EditorWindow
	{
		private const int Cramped = 470;
		private const int Tight = 340;

		/// <summary>The categories panel's width, which the stylesheet fixes, and the gap it keeps
		/// from the edges of the window.</summary>
		private const float Wide = 240f;

		private const float Edge = 6f;
		private const float Least = 90f;

		private readonly List<SifterEntry> showing = new List<SifterEntry>();

		private TwoPaneSplitView split;
		private ListView list;
		private SifterTrace trace;
		private SifterSearchBox search;
		private SifterCategoryList categories;
		private Button tags;
		private SifterSettingsView settings;
		private VisualElement empty;
		private Label counted;
		private SifterLevel logs;
		private SifterLevel warnings;
		private SifterLevel errors;
		private SifterEntry chosen;
		private string looking = string.Empty;
		private bool narrow;

		[MenuItem("Window/CookieJarTools/Sifter")]
		public static void Open()
		{
			var window = GetWindow<SifterWindow>();
			window.titleContent = new GUIContent("Sifter");
			window.minSize = new Vector2(260f, 200f);
			window.Show();
		}

		private void CreateGUI()
		{
			SifterStyle.Apply(rootVisualElement);
			Toolbar();
			Split();
			Nowhere();
			Settings();
			SifterLogs.Current.Changed += Refill;
			rootVisualElement.RegisterCallback<GeometryChangedEvent>(Resized);
			Shaped();
			Refill();
		}

		private void OnDisable()
		{
			SifterLogs.Current.Changed -= Refill;
			SifterCategories.Grew -= Listed;
		}

		/// <summary>
		/// Clear on the left because it is a thing you do, the counts and the settings on the right
		/// because they are things you read and things you visit, and the search between them taking
		/// whatever is spare.
		/// </summary>
		private void Toolbar()
		{
			var toolbar = SifterStyle.Classed(new VisualElement(), "sifter__toolbar");
			toolbar.Add(Ghost("Clear",
				"Throw away everything held, including what survived the last recompile.", Wipe));

			search = new SifterSearchBox("Search logs");
			search.Searched += Looking;
			toolbar.Add(search);

			logs = Level("console.infoicon", "logs", SifterKind.Log);
			warnings = Level("console.warnicon", "warnings", SifterKind.Warning);
			errors = Level("console.erroricon", "errors", SifterKind.Error);
			tags = Ghost("Categories", "Choose which categories are shown.", Tags);
			toolbar.Add(tags);
			toolbar.Add(logs);
			toolbar.Add(warnings);
			toolbar.Add(errors);
			toolbar.Add(Gear());
			rootVisualElement.Add(toolbar);

			categories = new SifterCategoryList();
			categories.Changed += Refill;
			categories.style.display = DisplayStyle.None;
			rootVisualElement.Add(categories);
			rootVisualElement.RegisterCallback<PointerDownEvent>(Elsewhere, TrickleDown.TrickleDown);
			SifterCategories.Grew += Listed;
		}

		/// <summary>
		/// Opens the categories under the button that opened them. Positioned by hand because the
		/// panel belongs to the window rather than to the toolbar, which is what lets it draw over
		/// the list underneath.
		/// </summary>
		private void Tags()
		{
			if (categories.style.display == DisplayStyle.Flex)
			{
				Shut();
				return;
			}

			Listed();
			Placed();
			categories.style.display = DisplayStyle.Flex;

			// UI Toolkit draws siblings in order and has no z-index, so a panel meant to sit over the
			// list has to be the last child at the moment it is shown. It is built before the list so
			// that the toolbar can hold on to it, which puts it underneath until this is called.
			categories.BringToFront();
			tags.AddToClassList("sifter__ghost--on");
		}

		/// <summary>
		/// Puts the panel under the button that opened it and inside the window.
		/// <para>
		/// Positions in here are the window's, not the screen's, and nothing clamps them for you, so
		/// a panel opened from a toolbar in a short docked window simply carried on past the bottom
		/// edge. It is given whatever height is left under the button instead, and its list scrolls.
		/// </para>
		/// </summary>
		private void Placed()
		{
			var box = rootVisualElement.worldBound;
			var under = tags.worldBound.yMax - box.yMin + 2f;
			var across = tags.worldBound.xMin - box.xMin;
			categories.style.left = Mathf.Clamp(across, Edge, Mathf.Max(Edge, box.width - Wide - Edge));
			categories.style.top = under;
			categories.style.maxHeight = Mathf.Max(box.height - under - Edge, Least);
		}

		private void Shut()
		{
			categories.style.display = DisplayStyle.None;
			tags.RemoveFromClassList("sifter__ghost--on");
		}

		/// <summary>A click anywhere that is not in the panel or on the button that opened it shuts it.</summary>
		private void Elsewhere(PointerDownEvent evt)
		{
			if (categories.style.display == DisplayStyle.None)
			{
				return;
			}

			var hit = evt.target as VisualElement;
			if (!Within(hit, categories) && !Within(hit, tags))
			{
				Shut();
			}
		}

		private static bool Within(VisualElement hit, VisualElement box)
		{
			for (var at = hit; at != null; at = at.parent)
			{
				if (at == box)
				{
					return true;
				}
			}

			return false;
		}

		private void Listed()
		{
			categories.Show(Counted());
		}

		/// <summary>How many held logs are in each category, which is what makes the list worth reading.</summary>
		private Dictionary<string, int> Counted()
		{
			var many = new Dictionary<string, int>();
			var log = SifterLogs.Current;
			for (var i = 0; i < log.Count; i++)
			{
				Tally(many, log.At(i));
			}

			return many;
		}

		private static void Tally(Dictionary<string, int> many, SifterEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			int had;
			many[entry.Category] = many.TryGetValue(entry.Category, out had) ? had + 1 : 1;
		}

		private Button Gear()
		{
			var button = SifterStyle.Classed(new Button(Visit) { text = string.Empty }, "sifter__gear");
			SifterTips.On(button, "Settings.");
			var mark = new Image
			{
				image = EditorGUIUtility.IconContent("d_Settings@2x").image,
				scaleMode = ScaleMode.ScaleToFit,
				pickingMode = PickingMode.Ignore
			};
			button.Add(SifterStyle.Classed(mark, "sifter__gear-mark"));
			return button;
		}

		private void Split()
		{
			split = new TwoPaneSplitView(0, 320f, TwoPaneSplitViewOrientation.Vertical);
			list = SifterStyle.Classed(new ListView(), "sifter__list");
			list.fixedItemHeight = SifterShown.RowHeight;
			list.makeItem = () => new SifterRow();
			list.bindItem = Bind;
			list.itemsSource = showing;
			list.selectionType = SelectionType.Single;
			list.selectionChanged += Picked;
			list.RegisterCallback<GeometryChangedEvent>(Squeezed);
			split.Add(list);

			trace = new SifterTrace();
			trace.Changed += Restyle;
			split.Add(trace);
			rootVisualElement.Add(split);
		}

		/// <summary>
		/// What is there when there is nothing. Every pane that can be empty gets one of these,
		/// including the ones nobody expects to see empty.
		/// </summary>
		private void Nowhere()
		{
			empty = SifterStyle.Classed(new VisualElement(), "sifter__empty");
			empty.pickingMode = PickingMode.Ignore;
			var mark = new Image
			{
				image = EditorGUIUtility.IconContent("console.infoicon").image,
				scaleMode = ScaleMode.ScaleToFit
			};
			empty.Add(SifterStyle.Classed(mark, "sifter__empty-mark"));
			empty.Add(SifterStyle.Classed(new Label("Nothing to read"), "sifter__empty-title"));
			counted = SifterStyle.Classed(new Label(), "sifter__empty-text");
			empty.Add(counted);
			rootVisualElement.Add(empty);
		}

		private void Settings()
		{
			settings = new SifterSettingsView();
			settings.Closed += Leave;
			settings.Changed += Restyle;
			settings.style.display = DisplayStyle.None;
			rootVisualElement.Add(settings);
		}

		private void Visit()
		{
			settings.Drawn();
			settings.style.display = DisplayStyle.Flex;
		}

		private void Leave()
		{
			settings.style.display = DisplayStyle.None;
			Shaped();
			Refill();
		}

		/// <summary>
		/// The two things that change the shape of the window rather than what is in it: how tightly
		/// everything is drawn, and which side the selected log opens on.
		/// </summary>
		private void Shaped()
		{
			rootVisualElement.EnableInClassList("sifter-root--compact", SifterShown.Compact);
			var beside = SifterShown.Beside;
			split.orientation = beside
				? TwoPaneSplitViewOrientation.Horizontal
				: TwoPaneSplitViewOrientation.Vertical;
			split.fixedPaneInitialDimension = beside ? 440f : 320f;
		}

		private void Bind(VisualElement element, int index)
		{
			var row = (SifterRow)element;
			row.Show(showing[index]);
			row.Narrow(narrow);
			row.Odd(index % 2 == 1);
		}

		private void Picked(IEnumerable<object> picked)
		{
			chosen = null;
			foreach (var one in picked)
			{
				chosen = (SifterEntry)one;
				break;
			}

			trace.Show(chosen);
		}

		private void Refill()
		{
			var bottom = AtBottom();
			showing.Clear();
			Gather();
			Counts();
			list.fixedItemHeight = SifterShown.RowHeight;
			list.RefreshItems();
			Reselect();
			empty.style.display = showing.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
			counted.text = Nothing();
			Settle(bottom);
		}

		/// <summary>
		/// The list is bound by position, and positions move as the oldest entries are dropped. The
		/// log being read has to be found again, or the pane underneath quietly starts showing a
		/// different one.
		/// </summary>
		private void Reselect()
		{
			var at = chosen == null ? -1 : showing.IndexOf(chosen);
			if (at < 0)
			{
				list.ClearSelection();
				return;
			}

			list.SetSelectionWithoutNotify(new[] { at });
		}

		private void Gather()
		{
			var log = SifterLogs.Current;
			for (var i = 0; i < log.Count; i++)
			{
				Consider(log.At(i));
			}
		}

		private void Consider(SifterEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			SifterCategories.Met(entry.Category);
			if (!SifterLevels.Shown(entry.Kind) || !SifterCategories.Shown(entry.Category) || !Matches(entry))
			{
				return;
			}

			showing.Add(entry);
		}

		private bool Matches(SifterEntry entry)
		{
			return looking.Length == 0
				|| entry.Message.IndexOf(looking, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private string Nothing()
		{
			var held = SifterLogs.Current.Count;
			if (held == 0)
			{
				return "Logs land here as they happen, and stay put through a recompile.";
			}

			return held + " held, and none of them match what is turned on up there.";
		}

		private void Counts()
		{
			var log = SifterLogs.Current;
			logs.Show(log.Logs, SifterLevels.Shown(SifterKind.Log));
			warnings.Show(log.Warnings, SifterLevels.Shown(SifterKind.Warning));
			errors.Show(log.Errors, SifterLevels.Shown(SifterKind.Error));
		}

		private SifterLevel Level(string icon, string name, SifterKind kind)
		{
			return new SifterLevel(icon, "Show or hide " + name + ".", () => Toggled(kind));
		}

		private void Toggled(SifterKind kind)
		{
			SifterLevels.Show(kind, !SifterLevels.Shown(kind));
			Refill();
		}

		private void Looking(string typed)
		{
			looking = typed ?? string.Empty;
			Refill();
		}

		private void Wipe()
		{
			SifterLogs.Current.Clear();
			SifterSession.Forget();
			chosen = null;
			trace.Show(null);
			Refill();
		}

		/// <summary>Everything drawn from a colour or a toggle, drawn again. Nothing is refiltered.</summary>
		private void Restyle()
		{
			Shaped();
			trace.Show(chosen);
			list.fixedItemHeight = SifterShown.RowHeight;
			list.RefreshItems();
		}

		/// <summary>
		/// Whether the list was already at the end, which is the only time something arriving should
		/// drag it there. Scrolling up to read a log and being yanked back down is the worst thing a
		/// console can do.
		/// </summary>
		private bool AtBottom()
		{
			var scroller = list == null ? null : list.Q<ScrollView>();
			if (scroller == null)
			{
				return true;
			}

			var vertical = scroller.verticalScroller;
			return vertical.highValue <= 0f || vertical.value >= vertical.highValue - 1f;
		}

		private void Settle(bool bottom)
		{
			if (bottom && showing.Count > 0)
			{
				list.ScrollToItem(showing.Count - 1);
			}
		}

		/// <summary>
		/// Docked in a column is how most people will see this, so the toolbar has to stay usable
		/// there. The search box and the categories fold away first, because everything else up
		/// there is either a count or the way into the settings.
		/// </summary>
		private void Resized(GeometryChangedEvent evt)
		{
			var roomy = evt.newRect.width >= Cramped;
			search.style.display = roomy ? DisplayStyle.Flex : DisplayStyle.None;
			tags.style.display = roomy ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// What a row gives up is decided by the list's own width and not the window's. With the log
		/// opening beside it the list is a fraction of the window, and measuring the window there
		/// kept the time and the place on rows with no room left for either.
		/// </summary>
		private void Squeezed(GeometryChangedEvent evt)
		{
			var tight = evt.newRect.width < Tight;
			if (tight == narrow)
			{
				return;
			}

			narrow = tight;
			list.RefreshItems();
		}

		private static Button Ghost(string text, string tooltip, Action clicked)
		{
			var button = SifterStyle.Classed(new Button(clicked) { text = text }, "sifter__ghost");
			SifterTips.On(button, tooltip);
			return button;
		}
	}
}
