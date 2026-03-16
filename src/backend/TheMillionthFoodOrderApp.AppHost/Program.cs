var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("sql-data")
    .AddDatabase("platform");

var keycloak = builder.AddKeycloak("keycloak")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("keycloak-data")
    .WithRealmImport("keycloak");

var api = builder.AddProject<Projects.TheMillionthFoodOrderApp_Api>("api")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(keycloak);

builder.AddProject<Projects.TheMillionthFoodOrderApp_Bff>("bff")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__UseMockAuth", "true");

builder.Build().Run();
