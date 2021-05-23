using Common;
using System.Threading.Tasks;

namespace ClientFirst
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new Client().ConnectAsync();
        }
    }
}
