using DotReview.AI.Services;
using DotReview.Application.Interface;
using DotReview.Application.Services;
using DotReview.Application.Services.Rules;
using DotReview.Application.Services.Scoring;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<
    ICodeReviewService,
    CodeReviewService>();
builder.Services.AddScoped<
    ICodeReviewService,
    CodeReviewService>();

builder.Services.AddScoped<
    IGeminiService,
    GeminiService>();
builder.Services.AddScoped<ICodeReviewRule, AvoidUnfilteredToListRule>();
builder.Services.AddScoped<
    ICodeReviewScoringService,
    CodeReviewScoringService>();
builder.Services.AddScoped<ICodeReviewRule, SqlInjectionRule>();
builder.Services.AddScoped<
    ICodeReviewRule,
    NPlusOneQueryRule>();

builder.Services.AddScoped<
    IIssueFingerprintService,
    IssueFingerprintService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
