using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace RevEx.Desktop.Updates.Controls;

public class PawLoadingIndicator : TemplatedControl
{
    private const double CycleSeconds = 2.8;
    private const int PrintCount = 6;
    private static readonly Geometry Pad = Geometry.Parse(
        "M 15,19 C 21,19 25,25 25,29 C 25,35 5,35 5,29 C 5,25 9,19 15,19 Z");
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly Stopwatch _playback = new();

    static PawLoadingIndicator()
    {
        AffectsRender<PawLoadingIndicator>(ForegroundProperty);
    }

    public PawLoadingIndicator()
    {
        Width = 168;
        Height = 56;
        IsHitTestVisible = false;
        this.Bind(ForegroundProperty, this.GetResourceObservable("AccentBrush"));
        _timer.Tick += AdvanceFrame;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = Math.Min(Bounds.Width / 340, Bounds.Height / 110);
        if (scale <= 0 || Foreground == null)
        {
            return;
        }

        var offsetX = (Bounds.Width - 340 * scale) / 2;
        var offsetY = (Bounds.Height - 110 * scale) / 2;
        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY)))
        {
            for (var index = 0; index < PrintCount; index++)
            {
                var phase = (_playback.Elapsed.TotalSeconds / CycleSeconds - (double)index / PrintCount + 1) % 1;
                var opacity = GetPrintOpacity(phase);
                var x = 7 + index * 53;
                var y = index % 2 == 0 ? 21 : 55;
                // Every print points to the right, including the opposite row.
                var transform = new Matrix(0, 1, -1, 0, x + 36, y);
                using (context.PushOpacity(opacity))
                using (context.PushTransform(transform))
                {
                    DrawToe(context, new Point(2, 14), -28);
                    DrawToe(context, new Point(10, 6), -10);
                    DrawToe(context, new Point(20, 6), 10);
                    DrawToe(context, new Point(28, 14), 28);
                    context.DrawGeometry(Foreground, null, Pad);
                }
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePlayback();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        _playback.Stop();
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
            _playback.Stop();
            return;
        }

        if (_timer.IsEnabled)
        {
            return;
        }

        _playback.Restart();
        InvalidateVisual();
        _timer.Start();
    }

    private void AdvanceFrame(object? sender, EventArgs e)
    {
        if (IsEffectivelyVisible)
        {
            InvalidateVisual();
        }
    }

    private void DrawToe(DrawingContext context, Point center, double angle)
    {
        var transform = Matrix.CreateTranslation(-center.X, -center.Y)
            * Matrix.CreateRotation(angle * Math.PI / 180)
            * Matrix.CreateTranslation(center.X, center.Y);
        using (context.PushTransform(transform))
        {
            context.DrawEllipse(Foreground, null, center, 4, 5);
        }
    }

    private static double GetPrintOpacity(double phase)
    {
        if (phase < 0.04)
        {
            return 0.08 + 0.92 * phase / 0.04;
        }

        if (phase < 0.18)
        {
            return 1;
        }

        if (phase < 0.62)
        {
            return 1 - 0.92 * (phase - 0.18) / 0.44;
        }

        return 0.08;
    }
}
