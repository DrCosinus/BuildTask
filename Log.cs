using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    internal static class Log
    {
        private static int indentCount = 0;
        private static string indentString = "";

        internal static void PushIndent()
        {
            indentCount++;
            indentString = new string(' ', 2 * indentCount);
        }

        internal static void PopIndent()
        {
            indentCount--;
            indentString = new string(' ', 2 * indentCount);
        }

        private static void WriteIndent()
        {
            Console.Write(indentString);
            Debug.Write(indentString);
        }

        internal static void WriteLine()
        {
            WriteIndent();
            Console.WriteLine();
            Debug.WriteLine("");
        }
        internal static void WriteLine(string _message)
        {
            WriteIndent();
            Console.WriteLine(_message);
            Debug.WriteLine(_message);
        }
        internal static void WriteLine(StringBuilder _message)
        {
            WriteIndent();
            string message = _message.ToString();
            Console.WriteLine(_message);
            Debug.WriteLine(_message);
        }
        internal static void WriteLine(string _format, params object[] _args)
        {
            WriteIndent();
            string message = string.Format(_format, _args);
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }
    }
}
