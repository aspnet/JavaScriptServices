// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var childProcess = require('child_process');
var net = require('net');
var readline = require('readline');
var url = require('url');
var timeoutSeconds = 50;

module.exports = {
    startAngularCliBuilder: function startAngularCliBuilder(callback, appName) {    
        var proc = executeAngularCli([
            'build',
            '-app', appName,
            '--watch'
        ]);
        proc.stdout.pipe(process.stdout);
        waitForLine(proc.stdout, /chunk/, function () { callback() }, timeoutSeconds * 1000, function () {
            callback('The ng build process timed out after ' + timeoutSeconds + ' seconds. Check the output log for error information.');
        });
    },

    startAngularCliServer: function startAngularCliServer(callback, options) {
        getOSAssignedPortNumber(function (err, portNumber) {
            if (err) {
                callback(err);
                return;
            }

            // Start @angular/cli dev server on private port, and pipe its output
            // back to the ASP.NET host process
            var devServerProc = executeAngularCli([
                'serve',
                '--port', portNumber.toString(),
                '--deploy-url', '/dist/', // Value should come from .angular-cli.json, but https://github.com/angular/angular-cli/issues/7347
                '--extract-css'
            ]);
            devServerProc.stdout.pipe(process.stdout);

            // Wait until the CLI dev server is listening before letting ASP.NET start the app
            console.log('Waiting for @angular/cli service to start...');
            waitForLine(devServerProc.stdout, /open your browser on (http\S+)/, function (matches) {
                var devServerUrl = url.parse(matches[1]);
                console.log('@angular/cli service has started on internal port ' + devServerUrl.port);
                callback(null, {
                    Port: parseInt(devServerUrl.port)
                });
            }, timeoutSeconds * 1000, function () {
                callback('The @angular/cli service did not start within the timeout period of ' + timeoutSeconds + ' seconds. Check the output log for error information.');
            });
        });
    }
};

function waitForLine(stream, regex, successCallback, timeoutMilliseconds, timeoutCallback) {
    var lineReader = readline.createInterface({ input: stream });
    var listener = function (line) {
        var matches = regex.exec(line);
        if (matches) {
            lineReader.removeListener('line', listener);
            if (timeoutId !== null) {
                clearTimeout(timeoutId);
            }
            successCallback(matches);
        }
    };

    var timeoutId = null;
    if (timeoutMilliseconds > 0) {
        timeoutId = setTimeout(function () {
            lineReader.removeListener('line', listener);
            if (timeoutCallback) {
                timeoutCallback();
            }
        }, timeoutMilliseconds);
    }

    lineReader.addListener('line', listener);
}

function executeAngularCli(args) {
    var angularCliBin = require.resolve('@angular/cli/bin/ng');
    return childProcess.fork(angularCliBin, args, {
        stdio: [/* stdin */ 'ignore', /* stdout */ 'pipe', /* stderr */ 'inherit', 'ipc']
    });
}

function getOSAssignedPortNumber(callback) {
    var server = net.createServer();
    server.listen(0, 'localhost', function (err) {
        if (err) {
            callback(err);
        } else {
            var portNumber = server.address().port;
            server.close(function () { callback(null, portNumber); });
        }
    });
}
