using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// Every colour the window draws with, and all of them changeable.
	/// <para>
	/// The parts of a frame are the point of the whole tool: a stack is the most structured thing in
	/// the editor and Unity draws every part of it the same grey. Beside them sit the two kind
	/// colours, and a colour per category.
	/// </para>
	/// <para>
	/// A category that has never been given a colour gets one from its own name rather than from the
	/// order it turned up in, so Combat is the same colour in your project today as it is tomorrow,
	/// and on somebody else's machine.
	/// </para>
	/// <para>
	/// Version goes up whenever anything changes, so anything holding a painted string knows to
	/// throw it away.
	/// </para>
	/// </summary>
	public static class SifterPalette
	{
		private const string Key = "CookieJarTools.Sifter.Colour.";
		private const string CatKey = "CookieJarTools.Sifter.Cat.";

		private static readonly Color[] Usually =
		{
			new Color32(0x6e, 0x76, 0x81, 0xff),
			new Color32(0x79, 0xc0, 0xff, 0xff),
			new Color32(0xd2, 0xa8, 0xff, 0xff),
			new Color32(0x7e, 0xe7, 0x87, 0xff),
			new Color32(0x8b, 0x94, 0x9e, 0xff),
			new Color32(0xf0, 0x88, 0x3e, 0xff)
		};

		private static readonly Color RedByDefault = new Color32(0xf2, 0x8f, 0x93, 0xff);
		private static readonly Color AmberByDefault = new Color32(0xe3, 0xbb, 0x6c, 0xff);

		private static readonly Color[] Now = new Color[Usually.Length];
		private static bool read;

		/// <summary>Punctuation between the parts, which should be there and not be read.</summary>
		public static Color Faint
		{
			get { return new Color32(0x4d, 0x55, 0x60, 0xff); }
		}

		public static int Version { get; private set; }

		/// <summary>An error, a fatal, an assert, an exception and a compile error.</summary>
		public static Color Error
		{
			get { return Saved("Error", RedByDefault); }
			set { Keep("Error", value); }
		}

		/// <summary>A warning and a compile warning.</summary>
		public static Color Warning
		{
			get { return Saved("Warning", AmberByDefault); }
			set { Keep("Warning", value); }
		}

		public static Color Of(SifterPart part)
		{
			Read();
			return Now[(int)part];
		}

		public static void Set(SifterPart part, Color colour)
		{
			Read();
			Now[(int)part] = colour;
			EditorPrefs.SetString(Key + part, ColorUtility.ToHtmlStringRGB(colour));
			Version++;
		}

		/// <summary>
		/// A category's colour: whatever it was given, or one worked out from its own name.
		/// </summary>
		public static Color Category(string name)
		{
			Color found;
			var hex = EditorPrefs.GetString(CatKey + name, string.Empty);
			return ColorUtility.TryParseHtmlString("#" + hex, out found) ? found : Hashed(name);
		}

		public static void SetCategory(string name, Color colour)
		{
			EditorPrefs.SetString(CatKey + name, ColorUtility.ToHtmlStringRGB(colour));
			Version++;
		}

		public static void ForgetCategory(string name)
		{
			EditorPrefs.DeleteKey(CatKey + name);
			Version++;
		}

		public static void Reset()
		{
			for (var i = 0; i < Usually.Length; i++)
			{
				Set((SifterPart)i, Usually[i]);
			}

			Error = RedByDefault;
			Warning = AmberByDefault;
		}

		/// <summary>The hex a rich text tag wants, with no hash in front of it.</summary>
		public static string Hex(SifterPart part)
		{
			return ColorUtility.ToHtmlStringRGB(Of(part));
		}

		public static string Hex(Color colour)
		{
			return ColorUtility.ToHtmlStringRGB(colour);
		}

		/// <summary>The same colour behind something, for a pill that has to stay readable.</summary>
		public static Color Behind(Color colour)
		{
			return new Color(colour.r, colour.g, colour.b, 0.15f);
		}

		/// <summary>
		/// A colour from a name, picked by hue so that two categories are rarely confusable, and kept
		/// away from the ends of the saturation and value ranges so every one of them reads on a near
		/// black background. The saturation and the value are nudged as well, so that two names which
		/// do land near each other on the wheel still do not look like the same colour.
		/// </summary>
		private static Color Hashed(string name)
		{
			var hash = Stable(name);
			var hue = hash % 360u / 360f;
			var saturation = 0.42f + (hash >> 9) % 5u * 0.04f;
			var value = 0.88f + (hash >> 17) % 4u * 0.03f;
			return Color.HSVToRGB(hue, saturation, value);
		}

		/// <summary>
		/// Written out rather than using GetHashCode, which is allowed to differ between runs and would
		/// hand the same category a different colour every time the editor started.
		/// <para>
		/// The mixing on the end is not decoration. FNV leaves its low bits poorly spread, and taking
		/// those modulo 360 put five of the first eleven categories in this project within a few
		/// degrees of each other, which is the one thing a colour per category must not do.
		/// </para>
		/// </summary>
		private static uint Stable(string name)
		{
			var hash = 2166136261u;
			foreach (var letter in name)
			{
				hash = (hash ^ letter) * 16777619u;
			}

			return Mixed(hash);
		}

		private static uint Mixed(uint hash)
		{
			hash ^= hash >> 16;
			hash *= 2246822519u;
			hash ^= hash >> 13;
			hash *= 3266489917u;
			return hash ^ (hash >> 16);
		}

		private static void Keep(string name, Color colour)
		{
			EditorPrefs.SetString(Key + name, ColorUtility.ToHtmlStringRGB(colour));
			Version++;
		}

		private static void Read()
		{
			if (read)
			{
				return;
			}

			read = true;
			for (var i = 0; i < Usually.Length; i++)
			{
				Now[i] = Saved(((SifterPart)i).ToString(), Usually[i]);
			}
		}

		private static Color Saved(string name, Color fallback)
		{
			Color found;
			var hex = EditorPrefs.GetString(Key + name, string.Empty);
			return ColorUtility.TryParseHtmlString("#" + hex, out found) ? found : fallback;
		}
	}
}
