﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;

namespace ConsoleApplication
{
    // This project is a micro-benchmark for .NET->Node RPC via NodeServices. It doesn't reflect
    // real-world usage patterns (you're not likely to make hundreds of sequential calls like this),
    // but is a starting point for comparing the overhead of different hosting models and transports.
    public class Program
    {
        public static void Main(string[] args) {
            using (var nodeServices = CreateNodeServices(Configuration.DefaultNodeHostingModel)) {
                MeasureLatency(nodeServices).Wait();
            }
        }

        private static async Task MeasureLatency(INodeServices nodeServices) {
            // Ensure the connection is open, so we can measure per-request timings below
            var response = await nodeServices.Invoke<string>("latencyTest", "C#");
            Console.WriteLine(response);

            // Now perform a series of requests, capturing the time taken
            const int requestCount = 100;
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < requestCount; i++) {
                await nodeServices.Invoke<string>("latencyTest", "C#");
            }

            // Display results
            var elapsedSeconds = (float)watch.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine("\nTotal time: {0:F2} milliseconds", 1000 * elapsedSeconds);
            Console.WriteLine("\nTime per invocation: {0:F2} milliseconds", 1000 * elapsedSeconds / requestCount);
        }

        private static INodeServices CreateNodeServices(NodeHostingModel hostingModel) {
            return Configuration.CreateNodeServices(new NodeServicesOptions {
                HostingModel = hostingModel,
                ProjectPath = Directory.GetCurrentDirectory(),
                WatchFileExtensions = new string[] {} // Don't watch anything
            });
        }
    }
}
