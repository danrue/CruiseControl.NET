using System;
using System.Diagnostics;

namespace ThoughtWorks.CruiseControl.Core.Util
{
	/// <summary>
	/// The ProcessExecutor executes a new <see cref="Process"/> using the properties specified in the input <see cref="ProcessInfo" />.
	/// ProcessExecutor is designed specifically to deal with processes that redirect the results of both
	/// the standard output and the standard error streams.  Reading from these streams is performed in
	/// a separate thread using the <see cref="ProcessReader"/> class, in order to prevent deadlock while 
	/// blocking on <see cref="Process.WaitForExit"/>.
	/// </summary>
	public class ProcessExecutor
	{
		public virtual ProcessResult Execute(ProcessInfo processInfo)
		{
			using(Process process = processInfo.CreateAndStartNewProcess())
			{
				ProcessReader standardOutput = new ProcessReader(process.StandardOutput);
				ProcessReader standardError = new ProcessReader(process.StandardError);
				standardOutput.Start();
				standardError.Start();

				bool hasExited = process.WaitForExit(processInfo.TimeOut);
				if (! hasExited)
				{
					Log.Warning(string.Format("Process timed out: {0} {1}.  Process id: {2}", processInfo.FileName, processInfo.Arguments, process.Id));
					process.Kill();
					Log.Warning(string.Format("The timed out process has been killed: {0}", process.Id));
				}
				standardOutput.WaitForExit();
				standardError.WaitForExit();

				return new ProcessResult(standardOutput.Output, standardError.Output, process.ExitCode, ! hasExited);
			}				
		}
	}
}