using System.IO;
using System.Reflection;

namespace IFB
{
    internal static class Files
    {
        internal static readonly string ExecutablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // allow calling the program from a remote dir
    }
}