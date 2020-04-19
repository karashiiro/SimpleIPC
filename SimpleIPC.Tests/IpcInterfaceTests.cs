using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleIPC.Tests
{
    [TestFixture]
    public class IpcInterfaceTests
    {
        private IpcInterface i1;
        private IpcInterface i2;

        [TearDown]
        public void Teardown()
        {
            i1.Dispose();
            i2.Dispose();
        }

        [Test]
        public async Task DefaultConstructor_PassesMessages()
        {
            i1 = new IpcInterface();
            i2 = new IpcInterface(i1.PartnerPort, i1.Port);

            var spinLock = true;
            i1.On<DummyClass>(dummyClass => { spinLock = false; });
            try
            {
                await i2.SendMessage(new DummyClass());
            }
            catch (HttpRequestException)
            {
                Assert.Fail("HttpRequestException thrown, i2 port {0} and i2 partner port {1}", i2.Port, i2.PartnerPort);
            }

            while (spinLock)
                await Task.Delay(1);
        }
    }
}