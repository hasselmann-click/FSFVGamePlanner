using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests
{
    [TestClass]
    public class SlotServiceTest
    {

        private SlotService slotService;

        [ClassInitialize]
        public void Setup()
        {
            var loggerMock = new Mock<ILogger<SlotService>>();
            this.slotService = new SlotService(loggerMock.Object);
        }

        [TestMethod]
        public void TestMethod1()
        {
            //slotService.Slot()
        }
    }
}