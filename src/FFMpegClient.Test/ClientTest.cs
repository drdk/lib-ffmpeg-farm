using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DR.FFMpegClient.Test
{
    [TestFixture]
    public class ClientTest
    {
        private AudioJobClient _audioClient;
        private StatusClient _statusClient;
        private static string ServiceUri => Configuration.FFMPEGFarmUrl;
        private static string TestRoot => Configuration.TestRoot;
        [SetUp]
        public void SetUp()
        {
            var httpClient = new HttpClient();
            _audioClient = new AudioJobClient(httpClient) { BaseUrl = ServiceUri };
            _statusClient = new StatusClient(httpClient) { BaseUrl = ServiceUri };
        }

        [Test]
        public void InvalidOrderJsonExceptionTest()
        {
            var req =new AudioJobRequestModel();
            var jobTask = _audioClient.CreateNewAsync(req);
            var innerException =
                Assert.Throws<AggregateException>(() => 
                jobTask.Wait()).InnerException as JsonSerializationException;
            
            Console.WriteLine(innerException.ToString());
        }

        [Test]
        public void InvalidOrderNoTargetsExceptionTest()
        {
            var req = new AudioJobRequestModel
            {
                //Targets = null,
                Inpoint = TestRoot,
                SourceFilenames = new ObservableCollection<string> { "cliptest1.mov" },
                OutputFolder = TestRoot + @"\FFMpg",
                DestinationFilenamePrefix = "",
                Needed = DateTime.UtcNow
            };
            var jobTask = _audioClient.CreateNewAsync(req);
            var agg = Assert.Throws<AggregateException>(() =>
                jobTask.Wait());
            var innerException = agg.InnerException;
            Console.WriteLine(innerException?.Message);

        }
    }
}
