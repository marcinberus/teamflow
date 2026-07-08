using TeamFlow.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceConfiguration(builder.Configuration);

var app = builder.Build();

app.UsePipelineConfiguration();

app.Run();

public partial class Program { }

