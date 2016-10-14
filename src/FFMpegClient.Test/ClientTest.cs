using System;
using System.Linq;
using System.Threading;
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
            _audioClient = new AudioJobClient(ServiceUri);
            _statusClient = new StatusClient(ServiceUri);
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
                SourceFilename = "cliptest1.mov",
                OutputFolder = "\\\\ondnas01\\MediaCache\\Test\\FFMpg",
                Needed = DateTime.UtcNow
            };
            var jobTask = _audioClient.CreateNewAsync(req);
            var innerException =
                Assert.Throws<AggregateException>(() =>
                jobTask.Wait()).InnerException as SwaggerException;

        }
    }
}
