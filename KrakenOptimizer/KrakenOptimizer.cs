using Kraken;
using Kraken.Http;
using Kraken.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenOptimizer
{
    public class KrakenOptimizer : IDisposable
    {
        private Connection connection;
        private Client client;

        public KrakenOptimizer()
        {
            var krakenApiKey = ConfigurationManager.AppSettings["KrakenApiKey"];
            var krakenApiSecret = ConfigurationManager.AppSettings["KrakenApiSecret"];
            connection = Connection.Create(krakenApiKey, krakenApiSecret);
            client = new Client(connection);
        }

        public async Task<IApiResponse<OptimizeWaitResult>> Optimize(string imagePath)
        {
            var options = new OptimizeUploadWaitRequest()
            {
                Lossy = true
            };
            var task = client.OptimizeWait(imagePath, options);
            return await task;
        }

        public void Dispose()
        {
            client.Dispose();
            connection.Dispose();
        }
    }
}
