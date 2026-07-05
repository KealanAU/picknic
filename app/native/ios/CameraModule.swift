import AVFoundation
import Foundation
import Lynx
import UIKit

/// Native camera module for the iOS host app.
///
/// Contract with the JS boundary (`app/src/native/camera.ts`):
///   JS:  NativeModules.CameraModule.capture({ quality, facing }, (result) => {})
///   result on success:  ["base64": String, "width": Int, "height": Int, "mime": "image/jpeg"]
///   result on failure:  ["error": String]   ("cancel" anywhere in the message = user backed out)
///
/// Register once at host startup (e.g. in your LynxView bootstrap):
///   let config = LynxConfig(provider: templateProvider)
///   config.register(CameraModule.self)
///
/// This uses UIImagePickerController for brevity. For an in-app shutter and a
/// live filtered preview you'd instead register a custom LynxUI element wrapping
/// AVCaptureVideoPreviewLayer — see app/native/README.md.
@objcMembers
public final class CameraModule: NSObject, LynxModule {
    public static var name: String { "CameraModule" }

    /// Maps JS method names to the Objective-C selectors Lynx invokes.
    public static var methodLookup: [String: String] {
        ["capture": NSStringFromSelector(#selector(capture(_:callback:)))]
    }

    public override required init() { super.init() }
    public required init(param: Any) { super.init() }

    public func capture(_ options: [String: Any], callback: @escaping LynxCallbackBlock) {
        let quality = (options["quality"] as? NSNumber)?.doubleValue ?? 0.9
        let facing = (options["facing"] as? String) ?? "back"

        // UIKit must be driven on the main thread.
        DispatchQueue.main.async {
            CameraCaptureController.present(quality: CGFloat(quality), facing: facing) { result in
                callback(result)
            }
        }
    }
}

/// Presents the system camera and returns the JS-facing result dictionary.
private final class CameraCaptureController: NSObject, UIImagePickerControllerDelegate,
    UINavigationControllerDelegate {
    private let quality: CGFloat
    private let completion: ([String: Any]) -> Void
    private var retain: CameraCaptureController?

    private init(quality: CGFloat, completion: @escaping ([String: Any]) -> Void) {
        self.quality = quality
        self.completion = completion
    }

    static func present(quality: CGFloat, facing: String,
                        completion: @escaping ([String: Any]) -> Void) {
        guard UIImagePickerController.isSourceTypeAvailable(.camera) else {
            completion(["error": "Camera not available on this device."])
            return
        }
        let controller = CameraCaptureController(quality: quality, completion: completion)
        controller.retain = controller // keep alive until the delegate fires

        let picker = UIImagePickerController()
        picker.sourceType = .camera
        picker.cameraDevice = (facing == "front") ? .front : .rear
        picker.delegate = controller

        Self.topViewController()?.present(picker, animated: true)
    }

    func imagePickerController(_ picker: UIImagePickerController,
                              didFinishPickingMediaWithInfo info: [UIImagePickerController.InfoKey: Any]) {
        defer { retain = nil }
        picker.dismiss(animated: true)

        guard let image = info[.originalImage] as? UIImage,
              let data = image.jpegData(compressionQuality: quality) else {
            completion(["error": "Could not encode captured image."])
            return
        }
        completion([
            "base64": data.base64EncodedString(),
            "width": Int(image.size.width * image.scale),
            "height": Int(image.size.height * image.scale),
            "mime": "image/jpeg",
        ])
    }

    func imagePickerControllerDidCancel(_ picker: UIImagePickerController) {
        defer { retain = nil }
        picker.dismiss(animated: true)
        completion(["error": "cancelled"])
    }

    private static func topViewController() -> UIViewController? {
        let scene = UIApplication.shared.connectedScenes
            .first { $0.activationState == .foregroundActive } as? UIWindowScene
        var top = scene?.windows.first { $0.isKeyWindow }?.rootViewController
        while let presented = top?.presentedViewController { top = presented }
        return top
    }
}
