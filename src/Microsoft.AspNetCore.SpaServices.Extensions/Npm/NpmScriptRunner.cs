using Microsoft.AspNetCore.NodeServices.Util;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

// This is under the NodeServices namespace because post 2.1 it will be moved to that package
namespace Microsoft.AspNetCore.NodeServices.Npm
{
    internal class NpmScriptRunner
    {
        public EventedStreamReader StdOut { get; }
        public EventedStreamReader StdErr { get; }

        public NpmScriptRunner(string workingDirectory, string scriptName, string arguments)
        {
            if (string.IsNullOrEmpty(workingDirectory))
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(workingDirectory));
            }

            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(scriptName));
            }

            var npmExe = "npm";
            var completeArguments = $"run {scriptName} -- {arguments ?? string.Empty}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                npmExe = "cmd";
                completeArguments = $"/c npm {completeArguments}";
            }

            var process = LaunchNodeProcess(new ProcessStartInfo(npmExe)
            {
                Arguments = completeArguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            });

            StdOut = new EventedStreamReader(process.StandardOutput);
            StdErr = new EventedStreamReader(process.StandardError);
        }

        public void CopyOutputToLogger(ILogger logger)
        {
            StdOut.OnReceivedLine += line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    logger.LogInformation(line);
                }
            };

            StdErr.OnReceivedLine += line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    logger.LogError(line);
                }
            };
        }

        private static Process LaunchNodeProcess(ProcessStartInfo startInfo)
        {
            try
            {
                var process = Process.Start(startInfo);

                // See equivalent comment in OutOfProcessNodeInstance.cs for why
                process.EnableRaisingEvents = true;

                return process;
            }
            catch (Exception ex)
            {
                var message = $"Failed to start 'npm'. To resolve this:.\n\n"
                            + "[1] Ensure that 'npm' is installed and can be found in one of the PATH directories.\n"
                            + $"    Current PATH enviroment variable is: { Environment.GetEnvironmentVariable("PATH") }\n"
                            + "    Make sure the executable is in one of those directories, or update your PATH.\n\n"
                            + "[2] See the InnerException for further details of the cause.";
                throw new InvalidOperationException(message, ex);
            }
        }
    }
}
