using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace RevEx.Desktop.Views.Common;

public sealed class LoadingIndicator : Image
{
    private const int FrameWidth = 192;
    private const int FrameHeight = 208;
    private const int FrameCount = 9;

    private static readonly Lazy<IImage[]> Frames = new(LoadFrames);
    private readonly DispatcherTimer _timer = new();
    private int _frameIndex;
    private int _direction;

    public LoadingIndicator()
    {
        Width = 48;
        Height = 52;
        _timer.Tick += AdvanceFrame;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePlayback();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
        {
            UpdatePlayback();
        }
    }

    private void UpdatePlayback()
    {
        if (!IsVisible || !this.IsAttachedToVisualTree())
        {
            _timer.Stop();
            return;
        }

        if (_timer.IsEnabled)
        {
            return;
        }

        _frameIndex = FrameCount / 2;
        _direction = 1;
        ShowFrame();
        _timer.Start();
    }

    private void AdvanceFrame(object? sender, EventArgs e)
    {
        _frameIndex += _direction;
        if (_frameIndex == 0 || _frameIndex == FrameCount - 1)
        {
            _direction = -_direction;
        }

        ShowFrame();
    }

    private void ShowFrame()
    {
        Source = Frames.Value[_frameIndex];
        var delay = _frameIndex switch
        {
            0 or FrameCount - 1 => 440,
            FrameCount / 2 => 110,
            _ => 75
        };
        _timer.Interval = TimeSpan.FromMilliseconds(delay);
    }

    private static IImage[] LoadFrames()
    {
        using var stream = AssetLoader.Open(new Uri("avares://RevEx.Desktop/Assets/TyubikLoading.png"));
        var atlas = new Bitmap(stream);
        var frames = new IImage[FrameCount];
        for (var index = 0; index < FrameCount; index++)
        {
            var frameBounds = new PixelRect(index * FrameWidth, 0, FrameWidth, FrameHeight);
            frames[index] = new CroppedBitmap(atlas, frameBounds);
        }

        return frames;
    }
}
