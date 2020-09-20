using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace XlProcessor
{
    static class Config
    {
        // Get appSettings.json config file
        public static IConfigurationRoot Get()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appSettings.json", true, true).Build();
        }
    }
}
