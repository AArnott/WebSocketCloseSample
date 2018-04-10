using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace WebSocketRepro
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            TestServer testServer = InitializeServer();
            var webSocket = await EstablishWebSocket(testServer);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated close", CancellationToken.None);
        }

        private static async Task<WebSocket> EstablishWebSocket(TestServer testServer)
        {
            var testClient = testServer.CreateWebSocketClient();
            return await testClient.ConnectAsync(testServer.BaseAddress, CancellationToken.None);
        }

        private static TestServer InitializeServer()
        {
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder(Array.Empty<string>())
                .UseStartup<Startup>();
            return new TestServer(webHostBuilder);
        }

        private class Startup
        {
            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                app.Use(async (context, next) =>
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var buffer = WebSocket.CreateServerBuffer(4096);
                    var msg = await webSocket.ReceiveAsync(buffer, context.RequestAborted); // wait for the close message
                    await webSocket.CloseAsync(msg.CloseStatus.Value, msg.CloseStatusDescription, context.RequestAborted);
                });
            }
        }
    }
}
