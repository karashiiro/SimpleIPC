using System.Threading.Tasks;
using SimpleIPCHttp.Tests;

namespace SimpleIPCHttp.TestDummy
{
    public static class Program
    {
        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            var i2 = new IpcInterface(int.Parse(args[0]), int.Parse(args[1]));
            await i2.SendMessage(new DummyClass());
            await Task.Delay(-1);
        }
    }
}
