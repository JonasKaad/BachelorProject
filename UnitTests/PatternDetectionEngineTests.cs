using PatternDetectionEngine;

namespace UnitTests;

[TestCategory("Engine")]
[TestClass]
public class PatternDetectionEngineTests
{
    [TestMethod]
    public void Engine_ShouldRemovePoints()
    {
        // Arrange
        var flight = UnitTestHelper.RetriveFlightData("flight_with_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = engine.RemoveUnnecessaryPoints(flight, "test");

        // Assert
        result.Count.Should().NotBe(flight.Count);
    }
    
    [TestMethod]
    public void Engine_ShouldNotHavePattern()
    {
        var flight = UnitTestHelper.RetriveFlightData("flight_without_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = engine.AnalyseFlight(flight);

        result.Should().BeFalse();
    }
    
    [TestMethod]
    public void Engine_ShouldHavePattern()
    {
        // Arrange
        var flight = UnitTestHelper.RetriveFlightData("flight_with_holding_pattern.json");
        var engine = new DetectionEngine(0.5);
        
        // Act
        var result = engine.AnalyseFlight(flight);

        // Result
        result.Should().BeTrue();
    }
}