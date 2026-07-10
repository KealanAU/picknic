import Foundation
import Lynx

/// Fetches the Lynx bundle over HTTP from the rspeedy dev server
/// (`pnpm dev` in app/ — the same URL the Lynx Go QR code encodes).
final class PicknicTemplateProvider: NSObject, LynxTemplateProvider {
    func loadTemplate(withUrl urlString: String!, onComplete callback: LynxTemplateLoadBlock!) {
        guard let urlString, let url = URL(string: urlString) else {
            callback?(nil, NSError(
                domain: "PicknicHost",
                code: 1,
                userInfo: [NSLocalizedDescriptionKey: "Bad bundle URL: \(urlString ?? "nil")"]
            ))
            return
        }

        // The dev server rebuilds the bundle on save; never serve a cached copy.
        var request = URLRequest(url: url)
        request.cachePolicy = .reloadIgnoringLocalCacheData

        URLSession.shared.dataTask(with: request) { data, _, error in
            callback?(data, error)
        }.resume()
    }
}
