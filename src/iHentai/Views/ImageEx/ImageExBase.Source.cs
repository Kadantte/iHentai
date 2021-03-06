// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI;

namespace iHentai.Views.ImageEx
{
    /// <summary>
    ///     Base code for ImageEx
    /// </summary>
    public partial class ImageExBase
    {
        /// <summary>
        ///     Identifies the <see cref="Source" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source),
            typeof(object), typeof(ImageExBase), new PropertyMetadata(null, SourceChanged));

        private bool _isHttpSource;
        private object _lazyLoadingSource;
        private CancellationTokenSource _tokenSource;

        private Uri _uri;

        /// <summary>
        ///     Gets or sets the source used by the image
        /// </summary>
        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ImageExBase;

            if (control == null)
            {
                return;
            }

            if (e.OldValue == null || e.NewValue == null || !e.OldValue.Equals(e.NewValue))
            {
                if (IsLazyLoadingSupported)
                {
                    if (e.NewValue == null || !control.EnableLazyLoading || control._isInViewport)
                    {
                        control._lazyLoadingSource = null;
                        control.SetSource(e.NewValue);
                    }
                    else
                    {
                        control._lazyLoadingSource = e.NewValue;
                    }
                }
                else
                {
                    control.SetSource(e.NewValue);
                }
            }
        }

        private static bool IsHttpUri(Uri uri)
        {
            return uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https");
        }

        private void AttachSource(ImageSource source)
        {
            var image = Image as Image;
            var brush = Image as ImageBrush;

            if (image != null)
            {
                image.Source = source;
            }
            else if (brush != null)
            {
                brush.ImageSource = source;
            }
        }

        private async void SetSource(object source)
        {
            if (!IsInitialized)
            {
                return;
            }

            _tokenSource?.Cancel();

            _tokenSource = new CancellationTokenSource();

            AttachSource(null);

            if (source == null)
            {
                VisualStateManager.GoToState(this, UnloadedState, true);
                return;
            }

            VisualStateManager.GoToState(this, LoadingState, true);

            var imageSource = source as ImageSource;
            if (imageSource != null)
            {
                AttachSource(imageSource);

                ImageExOpened?.Invoke(this, new ImageExOpenedEventArgs());
                VisualStateManager.GoToState(this, LoadedState, true);
                return;
            }

            _uri = source as Uri;
            if (_uri == null)
            {
                var url = source as string ?? source.ToString();
                if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _uri))
                {
                    VisualStateManager.GoToState(this, FailedState, true);
                    return;
                }
            }

            _isHttpSource = IsHttpUri(_uri);
            if (!_isHttpSource && !_uri.IsAbsoluteUri)
            {
                _uri = new Uri("ms-appx:///" + _uri.OriginalString.TrimStart('/'));
            }

            await LoadImageAsync(_uri);
        }

        private async Task LoadImageAsync(Uri imageUri)
        {
            if (_uri != null)
            {
                if (IsCacheEnabled)
                {
                    switch (CachingStrategy)
                    {
                        case ImageExCachingStrategy.Custom when _isHttpSource:
                            await SetHttpSourceCustomCached(imageUri);
                            break;
                        case ImageExCachingStrategy.Custom:
                        case ImageExCachingStrategy.Internal:
                        default:
                        {
                            if (imageUri.IsAbsoluteUri && imageUri.IsFile)
                            {
                                await SetFileSource(imageUri);
                            }
                            else
                            {
                                AttachSource(new BitmapImage(imageUri));
                            }
                        }
                            break;
                    }
                }
                else if (string.Equals(_uri.Scheme, "data", StringComparison.OrdinalIgnoreCase))
                {
                    var source = _uri.OriginalString;
                    const string base64Head = "base64,";
                    var index = source.IndexOf(base64Head);
                    if (index >= 0)
                    {
                        var bytes = Convert.FromBase64String(source.Substring(index + base64Head.Length));
                        var bitmap = new BitmapImage();
                        AttachSource(bitmap);
                        await bitmap.SetSourceAsync(new MemoryStream(bytes).AsRandomAccessStream());
                    }
                }
                else
                {
                    AttachSource(new BitmapImage(_uri)
                    {
                        CreateOptions = BitmapCreateOptions.IgnoreImageCache
                    });
                }
            }
        }

        private async Task SetFileSource(Uri imageUri)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(imageUri.LocalPath);
                using IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                var img = new BitmapImage { DecodePixelHeight = Convert.ToInt32(Height) };
                await img.SetSourceAsync(fileStream);

                lock (LockObj)
                {
                    // If you have many imageEx in a virtualized listview for instance
                    // controls will be recycled and the uri will change while waiting for the previous one to load
                    if (_uri == imageUri)
                    {
                        AttachSource(img);
                        ImageExOpened?.Invoke(this, new ImageExOpenedEventArgs());
                        VisualStateManager.GoToState(this, LoadedState, true);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do as cancellation has been requested.
            }
            catch (Exception e)
            {
                lock (LockObj)
                {
                    if (_uri == imageUri)
                    {
                        ImageExFailed?.Invoke(this, new ImageExFailedEventArgs(e));
                        VisualStateManager.GoToState(this, FailedState, true);
                    }
                }
            }
        }

        private async Task SetHttpSourceCustomCached(Uri imageUri)
        {
            try
            {
                var propValues = new List<KeyValuePair<string, object>>();

                if (DecodePixelHeight > 0)
                {
                    propValues.Add(new KeyValuePair<string, object>(nameof(DecodePixelHeight), DecodePixelHeight));
                }

                if (DecodePixelWidth > 0)
                {
                    propValues.Add(new KeyValuePair<string, object>(nameof(DecodePixelWidth), DecodePixelWidth));
                }

                if (propValues.Count > 0)
                {
                    propValues.Add(new KeyValuePair<string, object>(nameof(DecodePixelType), DecodePixelType));
                }

                var img = await ImageCache.Instance.GetFromCacheAsync(imageUri, true, _tokenSource.Token, propValues);

                lock (LockObj)
                {
                    // If you have many imageEx in a virtualized listview for instance
                    // controls will be recycled and the uri will change while waiting for the previous one to load
                    if (_uri == imageUri)
                    {
                        AttachSource(img);
                        ImageExOpened?.Invoke(this, new ImageExOpenedEventArgs());
                        VisualStateManager.GoToState(this, LoadedState, true);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do as cancellation has been requested.
            }
            catch (Exception e)
            {
                lock (LockObj)
                {
                    if (_uri == imageUri)
                    {
                        ImageExFailed?.Invoke(this, new ImageExFailedEventArgs(e));
                        VisualStateManager.GoToState(this, FailedState, true);
                    }
                }
            }
        }
    }
}