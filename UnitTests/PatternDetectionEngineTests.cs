using PatternDetectionEngine;

namespace UnitTests;

[TestCategory("Engine")]
[TestClass]
public class PatternDetectionEngineTests
{
    [TestMethod]
    public async Task Engine_ShouldRemovePoints()
    {
        // Arrange
        var flight = UnitTestHelper.RetriveFlightData("flight_with_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = await engine.RemoveUnnecessaryPoints(flight);

        // Assert
        result.Count.Should().NotBe(flight.Count);
    }
    
    [TestMethod]
    public async Task Engine_ShouldNotHavePattern()
    {
        var flight = UnitTestHelper.RetriveFlightData("flight_without_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = await engine.AnalyseFlight(flight);

        result.IsHolding.Should().BeFalse();
    }
    
    [TestMethod]
    public async Task Engine_ShouldHavePattern()
    {
        // Arrange
        var flight = UnitTestHelper.RetriveFlightData("flight_with_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = await engine.AnalyseFlight(flight);

        // Result
        result.IsHolding.Should().BeTrue();
    }
}