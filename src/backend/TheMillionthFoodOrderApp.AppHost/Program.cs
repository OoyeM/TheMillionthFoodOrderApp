var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.TheMillionthFoodOrderApp_Api>("api");

builder.AddProject<Projects.TheMillionthFoodOrderApp_Bff>("bff")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
