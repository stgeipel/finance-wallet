using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> username = builder.AddParameter("postgresUsername", secret: true);
IResourceBuilder<ParameterResource> password = builder.AddParameter("postgresPassword", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("Finance-Wallet-Postgres", username, password, port: 5432)
    .WithDataVolume(name: "finance-wallet-postgres-data")
    .WithPgWeb();

IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("finance-wallet");

builder.AddProject<DatabaseMigration>("migration")
    .WithReference(database);


IResourceBuilder<ProjectResource> apiService = builder
    .AddProject<Server>("api")
    .WithReference(database);

builder.AddProject<Client>("web")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

await builder.Build().RunAsync();