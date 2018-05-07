using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    abstract class AbstractCompiler : ICompiler
    {
        protected virtual void SetupEnvironmentVariables() { }
        protected abstract string ExecutableName { get; }
        public ECppVersion? CppVersion { protected get; set; }
        public string OutputFilepath { set; protected get; }
        public EWarningLevel? WarningLevel { set; protected get; }
        public EDebugLevel? DebugLevel { protected get; set; }
        public bool WarningAsErrors { set; protected get; }
        protected virtual string CompilationParameters { get; }
        public List<string> SourceFilePaths { set; protected get; }
        public string IntermediaryFileFolderName { set; protected get; }

        private StringBuilder OutputBuilder = null;

        protected virtual void PrepareRun()
        {
            new FileInfo(OutputFilepath).Directory.Create();
        }
        public int Run()
        {
            PrepareRun();
            SetupEnvironmentVariables();

            var arguments = CompilationParameters;
            Log.WriteLine(arguments);

            var process = new Process();
            process.StartInfo.FileName = ExecutableName;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.Arguments = arguments;
            process.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
            OutputBuilder = new StringBuilder();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            Log.WriteLine(OutputBuilder);
            var exit_code = process.ExitCode;
            process.Close();

            return exit_code;
        }

        private void OutputDataHandler(object sendingProcess, DataReceivedEventArgs _data_args)
        {
            OutputBuilder.AppendLine(_data_args.Data);
        }
        private void ErrorDataHandler(object sendingProcess, DataReceivedEventArgs _data_args)
        {
            OutputBuilder.AppendLine(_data_args.Data);
        }
    }
}
