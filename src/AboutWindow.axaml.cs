/*
    Copyright (c) 2024 hawkeye116477

    This file is part of NandroidDataConverter.

    NandroidDataConverter is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    NandroidDataConverter is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using Avalonia.Controls;
using Avalonia.Interactivity;
using NandroidDataConverter;

namespace NandroidDataConverter;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Title = $"{Localization.AboutMenu} - {AppInfo.AppName}";
        LicenseTxt.Text =
            "Copyright (c) 2024 hawkeye116477\n\n" +
            "This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or\n" +
            "(at your option) any later version.\n\n" +
            "This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.\n\n" +
            "You should have received a copy of the GNU General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.";
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}