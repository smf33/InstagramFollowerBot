using System.Threading.Tasks;

namespace IFB
{
    internal interface IBotAction
    {
        public Task RunAsync();
    }
}