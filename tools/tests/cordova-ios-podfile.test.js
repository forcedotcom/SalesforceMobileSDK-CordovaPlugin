const { test, beforeEach, afterEach } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const hook = require('../cordova-ios-podfile');
const cordovaRoot = path.dirname(require.resolve('cordova-ios/package.json'));
const { Podfile } = require('cordova-ios/lib/Podfile');
const originalParser = Podfile.prototype.__parseForDeclarations;
const parserFile = path.join(cordovaRoot, 'lib', 'Podfile.js');
const originalSource = fs.readFileSync(parserFile);
let root;
let ios;
let context;

beforeEach(() => {
    Podfile.prototype.__parseForDeclarations = originalParser;
    root = fs.mkdtempSync(path.join(os.tmpdir(), 'cordova-podfile-test-'));
    ios = path.join(root, 'platforms', 'ios');
    fs.mkdirSync(path.join(ios, 'cordova'), { recursive: true });
    fs.writeFileSync(path.join(ios, 'cordova', 'Api.js'), "module.exports = require('cordova-ios');\n");
    fs.mkdirSync(path.join(root, 'node_modules'), { recursive: true });
    fs.symlinkSync(cordovaRoot, path.join(root, 'node_modules', 'cordova-ios'), 'dir');
    context = { opts: { projectRoot: root, cordova: { platforms: ['ios'] } } };
});

afterEach(() => {
    Podfile.prototype.__parseForDeclarations = originalParser;
    assert.deepEqual(fs.readFileSync(parserFile), originalSource);
    fs.rmSync(root, { recursive: true, force: true });
});

function seed(declarations = '', newline = '\n') {
    const text = [
        "deploymentTarget = '18.0'",
        'platform :ios, deploymentTarget',
        declarations,
        "target 'App' do",
        "  pod 'Example', :path => '../Example'",
        'end', ''
    ].join('\n').replace(/\n/g, newline);
    fs.writeFileSync(path.join(ios, 'Podfile'), text);
    return text;
}

function record(declarations) {
    fs.writeFileSync(path.join(ios, 'pods.json'), JSON.stringify({ declarations }));
}

function fakeVersion(version) {
    const folder = path.join(root, 'node_modules', 'cordova-ios');
    fs.unlinkSync(folder);
    fs.mkdirSync(folder);
    fs.writeFileSync(path.join(folder, 'package.json'), JSON.stringify({ version }));
}

test('repairs an already loaded real parser and preserves declarations on write', () => {
    seed('use_frameworks!\ninhibit_all_warnings!');
    assert.deepEqual(originalParser.call(Podfile.prototype, fs.readFileSync(path.join(ios, 'Podfile'), 'utf8')), {});
    hook(context);
    const podfile = new Podfile(path.join(ios, 'Podfile'), 'App', '18.0');
    assert.deepEqual(Object.keys(podfile.declarations), ['use_frameworks!', 'inhibit_all_warnings!']);
    podfile.write();
    const text = fs.readFileSync(path.join(ios, 'Podfile'), 'utf8');
    assert.match(text, /platform :ios, deploymentTarget/);
    assert.match(text, /deploymentTarget = '18.0'/);
    assert.match(text, /use_frameworks!/);
    assert.match(text, /:path => '\.\.\/Example'/);
    assert.doesNotMatch(text, /platform :ios, 'deploymentTarget'/);
});

test('does not wrap twice or rewrite an intact Podfile', () => {
    const text = seed('use_frameworks!');
    record({ 'use_frameworks!': { count: 1 } });
    hook(context);
    const parser = Podfile.prototype.__parseForDeclarations;
    hook(context);
    assert.equal(Podfile.prototype.__parseForDeclarations, parser);
    assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), text);
});

test('restores recorded declarations in a damaged Podfile without changing its target or pods', () => {
    const text = seed();
    record({ 'use_frameworks!': { count: 1 }, 'inhibit_all_warnings!': { count: 2 } });
    hook(context);
    const restored = fs.readFileSync(path.join(ios, 'Podfile'), 'utf8');
    assert.equal(restored, text.replace('platform :ios, deploymentTarget\n',
        'platform :ios, deploymentTarget\nuse_frameworks!\ninhibit_all_warnings!\n'));
    hook(context);
    assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), restored);
});

test('preserves CRLF and recognizes existing declarations with comments', () => {
    seed('use_frameworks! # existing', '\r\n');
    record({ 'use_frameworks!': { count: 1 }, 'inhibit_all_warnings!': { count: 1 } });
    hook(context);
    const text = fs.readFileSync(path.join(ios, 'Podfile'), 'utf8');
    assert.equal((text.match(/use_frameworks!/g) || []).length, 1);
    assert.equal(text.replace(/\r\n/g, '').includes('\n'), false);
    assert.match(text, /inhibit_all_warnings!\r\n/);
    assert.ok(Object.keys(new Podfile(path.join(ios, 'Podfile'), 'App', '18.0').declarations)
        .some(line => line.trim() === 'inhibit_all_warnings!'));
});

test('does not restore removed, unknown or unrecorded declarations', () => {
    const text = seed();
    record({ 'use_frameworks!': { count: 0 }, 'inhibit_all_warnings!': { count: 0 }, "puts 'unrelated'": { count: 1 } });
    hook(context);
    assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), text);
});

test('preserves an existing framework linkage option', () => {
    const text = seed('use_frameworks! :linkage => :static');
    record({ 'use_frameworks!': { count: 1 } });
    hook(context);
    assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), text);
    const podfile = new Podfile(path.join(ios, 'Podfile'), 'App', '18.0');
    podfile.write();
    const rewritten = fs.readFileSync(path.join(ios, 'Podfile'), 'utf8');
    assert.match(rewritten, /use_frameworks! :linkage => :static/);
    assert.equal((rewritten.match(/use_frameworks!/g) || []).length, 1);
});

test('preserves old quoted platform lines and arbitrary existing declarations', () => {
    hook(context);
    const text = "platform :ios, '18.0'\nuse_frameworks!\ncustom_setting!\ntarget 'App' do\nend\n";
    assert.deepEqual(Object.keys(Podfile.prototype.__parseForDeclarations(text)), ['use_frameworks!', 'custom_setting!']);
});

test('skips an upstream-corrected parser without replacing it', () => {
    const fixedParser = text => originalParser.call(Podfile.prototype,
        text.replace('platform :ios, deploymentTarget', "platform :ios, '18.0'"));
    Podfile.prototype.__parseForDeclarations = fixedParser;
    hook(context);
    assert.equal(Podfile.prototype.__parseForDeclarations, fixedParser);
});

for (const version of ['8.1.1', '8.2.1']) {
    test('leaves Cordova ' + version + ' untouched', () => {
        fakeVersion(version);
        const text = seed();
        record({ 'use_frameworks!': { count: 1 } });
        hook(context);
        assert.equal(Podfile.prototype.__parseForDeclarations, originalParser);
        assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), text);
    });
}

test('skips Android-only operations', () => {
    context.opts.cordova.platforms = ['android'];
    hook(context);
    assert.equal(Podfile.prototype.__parseForDeclarations, originalParser);
});

test('skips projects whose iOS platform is not installed yet', () => {
    fs.unlinkSync(path.join(ios, 'cordova', 'Api.js'));
    hook(context);
    assert.equal(Podfile.prototype.__parseForDeclarations, originalParser);
});

test('reports an unrecognized damaged Podfile instead of rewriting unrelated content', () => {
    fs.writeFileSync(path.join(ios, 'Podfile'), '# custom unrecognized Podfile\n');
    record({ 'use_frameworks!': { count: 1 } });
    assert.throws(() => hook(context), /platform line missing/);
    assert.equal(fs.readFileSync(path.join(ios, 'Podfile'), 'utf8'), '# custom unrecognized Podfile\n');
});
