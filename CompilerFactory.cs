using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    class CompilerFactory
    {
        public static ICompiler Create(ECompiler compiler)
        {
            switch (compiler)
            {
                case ECompiler.Clang6_0: return new Compilers.Clang();
                default: return new Compilers.MSVC();
            }
        }
    }
}
