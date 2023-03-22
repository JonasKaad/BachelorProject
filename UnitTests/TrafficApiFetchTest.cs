using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrafficApiClient;

namespace UnitTests
{
    [TestClass]
    public class TrafficApiFetchTest
    {
        [Ignore("Only used for debugging")]
        [TestMethod]
        public async Task AircraftApi_GetTrafficData_ShouldRespond()
        {
            // Arrange
            var api = new TrafficClient("https://traffic-service.acqa.foreflight.com");

            // Act
            var result = await api.HistoryAsync(1935774362, -1);

            // Assert
            result.Should().NotBeEmpty();
        }
    }
}