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
        internal static void WriteLine(string _message)
        {
            Console.WriteLine(_message);
            Debug.WriteLine(_message);
        }
        internal static void WriteLine(StringBuilder _message)
        {
            string message = _message.ToString();
            Console.WriteLine(_message);
            Debug.WriteLine(_message);
        }
        internal static void WriteLine(string _format, params object[] _args)
        {
            string message = string.Format(_format, _args);
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }
    }
}
