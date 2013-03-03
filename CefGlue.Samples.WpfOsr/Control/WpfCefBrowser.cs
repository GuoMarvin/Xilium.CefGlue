﻿namespace Xilium.CefGlue.Samples.WpfOsr
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    public class WpfCefBrowser : ContentControl, IDisposable
    {
        private bool _disposed;
        private bool _created;

        private Image _browserPageImage;
        private WriteableBitmap _browserPageBitmap;

        private double _browserWidth;
        private double _browserHeight;
        private bool _browserSizeChanged;

        private CefBrowser _browser;
        private CefBrowserHost _browserHost;
        private WpfCefClient _cefClient;

        Dispatcher _mainUiDispatcher;

        public WpfCefBrowser()
        {
            StartUrl = "about:blank";

            KeyboardNavigation.SetAcceptsReturn(this, true);
            _mainUiDispatcher = Dispatcher.CurrentDispatcher;
        }

        #region Disposable

        ~WpfCefBrowser()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_browserPageImage != null)
                {
                    _browserPageImage.Source = null;
                    _browserPageImage = null;
                }

                if (_browserPageBitmap != null)
                    _browserPageBitmap = null;

                // 					if (this.browserPageD3dImage != null)
                // 						this.browserPageD3dImage = null;

                // TODO: What's the right way of disposing the browser instance?
                if (_browserHost != null)
                {
                    _browserHost.CloseBrowser();
                    _browserHost = null;
                }

                if (_browser != null)
                {
                    _browser.Dispose();
                    _browser = null;
                }
            }

            _disposed = true;
        }

        #endregion

        public string StartUrl { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //this.browserPageD3dImage = new D3DImage();

            _browserPageImage = new Image()
            {
                Focusable = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.None
            };

            this.Content = _browserPageImage;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var size = base.ArrangeOverride(arrangeBounds);

            if (_browserPageImage != null)
            {
                var newWidth = size.Width;
                var newHeight = size.Height;

                //Debug.WriteLine("BrowserResize: {0}x{1}.", newWidth, newHeight);

                if (newWidth > 0 && newHeight > 0)
                {
                    if (!_created)
                    {
                        AttachEventHandlers(this); // TODO: ?

                        // Create the bitmap that holds the rendered website bitmap
                        _browserWidth = newWidth;
                        _browserHeight = newHeight;
                        _browserSizeChanged = true;

                        // Find the window that's hosting us
                        Window parentWnd = FindParentOfType<Window>(this);
                        if (parentWnd != null)
                        {
                            IntPtr hParentWnd = new WindowInteropHelper(parentWnd).Handle;

                            var windowInfo = CefWindowInfo.Create();
                            windowInfo.SetAsOffScreen(hParentWnd);

                            var settings = new CefBrowserSettings();
                            _cefClient = new WpfCefClient(this);

                            // This is the first time the window is being rendered, so create it.
                            CefBrowserHost.CreateBrowser(windowInfo, _cefClient, settings, !string.IsNullOrEmpty(StartUrl) ? StartUrl : "about:blank");

                            _created = true;
                        }
                    }
                    else
                    {
                        // Only update the bitmap if the size has changed
                        if (_browserPageBitmap == null || (_browserPageBitmap.Width != newWidth || _browserPageBitmap.Height != newHeight))
                        {
                            _browserWidth = newWidth;
                            _browserHeight = newHeight;
                            _browserSizeChanged = true;

                            // If the window has already been created, just resize it
                            if (_browserHost != null)
                                _browserHost.WasResized();
                        }
                    }
                }
            }

            return size;
        }

        private void AttachEventHandlers(UIElement uiElement)
        {
            uiElement.GotFocus += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        // this.browserHost.SetFocus(true);
                        _browserHost.SendFocusEvent(true);
                    }
                }
                catch
                {
                }
            };

            uiElement.LostFocus += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        // this.browserHost.SetFocus(false);
                        _browserHost.SendFocusEvent(false);
                    }
                }
                catch
                {
                }
            };

            uiElement.LostMouseCapture += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        _browserHost.SendCaptureLostEvent();
                        //Debug.WriteLine("Browser_LostMouseCapture");
                    }
                }
                catch
                {
                }
            };

            uiElement.MouseLeave += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = 0,
                            Y = 0
                        };

                        _browserHost.SendMouseMoveEvent(mouseEvent, true);
                        //Debug.WriteLine("Browser_MouseLeave");
                    }
                }
                catch
                {
                }
            };

            uiElement.MouseMove += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y
                        };

                        _browserHost.SendMouseMoveEvent(mouseEvent, false);

                        //Debug.WriteLine(string.Format("Browser_MouseMove: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch
                {
                }

                arg.Handled = true;
            };

            uiElement.MouseDown += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        CaptureMouse();
                        Keyboard.Focus(this);

                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y,
                        };

                        mouseEvent.Modifiers = GetMouseModifiers();

                        if (arg.ChangedButton == MouseButton.Left)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Left, false, 1);
                        else if (arg.ChangedButton == MouseButton.Middle)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Middle, false, 1);
                        else if (arg.ChangedButton == MouseButton.Right)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Right, false, 1);

                        //Debug.WriteLine(string.Format("Browser_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch
                {
                }

                arg.Handled = true;
            };

            uiElement.MouseUp += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y,
                        };

                        mouseEvent.Modifiers = GetMouseModifiers();

                        if (arg.ChangedButton == MouseButton.Left)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Left, true, 1);
                        else if (arg.ChangedButton == MouseButton.Middle)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Middle, true, 1);
                        else if (arg.ChangedButton == MouseButton.Right)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Right, true, 1);

                        //Debug.WriteLine(string.Format("Browser_MouseUp: ({0},{1})", cursorPos.X, cursorPos.Y));

                        ReleaseMouseCapture();
                    }
                }
                catch
                {
                }

                arg.Handled = true;
            };

            uiElement.MouseWheel += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y,
                        };

                        _browserHost.SendMouseWheelEvent(mouseEvent, 0, arg.Delta);
                    }
                }
                catch
                {
                }

                arg.Handled = true;
            };

            // TODO: require more intelligent processing
            uiElement.PreviewTextInput += (sender, arg) =>
            {
                if (_browserHost != null)
                {
                    // Debug.WriteLine(string.Format("TextInput: text {0}", arg.Text));

                    foreach (var c in arg.Text)
                    {
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.Char,
                            WindowsKeyCode = (int)c,
                            // Character = c,
                        };

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }

                arg.Handled = true;
            };

            // TODO: require more intelligent processing
            uiElement.PreviewKeyDown += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        // Debug.WriteLine(string.Format("KeyDown: system key {0}, key {1}", arg.SystemKey, arg.Key));
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.RawKeyDown,
                            WindowsKeyCode = KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key),
                            NativeKeyCode = 0,
                            IsSystemKey = arg.Key == Key.System,
                        };

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }
                catch
                {
                }

                arg.Handled = false;
            };

            // TODO: require more intelligent processing
            uiElement.PreviewKeyUp += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        // Debug.WriteLine(string.Format("KeyUp: system key {0}, key {1}", arg.SystemKey, arg.Key));
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.KeyUp,
                            WindowsKeyCode = KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key),
                            NativeKeyCode = 0,
                            IsSystemKey = arg.Key == Key.System,
                        };

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }
                catch
                {
                }

                arg.Handled = false;
            };
        }

        #region Handlers

        public void HandleAfterCreated(CefBrowser browser)
        {
            int width = 0, height = 0;

            bool hasAlreadyBeenInitialized = false;

            _mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (_browser != null)
                {
                    hasAlreadyBeenInitialized = true;
                }
                else
                {
                    _browser = browser;
                    _browserHost = _browser.GetHost();
                    // _browserHost.SetFocus(IsFocused);

                    width = (int)_browserWidth;
                    height = (int)_browserHeight;
                }
            }));

            // Make sure we don't initialize ourselves more than once. That seems to break things.
            if (hasAlreadyBeenInitialized)
                return;

            if (width > 0 && height > 0)
                _browserHost.WasResized();

            // 			mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            // 			{
            // 				if (!string.IsNullOrEmpty(this.initialUrl))
            // 				{
            // 					NavigateTo(this.initialUrl);
            // 					this.initialUrl = string.Empty;
            // 				}
            // 			}));
        }

        internal bool GetViewRect(ref CefRectangle rect)
        {
            bool rectProvided = false;
            CefRectangle browserRect = new CefRectangle();

            // TODO: simplify this
            _mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                try
                {
                    // The simulated screen and view rectangle are the same. This is necessary
                    // for popup menus to be located and sized inside the view.
                    browserRect.X = browserRect.Y = 0;
                    browserRect.Width = (int)_browserWidth;
                    browserRect.Height = (int)_browserHeight;

                    rectProvided = true;

                    // 					// The simulated screen and view rectangle are the same. This is necessary
                    // 					// for popup menus to be located and sized inside the view.
                    // 					tempRect.X = tempRect.Y = 0;
                    // 					tempRect.Width = this.browserWidth;
                    // 					tempRect.Height = this.browserHeight;
                    // 
                    // 					rectProvided = true;
                }
                catch (Exception ex)
                {
                    //LogWriter.WriteMsg(LogLevel.Error, "WpfCefBrowser: Caught exception in HandleGetRect(): {0} - {1}", ex.GetType(), ex.Message);
                    rectProvided = false;
                }
            }));

            if (rectProvided)
                rect = browserRect;

            return rectProvided;
        }

        internal void GetScreenPoint(int viewX, int viewY, ref int screenX, ref int screenY)
        {
            Point ptScreen = new Point();

            _mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                try
                {
                    Point ptView = new Point(viewX, viewY);
                    ptScreen = PointToScreen(ptView);
                }
                catch (Exception ex)
                {
                    //LogWriter.WriteMsg(LogLevel.Error, "WpfCefBrowser: Caught exception in HandleGetScreenPoint(): {0} - {1}", ex.GetType(), ex.Message);
                }
            }));

            screenX = (int)ptScreen.X;
            screenY = (int)ptScreen.Y;
        }

        internal void HandleViewPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            _mainUiDispatcher.Invoke(DispatcherPriority.Render, new Action(delegate
            {
                try
                {
                    if (_browserSizeChanged)
                    {
                        _browserPageBitmap = new WriteableBitmap((int)_browserWidth, (int)_browserHeight, 96, 96, PixelFormats.Bgr32, null);
                        _browserPageImage.Source = _browserPageBitmap;

                        _browserSizeChanged = false;
                    }

                    if (_browserPageBitmap != null)
                        DoRenderBrowser(_browserPageBitmap, width, height, dirtyRects, buffer);

                    // 					Debug.WriteLine(string.Format("HandleViewPaint: Dirty rect [{0},{1},{2},{3}]. Browser dimensions are {4}x{5}.",
                    // 						dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height, browserWidth, browserHeight));
                }
                catch (Exception ex)
                {
                    //LogWriter.WriteMsg(LogLevel.Error, "WpfCefBrowser: Caught exception in HandleViewPaint(): {0} - {1}", ex.GetType(), ex.Message);
                }
            }));
        }

        private void DoRenderBrowser(WriteableBitmap bitmap, int browserWidth, int browserHeight, CefRectangle[] dirtyRects, IntPtr sourceBuffer)
        {
            int stride = browserWidth * 4;
            int sourceBufferSize = stride * browserHeight;

            foreach (CefRectangle dirtyRect in dirtyRects)
            {
                if (dirtyRect.X > bitmap.Width || dirtyRect.Y > bitmap.Height || dirtyRect.Width == 0 || dirtyRect.Height == 0 || browserWidth == 0 || browserHeight == 0)
                    return;

                // If the window has been resized, make sure we never try to render too much
                int adjustedWidth = (int)dirtyRect.Width;
                if (dirtyRect.X + dirtyRect.Width > (int)bitmap.Width)
                    adjustedWidth = (int)bitmap.Width - (int)dirtyRect.X;

                int adjustedHeight = (int)dirtyRect.Height;
                if (dirtyRect.Y + dirtyRect.Height > (int)bitmap.Height)
                    adjustedHeight = (int)bitmap.Height - (int)dirtyRect.Y;

                // Update the dirty region
                Int32Rect sourceRect = new Int32Rect((int)dirtyRect.X, (int)dirtyRect.Y, adjustedWidth, adjustedHeight);
                bitmap.WritePixels(sourceRect, sourceBuffer, sourceBufferSize, stride, (int)dirtyRect.X, (int)dirtyRect.Y);

                // 			int adjustedWidth = browserWidth;
                // 			if (browserWidth > (int)bitmap.Width)
                // 				adjustedWidth = (int)bitmap.Width;
                // 
                // 			int adjustedHeight = browserHeight;
                // 			if (browserHeight > (int)bitmap.Height)
                // 				adjustedHeight = (int)bitmap.Height;
                // 
                // 			int sourceBufferSize = browserWidth * browserHeight * 4;
                // 			int stride = browserWidth * 4;
                // 
                // 			Int32Rect sourceRect = new Int32Rect(0, 0, adjustedWidth, adjustedHeight);
                // 			bitmap.WritePixels(sourceRect, sourceBuffer, sourceBufferSize, stride, 0, 0);
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Finds a parent of the specific type
        /// </summary>
        private static T FindParentOfType<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parentObj = VisualTreeHelper.GetParent(obj);
            if (parentObj == null)
                return null;

            // Try to type cast the parent to the desired type.
            // If the cast succeeds, we've found the desired parent.
            T parent = parentObj as T;
            if (parent != null)
                return parent;

            // If we get here, the current parent wasn't of the right type, so keep looking recursively
            return FindParentOfType<T>(parentObj);
        }

        private static CefEventFlags GetMouseModifiers()
        {
            CefEventFlags modifiers = new CefEventFlags();

            if (Mouse.LeftButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.LeftMouseButton;

            if (Mouse.MiddleButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.MiddleMouseButton;

            if (Mouse.RightButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.RightMouseButton;

            return modifiers;
        }

        #endregion

        #region Methods

        public void NavigateTo(string url)
        {
            // Remove leading whitespace from the URL
            url = url.TrimStart();

            if (_browser != null)
                _browser.GetMainFrame().LoadUrl(url);
            else
                StartUrl = url;
        }

        public bool CanGoBack()
        {
            if (_browser != null)
                return _browser.CanGoBack;
            else
                return false;
        }

        public void GoBack()
        {
            if (_browser != null)
                _browser.GoBack();
        }

        public bool CanGoForward()
        {
            if (_browser != null)
                return _browser.CanGoForward;
            else
                return false;
        }

        public void GoForward()
        {
            if (_browser != null)
                _browser.GoForward();
        }

        public void Refresh()
        {
            if (_browser != null)
                _browser.Reload();
        }

        #endregion
    }
}
