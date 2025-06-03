using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Allow CORS for your Blazor app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

// Print endpoint
app.MapPost("/print", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        var printJob = JsonSerializer.Deserialize<PrintJob>(requestBody);

        if (printJob == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid print job");
            return;
        }

        // ESC/POS Commands
        byte[] initPrinter = new byte[] { 0x1B, 0x40 };         // Initialize printer
        byte[] cutPaper = new byte[] { 0x1D, 0x56, 0x41, 0x00 }; // Full cut after feeding
        
        using var printerStream = new FileStream("LPT1", FileMode.OpenOrCreate);
        // Send init command
        printerStream.Write(initPrinter, 0, initPrinter.Length);
        
        // Send content
        byte[] contentBytes = Encoding.ASCII.GetBytes(printJob.Content);
        printerStream.Write(contentBytes, 0, contentBytes.Length);
        
        // Send line feeds
        for (int i = 0; i < printJob.Options.FeedLines; i++)
        {
            printerStream.WriteByte(0x0A); // Line feed
        }
        
        // Cut paper if requested
        if (printJob.Options.AutoCut)
        {
            printerStream.Write(cutPaper, 0, cutPaper.Length);
        }
        
        await context.Response.WriteAsync("Print job sent successfully");
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"Printing error: {ex.Message}");
    }
});

app.Run("http://localhost:9100");

public class PrintJob
{
    public string Content { get; set; } = "";
    public PrintOptions Options { get; set; } = new PrintOptions();
}

public class PrintOptions
{
    public bool AutoCut { get; set; } = true;
    public int FeedLines { get; set; } = 5;
}