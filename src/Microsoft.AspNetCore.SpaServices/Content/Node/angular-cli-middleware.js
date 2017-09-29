// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var childProcess = require('child_process');
var net = require('net');
var readline = require('readline');
var url = require('url');

module.exports = {
    startAngularCliBuilder: function startAngularCliBuilder(callback, appName) {    
        var proc = executeAngularCli([
            'build',
            '-app', appName,
            '--watch'
        ]);
        proc.stdout.pipe(process.stdout);
        waitForLine(proc.stdout, /chunk/).then(function () {
            callback();
        });
    },

    startAngularCliServer: function startAngularCliServer(callback, options) {
        getOSAssignedPortNumber().then(function (portNumber) {
            // Start @angular/cli dev server on private port, and pipe its output
            // back to the ASP.NET host process.
            // TODO: Support streaming arbitrary chunks to host process's stdout
            // rather than just full lines, so we can see progress being logged
            var devServerProc = executeAngularCli([
                'serve',
                '--port', portNumber.toString(),
                '--deploy-url', '/dist/', // Value should come from .angular-cli.json, but https://github.com/angular/angular-cli/issues/7347
                '--extract-css'
            ]);
            devServerProc.stdout.pipe(process.stdout);

            // Wait until the CLI dev server is listening before letting ASP.NET start the app
            console.log('Waiting for @angular/cli service to start...');
            waitForLine(devServerProc.stdout, /open your browser on (http\S+)/).then(function (matches) {
                var devServerUrl = url.parse(matches[1]);
                console.log('@angular/cli service has started on internal port ' + devServerUrl.port);
                callback(null, {
                    Port: parseInt(devServerUrl.port)
                });
            });
        });
    }
};

function waitForLine(stream, regex) {
    return new Promise(function (resolve, reject) {
        var lineReader = readline.createInterface({ input: stream });
        lineReader.on('line', function (line) {
            var matches = regex.exec(line);
            if (matches) {
                lineReader.close();
                resolve(matches);
            }
        });
    });
}

function executeAngularCli(args) {
    var angularCliBin = require.resolve('@angular/cli/bin/ng');
    return childProcess.fork(angularCliBin, args, {
        stdio: [/* stdin */ 'ignore', /* stdout */ 'pipe', /* stderr */ 'inherit', 'ipc']
    });
}

function getOSAssignedPortNumber() {
    return new Promise(function (resolve, reject) {
        var server = net.createServer();
        server.listen(0, 'localhost', function () {
            var portNumber = server.address().port;
            server.close(function () { resolve(portNumber); });
        });
    });
}
