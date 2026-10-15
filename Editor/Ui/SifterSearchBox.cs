using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// A search box of our own: a mark, what is typed, and a way to clear it. Unity's own toolbar
	/// search field brings a skin with it that cannot be talked out of looking like a toolbar.
	/// <para>
	/// Unity's USS has no <c>:focus-within</c>, so the box is told when the field takes and loses
	/// focus and lights its own border.
	/// </para>
	/// </summary>
	public sealed class SifterSearchBox : VisualElement
	{
		private const string Prefix = "sifter__search";

		private readonly TextField field;
		private readonly Label hint;
		private readonly Button clear;

		public SifterSearchBox(string placeholder)
		{
			AddToClassList(Prefix);
			var mark = new Image
			{
				image = EditorGUIUtility.IconContent("Search Icon").image,
				scaleMode = ScaleMode.ScaleToFit,
				pickingMode = PickingMode.Ignore,
				tintColor = new Color(0.46f, 0.49f, 0.53f)
			};
			Add(SifterStyle.Classed(mark, Prefix + "-mark"));

			field = SifterStyle.Classed(new TextField { isDelayed = false }, Prefix + "-input");
			field.RegisterValueChangedCallback(evt => Changed(evt.newValue));
			field.RegisterCallback<FocusInEvent>(_ => EnableInClassList(Prefix + "--on", true));
			field.RegisterCallback<FocusOutEvent>(_ => EnableInClassList(Prefix + "--on", false));
			field.RegisterCallback<KeyDownEvent>(Escaped, TrickleDown.TrickleDown);
			Add(field);

			hint = SifterStyle.Classed(new Label(placeholder) { pickingMode = PickingMode.Ignore }, Prefix + "-hint");
			Add(hint);

			clear = SifterStyle.Classed(new Button(Empty) { text = "×" }, Prefix + "-clear");
			SifterTips.On(clear, "Clear the search.");
			clear.style.display = DisplayStyle.None;
			Add(clear);
		}

		public event Action<string> Searched;

		public string Value
		{
			get { return field.value; }
		}

		/// <summary>
		/// Empties the box. Not called Clear: VisualElement has a Clear of its own that throws away
		/// every child, and a name that quietly hides it is a trap left for somebody later.
		/// </summary>
		public void Empty()
		{
			field.value = string.Empty;
			field.Focus();
		}

		private void Changed(string value)
		{
			var typed = !string.IsNullOrEmpty(value);
			hint.style.display = typed ? DisplayStyle.None : DisplayStyle.Flex;
			clear.style.display = typed ? DisplayStyle.Flex : DisplayStyle.None;
			if (Searched != null)
			{
				Searched(value);
			}
		}

		private void Escaped(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Escape && !string.IsNullOrEmpty(field.value))
			{
				Empty();
				evt.StopPropagation();
			}
		}
	}
}
