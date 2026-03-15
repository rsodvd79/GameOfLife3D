#nullable enable

using Avalonia.Controls;
using Avalonia.Input;

namespace GameOfLife3D.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ZoomInButton.Click    += (_, _) => GameControl.ZoomIn();
        ZoomOutButton.Click   += (_, _) => GameControl.ZoomOut();
        ZoomResetButton.Click += (_, _) => GameControl.ResetZoom();

        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Add or Key.OemPlus:
                    GameControl.ZoomIn();
                    e.Handled = true;
                    break;
                case Key.Subtract or Key.OemMinus:
                    GameControl.ZoomOut();
                    e.Handled = true;
                    break;
                case Key.D0 or Key.NumPad0:
                    GameControl.ResetZoom();
                    e.Handled = true;
                    break;
            }
        };
    }
}
