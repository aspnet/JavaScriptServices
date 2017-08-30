var spawn = require('child_process').spawn;
var selenium = spawn('node', ['start-selenium.js']);

selenium.stdout.on('data', function (data) {
    process.stdout.write("[selenium] " + data);
});

selenium.stderr.on('data', function (data) {
    process.stdout.write("[selenium] stderr: " + data);
});

var wdio = spawn('node', ['./node_modules/webdriverio/bin/wdio']);

wdio.stdout.on('data', function (data) {
    process.stdout.write("[wdio] " + data);
});

wdio.stderr.on('data', function (data) {
    process.stdout.write("[wdio] stderr: " + data);
});

wdio.on('close', function (code) {
    process.stdout.write("wdio exited with code " + code);
    selenium.kill();
    if (code != null) {
        process.exit(code);
    }
});

selenium.on('close', function (code) {
    process.stdout.write("selenium exited with code " + code);
    if (!wdio.killed) {
        wdio.kill();
    }
    if (code != null) {
        process.exit(code);
    }
});
