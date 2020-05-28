using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace WebApplication
{
    public class LoggingFactory
    {
        private readonly IConfiguration _configuration;

        public LoggingFactory(IConfiguration configuration) => _configuration = configuration;

        public ILogger CreateLogger(string environment)
        {
            var settings = _configuration.GetSection("SeqServer");
            var seqServerUrl = settings["Url"] ?? "http://localhost:5341";
            var seqServerApiKey = settings["ApiKey"] ?? "";

            var assembly = Assembly.GetEntryAssembly() ?? throw new NullReferenceException();
            var assemblyName = assembly.GetName().Name;
            var assemblyVersion = assembly.GetName().Version;

            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

            var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .If(environment.ToLower() == "prod", _ => _.MinimumLevel.Override("Microsoft", LogEventLevel.Warning))
                    .If(environment.ToLower() == "prod", _ => _.MinimumLevel.Override("System", LogEventLevel.Warning))
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProcessId()
                    //.Enrich.WithExceptionDetails()
                    .Enrich.WithCorrelationIdHeader()
                    .Enrich.WithProperty("ApplicationName", assemblyName)
                    .Enrich.WithProperty("ApplicationVersion", assemblyVersion)
                    .Enrich.WithProperty("EnvironmentType", environment)
                    .If(environment.ToLower() != "prod", c => c.WriteTo.Console(theme: SystemConsoleTheme.Colored))
                    //.WriteTo.ApplicationInsights(new TraceTelemetryConverter())
                    .WriteTo.Seq(
                        seqServerUrl,
                        compact: true,
                        apiKey: seqServerApiKey,
                        controlLevelSwitch: levelSwitch)
                    ;

            return loggerConfig.CreateLogger();
        }
    }
}
