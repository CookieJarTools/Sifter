using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CookieJarTools.Sifter.Editor
{
	/// <summary>Sifter's own sheet, found by name so the tool can live anywhere in the project.</summary>
	public static class SifterStyle
	{
		private const string SheetName = "Sifter";
		private const string RootClass = "sifter-root";

		public static void Apply(VisualElement root)
		{
			root.AddToClassList(RootClass);
			foreach (var guid in AssetDatabase.FindAssets("t:StyleSheet " + SheetName))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith("/" + SheetName + ".uss"))
				{
					root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(path));
					return;
				}
			}
		}

		public static T Classed<T>(T element, string className) where T : VisualElement
		{
			element.AddToClassList(className);
			return element;
		}

		/// <summary>
		/// A label that is allowed to be cut short without Unity popping its own tooltip up.
		/// <para>
		/// TextElement turns displayTooltipWhenElided on by itself, so any label with an ellipsis
		/// quietly grows a native tooltip that nothing in this tool asked for. It is drawn by the
		/// editor rather than the panel, so it ignores the stylesheet, and it appears next to the
		/// one Sifter draws rather than instead of it.
		/// </para>
		/// </summary>
		public static Label Clipped(Label label)
		{
			label.displayTooltipWhenElided = false;
			return label;
		}

		/// <summary>
		/// A colour field that does not announce its own RGBA in Unity's tooltip. Unity puts that on
		/// the swatch and writes it again every time the value changes, so it is cleared on the way
		/// in and again on the way out of every edit.
		/// </summary>
		public static ColorField Quiet(ColorField field)
		{
			Hush(field);
			field.RegisterValueChangedCallback(_ => Hush(field));
			field.RegisterCallback<GeometryChangedEvent>(_ => Hush(field));
			return field;
		}

		private static void Hush(ColorField field)
		{
			var swatch = field.Q(className: "unity-color-field__color-container");
			if (swatch != null)
			{
				swatch.tooltip = string.Empty;
			}
		}
	}
}
