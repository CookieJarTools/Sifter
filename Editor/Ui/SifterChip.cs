using System;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// A pill that is either on or off and says what it is rather than saying On.
	/// <para>
	/// Used where the label is the thing being turned on, such as the parts of a frame: a row of
	/// these reads as the line they build, which a column of checkboxes does not.
	/// </para>
	/// </summary>
	public sealed class SifterChip : Button
	{
		public SifterChip(string name, Action clicked) : base(clicked)
		{
			AddToClassList("sifter__chip");
			text = name;
		}

		public void On(bool on)
		{
			EnableInClassList("sifter__chip--on", on);
		}
	}
}
