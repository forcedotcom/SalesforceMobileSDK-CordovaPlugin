# Android status-bar compatibility

Cordova Android 15.1.0 provides `window.statusbar`. The optional
`cordova-plugin-statusbar` provides the differently named `window.StatusBar`.
When the optional plugin is present, the platform API forwards several operations
to it. These APIs are not interchangeable across every platform or operation.

The generic Mobile SDK hybrid local and remote templates install the Salesforce
plugin without adding `cordova-plugin-statusbar`. An application or a custom
template can add it separately. Inspect the generated app's installed plugins;
the platform API's presence alone does not mean the optional plugin is installed.
Keep an existing plugin dependency until its application uses have been reviewed.

## Fractional-alpha limitation

The unmodified Cordova Android 15.1.0 JavaScript has a color conversion issue when
the platform API forwards a fractional CSS alpha value to the optional plugin.
For example, the computed color `rgba(255, 0, 153, 0.5)` is forwarded as
`#7f.8ff0099`, which is not a valid Android hex color. This is upstream platform
JavaScript, not a change to the Salesforce plugin.

Upstream tracking: [Cordova Android issue 2048](https://github.com/apache/cordova-android/issues/2048).
Revisit this limitation when adopting a Cordova Android release containing a
verified fix.

Without the optional plugin, the same call passes numeric RGBA components to
Cordova's native `SystemBar` plugin. That native implementation rounds the alpha
component. The malformed hex conversion belongs to the optional forwarding path.
A focused runtime check on Android API 34 and 36 registered the released
statusbar plugin 4.0.0 Java and JavaScript sources in a Mobile SDK test fixture.
The optional plugin received the malformed color, logged the parse error and
retained the preceding native window color without crashing. An opaque platform
color, an explicitly valid `#80ff0099` color through the optional plugin API,
and its show/hide window flags updated as expected. These observations cover
native window state, not pixel rendering, every edge-to-edge configuration or
installation into an application generated from a custom template.

Mobile SDK 14.1 intentionally ships with this documented limitation, preserving
the published Cordova Android 15.1.0 JavaScript and existing plugin choices.
It does not remove the optional plugin or add a vendor patch. An affected Android
app can avoid fractional alpha on the forwarding path, or use the optional
plugin's own color API with an explicitly valid Android color after checking its
cross-platform behavior. Validate the app's actual UI and edge-to-edge layout on
supported Android versions before selecting an application workaround.

## Review before release

Record the installed plugin version and all application uses of both APIs.
Exercise opaque and translucent colors, light and dark icons, visibility,
fullscreen, rotation and WebView
recreation. Test the configuration with the optional plugin separately when the
app uses it. A JavaScript forwarding probe does not prove native rendering.

If a patch, removal or API migration is needed, select that work explicitly and
keep the native and JavaScript distributions aligned. A dependency removal can
affect iOS and downstream custom templates even when a generic Android template
does not install the plugin.

See the Apache [statusbar plugin documentation](https://cordova.apache.org/docs/en/latest/reference/cordova-plugin-statusbar/)
and the exact [Cordova Android 15.1.0 forwarding implementation](https://github.com/apache/cordova-android/blob/rel/15.1.0/cordova-js-src/plugin/android/statusbar.js).
