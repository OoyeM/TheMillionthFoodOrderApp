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

// Dev mail catcher for the digital receipt email (US-FP-051). Mailpit exposes an SMTP
// server (1025) and a web UI (8025) to inspect captured messages. Ephemeral — no volume.
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "web")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

var mailpitSmtp = mailpit.GetEndpoint("smtp");

var api = builder.AddProject<Projects.TheMillionthFoodOrderApp_Api>("api")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(keycloak)
    .WaitFor(mailpit)
    .WithEnvironment("Email__Host", mailpitSmtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Port", mailpitSmtp.Property(EndpointProperty.Port));

// Mock auth is controlled via Bff/appsettings.Development.json (UseMockAuth).
// Set to false there to use Keycloak OIDC instead.
builder.AddProject<Projects.TheMillionthFoodOrderApp_Bff>("bff")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.Build().Run();
