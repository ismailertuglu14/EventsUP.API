﻿using System.Net;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using StackExchange.Redis;
using Topluluk.Services.User.Model.Mapper;
using Topluluk.Services.User.Services.Core;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Middleware;
using Newtonsoft.Json;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IMongoClient>(new MongoClient());
builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AllowNullCollections = true;
    cfg.AddProfile(new GeneralMapper());
});
builder.Services.AddSingleton(mapperConfig.CreateMapper());
IConfiguration configuration = builder.Configuration;

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(configuration.GetSection("RabbitMQ:Host").Value), host =>
        {
            host.Username(configuration.GetSection("RabbitMQ:Username").Value);
            host.Password(configuration.GetSection("RabbitMQ:Password").Value);
        });

    });
});

builder.Services.AddMassTransitHostedService();
builder.Services.AddInfrastructure();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = false,
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidateAudience = false,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT Secret key can not bu null.")))
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context => {
            context.HandleResponse();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var errorResponse = new Topluluk.Shared.Dtos.Response<string>
            {
                Data = null!,
                StatusCode = Topluluk.Shared.Enums.ResponseStatus.Unauthorized,
                IsSuccess = false,
                Errors = new List<string> { "Unauthorized access denied." }
            };
            var json = JsonConvert.SerializeObject(errorResponse);
            await context.Response.WriteAsync(json);
        },
        OnAuthenticationFailed = async context =>
        {
            context.Fail("Unauthorized access denied1.");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var errorResponse = new Topluluk.Shared.Dtos.Response<string>
            {
                Data = null!,
                StatusCode = Topluluk.Shared.Enums.ResponseStatus.Unauthorized,
                IsSuccess = false,
                Errors = new List<string> { "Unauthorized access denied2."}
            };
            var json = JsonConvert.SerializeObject(errorResponse);
            await context.Response.WriteAsync(json);
        },
        
        
    };
});
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication(); // For jwt
app.UseAuthorization();
app.UseCors();
app.MapControllers();
app.Run();
