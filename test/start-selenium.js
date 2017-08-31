var seleniumStandalone = require('selenium-standalone');
var which = require('which');
var path = require('path');
var fs = require('fs');

var isWindows = /^win/.test(process.platform);
var javaExeName = isWindows ? 'java.exe' : 'java';

var java = which.sync(javaExeName, { nothrow: true });

if (!java && process.env['JAVA_HOME']) {
    java = path.join(process.env['JAVA_HOME'], 'bin', javaExeName);
}

if (!java && isWindows) {
    var searchPaths = [
        path.join(process.env['ProgramFiles'], 'Java'),
        path.join(process.env['ProgramFiles(x86)'], 'Java'),
    ];

    for (var x in searchPaths) {
        var searchPath = searchPaths[x];
        var paths = fs.readdirSync(searchPath)
            .map(function (subdir) {
                return path.join(searchPath, subdir, 'bin', javaExeName);
            })
            .filter(function (canditate) {
                return fs.existsSync(canditate);
            });

        if (paths && paths.length > 0) {
            // pick the last one, which is likely to be the most recent JDK
            java = paths[paths.length - 1];
            break;
        }
    }
}

if (!java) {
    console.log("Could not find Java automatically");
} else {
    console.log("Using java = " + java);
}

var installOptions = {
    progressCb: function(totalLength, progressLength, chunkLength) {
        var percent = 100 * progressLength / totalLength;
        console.log('Installing selenium-standalone: ' + percent.toFixed(0) + '%');
    }
};

console.log('Installing selenium-standalone...');
seleniumStandalone.install(installOptions, function(err) {
    if (err) {
        throw err;
    }

    var startOptions = {
        javaPath: java,
        javaArgs: ['-Djna.nosys=true'],
        spawnOptions: { stdio: 'inherit' }
    };

    console.log('Starting selenium-standalone...');
    seleniumStandalone.start(startOptions, function(err, seleniumProcess) {
        if (err) {
            throw err;
        }

        console.log('Started Selenium server');

    });
});
