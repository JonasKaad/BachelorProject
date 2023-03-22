using PatternDetectionEngine;
using TrafficApiClient;

namespace UnitTests;

[TestClass]
public class PatternDetectionEngineTests
{
    [TestMethod]
    public async Task Engine_ShouldRemovePoints()
    {
        // Arrange
        var api = new TrafficClient("https://traffic-service.acqa.foreflight.com");
        var apiResult = await api.HistoryAsync(1939169898, -1);
        var apiList = apiResult.ToList();
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = engine.RemoveUnnecessaryPoints(apiList, "test");

        // Assert
        result.Count.Should().NotBe(apiList.Count);
    }
}