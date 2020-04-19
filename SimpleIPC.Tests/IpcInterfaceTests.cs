using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleIPC.Tests
{
    [TestFixture]
    public class IpcInterfaceTests
    {
        private const int SpinlockWait = 1;

        // ReSharper disable InconsistentNaming
        private IpcInterface i1;
        private IpcInterface i2;
        // ReSharper restore InconsistentNaming

        [TearDown]
        public void Teardown()
        {
            i1.Dispose();
            i2.Dispose();
        }

        [Test]
        public void Constructor_Default_PortsAreConsistent()
        {
            i1 = new IpcInterface();
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);
            Assert.IsTrue(i1.Port == i2.PartnerPort && i1.PartnerPort == i2.Port, "Got i1: [{0} {1}] i2: [{2} {3}], expected i1: [{0} {1}] i2: [{0} {1}]", i1.Port, i1.PartnerPort, i2.Port, i2.PartnerPort);
        }

        [Test]
        public async Task Constructor_Default_PassesMessages()
        {
            i1 = new IpcInterface();
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task Constructor_Port_PassesMessages()
        {
            i1 = new IpcInterface(13773);
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task Constructor_PortPartnerPort_PassesMessages()
        {
            i1 = new IpcInterface(13773, 13774);
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task Constructor_HttpClient_PassesMessages()
        {
            using var http = new HttpClient();
            i1 = new IpcInterface(http);
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task Constructor_HttpClientPort_PassesMessages()
        {
            using var http = new HttpClient();
            i1 = new IpcInterface(http, 13773);
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task Constructor_HttpClientPortPartnerPort_PassesMessages()
        {
            using var http = new HttpClient();
            i1 = new IpcInterface(http, 13773, 13774);
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            await SpinlockForMessage();
        }

        [Test]
        public async Task On_FiresOnCorrectTypeOnly()
        {
            i1 = new IpcInterface();
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            var spinLock = true;
            i1.On<DummyClass>(dummyClass => { spinLock = false; });
            i1.On<DummierClass>(dummierClass => {});
            await i2.SendMessage(new DummierClass());

            var iterations = 0;
            while (spinLock && iterations < SpinlockWait * 30)
            {
                await Task.Delay(SpinlockWait);
                iterations++;
            }

            Assert.IsTrue(spinLock);
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