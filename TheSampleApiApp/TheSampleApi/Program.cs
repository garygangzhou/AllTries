using TheSampleApi.Endpoints;
using TheSampleApi.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.AddDependencies();

var app = builder.Build();

app.UseOpenApi();
app.UseHttpsRedirection();
app.UseCorsConfig(); //allow external request 
app.UseHealthChecks();

app.AddRootEndpoints();
app.AddErrorEndpoints();
app.AddCourseEndpoints();

app.Run();
