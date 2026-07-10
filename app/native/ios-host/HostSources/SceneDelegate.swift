import Lynx
import UIKit

// Scene counterpart of the old AppDelegate window setup: one window, one
// ViewController, no multi-window (see UIApplicationSceneManifest in
// project.yml).
class SceneDelegate: UIResponder, UIWindowSceneDelegate {
    var window: UIWindow?

    func scene(
        _ scene: UIScene,
        willConnectTo session: UISceneSession,
        options connectionOptions: UIScene.ConnectionOptions
    ) {
        guard let windowScene = scene as? UIWindowScene else { return }
        let window = UIWindow(windowScene: windowScene)
        window.rootViewController = ViewController()
        window.makeKeyAndVisible()
        self.window = window
    }

    func sceneDidBecomeActive(_ scene: UIScene) {
        // Lynx resolves env(safe-area-inset-*) from statics that
        // initLayoutConfig fills by reading the key window — which is nil
        // while the LynxView is being built in viewDidLoad, so they log as
        // 0/0. Re-run it now that the scene is foreground-active and the
        // window is key; re-activation is a harmless refresh.
        guard let window else { return }
        LynxEnv.sharedInstance().initLayoutConfig(window.bounds.size)
    }
}
