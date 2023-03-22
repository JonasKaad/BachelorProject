using System.Net;

namespace UnitTests
{
    public class ForeFlightApiRequiredTestAttribute : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (!DetectForeFlightVPN())
            {
                var message = $"No ForeFlight VPN detected. Test skipped!";
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException(message)
                    }
                };
            }
            return base.Execute(testMethod);
        }

        private const string ForeFlightVpnBaseUrl = "https://traffic-service.acqa.foreflight.com/";
        private const string ForeFlightEndpoint = "traffic/v1/Aircraft/history/1";
        private static object _cachedForeFlightVpnLock = new object();
        private static bool? _cachedForeFlightVpn;
        public static bool DetectForeFlightVPN()
        {
            if (!_cachedForeFlightVpn.HasValue)
            {
                lock (_cachedForeFlightVpnLock)
                {
                    if (!_cachedForeFlightVpn.HasValue)
                    {
                        // Check if on VPN.
                        var request = new HttpClient();
                        request.BaseAddress = new Uri(ForeFlightVpnBaseUrl);
                        request.Timeout = TimeSpan.FromSeconds(2);
                        try
                        {
                            var response = request.GetAsync(ForeFlightEndpoint).GetAwaiter().GetResult();
                            _cachedForeFlightVpn = response.StatusCode == HttpStatusCode.OK;
                        }
                        catch (TaskCanceledException)
                        {
                            _cachedForeFlightVpn = false;
                        }
                    }
                }
            }

            return _cachedForeFlightVpn.Value;
        }
    }
}
