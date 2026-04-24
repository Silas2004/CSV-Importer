using System.Windows;
using System.Windows.Controls;

namespace CsvImporter.WPF.Helpers;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject d) =>
        (string)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string value) =>
        d.SetValue(BoundPasswordProperty, value);

    private static bool GetIsUpdating(DependencyObject d) =>
        (bool)d.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject d, bool value) =>
        d.SetValue(IsUpdatingProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb) return;

        pb.PasswordChanged -= PasswordChanged;

        if (!GetIsUpdating(pb))
        {
            var newVal = e.NewValue as string ?? string.Empty;
            if (pb.Password != newVal)
                pb.Password = newVal;
        }

        pb.PasswordChanged += PasswordChanged;
    }

    private static void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb) return;

        SetIsUpdating(pb, true);
        SetBoundPassword(pb, pb.Password);
        SetIsUpdating(pb, false);
    }
}