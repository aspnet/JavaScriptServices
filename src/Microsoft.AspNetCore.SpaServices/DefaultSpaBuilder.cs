// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class DefaultSpaBuilder : ISpaBuilder
    {
        private readonly object _startupTasksLock = new object();

        public DefaultSpaBuilder(IApplicationBuilder appBuilder, string publicPath, PathString defaultFilePath)
        {
            AppBuilder = appBuilder;
            DefaultFilePath = defaultFilePath;
            Properties = new Dictionary<object, object>();
            PublicPath = publicPath;
        }

        public IApplicationBuilder AppBuilder { get; }
        public PathString DefaultFilePath { get; }
        public IDictionary<object, object> Properties { get; }
        public string PublicPath { get; }
        public Task StartupTasks { get; private set; } = Task.CompletedTask;

        public void AddStartupTask(Task task)
        {
            lock (_startupTasksLock)
            {
                StartupTasks = Task.WhenAll(StartupTasks, task);
            }
        }
    }
}
