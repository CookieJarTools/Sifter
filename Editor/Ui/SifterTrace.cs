using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// The selected log in full: what it said, and its stack with every part coloured and every
	/// frame clickable. Clicking any frame opens that file at that line, not only the one that
	/// called Debug.Log, because the line you want is almost never the line that logged.
	/// <para>
	/// The stack sits in a box of its own that shuts down to the frame it happened in, because a
	/// stack is worth having and is not worth reading twenty frames of every time.
	/// </para>
	/// </summary>
	public sealed class SifterTrace : VisualElement
	{
		private readonly ScrollView scroll;
		private readonly VisualElement head;
		private readonly Label belongs;
		private readonly Label kind;
		private readonly Label when;
		private readonly Label message;

		private SifterEntry showing;
		private readonly VisualElement stack;
		private readonly VisualElement caret;
		private readonly Label counted;
		private readonly VisualElement frames;
		private readonly Label nothing;

		public SifterTrace()
		{
			AddToClassList("sifter__trace");

			// A real ScrollView rather than overflow on a plain element, which in UI Toolkit clips
			// the content and hands you no way to reach the rest of it. A stack is as tall as it
			// likes and its lines do not wrap, so this scrolls both ways.
			scroll = SifterStyle.Classed(new ScrollView(ScrollViewMode.VerticalAndHorizontal),
				"sifter__trace-scroll");
			Add(scroll);

			head = SifterStyle.Classed(new VisualElement(), "sifter__trace-head");
			belongs = SifterStyle.Classed(new Label(), "sifter__trace-tag");
			kind = SifterStyle.Classed(new Label(), "sifter__trace-tag");
			when = SifterStyle.Classed(new Label(), "sifter__trace-when");
			head.Add(belongs);
			head.Add(kind);
			head.Add(when);
			head.Add(SifterStyle.Classed(new VisualElement(), "sifter__spacer"));
			head.Add(Copies("Copy", "Copy the message on its own.", () => Copy(false)));
			head.Add(Copies("Copy With Stack", "Copy the message and every frame under it.", () => Copy(true)));
			scroll.Add(head);

			message = SifterStyle.Classed(new Label(), "sifter__message");
			message.selection.isSelectable = true;
			scroll.Add(message);

			stack = SifterStyle.Classed(new VisualElement(), "sifter__stack");
			var bar = SifterStyle.Classed(new Button(Toggled) { text = string.Empty }, "sifter__stack-head");
			// Drawn rather than an icon. Unity's foldout arrow has no dark skin version, so asking
			// for it logs "Unable to load the icon" into the very console this is part of.
			caret = new VisualElement { pickingMode = PickingMode.Ignore };
			bar.Add(SifterStyle.Classed(caret, "sifter__stack-caret"));
			bar.Add(SifterStyle.Classed(new Label("Stack"), "sifter__stack-title"));
			counted = SifterStyle.Classed(new Label(), "sifter__stack-count");
			bar.Add(counted);
			stack.Add(bar);

			frames = SifterStyle.Classed(new VisualElement(), "sifter__frames");
			stack.Add(frames);
			scroll.Add(stack);

			nothing = SifterStyle.Classed(new Label("Select a log to see where it came from."),
				"sifter__trace-empty");
			Add(nothing);
			Show(null);
		}

		/// <summary>Raised when something in here changed a setting the whole window draws from.</summary>
		public event Action Changed = delegate { };

		public void Show(SifterEntry entry)
		{
			frames.Clear();
			showing = entry;
			nothing.style.display = entry == null ? DisplayStyle.Flex : DisplayStyle.None;
			message.style.display = entry == null ? DisplayStyle.None : DisplayStyle.Flex;
			stack.style.display = entry == null ? DisplayStyle.None : DisplayStyle.Flex;
			head.style.display = entry == null ? DisplayStyle.None : DisplayStyle.Flex;
			if (entry == null)
			{
				return;
			}

			Heading(entry);
			Wrapping();
			message.text = entry.Message;
			Fill(entry);
		}

		/// <summary>
		/// What this log is, above the message rather than in it: the category it belongs to, what
		/// kind it is, and when it happened. The row in the list says all three too, but once you
		/// are reading one log the list is scrolled away behind the pane.
		/// </summary>
		private void Heading(SifterEntry entry)
		{
			var colour = SifterPalette.Category(entry.Category);
			belongs.text = entry.Category;
			belongs.style.color = colour;
			belongs.style.backgroundColor = SifterPalette.Behind(colour);

			var tone = Tone(entry.Kind);
			kind.text = SifterKinds.Say(entry.Kind);
			kind.style.color = tone;
			kind.style.backgroundColor = SifterPalette.Behind(tone);

			when.text = entry.When.ToString("HH:mm:ss") + Frame(entry) + Thread(entry);
		}

		/// <summary>
		/// Wrapping and scrolling sideways are the same choice asked twice: a frame that wraps never
		/// needs the horizontal scroller, and leaving the scroller on would leave a dead bar along
		/// the bottom of the pane.
		/// </summary>
		private void Wrapping()
		{
			var wrap = SifterShown.WrapFrames;
			scroll.mode = wrap ? ScrollViewMode.Vertical : ScrollViewMode.VerticalAndHorizontal;
			frames.EnableInClassList("sifter__frames--wrap", wrap);
		}

		private static Color Tone(SifterKind kind)
		{
			if (SifterKinds.IsError(kind))
			{
				return SifterPalette.Error;
			}

			return SifterKinds.IsWarning(kind) ? SifterPalette.Warning : new Color(0.72f, 0.75f, 0.79f);
		}

		private static string Frame(SifterEntry entry)
		{
			return entry.Frame < 0 ? string.Empty : "   frame " + entry.Frame;
		}

		private static string Thread(SifterEntry entry)
		{
			return entry.OffMainThread ? "   off the main thread" : string.Empty;
		}

		/// <summary>
		/// The message on its own, or the message with its stack. Two buttons rather than one,
		/// because pasting a wall of frames into a chat when all you wanted was the line is the
		/// commonest small annoyance a console has.
		/// </summary>
		private void Copy(bool withStack)
		{
			if (showing == null)
			{
				return;
			}

			EditorGUIUtility.systemCopyBuffer = withStack && showing.Trace.Length > 0
				? showing.Message + "\n" + showing.Trace
				: showing.Message;
		}

		private static Button Copies(string text, string says, Action clicked)
		{
			var button = SifterStyle.Classed(new Button(clicked) { text = text }, "sifter__trace-copy");
			SifterTips.On(button, says);
			return button;
		}

		private void Fill(SifterEntry entry)
		{
			var worth = SifterPainter.Worth(entry.Frames);
			var showing = SifterShown.Collapsed ? SifterPainter.Top(worth) : worth;
			var ours = showing.Exists(frame => frame.Mine);
			foreach (var frame in showing)
			{
				frames.Add(Line(frame, ours));
			}

			Head(entry.Frames.Count, showing.Count);
			Folded(worth.Count == 0, entry.Frames.Count);
			if (entry.Frames.Count == 0 && entry.File.Length > 0)
			{
				frames.Add(Only(entry));
			}
		}

		private void Head(int all, int showing)
		{
			caret.EnableInClassList("sifter__stack-caret--open", !SifterShown.Collapsed);
			counted.text = Says(all, showing);
			stack.EnableInClassList("sifter__stack--shut", SifterShown.Collapsed);
		}

		private static string Says(int all, int showing)
		{
			if (all == 0)
			{
				return "no frames";
			}

			var frames = all + (all == 1 ? " frame" : " frames");
			return showing < all ? showing + " of " + frames : frames;
		}

		private void Toggled()
		{
			SifterShown.Collapsed = !SifterShown.Collapsed;
			Changed();
		}

		/// <summary>
		/// The way back from having folded too much.
		/// <para>
		/// When folding has taken every frame there is, saying how many went is not enough: the box
		/// is empty and nothing in it looks like the cause. It says what happened instead, because a
		/// filter that removed everything has told you nothing and needs to offer the way out.
		/// </para>
		/// </summary>
		private void Folded(bool everything, int all)
		{
			if (!everything || all < 1)
			{
				return;
			}

			var button = SifterStyle.Classed(
				new Button(Unfold) { text = "None of these " + all + " frames are yours. Show them all" },
				"sifter__folded");
			SifterTips.On(button, "Show every frame, including Unity's and your packages'.");
			frames.Add(button);
		}

		private void Unfold()
		{
			SifterShown.MineOnly = false;
			SifterShown.Honour = false;
			Changed();
		}

		/// <summary>
		/// A compiler message has no stack at all. The file and line it names are the whole of where
		/// it happened, and they still have to be clickable.
		/// </summary>
		private static VisualElement Only(SifterEntry entry)
		{
			var row = SifterStyle.Classed(new Label(entry.File + ":" + entry.Line), "sifter__frame");
			row.AddToClassList("sifter__frame--mine");
			row.RegisterCallback<ClickEvent>(_ => SifterOpen.At(entry.File, entry.Line));
			return row;
		}

		/// <summary>
		/// Holding back everything that is not the project's own only says anything when some of it
		/// is. A stack entirely inside Unity and its packages, which is what most of an editor stack
		/// is, would otherwise be drawn faint from top to bottom and simply be harder to read.
		/// </summary>
		private static VisualElement Line(SifterFrame frame, bool ours)
		{
			var row = SifterStyle.Classed(new Label(SifterPainter.Paint(frame)), "sifter__frame");
			row.enableRichText = true;
			SifterTips.On(row, frame.Raw);
			row.EnableInClassList("sifter__frame--mine", frame.Mine || !ours);
			row.EnableInClassList("sifter__frame--dead", !frame.Somewhere);
			row.RegisterCallback<ClickEvent>(_ => SifterOpen.At(frame));
			return row;
		}
	}
}
