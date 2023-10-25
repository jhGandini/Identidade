using Identidade.Server.Extensions;
using Microsoft.IdentityModel.Logging;
using Serilog;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
.CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var conf = builder.ConfigureAppSettings();

    builder.Host.ConfigureSerilog(); 

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    IdentityModelEventSource.ShowPII = true;

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "StopTheHostException") // https://github.com/dotnet/runtime/issues/60600
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}