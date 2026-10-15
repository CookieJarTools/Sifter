using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>
	/// Categories are worked out from the log and from nothing else, so these are the rules that
	/// decide what a project sees in its own dropdown on the day it installs this.
	/// </summary>
	public sealed class SifterCategoryTests
	{
		private int wasDepth;

		[SetUp]
		public void Setup()
		{
			wasDepth = SifterShown.CategoryDepth;
			SifterShown.CategoryDepth = 2;
		}

		[TearDown]
		public void Teardown()
		{
			SifterShown.CategoryDepth = wasDepth;
		}

		[Test]
		public void AWordInBracketsAtTheFrontWins()
		{
			Assert.AreEqual("Combat", Named("[Combat] two hits on one frame", Trace()));
			Assert.AreEqual("Combat.AI", Named("[Combat.AI] lost the path", Trace()));
		}

		/// <summary>
		/// Plenty of logs start with a number in brackets. Without this every one of them would be
		/// its own category and the list would be useless within a minute.
		/// </summary>
		[Test]
		public void SomethingInBracketsThatIsNotAWordIsNotACategory()
		{
			Assert.AreNotEqual("00:12", Named("[00:12] ticked", Trace()));
			Assert.AreNotEqual("1", Named("[1] first", Trace()));
		}

		[Test]
		public void ACompilerMessageIsItsOwnCategory()
		{
			var entry = new SifterEntry(SifterKind.CompileError, "Assets/X.cs(1,1): error CS0029: no",
				string.Empty, 1, "Assets/X.cs", 1);

			Assert.AreEqual("Compiler", entry.Category);
		}

		[Test]
		public void TheProjectsOwnCodeIsNamedAfterItsNamespace()
		{
			Assert.AreEqual("CookieJarTools.Sifter",
				Named("something happened", "CookieJarTools.Sifter.Editor.SifterLog:Add () (at Assets/Sifter/SifterLog.cs:9)"));
		}

		[Test]
		public void HowDeepACategoryGoesIsASetting()
		{
			var trace = "CookieJarTools.Sifter.Editor.SifterLog:Add () (at Assets/Sifter/SifterLog.cs:9)";

			SifterShown.CategoryDepth = 1;
			Assert.AreEqual("CookieJarTools", Named("a log", trace));

			SifterShown.CategoryDepth = 3;
			Assert.AreEqual("CookieJarTools.Sifter.Editor", Named("a log", trace));
		}

		[Test]
		public void ANamespaceShorterThanTheDepthIsTakenWhole()
		{
			Assert.AreEqual("MyGame", Named("a log", "MyGame.Runner:Go () (at Assets/Runner.cs:4)"));
		}

		[Test]
		public void AnythingWithNoneOfTheProjectsOwnCodeInItIsUnity()
		{
			Assert.AreEqual("Unity", Named("a log", "UnityEngine.Debug:Log (object)"));
			Assert.AreEqual("Unity", Named("a log", string.Empty));
		}

		[Test]
		public void TheProjectsOwnCodeInNoNamespaceAtAllHasSomewhereToGo()
		{
			Assert.AreEqual("Project", Named("a log", "Runner:Go () (at Assets/Runner.cs:4)"));
		}

		[Test]
		public void ACategoryKnowsEverythingItSitsUnder()
		{
			var branches = SifterCategories.Branches("A.B.C");

			Assert.AreEqual(3, branches.Count);
			Assert.AreEqual("A", branches[0]);
			Assert.AreEqual("A.B", branches[1]);
			Assert.AreEqual("A.B.C", branches[2]);
		}

		[Test]
		public void ACategoryWithNoParentsIsJustItself()
		{
			var branches = SifterCategories.Branches("Combat");

			Assert.AreEqual(1, branches.Count);
			Assert.AreEqual("Combat", branches[0]);
		}

		[Test]
		public void TheLeafIsTheOnlyPartThatIsNotItsParents()
		{
			Assert.AreEqual("Sifter", SifterCategories.Leaf("CookieJarTools.Sifter"));
			Assert.AreEqual("Combat", SifterCategories.Leaf("Combat"));
			Assert.AreEqual(2, SifterCategories.Depth("A.B.C"));
			Assert.AreEqual(0, SifterCategories.Depth("A"));
		}

		/// <summary>
		/// Turning off a parent has to turn off what is under it, or a tree where switching off
		/// CookieJarTools left its six tools still talking would not be a tree at all.
		/// </summary>
		[Test]
		public void TurningOffAParentTurnsOffWhatIsUnderIt()
		{
			SifterCategories.Show("SifterTestParent", true);
			SifterCategories.Show("SifterTestParent.Child", true);
			Assert.IsTrue(SifterCategories.Shown("SifterTestParent.Child"));

			SifterCategories.Show("SifterTestParent", false);
			Assert.IsFalse(SifterCategories.Shown("SifterTestParent.Child"));
			Assert.IsTrue(SifterCategories.Alone("SifterTestParent.Child"),
				"the child itself was never switched off, only what it sits under");

			SifterCategories.Show("SifterTestParent", true);
		}

		private static string Named(string message, string trace)
		{
			return new SifterEntry(SifterKind.Log, message, trace, 1).Category;
		}

		private static string Trace()
		{
			return "UnityEngine.Debug:Log (object)";
		}
	}
}
