using Avalonia.Controls;
using Avalonia.Input;

namespace WhisperVoice.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Allow dragging the window from the titlebar area
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var position = e.GetPosition(TitleBar);
            if (position.Y >= 0 && position.Y <= TitleBar.Bounds.Height &&
                position.X >= 0 && position.X <= TitleBar.Bounds.Width)
            {
                BeginMoveDrag(e);
            }
        }
    }
}
