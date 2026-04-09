using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;

namespace GoodGovernanceApp.Converters;

/// <summary>
/// Converts a hex color string (e.g. "#FF009688") to a SolidColorBrush.
/// </summary>
public class StringToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to Visibility (true → Visible, false → Collapsed unless InvertedBooleanToVisibilityConverter).
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a non-empty string to Visibility (non-empty → Visible, empty/null → Collapsed).
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is string s && !string.IsNullOrWhiteSpace(s))
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

/// <summary>
/// Converts a bool to Visibility: true → Visible, false → Collapsed.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is System.Windows.Visibility v && v == System.Windows.Visibility.Visible;
}

/// <summary>
/// Inverts a bool then maps to Visibility: false → Visible, true → Collapsed.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Colors an unallocated-amount value: green if > 0, red if negative or zero.
/// Used by BudgetAllocationView to highlight the remaining unallocated budget.
/// </summary>
public class AllocationColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d >= 0
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))   // green
                : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));  // red
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns a progress-bar foreground brush based on what share of the budget remains.
/// </summary>
public class ProgressColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d < 0)
                return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); // red  – over budget
            if (d < 1000)
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)); // orange – low balance
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));     // green  – healthy
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Colors a project's remaining-fund value: green if positive, red if zero or negative.
/// </summary>
public class RemainingFundColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        decimal amount = value switch
        {
            decimal d  => d,
            double  db => (decimal)db,
            float   f  => (decimal)f,
            int     i  => i,
            _          => 0m
        };

        return amount > 0
            ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))  // green
            : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); // red
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
