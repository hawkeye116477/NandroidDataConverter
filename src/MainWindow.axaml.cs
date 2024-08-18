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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Data.Sqlite;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using VisualCard.Converters;
using VisualCard.Parts;

namespace NandroidDataConverter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void ExitApp()
    {
        Close();
    }

    public async Task ShowAboutWindow()
    {
        AboutWindow aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

    private async void ChoosePathBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Localization.SelectFolder,
            AllowMultiple = false,
        });
        if (folder.Count != 1) return;
        SelectedPathTxt.Text = folder[0].Path.AbsolutePath;
    }

    private async void ConvertBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedPathTxt.Text)) return;
        if (!Directory.Exists(SelectedPathTxt.Text)) return;
        if (string.IsNullOrEmpty(SelectedOutputPathTxt.Text)) return;
        if (!Directory.Exists(SelectedOutputPathTxt.Text)) return;
        var convertPath = SelectedOutputPathTxt.Text;
        var searchedFiles = new List<string>();
        if ((bool)SmsChk.IsChecked)
        {
            searchedFiles.Add("bugle_db");
        }

        if ((bool)ContactsChk.IsChecked)
        {
            searchedFiles.Add("contacts2.db");
        }

        if ((bool)CallLogChk.IsChecked)
        {
            if (!(bool)ContactsChk.IsChecked)
            {
                searchedFiles.Add("contacts2.db");
            }

            searchedFiles.Add("calllog.db");
        }

        ProgressB.IsIndeterminate = true;
        var smsQuantity = 0;
        var contactsQuantity = 0;
        var callsQuantity = 0;
        foreach (var searchedFile in searchedFiles)
        {
            string[] files = Directory.GetFiles(SelectedPathTxt.Text, searchedFile,
                SearchOption.AllDirectories);
            var databasePath = Path.Combine(files[0]);
            if (databasePath.Contains("bugle_db"))
            {
                var db = new SqliteConnection($"Data Source={databasePath};");
                db.Open();
                using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM messages", db);
                try
                {
                    using var countReader = await countCommand.ExecuteReaderAsync();
                    if (countReader.HasRows)
                    {
                        while (countReader.Read())
                        {
                            smsQuantity = countReader.GetInt32(0);
                            if (smsQuantity > 0)
                            {
                                var doc = new XmlDocument();
                                var xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                                doc.AppendChild(xmldecl);
                                var smsesXmlEl = doc.CreateElement("smses");
                                smsesXmlEl.SetAttribute("count", smsQuantity.ToString());
                                var sql = """
                                          SELECT ppl.normalized_destination as num,
                                                 p.timestamp as date,
                                                 CASE WHEN m.sender_id in (select _id from participants where sub_id=-2) then 1 else 2 end incoming,
                                                 p.text as body,
                                                 m.read as m_read,
                                                 ppl.blocked as locked
                                          FROM messages m, conversations c, parts p, participants ppl, conversation_participants cp
                                          WHERE (m.conversation_id = c._id) and (m._id = p.message_id) 
                                          and (cp.conversation_id = c._id) and (cp.participant_id = ppl._id);
                                          """;
                                using var command = new SqliteCommand(sql, db);
                                using var reader = await command.ExecuteReaderAsync();
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        var singleSmsXml = doc.CreateElement("sms");
                                        singleSmsXml.SetAttribute("address", reader.GetString(0));
                                        singleSmsXml.SetAttribute("body", reader.GetString(3));
                                        singleSmsXml.SetAttribute("date", reader.GetInt64(1).ToString());
                                        singleSmsXml.SetAttribute("type", reader.GetInt32(2).ToString());
                                        singleSmsXml.SetAttribute("locked", reader.GetInt32(5).ToString());
                                        singleSmsXml.SetAttribute("protocol", "0");
                                        singleSmsXml.SetAttribute("read", reader.GetInt32(4).ToString());
                                        smsesXmlEl.AppendChild(singleSmsXml);
                                    }
                                }

                                doc.AppendChild(smsesXmlEl);
                                doc.Save(Path.Combine(convertPath, $"sms-{DateTime.Now:yyyyMMdd_HHmmss}.xml"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                db.Close();
            }

            if (databasePath.Contains("contacts2.db") && (bool)ContactsChk.IsChecked)
            {
                Card[] contacts = AndroidContactsDb.GetContactsFromDb(databasePath);
                StringBuilder contactsContent = new StringBuilder();
                foreach (var contact in contacts)
                {
                    contactsContent.Append(contact);
                }

                await File.WriteAllTextAsync(
                    Path.Combine(convertPath, $"contacts-{DateTime.Now:yyyyMMdd_HHmmss}.vcf"),
                    contactsContent.ToString());
                contactsQuantity = contacts.Length;
            }

            if ((databasePath.Contains("calllog.db") || databasePath.Contains("contacts2.db")) &&
                (bool)CallLogChk.IsChecked)
            {
                var db = new SqliteConnection($"Data Source={databasePath};");
                db.Open();
                using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM calls", db);
                try
                {
                    using var countReader = await countCommand.ExecuteReaderAsync();
                    if (countReader.HasRows)
                    {
                        while (countReader.Read())
                        {
                            callsQuantity = countReader.GetInt32(0);
                            using var command = new SqliteCommand("""
                                                                  SELECT number, duration, date, type, presentation, subscription_id, post_dial_digits, 
                                                                  subscription_component_name, name FROM calls ORDER BY date DESC
                                                                  """, db);
                            if (callsQuantity > 0)
                            {
                                var doc = new XmlDocument();
                                var xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                                doc.AppendChild(xmldecl);
                                var callsXmlEl = doc.CreateElement("calls");
                                callsXmlEl.SetAttribute("count", callsQuantity.ToString());
                                using var commandReader = await command.ExecuteReaderAsync();
                                if (commandReader.HasRows)
                                {
                                    while (commandReader.Read())
                                    {
                                        var singleCallXml = doc.CreateElement("call");
                                        singleCallXml.SetAttribute("number", commandReader.GetString(0));
                                        singleCallXml.SetAttribute("duration", commandReader.GetInt32(1).ToString());
                                        singleCallXml.SetAttribute("date", commandReader.GetInt64(2).ToString());
                                        singleCallXml.SetAttribute("type", commandReader.GetInt32(3).ToString());
                                        singleCallXml.SetAttribute("presentation",
                                            commandReader.GetInt32(4).ToString());

                                        if (!commandReader.IsDBNull(5))
                                        {
                                            singleCallXml.SetAttribute("subscription_id", commandReader.GetString(5));
                                        }

                                        if (!commandReader.IsDBNull(6))
                                        {
                                            singleCallXml.SetAttribute("post_dial_digits", commandReader.GetString(6));
                                        }

                                        if (!commandReader.IsDBNull(7))
                                        {
                                            singleCallXml.SetAttribute("subscription_component_name",
                                                commandReader.GetString(7));
                                        }

                                        if (!commandReader.IsDBNull(8))
                                        {
                                            singleCallXml.SetAttribute("contact_name", commandReader.GetString(8));
                                        }

                                        callsXmlEl.AppendChild(singleCallXml);
                                    }
                                }

                                doc.AppendChild(callsXmlEl);
                                doc.Save(Path.Combine(convertPath, $"calls-{DateTime.Now:yyyyMMdd_HHmmss}.xml"));
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }

                db.Close();
            }
        }

        ProgressB.IsIndeterminate = false;
        if (contactsQuantity > 0 || smsQuantity > 0 || callsQuantity > 0)
        {
            var processedText = new StringBuilder($"{Localization.Processed}\n");
            if (smsQuantity == 1)
            {
                processedText.AppendLine(Localization.ProcessedSmsMessage_one);
            }
            else if (smsQuantity > 1)
            {
                processedText.AppendLine(Localization.ProcessedSmsMessage_other);
            }

            if (contactsQuantity == 1)
            {
                processedText.AppendLine(Localization.ProcessedContacts_one);
            }
            else if (contactsQuantity > 1)
            {
                processedText.AppendLine(Localization.ProcessedContacts_other);
            }

            if (callsQuantity == 1)
            {
                processedText.AppendLine(Localization.ProcessedCalls_one);
            }
            else if (callsQuantity > 1)
            {
                processedText.AppendLine(Localization.ProcessedCalls_other);
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                Localization.ConversionResult,
                processedText.ToString().Replace("${smsQuantity}", smsQuantity.ToString())
                    .Replace("${contactsQuantity}", contactsQuantity.ToString())
                    .Replace("${callsQuantity}", callsQuantity.ToString()),
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
        }
    }

    private async void ChooseOutputPathBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var lastFolder =
            await StorageProviderExtensions.TryGetFolderFromPathAsync(StorageProvider, SelectedPathTxt.Text);
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = lastFolder,
            Title = Localization.SelectFolder,
            AllowMultiple = false,
        });
        if (folder.Count != 1) return;
        SelectedOutputPathTxt.Text = folder[0].Path.AbsolutePath;
    }
}