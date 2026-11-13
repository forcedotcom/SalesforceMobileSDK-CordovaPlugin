const fs = require('node:fs');
const path = require('node:path');

/*
 * Cordova iOS 8.2.0 writes a variable in its platform declaration, but its
 * declaration parser expects a quoted literal. Remove this compatibility hook
 * when the supported Cordova version includes the upstream fix:
 * https://github.com/apache/cordova-ios/issues/1751
 */
module.exports = function (context) {
    const platforms = context.opts.cordova && context.opts.cordova.platforms;
    if (platforms && platforms.length && !platforms.includes('ios')) {
        return;
    }

    const iosRoot = path.join(context.opts.projectRoot, 'platforms', 'ios');
    const apiPath = path.join(iosRoot, 'cordova', 'Api.js');
    if (!fs.existsSync(apiPath)) {
        return;
    }

    const packagePath = require.resolve('cordova-ios/package.json', {
        paths: [path.dirname(apiPath)]
    });
    if (JSON.parse(fs.readFileSync(packagePath, 'utf8')).version !== '8.2.0') {
        return;
    }

    const { Podfile } = require(path.join(path.dirname(packagePath), 'lib', 'Podfile'));
    const parseDeclarations = Podfile.prototype.__parseForDeclarations;
    const probe = "platform :ios, deploymentTarget\nuse_frameworks!\ntarget 'App' do\nend\n";
    if (!parseDeclarations.call(Podfile.prototype, probe)['use_frameworks!']) {
        /*
         * Adapt only the parser input, including already loaded module users.
         * The quoted marker is never written to the Podfile or used as an iOS
         * version. Repeated hooks detect the adapted parser through the probe.
         */
        Podfile.prototype.__parseForDeclarations = function (text) {
            return parseDeclarations.call(this, text.replace(
                /^([ \t]*platform :ios,[ \t]*)deploymentTarget\b/gm,
                "$1'deploymentTarget'"
            ));
        };
        console.log('Applied Cordova iOS 8.2.0 Podfile declaration compatibility');
    }

    const podfilePath = path.join(iosRoot, 'Podfile');
    const podsPath = path.join(iosRoot, 'pods.json');
    if (!fs.existsSync(podfilePath) || !fs.existsSync(podsPath)) {
        return;
    }

    const pods = JSON.parse(fs.readFileSync(podsPath, 'utf8'));
    const text = fs.readFileSync(podfilePath, 'utf8');
    const lines = text.split(/\r?\n/).map(line => line.split('#')[0].trim());
    const missing = ['use_frameworks!', 'inhibit_all_warnings!'].filter(declaration => {
        const record = (pods.declarations || {})[declaration];
        return record && Number.isInteger(record.count) && record.count > 0 &&
            !lines.some(line => line === declaration ||
                line.startsWith(declaration + ' ') ||
                line.startsWith(declaration + '\t') ||
                line.startsWith(declaration + '('));
    });
    if (!missing.length) {
        return;
    }

    /* Restore declarations erased before this hook was installed. Preserve the
     * generated deployment-target expression and all existing pod options. */
    const platformLine = /^[ \t]*platform :ios,[^\r\n]*(?:\r?\n|$)/m;
    if (!platformLine.test(text)) {
        throw new Error('Cannot restore Cordova Podfile declarations: platform line missing');
    }
    const newline = text.includes('\r\n') ? '\r\n' : '\n';
    fs.writeFileSync(podfilePath, text.replace(platformLine, line =>
        (line.endsWith('\n') ? line : line + newline) + missing.join(newline) + newline
    ), 'utf8');
    console.log('Restored recorded Cordova Podfile declarations: ' + missing.join(', '));
};
