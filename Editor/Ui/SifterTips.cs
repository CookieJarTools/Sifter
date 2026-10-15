using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// A tooltip in this tool's own clothes.
	/// <para>
	/// Unity's own tooltip is drawn by the editor rather than by the panel, so nothing in a
	/// stylesheet reaches it: square corners, its own grey, its own font. In a window that is
	/// otherwise near black with soft corners it is the one thing that looks borrowed.
	/// </para>
	/// <para>
	/// One of these is made per window, the first time anything in it wants a tip, and it is kept
	/// on the root so it can be drawn over the list and the panels.
	/// </para>
	/// </summary>
	public static class SifterTips
	{
		/// <summary>Long enough that moving across the window does not set off a trail of them.</summary>
		private const long Wait = 420;

		private const float Away = 14f;
		private const float Margin = 6f;

		private static IVisualElementScheduledItem waiting;
		private static Label showing;

		/// <summary>Gives an element a tip, worked out at the moment it is wanted.</summary>
		public static void On(VisualElement element, Func<string> text)
		{
			element.RegisterCallback<PointerEnterEvent>(evt => Entered(element, text, evt.position));
			element.RegisterCallback<PointerLeaveEvent>(_ => Hide());
			element.RegisterCallback<PointerDownEvent>(_ => Hide());
			element.RegisterCallback<DetachFromPanelEvent>(_ => Hide());
		}

		public static void On(VisualElement element, string text)
		{
			On(element, () => text);
		}

		public static void Hide()
		{
			Stop();
			if (showing != null)
			{
				showing.style.display = DisplayStyle.None;
			}
		}

		private static void Entered(VisualElement element, Func<string> text, Vector3 at)
		{
			Stop();
			waiting = element.schedule.Execute(() => Shown(element, text, at)).StartingIn(Wait);
		}

		private static void Stop()
		{
			if (waiting != null)
			{
				waiting.Pause();
				waiting = null;
			}
		}

		private static void Shown(VisualElement element, Func<string> text, Vector3 at)
		{
			var says = text();
			var root = Root(element);
			if (root == null || string.IsNullOrEmpty(says))
			{
				return;
			}

			showing = Label(root);
			showing.text = says;
			showing.style.display = DisplayStyle.Flex;
			showing.BringToFront();
			Place(showing, root, at);
		}

		/// <summary>
		/// Kept inside the window, and above the pointer rather than below it when there is no room,
		/// because a tip that runs off the bottom edge is worse than no tip.
		/// </summary>
		private static void Place(Label tip, VisualElement root, Vector3 at)
		{
			var box = root.worldBound;
			var size = tip.MeasureTextSize(tip.text, Widest(box), VisualElement.MeasureMode.AtMost,
				0f, VisualElement.MeasureMode.Undefined);
			var wide = Mathf.Min(size.x + 18f, Widest(box));
			var tall = size.y + 12f;
			tip.style.width = wide;
			tip.style.left = Mathf.Clamp(at.x - box.xMin + Away, Margin, box.width - wide - Margin);
			tip.style.top = Below(at.y - box.yMin, tall, box.height);
		}

		private static float Widest(Rect box)
		{
			return Mathf.Min(box.width - Margin * 2f, 420f);
		}

		private static float Below(float at, float tall, float height)
		{
			var under = at + Away + 4f;
			return under + tall < height ? under : Mathf.Max(Margin, at - tall - 6f);
		}

		private static Label Label(VisualElement root)
		{
			var found = root.Q<Label>(className: "sifter__tip");
			if (found != null)
			{
				return found;
			}

			var made = SifterStyle.Classed(new Label(), "sifter__tip");
			made.pickingMode = PickingMode.Ignore;
			root.Add(made);
			return made;
		}

		/// <summary>
		/// The nearest thing wearing the tool's root class, because the stylesheet is put on that
		/// and nothing above it would be styled at all.
		/// </summary>
		private static VisualElement Root(VisualElement element)
		{
			for (var at = element; at != null; at = at.parent)
			{
				if (at.ClassListContains("sifter-root"))
				{
					return at;
				}
			}

			return null;
		}
	}
}
