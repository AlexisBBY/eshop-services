using BuildingBlocks.Exceptions.Handler;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Order.API.Application;
using Order.API.Data;
using Order.API.Services;

// El driver de MongoDB (v3+) requiere indicar explicitamente como serializar Guid.
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<IOrderRepository, MongoOrderRepository>();

builder.Services.AddHttpClient<IBasketApiClient, BasketApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BasketApi"]!);
});

builder.Services.AddHttpClient<ICatalogApiClient, CatalogApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"]!);
});

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.MapCarter();
app.UseExceptionHandler(options => { });

app.Run();