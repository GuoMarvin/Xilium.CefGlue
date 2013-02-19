namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to browser life span. The
    /// methods of this class will be called on the UI thread unless otherwise
    /// indicated.
    /// </summary>
    public abstract unsafe partial class CefLifeSpanHandler
    {
        private int on_before_popup(cef_life_span_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_string_t* target_url, cef_string_t* target_frame_name, cef_popup_features_t* popupFeatures, cef_window_info_t* windowInfo, cef_client_t** client, cef_browser_settings_t* settings, int* no_javascript_access)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_targetUrl = cef_string_t.ToString(target_url);
            var m_targetFrameName = cef_string_t.ToString(target_frame_name);
            var m_popupFeatures = new CefPopupFeatures(popupFeatures);
            var m_windowInfo = CefWindowInfo.FromNative(windowInfo);
            var m_client = CefClient.FromNative(*client);
            var m_settings = new CefBrowserSettings(settings);
            var m_noJavascriptAccess = (*no_javascript_access) != 0;

            var o_client = m_client;
            var result = OnBeforePopup(m_browser, m_frame, m_targetUrl, m_targetFrameName, m_popupFeatures, m_windowInfo, ref m_client, m_settings, ref m_noJavascriptAccess);

            if ((object)o_client != m_client && m_client != null)
            {
                *client = m_client.ToNative();
            }

            *no_javascript_access = m_noJavascriptAccess ? 1 : 0;

            m_popupFeatures.Dispose();
            m_windowInfo.Dispose();
            m_settings.Dispose();

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called on the IO thread before a new popup window is created. The |browser|
        /// and |frame| parameters represent the source of the popup request. The
        /// |target_url| and |target_frame_name| values may be empty if none were
        /// specified with the request. The |popupFeatures| structure contains
        /// information about the requested popup window. To allow creation of the
        /// popup window optionally modify |windowInfo|, |client|, |settings| and
        /// |no_javascript_access| and return false. To cancel creation of the popup
        /// window return true. The |client| and |settings| values will default to the
        /// source browser's values. The |no_javascript_access| value indicates whether
        /// the new browser window should be scriptable and in the same process as the
        /// source browser.
        /// </summary>
        protected virtual bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref bool noJavascriptAccess)
        {
            return false;
        }


        private void on_after_created(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnAfterCreated(m_browser);
        }

        /// <summary>
        /// Called after a new window is created.
        /// </summary>
        protected virtual void OnAfterCreated(CefBrowser browser)
        {
        }


        private int run_modal(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            return RunModal(m_browser) ? 1 : 0;
        }

        /// <summary>
        /// Called when a modal window is about to display and the modal loop should
        /// begin running. Return false to use the default modal loop implementation or
        /// true to use a custom implementation.
        /// </summary>
        protected virtual bool RunModal(CefBrowser browser)
        {
            return false;
        }


        private int do_close(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            return DoClose(m_browser) ? 1 : 0;
        }

        /// <summary>
        /// Called when a window has recieved a request to close. Return false to
        /// proceed with the window close or true to cancel the window close. If this
        /// is a modal window and a custom modal loop implementation was provided in
        /// RunModal() this callback should be used to restore the opener window to a
        /// usable state.
        /// </summary>
        protected virtual bool DoClose(CefBrowser browser)
        {
            return false;
        }


        private void on_before_close(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnBeforeClose(m_browser);
        }

        /// <summary>
        /// Called just before a window is closed. If this is a modal window and a
        /// custom modal loop implementation was provided in RunModal() this callback
        /// should be used to exit the custom modal loop.
        /// </summary>
        protected virtual void OnBeforeClose(CefBrowser browser)
        {
        }
    }
}
