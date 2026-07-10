import Foundation
import Lynx

// UserDefaults-backed persistent key/value store. Implements the
// NativeKVModule contract in src/api/storage/native.ts — without it the app's
// storage falls back to the in-memory driver and auth/guest sessions are lost
// on every app close.
@objcMembers
public final class NativeKVModule: NSObject, LynxModule {
    public static var name: String { "NativeKVModule" }

    public static var methodLookup: [String: String] {
        [
            "getItem": NSStringFromSelector(#selector(getItem(_:callback:))),
            "setItem": NSStringFromSelector(#selector(setItem(_:value:))),
            "removeItem": NSStringFromSelector(#selector(removeItem(_:))),
        ]
    }

    public override required init() {
        super.init()
    }

    public required init(param: Any) {
        super.init()
    }

    public func getItem(_ key: String, callback: @escaping LynxCallbackBlock) {
        // nil bridges to NSNull -> null on the JS side.
        callback(UserDefaults.standard.string(forKey: key) as Any)
    }

    public func setItem(_ key: String, value: String) {
        UserDefaults.standard.set(value, forKey: key)
    }

    public func removeItem(_ key: String) {
        UserDefaults.standard.removeObject(forKey: key)
    }
}
