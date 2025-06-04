namespace testpool;

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

class Program
{
    static async Task Main(string[] args)
    {            
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            IHostEnvironment env = context.HostingEnvironment;
            // Add configuration sources if needed
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);            

            if (env.IsDevelopment())
            {
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddUserSecrets<Program>(optional: true);
            }
            
            config.AddEnvironmentVariables();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(option => 
            {
                option.IncludeScopes = true;
                option.TimestampFormat = "yyyy-mm-dd hh:mm:ss ";
                option.SingleLine = true;
            });
            logging.AddDebug();
        });
        
        builder.ConfigureServices(
            services =>
            {
                // Register your services here
                services.AddSingleton<IFileJobStorageRepository, FileJobStorage>();
                services.AddTransient<IQueue, QueueManager>();

                services.AddTransient<IQueue2<MyRecord>, Queue<MyRecord>>(
                    sp => new Queue<MyRecord>(
                        sp.GetRequiredService<ILogger<Queue<MyRecord>>>(),
                        sp.GetRequiredService<IConfiguration>()["ServiceBus:RecordTopicName"] ?? "defaultTopic"
                    )
                );

                services.AddTransient<IQueue2<ApiEvent>, Queue<ApiEvent>>(
                    sp => new Queue<ApiEvent>(
                        sp.GetRequiredService<ILogger<Queue<ApiEvent>>>(),
                        sp.GetRequiredService<IConfiguration>()["ServiceBus:ApiEventTopicName"] ?? "defaultApiEventTopic"
                    )
                );


                // services.AddKeyedTransient<IQueue, QueueManager>(
                //     "file",
                //     (sp, key) => new QueueManager(
                //         sp.GetRequiredService<ILogger<QueueManager>>(),
                //         sp.GetRequiredService<IConfiguration>(),
                //         (string)key
                //     )
                // );


                services.AddHostedService<FileIngestorService>();
                services.AddHostedService<FileProcessorService>();
                
            }
        );

        IHost host = builder.Build();
        

        await host.RunAsync();        
    }
}
