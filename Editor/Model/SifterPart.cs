namespace CookieJarTools.Sifter.Editor
{
	/// <summary>
	/// The pieces one line of a stack is made of, each of which can be coloured and each of which
	/// can be turned off on its own. Turning off namespaces and parameters alone takes most traces
	/// from two lines to one.
	/// </summary>
	public enum SifterPart
	{
		Namespace,
		Class,
		Method,
		Parameters,
		File,
		Line
	}
}
