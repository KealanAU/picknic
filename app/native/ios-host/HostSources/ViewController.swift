import Lynx
import UIKit

#if DEBUG
// Set this to a full bundle URL to skip discovery (e.g. a tunnel). When nil,
// the app scans `devServerHost` (injected at build time from the Mac's LAN
// IP — see DevServerHost.generated.swift) across the rspeedy port range and
// loads the first dev server that answers. Debug-only: Release builds load
// the embedded main.lynx.bundle instead (see loadEmbeddedBundle()).
private let manualBundleURL: String? = nil
private let devServerPorts = Array(3000...3010)
#endif

class ViewController: UIViewController {
    private var lynxView: LynxView?
    private var loaded = false
    private var resolvedBundleURL = ""
    private let statusLabel = UILabel()

    override func viewDidLoad() {
        super.viewDidLoad()
        // Picknic cream — matches the launch screen color asset so
        // launch -> first Lynx paint has no black flash.
        view.backgroundColor = UIColor(red: 1.0, green: 0xF8 / 255.0, blue: 0xF1 / 255.0, alpha: 1.0)

        let lynxView = LynxView { builder in
            let config = LynxConfig(provider: PicknicTemplateProvider())
            // Registers as "CameraModule" on the JS side; LynxCameraView.m
            // self-registers <camera-view> when compiled into the target.
            config.register(LynxCameraModule.self)
            builder.config = config
            builder.screenSize = self.view.frame.size
            builder.fontScale = 1.0
        }
        lynxView.preferredLayoutWidth = view.frame.size.width
        lynxView.preferredLayoutHeight = view.frame.size.height
        lynxView.layoutWidthMode = .exact
        lynxView.layoutHeightMode = .exact
        view.addSubview(lynxView)
        self.lynxView = lynxView

        statusLabel.frame = view.bounds.insetBy(dx: 24, dy: 48)
        statusLabel.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        statusLabel.numberOfLines = 0
        statusLabel.textColor = .black
        statusLabel.font = .monospacedSystemFont(ofSize: 13, weight: .regular)
        statusLabel.isHidden = true
        view.addSubview(statusLabel)

        lynxView.addLifecycleClient(self)

        #if DEBUG
        if let manual = manualBundleURL {
            load(bundleURL: manual)
        } else {
            discoverDevServer { [weak self] found in
                DispatchQueue.main.async {
                    guard let self else { return }
                    if let found {
                        self.load(bundleURL: found)
                    } else {
                        self.showStatus("""
                            Couldn't find the dev server.

                            Scanned http://\(devServerHost):\(devServerPorts.first!)-\(devServerPorts.last!) \
                            for /main.lynx.bundle.

                            Run `pnpm dev` in picknic/app, keep the phone on \
                            the same network as the Mac, then relaunch. \
                            (devServerHost is injected at build time — if the \
                            Mac changed networks since the last build, rebuild.)
                            """)
                    }
                }
            }
        }
        #else
        loadEmbeddedBundle()
        #endif
    }

    #if DEBUG
    private func load(bundleURL: String) {
        resolvedBundleURL = bundleURL
        NSLog("PicknicHost loading bundle: %@", bundleURL)
        lynxView?.loadTemplate(fromURL: bundleURL, initData: nil)
    }
    #else
    /// Release: load the bundle embedded by the "Embed Lynx bundle" build
    /// phase (project.yml) — no dev server, no local network.
    private func loadEmbeddedBundle() {
        guard let url = Bundle.main.url(forResource: "main.lynx", withExtension: "bundle") else {
            showStatus("""
                No embedded Picknic bundle in this build.

                Release builds ship main.lynx.bundle inside the app — the \
                "Embed Lynx bundle" build phase copies it from app/dist. \
                Run `pnpm build` in picknic/app and rebuild.
                """)
            return
        }
        do {
            let data = try Data(contentsOf: url)
            resolvedBundleURL = url.absoluteString
            NSLog("PicknicHost loading embedded bundle: %@", url.path)
            lynxView?.loadTemplate(data, withURL: url.absoluteString)
        } catch {
            showStatus("""
                Couldn't read the embedded Picknic bundle.

                \(url.path)

                \(error)
                """)
        }
    }
    #endif

    private func showStatus(_ message: String) {
        statusLabel.text = message
        statusLabel.isHidden = false
    }

    #if DEBUG
    /// Probes each candidate port with a cheap HEAD request; first responder
    /// with an HTTP 2xx wins.
    private func discoverDevServer(completion: @escaping (String?) -> Void) {
        func tryPort(at index: Int) {
            guard index < devServerPorts.count else {
                completion(nil)
                return
            }
            let candidate = "http://\(devServerHost):\(devServerPorts[index])/main.lynx.bundle"
            guard let url = URL(string: candidate) else {
                tryPort(at: index + 1)
                return
            }
            var request = URLRequest(url: url, cachePolicy: .reloadIgnoringLocalCacheData, timeoutInterval: 1.0)
            request.httpMethod = "HEAD"
            URLSession.shared.dataTask(with: request) { _, response, _ in
                if let http = response as? HTTPURLResponse, (200...299).contains(http.statusCode) {
                    completion(candidate)
                } else {
                    tryPort(at: index + 1)
                }
            }.resume()
        }
        tryPort(at: 0)
    }
    #endif
}

extension ViewController: LynxViewLifecycle {
    func lynxView(_ view: LynxView!, didLoadFinishedWithUrl url: String!) {
        DispatchQueue.main.async {
            self.loaded = true
            self.statusLabel.isHidden = true
        }
    }

    func lynxView(_ view: LynxView!, didRecieveError error: Error!) {
        let message = error.map { String(describing: $0) } ?? "unknown error"
        NSLog("PicknicHost lynx error: %@", message)
        DispatchQueue.main.async {
            // Errors after a successful render stay console-only; before it,
            // they replace the empty screen (bundle unreachable, ATS, etc.).
            guard !self.loaded else { return }
            self.showStatus("""
                Couldn't load the Picknic bundle.

                \(self.resolvedBundleURL)

                \(message)

                Check `pnpm dev` is running and the phone can open the URL \
                above in Safari (README: Troubleshooting).
                """)
        }
    }
}
