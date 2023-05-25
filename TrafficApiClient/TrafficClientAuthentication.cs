using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficApiClient
{
    partial class TrafficClient
    {
        private string? m_apiKey = null;
        public void SetApiKey(string? apiKey)
        {
            m_apiKey = apiKey;
        }

        partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, string url)
        {
            if (!string.IsNullOrWhiteSpace(m_apiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", m_apiKey);
            }
        }
    }
}
