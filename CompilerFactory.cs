namespace BuildTask
{
    static class CompilerFactory
    {
        public static ICompiler Create(ECompiler compiler)
        {
            switch (compiler)
            {
                case ECompiler.Clang6_0: return new Compilers.Clang();
                default: return new Compilers.MSVC();
            }
        }
        public static string GetShortName(ECompiler compiler)
        {
            switch (compiler)
            {
                case ECompiler.Clang6_0: return "clang";
                default: return "msvc";
            }
        }
    }
}
