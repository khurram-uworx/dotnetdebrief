using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ChatBots>("chatbots");

builder.Build().Run();
