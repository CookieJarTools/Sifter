using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// One of several, as a segmented control. Every setting with a handful of answers is one of
	/// these, including the ones that are only yes and no, so that nothing in the screen needs to
	/// be read twice to work out which way round it is.
	/// </summary>
	public sealed class SifterChoice : VisualElement
	{
		private readonly List<Button> segments = new List<Button>();

		public SifterChoice(string[] names, Action<int> picked)
		{
			AddToClassList("sifter__choice");
			for (var i = 0; i < names.Length; i++)
			{
				Segment(names[i], i, picked);
			}
		}

		public void Pick(int at)
		{
			for (var i = 0; i < segments.Count; i++)
			{
				segments[i].EnableInClassList("sifter__segment--on", i == at);
			}
		}

		private void Segment(string name, int at, Action<int> picked)
		{
			var button = SifterStyle.Classed(new Button(() => picked(at)) { text = name },
				"sifter__segment");
			segments.Add(button);
			Add(button);
		}
	}
}
