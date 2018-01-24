using System;
using Microsoft.Extensions.Configuration;

namespace DR.FFMpegClient.Test
{
    internal static class Configuration
    {
        private static readonly IConfigurationRoot CfgRoot;
        static Configuration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json");
            CfgRoot = builder.Build();
        }

        public static string FFMPEGFarmUrl => CfgRoot[nameof(FFMPEGFarmUrl)];
        public static string TestRoot => CfgRoot[nameof(TestRoot)];
    }
}
