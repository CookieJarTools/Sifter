using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One of the three toolbar toggles, as Unity's own icon and a number.
	/// <para>
	/// A Button draws its own text, and anything added to one is laid over that text, so the text is
	/// left empty and the icon and the count are children.
	/// </para>
	/// <para>
	/// The icon asked for is the large one rather than the .sml one beside it, drawn small. Unity
	/// ships the console icons at both sizes and the small one has no high density version, so on
	/// any screen above one to one it is the blurriest thing in the window.
	/// </para>
	/// </summary>
	public sealed class SifterLevel : Button
	{
		private readonly Label many;

		public SifterLevel(string icon, string says, Action clicked) : base(clicked)
		{
			AddToClassList("sifter__level");
			text = string.Empty;
			SifterTips.On(this, says);

			var mark = new Image
			{
				image = EditorGUIUtility.IconContent(icon).image,
				scaleMode = ScaleMode.ScaleToFit,
				pickingMode = PickingMode.Ignore
			};
			Add(SifterStyle.Classed(mark, "sifter__level-mark"));

			many = SifterStyle.Classed(new Label(), "sifter__level-count");
			many.pickingMode = PickingMode.Ignore;
			Add(many);
		}

		public void Show(int count, bool on)
		{
			many.text = count > 999 ? "999+" : count.ToString();
			EnableInClassList("sifter__level--on", on);
		}
	}
}
