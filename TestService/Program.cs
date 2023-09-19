using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestService
{
    class Program
    {
        static DateTime startTime;

        static void Main(string[] args)
        {
            startTime = DateTime.Now;

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("TEST_SERVICE_PORT") ?? "8080";
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context => await DoHostname(context));
                            endpoints.MapGet("/echo", async context => await DoEcho(context));
                            endpoints.MapPost("/echo", async context => await DoEcho(context));
                            endpoints.MapGet("/echoheaders", async context => await DoEchoHeaders(context));
                            endpoints.MapGet("/hostname", async context => await DoHostname(context));
                            endpoints.MapGet("/fqdn", async context => await DoFqdn(context));
                            endpoints.MapGet("/ip", async context => await DoIp(context));
                            endpoints.MapGet("/env", async context => await DoEnv(context));
                            endpoints.MapGet("/healthz", async context => await DoHealthz(context));
                            endpoints.MapGet("/healthz-fail", async context => await DoFailHealthz(context));
                            endpoints.MapGet("/exit/{exitCode:int}", async context => await DoExit(context));
                        });
                    });
                })
                .Build();

            host.Run();
        }

        static async Task DoEcho(HttpContext context)
        {
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                var requestBody = await streamReader.ReadToEndAsync();
                await context.Response.WriteAsync(requestBody);
            }
        }

        static async Task DoEchoHeaders(HttpContext context)
        {
            foreach (var (key, value) in context.Request.Headers)
            {
                await context.Response.WriteAsync($"{key}={value}\n");
            }
        }

        static async Task DoHostname(HttpContext context)
        {
            var hostname = Dns.GetHostName();
            await context.Response.WriteAsync($"{hostname}\n");
        }

        static async Task DoEnv(HttpContext context)
        {
            foreach (var environmentVariable in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
            {
                await context.Response.WriteAsync($"{environmentVariable.Key}={environmentVariable.Value}\n");
            }
        }

        static async Task DoIp(HttpContext context)
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                var unicastAddresses = ipProperties.UnicastAddresses;

                foreach (var unicastAddress in unicastAddresses)
                {
                    var ipAddress = unicastAddress.Address;
                    await context.Response.WriteAsync($"{ipAddress}\n");
                }
            }
        }

        static async Task DoFqdn(HttpContext context)
        {
            var hostname = Dns.GetHostName();
            var ipAddresses = Dns.GetHostAddresses(hostname);

            foreach (var ipAddress in ipAddresses)
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var ipString = ipAddress.ToString();
                    var hostEntry = Dns.GetHostEntry(ipString);

                    if (hostEntry != null && hostEntry.HostName != null)
                    {
                        var fqdn = hostEntry.HostName.Split('.').FirstOrDefault();
                        await context.Response.WriteAsync($"{fqdn}\n");
                        return;
                    }
                }
            }

            await context.Response.WriteAsync($"{hostname}\n");
        }

        static async Task DoExit(HttpContext context)
        {
            if (int.TryParse(context.Request.RouteValues["exitCode"].ToString(), out int exitCode))
            {
                Environment.Exit(exitCode);
            }
        }

        static async Task DoHealthz(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync($"Uptime {DateTime.Now - startTime}\n");
            await context.Response.WriteAsync("OK\n");
        }

        static async Task DoFailHealthz(HttpContext context)
        {
            double failAt = 10.0;
            double upTime = (DateTime.Now - startTime).TotalSeconds;

            if (upTime < failAt)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync($"still OK, {failAt - upTime:F1} seconds before failing\n");
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync($"failed since {upTime - failAt:F1} seconds\n");
            }

            await context.Response.WriteAsync($"Uptime {upTime:F1}\n");
        }
    }
}
