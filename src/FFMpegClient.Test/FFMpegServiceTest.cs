using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaInfoDotNet;
using NUnit.Framework;

namespace DR.FFMpegClient.Test
{
    [TestFixture]
    public sealed class FFMpegServiceTest
    {
        private AudioJobClient _audioJobClient;
        private MuxJobClient _muxJobClient;
        private StatusClient _statusClient;
        private const string ffmpegFarmUrl = "http://ffmpegctrl01udv.net.dr.dk:9000"; // prod : "http://XXXXXX:9000"; dev: "http://ffmpegctrl01udv.net.dr.dk:9000"
        private const string TestRoot = @"\\ondnas01\MediaCache\Test\";
        private const string MuxTestVideoFile = TestRoot + "FFMpegMuxJobTest.mov";
        private const string MuxTestAudioFile = TestRoot + "FFMpegMuxJobTest.wav";
        private const string AudioTestFile = TestRoot + "FFMpegAudioJobTest.Wav";
        private const string AudioIntroTestFile = TestRoot + "FFMpegAudioJobIntroTest.Wav";
        private const string AudioOutroTestFile = TestRoot + "FFMpegAudioJobOutroTest.Wav";
        private string _sourceMuxTestVideoFile = TestRoot + "UnitTestFileMux-{0}.mov";
        private string _sourceMuxTestAudioFile = TestRoot + "UnitTestFileMux-{0}.wav";
        private string _sourceAudioTestFile = TestRoot + "UnitTestFileAudio-{0}.wav";
        private string _sourceAudioIntroFile = TestRoot + "UnitTestFileAudioIntro-{0}.wav";
        private string _sourceAudioOutroFile = TestRoot + "UnitTestFileAudioOutro-{0}.wav";
        private string _targetTestPath = TestRoot + "UnitTest-{0}-{1}";
        private string _targetFileAudioPrefix = "UnitTest-Audio-{0}";
        private string _targetFileAudioIntroOutroPrefix = "UnitTest-Audio-IntroOutro-{0}";
        private string _targetFileMux = "UnitTest-Mux-{0}.mov";

        private static readonly ObservableCollection<AudioDestinationFormat> AudioTargets = new ObservableCollection
            <AudioDestinationFormat>()
            {
                new AudioDestinationFormat()
                {
                    AudioCodec = AudioDestinationFormatAudioCodec.MP3,
                    Bitrate = 32,
                    Channels = AudioDestinationFormatChannels.Mono,
                    Format = AudioDestinationFormatFormat.MP3
                },
                new AudioDestinationFormat()
                {
                    AudioCodec = AudioDestinationFormatAudioCodec.MP3,
                    Bitrate = 192,
                    Channels = AudioDestinationFormatChannels.Stereo,
                    Format = AudioDestinationFormatFormat.MP3
                },
                new AudioDestinationFormat()
                {
                    AudioCodec = AudioDestinationFormatAudioCodec.AAC,
                    Bitrate = 32,
                    Channels = AudioDestinationFormatChannels.Mono,
                    Format = AudioDestinationFormatFormat.MP4
                },
                new AudioDestinationFormat()
                {
                    AudioCodec = AudioDestinationFormatAudioCodec.AAC,
                    Bitrate = 192,
                    Channels = AudioDestinationFormatChannels.Stereo,
                    Format = AudioDestinationFormatFormat.MP4
                },
            };

        [SetUp]
        public void FixtureSetup()
        {
            var httpClient = new HttpClient();
            _audioJobClient = new AudioJobClient(httpClient) { BaseUrl = ffmpegFarmUrl };
            _muxJobClient = new MuxJobClient(httpClient) { BaseUrl = ffmpegFarmUrl };
            _statusClient = new StatusClient(httpClient) { BaseUrl = ffmpegFarmUrl };
            _sourceMuxTestVideoFile = string.Format(_sourceMuxTestVideoFile, Environment.MachineName);
            _sourceMuxTestAudioFile = string.Format(_sourceMuxTestAudioFile, Environment.MachineName);
            _sourceAudioTestFile = string.Format(_sourceAudioTestFile, Environment.MachineName);
            _sourceAudioIntroFile = string.Format(_sourceAudioIntroFile, Environment.MachineName);
            _sourceAudioOutroFile = string.Format(_sourceAudioOutroFile, Environment.MachineName);
            _targetTestPath = string.Format(_targetTestPath, Environment.MachineName, DateTime.Now.Ticks);
            _targetFileAudioPrefix = string.Format(_targetFileAudioPrefix, Environment.MachineName);
            _targetFileAudioIntroOutroPrefix = string.Format(_targetFileAudioIntroOutroPrefix, Environment.MachineName);
            _targetFileMux = string.Format(_targetFileMux, Environment.MachineName);

            if (!File.Exists(MuxTestVideoFile))
                throw new Exception("Test file missing " + MuxTestVideoFile);

            if (!File.Exists(MuxTestAudioFile))
                throw new Exception("Test file missing " + MuxTestAudioFile);

            if (!File.Exists(AudioTestFile))
                throw new Exception("Test file missing " + AudioTestFile);

            if (!File.Exists(AudioIntroTestFile))
                throw new Exception("Test file missing " + AudioIntroTestFile);

            if (!File.Exists(AudioOutroTestFile))
                throw new Exception("Test file missing " + AudioOutroTestFile);
            
            CleanUp();
        }

        private void CleanUp()
        {
            if (File.Exists(_sourceMuxTestVideoFile))
                File.Delete(_sourceMuxTestVideoFile);

            if (File.Exists(_sourceMuxTestAudioFile))
                File.Delete(_sourceMuxTestAudioFile);

            if (File.Exists(_sourceAudioTestFile))
                File.Delete(_sourceAudioTestFile);

            if (File.Exists(_sourceAudioIntroFile))
                File.Delete(_sourceAudioIntroFile);

            if (File.Exists(_sourceAudioOutroFile))
                File.Delete(_sourceAudioOutroFile);

            if (Directory.Exists(_targetTestPath))
                Directory.Delete(_targetTestPath, true);
        }

        [TearDown]
        public void FixtureTearDown()
        {
            CleanUp();
        }

        [Test, Explicit]
        public async Task StatusTest()
        {
            var sw = new Stopwatch();
            sw.Start();
            var res = await _statusClient.GetAllAsync(null);
            sw.Stop();
            Console.WriteLine("Status : " + res.Count + " response time " + sw.ElapsedMilliseconds + " ms");
            //Assert.That(res.Count, Is.GreaterThan(0));
        }

        //[Test]
        //public void WorkingNodesCountTest()
        //{
        //    Assert.That(_wfsService.GetWorkingNodes("WorkingMachines").Length, Is.GreaterThanOrEqualTo(1));
        //}

        [Test, Explicit]
        public async Task AudioJobTest()
        {
            File.Copy(AudioTestFile, _sourceAudioTestFile);
            Directory.CreateDirectory(_targetTestPath);
            
            AudioJobRequestModel request = new AudioJobRequestModel()
            {
                DestinationFilenamePrefix = _targetFileAudioPrefix,
                Inpoint = "0",
                Needed = DateTime.UtcNow,
                OutputFolder = _targetTestPath,
                SourceFilenames = new ObservableCollection<string>( new[] { _sourceAudioTestFile }),
                Targets = AudioTargets
            };

            var jobGuid = await _audioJobClient.CreateNewAsync(request);

            bool done;
            int maxCount = 240;
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Starting job {0}", jobGuid);
            sw.Start();
            FfmpegJobModel job;
            do
            {
                job = await _statusClient.GetAsync(jobGuid);
                var runningTask = job.Tasks.FirstOrDefault(t => t.State == FfmpegTaskModelState.InProgress);
                Console.WriteLine($"Jobstatus : {job.State}, time: {sw.ElapsedMilliseconds} ms, filename: {runningTask?.DestinationFilename}, {runningTask?.Progress:0.##} %");
                if (job.State == FfmpegJobModelState.Failed || job.State == FfmpegJobModelState.Canceled || job.State == FfmpegJobModelState.Unknown)
                    throw new Exception($"Error running job. job state: {job.State}");
                done = job.State == FfmpegJobModelState.Done;
                if (!done)
                {
                    Thread.Sleep(1000);
                }

            } while (!done && maxCount-- > 0);

            Assert.That(done, Is.True);
            Console.WriteLine($"Job done, time : {sw.ElapsedMilliseconds} ms ({maxCount})");
            sw.Stop();
            Assert.That(job.Tasks.Count, Is.EqualTo(request.Targets.Count));
            foreach (var target in job.Tasks)
            {
                string fileFullPath = Path.Combine(_targetTestPath, target.DestinationFilename);
                Console.WriteLine($"Checking file: {fileFullPath}");
                Assert.That(File.Exists(fileFullPath), Is.True, $"Expected to find transcoded file @ {fileFullPath}");
            }
        }

        [Test, Explicit]
        public async Task AudioIntroOutroJobTest()
        {
            File.Copy(AudioTestFile, _sourceAudioTestFile);
            File.Copy(AudioIntroTestFile, _sourceAudioIntroFile);
            File.Copy(AudioOutroTestFile, _sourceAudioOutroFile);
            Directory.CreateDirectory(_targetTestPath);

            var introDuration = new MediaFile(_sourceAudioIntroFile).Audio[0].Duration;
            var mainDuration = new MediaFile(_sourceAudioTestFile).Audio[0].Duration;
            var outroDuration = new MediaFile(_sourceAudioOutroFile).Audio[0].Duration;
            Assert.That(introDuration, Is.GreaterThan(0));
            Assert.That(mainDuration, Is.GreaterThan(0));
            Assert.That(outroDuration, Is.GreaterThan(0));

            AudioJobRequestModel request = new AudioJobRequestModel()
            {
                DestinationFilenamePrefix = _targetFileAudioIntroOutroPrefix,
                Inpoint = "0",
                Needed = DateTime.UtcNow,
                OutputFolder = _targetTestPath,
                SourceFilenames = new ObservableCollection<string>(new[] { _sourceAudioIntroFile, _sourceAudioTestFile, _sourceAudioOutroFile }),
                Targets = AudioTargets
            };

            var jobGuid = await _audioJobClient.CreateNewAsync(request);

            bool done;
            int maxCount = 240;
            Stopwatch sw = new Stopwatch();
            Console.WriteLine($"Starting job {jobGuid}");
            sw.Start();
            FfmpegJobModel job;
            do
            {
                job = await _statusClient.GetAsync(jobGuid);
                var runningTask = job.Tasks.FirstOrDefault(t => t.State == FfmpegTaskModelState.InProgress);
                Console.WriteLine($"Jobstatus : {job.State}, time: {sw.ElapsedMilliseconds} ms, filename: {runningTask?.DestinationFilename}, {runningTask?.Progress:0.##} %");
                if (job.State == FfmpegJobModelState.Failed || job.State == FfmpegJobModelState.Canceled || job.State == FfmpegJobModelState.Unknown)
                    throw new Exception($"Error running job. job state: {job.State}");
                done = job.State == FfmpegJobModelState.Done;
                if (!done)
                {
                    Thread.Sleep(1000);
                }

            } while (!done && maxCount-- > 0);

            Assert.That(done, Is.True);
            Console.WriteLine($"Job done, time : {sw.ElapsedMilliseconds} ms ({maxCount})");
            sw.Stop();
            Assert.That(job.Tasks.Count, Is.EqualTo(request.Targets.Count));
            foreach (var target in job.Tasks)
            {
                var fileFullPath = Path.Combine(_targetTestPath, target.DestinationFilename);
                Console.WriteLine("Checking file: " + fileFullPath);
                Assert.That(File.Exists(fileFullPath), Is.True, $"Expected to find transcoded file @ {fileFullPath}");
                var mi = new MediaFile(fileFullPath);
                if (mi.Audio[0].Duration == 0)
                {
                    Console.WriteLine($"Skips duration test for {fileFullPath} since media info thinks it has zero duration.");
                    continue; 
                }
                var destDuration = mi.Audio[0].Duration;
                Assert.That(Math.Abs(destDuration - (introDuration + mainDuration + outroDuration)), Is.LessThan(100), "Stiching should not differ more than 100 ms");
            }
        }

        [Test, Explicit]
        public async Task MuxingJobTest()
        {
            File.Copy(MuxTestAudioFile, _sourceMuxTestAudioFile);
            File.Copy(MuxTestVideoFile, _sourceMuxTestVideoFile);
            Directory.CreateDirectory(_targetTestPath);

            MuxJobRequestModel request = new MuxJobRequestModel()
            {
                AudioSourceFilename = _sourceMuxTestAudioFile,
                VideoSourceFilename = _sourceMuxTestVideoFile,
                DestinationFilename = _targetFileMux,
                Inpoint = "0",
                Needed = DateTime.UtcNow,
                OutputFolder = _targetTestPath
            };

            var jobGuid = await _muxJobClient.CreateNewAsync(request);

            bool done;
            int maxCount = 240;
            Stopwatch sw = new Stopwatch();
            Console.WriteLine($"Starting job {jobGuid}");
            sw.Start();
            FfmpegJobModel job;
            do
            {
                job = await _statusClient.GetAsync(jobGuid);
                var runningTask = job.Tasks.FirstOrDefault(t => t.State == FfmpegTaskModelState.InProgress);
                Console.WriteLine($"Jobstatus : {job.State}, time: {sw.ElapsedMilliseconds} ms, filename: {runningTask?.DestinationFilename}, {runningTask?.Progress:0.##} %");
                if (job.State == FfmpegJobModelState.Failed || job.State == FfmpegJobModelState.Canceled || job.State == FfmpegJobModelState.Unknown)
                    throw new Exception($"Error running job. job state: {job.State}");
                done = job.State == FfmpegJobModelState.Done;
                if (!done)
                {
                    Thread.Sleep(1000);
                }

            } while (!done && maxCount-- > 0);

            Assert.That(done, Is.True);
            Console.WriteLine($"Job done, time : {sw.ElapsedMilliseconds} ms ({maxCount})");
            sw.Stop();
            
            foreach (var target in job.Tasks)
            {
                string fileFullPath = Path.Combine(_targetTestPath, target.DestinationFilename);
                Console.WriteLine("Checking file: " + fileFullPath);
                Assert.That(File.Exists(fileFullPath), Is.True, $"Expected to find transcoded file @ {fileFullPath}");
            }
        }
    }
}
