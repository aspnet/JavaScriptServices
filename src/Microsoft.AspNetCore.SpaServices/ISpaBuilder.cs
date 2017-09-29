// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices
{
    /// <summary>
    /// Defines a class that provides mechanisms to configure a Single Page Application
    /// being hosted by an ASP.NET server.
    /// </summary>
    public interface ISpaBuilder
    {
        /// <summary>
        /// Gets the <see cref="IApplicationBuilder"/> for the host application.
        /// </summary>
        IApplicationBuilder AppBuilder { get; }

        /// <summary>
        /// Gets the path to the SPA's default file. By default, this is the file
        /// index.html within the <see cref="PublicPath"/>.
        /// </summary>
        PathString DefaultFilePath { get; }

        /// <summary>
        /// Gets the URL path, relative to the application's <c>PathBase</c>, from which
        /// the SPA files are served.
        ///</summary>
        ///<example>
        /// If the SPA files are located in <c>wwwroot/dist</c>, then the value would
        /// usually be <c>"dist"</c>, because that is the URL prefix from which clients
        /// can request those files.
        ///</example>
        string PublicPath { get; }

        /// <summary>
        /// Gets a key/value collection that can be used to share data between SPA middleware.
        /// </summary>
        IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets a <see cref="Task"/> that represents the completion of all registered
        /// SPA startup tasks.
        /// </summary>
        Task StartupTasks { get; }

        /// <summary>
        /// Registers a task that represents part of SPA startup process. Middleware
        /// may choose to wait for these tasks to complete before taking some action.
        /// </summary>
        /// <param name="task">The <see cref="Task"/>.</param>
        void AddStartupTask(Task task);
    }
}
