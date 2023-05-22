using FlightPatternDetection.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetectionEngine
{
    public class EngineFactory
    {
        public static IDetectionEngine GetEngine(double distance, INavDbManager navDbManager)
        {
            return new DetectionEngine(distance, navDbManager);
        }
    }
}
