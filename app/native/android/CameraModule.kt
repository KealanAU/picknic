package app.picknic.camera

import android.app.Activity
import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.Base64
import com.lynx.jsbridge.Callback
import com.lynx.jsbridge.LynxMethod
import com.lynx.jsbridge.LynxModule
import com.lynx.react.bridge.JavaOnlyMap
import com.lynx.react.bridge.ReadableMap
import java.io.ByteArrayOutputStream

/**
 * Native camera module for the Android host app.
 *
 * Contract with the JS boundary (app/src/native/camera.ts):
 *   JS:  NativeModules.CameraModule.capture({ quality, facing }, (result) => {})
 *   success:  { base64, width, height, mime: "image/jpeg" }
 *   failure:  { error }   ("cancel" in the message = user backed out)
 *
 * Register once at host startup:
 *   LynxEnv.inst().registerModule("CameraModule", CameraModule::class.java)
 *
 * The capture UI itself is delegated to the host (an Activity using CameraX or an
 * ACTION_IMAGE_CAPTURE intent) because a Lynx module has no Activity of its own.
 * Wire [CameraLauncher] to your app's capture flow; for a live filtered preview
 * register a custom LynxUI element instead — see app/native/README.md.
 */
class CameraModule(context: Context) : LynxModule(context) {

    @LynxMethod
    fun capture(options: ReadableMap, callback: Callback) {
        val quality = if (options.hasKey("quality")) options.getDouble("quality") else 0.9
        val facing = if (options.hasKey("facing")) options.getString("facing") else "back"

        CameraLauncher.launch(mContext, facing) { result ->
            when (result) {
                is CameraLauncher.Result.Cancelled ->
                    callback.invoke(errorMap("cancelled"))
                is CameraLauncher.Result.Failed ->
                    callback.invoke(errorMap(result.message))
                is CameraLauncher.Result.Captured ->
                    callback.invoke(encode(result.bitmap, quality))
            }
        }
    }

    private fun encode(bitmap: Bitmap, quality: Double): JavaOnlyMap {
        val stream = ByteArrayOutputStream()
        bitmap.compress(Bitmap.CompressFormat.JPEG, (quality * 100).toInt().coerceIn(1, 100), stream)
        val base64 = Base64.encodeToString(stream.toByteArray(), Base64.NO_WRAP)
        return JavaOnlyMap().apply {
            putString("base64", base64)
            putInt("width", bitmap.width)
            putInt("height", bitmap.height)
            putString("mime", "image/jpeg")
        }
    }

    private fun errorMap(message: String): JavaOnlyMap =
        JavaOnlyMap().apply { putString("error", message) }
}

/**
 * Host-provided bridge to whatever Activity actually captures the photo. Replace
 * this stub with a CameraX capture session or an ACTION_IMAGE_CAPTURE launcher
 * that resolves the callback on the main thread.
 */
object CameraLauncher {
    sealed interface Result {
        data class Captured(val bitmap: Bitmap) : Result
        data class Failed(val message: String) : Result
        object Cancelled : Result
    }

    fun launch(context: Context, facing: String?, onResult: (Result) -> Unit) {
        // TODO(host): start CameraX / the camera intent, then call onResult(...).
        // Example decode of a captured file the host wrote:
        //   val bmp = BitmapFactory.decodeFile(path) ?: return onResult(Result.Failed("decode failed"))
        //   onResult(Result.Captured(bmp))
        onResult(Result.Failed("CameraLauncher not wired into the host app yet."))
    }
}
