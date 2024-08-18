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

using System.Diagnostics;
using System.Reflection;

namespace NandroidDataConverter;

public class AppInfo
{
    public static string AppName
    {
        get
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductName;
        }
    }
    public static string AppVersion { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
}