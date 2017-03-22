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
        private const string ServiceUri = "http://od01udv:9000";
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
                Inpoint = "\\\\ondnas01\\MediaCache\\Test\\",
                SourceFilenames = new ObservableCollection<string> { "cliptest1.mov" },
                OutputFolder = "\\\\ondnas01\\MediaCache\\Test\\FFMpg",
                DestinationFilenamePrefix = "",
                Needed = DateTime.UtcNow
            };
            var jobTask = _audioClient.CreateNewAsync(req);
            var agg = Assert.Throws<AggregateException>(() =>
                jobTask.Wait());
            var innerException = agg.InnerException as SwaggerException;
            Console.WriteLine(innerException.Response);

        }
    }
}
