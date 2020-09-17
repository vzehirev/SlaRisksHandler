using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace XlProcessor
{
    static class Config
    {
        public static IConfigurationRoot Get()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appSettings.json", true, true).Build();
        }
    }
}
