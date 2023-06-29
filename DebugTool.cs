using System;
using System.Diagnostics;

namespace IEEE754Inspector;

internal static class DebugTool
{
	[Conditional("DEBUG")]
	public static void LogMsg(object msg, int frameDepth = 1)
	{
		if (!Debugger.IsAttached)
			ConsoleManager.Show();
		StackTrace ss = new(true);
		Debug.Assert(frameDepth > 0 && frameDepth < ss.FrameCount);
		var mb = ss.GetFrame(frameDepth).GetMethod();
		Console.Out.WriteLine($">{mb.DeclaringType.Name}.{mb.Name}:\n{msg}");
	}
}
