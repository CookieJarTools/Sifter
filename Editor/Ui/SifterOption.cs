using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One setting: what it is called, one line saying what it does, and the control that changes
	/// it. The sentence goes here rather than in a tooltip, because a screen you visit once in a
	/// while is exactly where somebody needs to be told what something means.
	/// </summary>
	public sealed class SifterOption : VisualElement
	{
		public SifterOption(string title, string note)
		{
			AddToClassList("sifter__option");
			var words = SifterStyle.Classed(new VisualElement(), "sifter__option-words");
			words.Add(SifterStyle.Classed(new Label(title), "sifter__option-title"));
			if (note.Length > 0)
			{
				words.Add(SifterStyle.Classed(new Label(note), "sifter__option-note"));
			}

			Add(words);
			Control = SifterStyle.Classed(new VisualElement(), "sifter__option-control");
			Add(Control);
		}

		/// <summary>Where the control that changes this setting goes.</summary>
		public VisualElement Control { get; }
	}
}
