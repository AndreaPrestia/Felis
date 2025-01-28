// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography.X509Certificates;
using Felis;
using Felis.Mqtt;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Started Felis.Broker.Mqtt.Console");
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    Console.WriteLine("Cancel event triggered");
    cts.Cancel();
    eventArgs.Cancel = true;
};
    
var port = args.FirstOrDefault(a => a.StartsWith("--port="))?.Split("=")[1] ?? "1883";
var certificateName = args.FirstOrDefault(a => a.StartsWith("--certificate-name="))?.Split("=")[1] ?? "Output.pfx";
var certificatePassword = args.FirstOrDefault(a => a.StartsWith("--certificate-password="))?.Split("=")[1] ?? "Password.1";

var certificate = new X509Certificate2(certificateName, certificatePassword);

var host = Host.CreateDefaultBuilder(args)
    .AddFelisBroker()
    .WithMqtt(int.Parse(port), certificate)
    .Build();

await host.RunAsync();