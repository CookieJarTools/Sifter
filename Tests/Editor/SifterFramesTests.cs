using CookieJarTools.Sifter.Editor;
using NUnit.Framework;

namespace CookieJarTools.Sifter.Tests
{
	/// <summary>
	/// Every line in here was copied out of a running editor rather than imagined, because the
	/// three shapes Unity writes a stack in are not documented anywhere and guessing at them is
	/// how the first version of this read a file path as a parameter list.
	/// </summary>
	public sealed class SifterFramesTests
	{
		[Test]
		public void ALogsOwnFormIsRead()
		{
			var frame = One("UnityEngine.Debug:LogWarning (object)");

			Assert.IsTrue(frame.Read);
			Assert.AreEqual("UnityEngine", frame.Namespace);
			Assert.AreEqual("Debug", frame.Class);
			Assert.AreEqual("LogWarning", frame.Method);
			Assert.AreEqual("object", frame.Parameters);
		}

		[Test]
		public void AFileAndLineComeOffTheEnd()
		{
			var frame = One("CookieJarTools.Sifter.Editor.SifterSession:Restore "
				+ "(CookieJarTools.Sifter.Editor.SifterLog,string) "
				+ "(at Assets/CookieJarTools/Sifter/Editor/Store/SifterSession.cs:81)");

			Assert.AreEqual("Assets/CookieJarTools/Sifter/Editor/Store/SifterSession.cs", frame.File);
			Assert.AreEqual(81, frame.Line);
			Assert.AreEqual("CookieJarTools.Sifter.Editor.SifterLog,string", frame.Parameters);
			Assert.IsTrue(frame.Somewhere);
		}

		/// <summary>
		/// The form LogException writes has no space in front of the bracket, which is the one
		/// difference that made an earlier version read the file path as the parameters.
		/// </summary>
		[Test]
		public void TheFormWithNoSpaceBeforeTheBracketIsRead()
		{
			var frame = One("Unity.Pipeline.Compilation.EvalCodeCompiler:ExecuteCompiledAssembly"
				+ "(Assembly) (at ./Library/PackageCache/x/EvalCodeCompiler.cs:190)");

			Assert.AreEqual("EvalCodeCompiler", frame.Class);
			Assert.AreEqual("ExecuteCompiledAssembly", frame.Method);
			Assert.AreEqual("Assembly", frame.Parameters);
			Assert.AreEqual(190, frame.Line);
		}

		[Test]
		public void TheFormWithADotInsteadOfAColonIsRead()
		{
			var frame = One("PipelineEvaluation.PipelineEval_3f1.Execute () (at <c559d4ac4f1041d4>:0)");

			Assert.AreEqual("PipelineEvaluation", frame.Namespace);
			Assert.AreEqual("PipelineEval_3f1", frame.Class);
			Assert.AreEqual("Execute", frame.Method);
		}

		[Test]
		public void AnExceptionsOwnFormIsRead()
		{
			var frame = One("  at CookieJarTools.Recipe.Tests.RecipeWorld+<>c__DisplayClass18_0."
				+ "<Breaks>b__0 (CookieJarTools.Recipe.RecipeArgs _) [0x00000] in "
				+ "F:\\Dev\\CookieJar\\Assets\\CookieJarTools\\Recipe\\Tests\\Editor\\RecipeWorld.cs:89 ");

			Assert.AreEqual("CookieJarTools.Recipe.Tests", frame.Namespace);
			Assert.AreEqual("RecipeWorld", frame.Class, "the display class is machinery, not a class anybody wrote");
			Assert.AreEqual("Breaks", frame.Method, "and the lambda should be named after the method holding it");
			Assert.AreEqual(89, frame.Line);
		}

		/// <summary>A Windows path has a colon of its own, two characters in.</summary>
		[Test]
		public void AWindowsPathIsNotSplitOnItsDriveLetter()
		{
			var frame = One("  at Thing.Go () [0x00000] in F:\\Dev\\CookieJar\\Assets\\Thing.cs:12 ");

			Assert.AreEqual("F:\\Dev\\CookieJar\\Assets\\Thing.cs", frame.File);
			Assert.AreEqual(12, frame.Line);
		}

		[Test]
		public void AnAsyncBodyIsNamedAfterTheMethodItCameFrom()
		{
			var frame = One("Unity.Pipeline.BasePipelineServer/<ExecuteGated>d__105:MoveNext () "
				+ "(at ./Library/x/BasePipelineServer.cs:1389)");

			Assert.AreEqual("BasePipelineServer", frame.Class);
			Assert.AreEqual("ExecuteGated", frame.Method);
		}

		/// <summary>
		/// The compact form separates a nested type with a dot rather than a slash, so the same
		/// generated class arrives spelt two different ways in the same session.
		/// </summary>
		[Test]
		public void ALambdaIsTidiedWhicheverWayItsClassIsSpelt()
		{
			var slashed = One("Unity.Pipeline.BasePipelineServer/<>c__DisplayClass134_0:"
				+ "<ExecuteCommand>b__1 () (at ./Library/x/BasePipelineServer.cs:2508)");
			var dotted = One("Unity.Pipeline.BasePipelineServer.<>c__DisplayClass134_0:"
				+ "<ExecuteCommand>b__1 () (at ./Library/x/BasePipelineServer.cs:2508)");

			Assert.AreEqual("BasePipelineServer", slashed.Class);
			Assert.AreEqual("BasePipelineServer", dotted.Class);
			Assert.AreEqual("ExecuteCommand", dotted.Method);
		}

		[Test]
		public void AGenericTypeLosesTheArityAndKeepsTheArgument()
		{
			var frame = One("Unity.Pipeline.Threading.Dispatcher/WorkItem`1<object>:ExecuteCore () "
				+ "(at ./Library/x/Dispatcher.cs:284)");

			Assert.AreEqual("Dispatcher.WorkItem<object>", frame.Class);
		}

		/// <summary>A frame compiled without debug information names a module, not a file.</summary>
		[Test]
		public void AModuleIdIsNotMistakenForAFile()
		{
			var frame = One("  at PipelineEvaluation.PipelineEval_8b4.Execute () [0x00028] in "
				+ "<432bbb1811484479950254e48c6ac947>:0 ");

			Assert.IsTrue(frame.Read, "the frame itself is still worth showing");
			Assert.AreEqual(string.Empty, frame.File);
			Assert.IsFalse(frame.Somewhere, "but there is nowhere to go");
		}

		[Test]
		public void ParametersWithSpacesAndBracketsSurvive()
		{
			var frame = One("System.Reflection.MethodBase:Invoke(Object, Object[])");

			Assert.AreEqual("Object, Object[]", frame.Parameters);
		}

		[Test]
		public void SomethingThatIsNotAFrameIsKeptWhole()
		{
			var frame = One("this line is not a stack frame at all");

			Assert.IsFalse(frame.Read);
			Assert.AreEqual("this line is not a stack frame at all", frame.Raw);
		}

		[Test]
		public void MineIsWhateverSitsUnderAssets()
		{
			Assert.IsTrue(One("A.B:C () (at Assets/Thing.cs:1)").Mine);
			Assert.IsTrue(One("A.B:C () (at F:\\Dev\\Game\\Assets\\Thing.cs:1)").Mine);
			Assert.IsFalse(One("A.B:C () (at ./Library/PackageCache/x/Thing.cs:1)").Mine);
			Assert.IsFalse(One("A.B:C () (at Packages/com.unity.thing/Thing.cs:1)").Mine);
			Assert.IsFalse(One("UnityEngine.Debug:Log (object)").Mine);
		}

		[Test]
		public void BlankLinesAreNotFrames()
		{
			Assert.AreEqual(0, SifterFrames.Of("\n\n  \n").Count);
			Assert.AreEqual(0, SifterFrames.Of(string.Empty).Count);
			Assert.AreEqual(0, SifterFrames.Of(null).Count);
		}

		[Test]
		public void AWholeTraceComesBackInOrder()
		{
			var frames = SifterFrames.Of("UnityEngine.Debug:Log (object)\n"
				+ "A.B:C () (at Assets/Thing.cs:4)\n");

			Assert.AreEqual(2, frames.Count);
			Assert.AreEqual("Log", frames[0].Method);
			Assert.AreEqual("C", frames[1].Method);
		}

		private static SifterFrame One(string line)
		{
			var frames = SifterFrames.Of(line);
			Assert.AreEqual(1, frames.Count, "one line should give one frame");
			return frames[0];
		}
	}
}
