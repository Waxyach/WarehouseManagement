using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class CategoryWindow : FluentWindow
{
    private readonly CategoryViewModel _viewModel;
    private bool _isDraggingSatVal;
    private bool _isDraggingHue;

    private double _hue;
    private double _sat = 0.8;
    private double _val = 0.8;

    public Category ResultCategory => _viewModel.ResultCategory;

    public CategoryWindow(CategoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += (_, _) => ApplyHexToColor(_viewModel.ColorHex);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryViewModel.ColorHex))
        {
            ApplyHexToColor(_viewModel.ColorHex);
        }
    }

    private void HueCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = true;
        HueCanvas.CaptureMouse();
        UpdateHue(e.GetPosition(HueCanvas));
    }

    private void HueCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingHue) UpdateHue(e.GetPosition(HueCanvas));
    }

    private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = false;
        HueCanvas.ReleaseMouseCapture();
    }

    private void UpdateHue(Point p)
    {
        double h = HueCanvas.ActualHeight;
        if (h <= 0) return;

        double y = Math.Clamp(p.Y, 0, h);
        Canvas.SetTop(HueThumb, y - 2);

        _hue = (y / h) * 360.0;
        if (_hue >= 360.0) _hue = 0;

        Color baseColor = ColorPaletteHelper.HsvToRgb(_hue, 1.0, 1.0);
        HueBackgroundRect.Fill = new SolidColorBrush(baseColor);

        UpdateFinalColor();
    }

    private void SatValCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSatVal = true;
        SatValCanvas.CaptureMouse();
        UpdateSatVal(e.GetPosition(SatValCanvas));
    }

    private void SatValCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingSatVal) UpdateSatVal(e.GetPosition(SatValCanvas));
    }

    private void SatValCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSatVal = false;
        SatValCanvas.ReleaseMouseCapture();
    }

    private void UpdateSatVal(Point p)
    {
        double w = SatValCanvas.ActualWidth;
        double h = SatValCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double x = Math.Clamp(p.X, 0, w);
        double y = Math.Clamp(p.Y, 0, h);

        Canvas.SetLeft(PickerThumb, x - 6);
        Canvas.SetTop(PickerThumb, y - 6);

        _sat = x / w;
        _val = 1.0 - (y / h);

        UpdateFinalColor();
    }

    private void UpdateFinalColor()
    {
        Color color = ColorPaletteHelper.HsvToRgb(_hue, _sat, _val);
        _viewModel.ColorHex = ColorPaletteHelper.ColorToHex(color);
    }

    private void ApplyHexToColor(string hex)
    {
        if (!ColorPaletteHelper.TryParseHex(hex, out Color c)) return;

        ColorPaletteHelper.RgbToHsv(c, out _hue, out _sat, out _val);

        double hCanvasH = HueCanvas.ActualHeight > 0 ? HueCanvas.ActualHeight : 140;
        double hueY = (_hue / 360.0) * hCanvasH;
        Canvas.SetTop(HueThumb, hueY - 2);

        Color baseColor = ColorPaletteHelper.HsvToRgb(_hue, 1.0, 1.0);
        HueBackgroundRect.Fill = new SolidColorBrush(baseColor);

        double svW = SatValCanvas.ActualWidth > 0 ? SatValCanvas.ActualWidth : 140;
        double svH = SatValCanvas.ActualHeight > 0 ? SatValCanvas.ActualHeight : 140;
        Canvas.SetLeft(PickerThumb, (_sat * svW) - 6);
        Canvas.SetTop(PickerThumb, ((1.0 - _val) * svH) - 6);
    }
}