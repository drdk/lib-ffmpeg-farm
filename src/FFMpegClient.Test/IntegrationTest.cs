using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DR.FFMpegClient.Test
{
    [TestFixture]
    public class IntegrationTest
    {
        private AudioJobClient _audioClient;
        private StatusClient _statusClient;
        private const string ServiceUri = "http://od01udv:9000";
        private readonly string _destination = $@"\\ondnas01\MediaCache\Test\lib-FFMpg-integrations-test-{Environment.MachineName}-{DateTime.Now:yyyy-MM-dd-HH-mm}";

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {

            _audioClient = new AudioJobClient(ServiceUri);
            _statusClient = new StatusClient(ServiceUri);

            if (Directory.Exists(_destination))
                throw new Exception("Destination already exists : " + _destination);

            Directory.CreateDirectory(_destination);
        }

        [SetUp]
        public void SetUp()
        {
        }
    }
}
