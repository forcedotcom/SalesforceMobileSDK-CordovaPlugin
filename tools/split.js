var fs = require('fs');

var cordovaForcePath = process.argv[2];
fs.readFile(cordovaForcePath, 'utf8', function (err, data) { 
    var lines = data.split(/\n/);

    var copyrightVersionLines = [];
    var i = 0;

    // Copyright + Version
    while (i < lines.length) {
        line = lines[i++];
        copyrightVersionLines.push(line);

        var matchVersion = line.match(/var SALESFORCE_MOBILE_SDK_VERSION =/);
        if (matchVersion) {
            break;
        }
    }

    // Defines
    while (i < lines.length) {
        var currentFileName = null;
        var currentFileLines = [];
        while (i < lines.length) {
            line = lines[i++];

            // Done with block
            var matchEndDefine = line.match(/^\}\);/);
            if (matchEndDefine) {
                break;
            }

            // In block
            if (currentFileName != null) {
                currentFileLines.push(line.substring(4));
            }

            // Starting block
            var matchDefine = line.match(/cordova.define\("(.*)"/);
            if (matchDefine) {
                currentFileName = matchDefine[1] + '.js';
                currentFileLines = copyrightVersionLines.slice(0); // clone
            }
        }
        if (currentFileName) {
            console.log('Creating ' + currentFileName);
            fs.writeFile('www/' + currentFileName, currentFileLines.join('\n'), function (err) { 
                if (err) { 
                    console.log(err); 
                } 
            });
        }
    }

});
