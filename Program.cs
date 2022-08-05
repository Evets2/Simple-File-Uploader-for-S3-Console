using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using S3Uploader.Services;
using S3Uploader.Interface;
using Serilog;

namespace S3Uploader
{
    class Program
    {
        public static IConfigurationRoot _configuration;
        
        public static IServiceProvider serviceProvider;

        public static async Task Main(string[] args)
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();

            Log.Information($"Program Startup with the configuration at : {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}");
            await RunAsync();
        }

        public static async Task RunAsync()
        {
            var s3Service = serviceProvider.GetService<IS3Service>();
            await s3Service.UploadFileToS3();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Build Configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
                .AddJsonFile("appsettings.json", optional:false, reloadOnChange: true)
                .Build();

            // Add access to generic IConfigurationRoot
            services.AddSingleton<IConfigurationRoot>(_configuration);
            services.AddScoped<IS3Service, S3Service>();

            // Add Logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();

            services.AddLogging(configure => configure.AddSerilog());
            
        }
    }
}
