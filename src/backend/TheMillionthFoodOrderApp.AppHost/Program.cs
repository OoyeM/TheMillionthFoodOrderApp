var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("sql-data")
    .AddDatabase("platform");

var api = builder.AddProject<Projects.TheMillionthFoodOrderApp_Api>("api")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.TheMillionthFoodOrderApp_Bff>("bff")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("Authentication__UseMockAuth", "true");

builder.Build().Run();
