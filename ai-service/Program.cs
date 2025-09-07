using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker; // Add this using directive

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
