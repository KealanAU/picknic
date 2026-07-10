import Lynx
import UIKit

// Programmatic window (no storyboard/SceneDelegate): the project is generated
// by XcodeGen from project.yml, so there is no Xcode template to lean on.
@main
class AppDelegate: UIResponder, UIApplicationDelegate {
    var window: UIWindow?

    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
    ) -> Bool {
        // Boots the Lynx runtime. The LynxService pods (Image/Log/Http)
        // self-register once linked; no explicit service wiring needed.
        LynxEnv.sharedInstance()

        let window = UIWindow(frame: UIScreen.main.bounds)
        window.rootViewController = ViewController()
        window.makeKeyAndVisible()
        self.window = window
        return true
    }
}
