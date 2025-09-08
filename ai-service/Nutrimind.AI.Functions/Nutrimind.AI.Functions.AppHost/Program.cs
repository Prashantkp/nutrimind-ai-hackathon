var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureFunctionsProject<Projects.Nutrimind_AI_Functions>("nutrimind-ai-functions");

builder.Build().Run();
