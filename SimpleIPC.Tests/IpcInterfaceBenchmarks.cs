using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleIPC.Tests
{
    [TestFixture]
    public class IpcInterfaceBenchmarks
    {
        private const int SpinlockWait = 1;

        // ReSharper disable InconsistentNaming
        private IpcInterface i1;
        private IpcInterface i2;
        // ReSharper restore InconsistentNaming

        [Test]
        public async Task SendMessage_AvgTimeIsBelow500Ms()
        {
            i1 = new IpcInterface();
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 5; i++)
                await SpinlockForMessage();
            stopwatch.Stop();

            var averageMs = stopwatch.ElapsedMilliseconds / 5;
            Assert.IsTrue(averageMs <= 500, "Expected <=500ms, got {0}ms", averageMs);
        }

        private async Task SpinlockForMessage()
        {
            var spinLock = true;
            i1.On<DummyClass>(dummyClass => { spinLock = false; });
            await i2.SendMessage(new DummyClass());

            while (spinLock)
                await Task.Delay(SpinlockWait);
        }
    }
}
