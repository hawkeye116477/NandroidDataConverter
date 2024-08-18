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

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NandroidDataConverter;
using YamlDotNet.RepresentationModel;

namespace NandroidDataConverter;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LoadLocalization();
    }

    public static void AddNewYamlValue(string yamlKey, string yamlValue)
    {
        if (!String.IsNullOrEmpty(yamlValue))
        {
            if (Localization.Localizations.ContainsKey(yamlKey))
            {
                Localization.Localizations.Remove(yamlKey);
            }
            Localization.Localizations.Add(yamlKey, yamlValue);
        }
    }
    
    
    public static void LoadLocalization()
    {
        var currentLanguage = CultureInfo.CurrentUICulture.Name;
        var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (Design.IsDesignMode)
        {
            appPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        }
        
        void loadString(string yamlPath)
        {
            try
            {
                string yamlFile = File.ReadAllText(yamlPath);
                if (File.Exists(yamlPath))
                {
                    using var sr = new StringReader(yamlFile);
                    var yaml = new YamlStream();
                    yaml.Load(sr);
                    YamlMappingNode root = (YamlMappingNode)yaml.Documents[0].RootNode;
                    var yamlKey = "";
                    var yamlValue = "";
                    foreach (var e in root.Children)
                    {
                        if (e.Value is not YamlScalarNode scalar)
                        {
                            YamlMappingNode subkeys = (YamlMappingNode)e.Value;
                            foreach (var subkey in subkeys.Children)
                            {
                                yamlKey = $"{e.Key}_{subkey.Key}";
                                yamlValue = subkey.Value.ToString();
                                AddNewYamlValue(yamlKey, yamlValue);
                            }
                        }
                        else
                        {
                            yamlKey = e.Key.ToString();
                            yamlValue = e.Value.ToString();
                            AddNewYamlValue(yamlKey, yamlValue);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse localization file {yamlPath}: {e.Message}");
            }
        }
        
        var localesDir = Path.Combine(appPath, @"Localization");
        var enYaml = Path.Combine(localesDir, "en-US.yaml");
        if (!File.Exists(enYaml))
        {
            return;
        }
        
        loadString(enYaml);
        if (currentLanguage == "en-US") return;
        var langYaml = Path.Combine(localesDir, $"{currentLanguage}.yaml");
        if (File.Exists(langYaml))
        {
            loadString(langYaml);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}