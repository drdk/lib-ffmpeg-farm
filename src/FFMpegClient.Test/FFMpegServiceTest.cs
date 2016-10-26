﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace DR.FFMpegClient.Test
{
    [TestFixture]
    public sealed class FFMpegServiceTest
    {
        private AudioJobClient _audioJobClient;
        private MuxJobClient _muxJobClient;
        private StatusClient _statusClient;
        private const string ffmpegFarmUrl = "http://od01udv:9000"; // prod : "http://XXXXXX:9000"; dev: "http://od01udv:9000"
        private const string TestRoot = @"\\ondnas01\MediaCache\Test\";
        private const string MuxTestVideoFile = TestRoot + "FFMpegMuxJobTest.mov";
        private const string MuxTestAudioFile = TestRoot + "FFMpegMuxJobTest.wav";
        private const string AudioTestFile = TestRoot + "FFMpegAudioJobTest.Wav";
        private string _sourceMuxTestVideoFile = TestRoot + "UnitTestFileMux-{0}.mov";
        private string _sourceMuxTestAudioFile = TestRoot + "UnitTestFileMux-{0}.wav";
        private string _sourceAudioTestFile = TestRoot + "UnitTestFileAudio-{0}.wav";
        private string _targetTestPath = TestRoot + "UnitTest-{0}";
        private string _targetFileAudioPrefix = "UnitTest-Audio-{0}";
        private string _targetFileMux = "UnitTest-Mux-{0}.mov";


        [SetUp]
        public void FixtureSetup()
        {
            _audioJobClient = new AudioJobClient(ffmpegFarmUrl);
            _muxJobClient = new MuxJobClient(ffmpegFarmUrl);
            _statusClient = new StatusClient(ffmpegFarmUrl);
            _sourceMuxTestVideoFile = string.Format(_sourceMuxTestVideoFile, Environment.MachineName);
            _sourceMuxTestAudioFile = string.Format(_sourceMuxTestAudioFile, Environment.MachineName);
            _sourceAudioTestFile = string.Format(_sourceAudioTestFile, Environment.MachineName);
            _targetTestPath = string.Format(_targetTestPath, Environment.MachineName);
            _targetFileAudioPrefix = string.Format(_targetFileAudioPrefix, Environment.MachineName);
            _targetFileMux = string.Format(_targetFileMux, Environment.MachineName);

            if (!File.Exists(MuxTestVideoFile))
                throw new Exception("Test file missing " + MuxTestVideoFile);

            if (!File.Exists(MuxTestAudioFile))
                throw new Exception("Test file missing " + MuxTestAudioFile);

            if (!File.Exists(AudioTestFile))
                throw new Exception("Test file missing " + AudioTestFile);                

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

            if (Directory.Exists(_targetTestPath))
                Directory.Delete(_targetTestPath, true);
        }

        [TearDown]
        public void FixtureTearDown()
        {
            CleanUp();
        }

        [Test]
        public async void StatusTest()
        {
            var sw = new Stopwatch();
            sw.Start();
            var res = await _statusClient.GetAllAsync();
            sw.Stop();
            Console.WriteLine("Status : " + res + " response time " + sw.ElapsedMilliseconds + " ms");
            //Assert.That(res.Count, Is.GreaterThan(0));
        }

        //[Test]
        //public void WorkingNodesCountTest()
        //{
        //    Assert.That(_wfsService.GetWorkingNodes("WorkingMachines").Length, Is.GreaterThanOrEqualTo(1));
        //}

        [Test, Explicit]
        public async void AudioJobTest()
        {
            File.Copy(AudioTestFile, _sourceAudioTestFile);
            Directory.CreateDirectory(_targetTestPath);

            AudioJobRequestModel request = new AudioJobRequestModel()
            {
                DestinationFilenamePrefix = _targetFileAudioPrefix,
                Inpoint = "0",
                Needed = DateTime.UtcNow,
                OutputFolder = _targetTestPath,
                SourceFilename = _sourceAudioTestFile,
                Targets = new System.Collections.ObjectModel.ObservableCollection<AudioDestinationFormat>()
                {
                    new AudioDestinationFormat() { AudioCodec = AudioCodec.MP3, Bitrate = 32, Channels = Channels.Mono, Format = Format.MP3 },
                    new AudioDestinationFormat() { AudioCodec = AudioCodec.MP3, Bitrate = 192, Channels = Channels.Stereo, Format = Format.MP3 },
                    new AudioDestinationFormat() { AudioCodec = AudioCodec.AAC, Bitrate = 32, Channels = Channels.Mono, Format = Format.MP4 },
                    new AudioDestinationFormat() { AudioCodec = AudioCodec.AAC, Bitrate = 192, Channels = Channels.Stereo, Format = Format.MP4 },
                }
            };

            var jobGuid = await _audioJobClient.CreateNewAsync(request);

            bool done;
            int maxCount = 240;
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Starting job {0}", jobGuid.ToString());
            sw.Start();
            FfmpegJobModel job;
            do
            {
                job = await _statusClient.GetAsync(jobGuid);
                var runningTask = job.Tasks.FirstOrDefault(t => t.State == State1.InProgress);
                Console.WriteLine("Jobstatus : {0}, time: {1} ms, filename: {3}, {2:0.##} %", job.State.ToString(), sw.ElapsedMilliseconds, runningTask?.Progress, runningTask?.DestinationFilename);
                if (job.State == State.Failed || job.State == State.Canceled || job.State == State.Unknown)
                    throw new Exception("Error running job. job state: " + job.State.ToString());
                done = job.State == State.Done;
                if (!done)
                {
                    Thread.Sleep(1000);
                }

            } while (!done && maxCount-- > 0);

            Assert.That(done, Is.True);
            Console.WriteLine("Job done, time : {0} ms ({1})", sw.ElapsedMilliseconds, maxCount);
            sw.Stop();
            Assert.That(job.Tasks.Count, Is.EqualTo(request.Targets.Count));
            foreach (var target in job.Tasks)
            {
                string fileFullPath = Path.Combine(_targetTestPath, target.DestinationFilename);
                Console.WriteLine("Checking file: " + fileFullPath);
                Assert.That(File.Exists(fileFullPath), Is.True, string.Format("Expected to find transcoded file @ " + fileFullPath));
            }
        }

        [Test, Explicit]
        public async void MuxingJobTest()
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
            Console.WriteLine("Starting job {0}", jobGuid.ToString());
            sw.Start();
            FfmpegJobModel job;
            do
            {
                job = await _statusClient.GetAsync(jobGuid);
                var runningTask = job.Tasks.FirstOrDefault(t => t.State == State1.InProgress);
                Console.WriteLine("Jobstatus : {0}, time: {1} ms, filename: {3}, {2:0.##} %", job.State.ToString(), sw.ElapsedMilliseconds, runningTask?.Progress, runningTask?.DestinationFilename);
                if (job.State == State.Failed || job.State == State.Canceled || job.State == State.Unknown)
                    throw new Exception("Error running job. job state: " + job.State.ToString());
                done = job.State == State.Done;
                if (!done)
                {
                    Thread.Sleep(1000);
                }

            } while (!done && maxCount-- > 0);

            Assert.That(done, Is.True);
            Console.WriteLine("Job done, time : {0} ms ({1})", sw.ElapsedMilliseconds, maxCount);
            sw.Stop();
            
            foreach (var target in job.Tasks)
            {
                string fileFullPath = Path.Combine(_targetTestPath, target.DestinationFilename);
                Console.WriteLine("Checking file: " + fileFullPath);
                Assert.That(File.Exists(fileFullPath), Is.True, string.Format("Expected to find transcoded file @ " + fileFullPath));
            }
        }
    }
}