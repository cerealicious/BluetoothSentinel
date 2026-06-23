using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // Required for Button and CheckBox
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Newtonsoft.Json;

namespace BluetoothSentinel
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<DeviceItem> _devices = new();
        private const string ConfigFile = "settings.json";

        public MainWindow()
        {
            InitializeComponent();
            DeviceList.ItemsSource = _devices;
            LoadSettings();
            
            // Fire and forget for initial load to keep UI responsive
            _ = RefreshDevices();
        }

        private async void RefreshDevices_Click(object sender, RoutedEventArgs e) => await RefreshDevices();

        private async Task RefreshDevices()
        {
            _devices.Clear();
            try
            {
                var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
                var deviceInfos = await DeviceInformation.FindAllAsync(selector);

                foreach (var info in deviceInfos)
                {
                    try
                    {
                        var btDevice = await BluetoothDevice.FromIdAsync(info.Id);
                        if (btDevice == null) continue;

                        var settings = LoadDeviceSetting(info.Id);
                        
                        _devices.Add(new DeviceItem
                        {
                            Id = info.Id,
                            Name = string.IsNullOrEmpty(info.Name) ? "Unknown Device" : info.Name,
                            IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
                            AutoConnect = settings.AutoConnect,
                            DeviceRef = btDevice
                        });

                        if (!settings.AutoConnect)
                        {
                            MonitorDevice(btDevice);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading device {info.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to refresh devices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoConnect_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is DeviceItem device)
            {
                SaveDeviceSetting(device.Id, device.AutoConnect);
                if (!device.AutoConnect && device.DeviceRef != null)
                {
                    MonitorDevice(device.DeviceRef);
                }
            }
        }

        private void MonitorDevice(BluetoothDevice device)
        {
            device.ConnectionStatusChanged += (s, args) =>
            {
                if (s.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    var item = _devices.FirstOrDefault(d => d.Id == s.DeviceId);
                    if (item != null && !item.AutoConnect)
                    {
                        // In .NET 8 WinRT, use Dispose() instead of Close()
                        s.Dispose();
                    }
                }
            };
        }

        private void PairDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("ms-settings:bluetooth");
            }
            catch
            {
                MessageBox.Show("Could not open Bluetooth settings.", "Error");
            }
        }

        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Bluetooth Sentinel v1.0\n\nDeveloper: cerealicious\nLocation: Scarborough, Toronto\n\nPurpose: Manage Bluetooth auto-connect preferences.",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var device = _devices.FirstOrDefault(d => d.Id == id);
                device?.DeviceRef?.Dispose();
            }
        }

        // --- Settings Management ---
        private DeviceSetting LoadDeviceSetting(string id)
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, DeviceSetting>>(json);
                    if (dict != null && dict.ContainsKey(id)) return dict[id];
                }
            }
            catch { /* Return default on corrupt file */ }
            return new DeviceSetting { AutoConnect = true };
        }

        private void SaveDeviceSetting(string id, bool autoConnect)
        {
            Dictionary<string, DeviceSetting> dict;
            try
            {
                dict = File.Exists(ConfigFile)
                    ? JsonConvert.DeserializeObject<Dictionary<string, DeviceSetting>>(File.ReadAllText(ConfigFile)) ?? new()
                    : new();
            }
            catch { dict = new(); }

            dict[id] = new DeviceSetting { AutoConnect = autoConnect };
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(dict, Formatting.Indented));
        }

        private void LoadSettings() { /* Initial load logic if needed */ }
    }

    public class DeviceItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool AutoConnect { get; set; }
        public BluetoothDevice? DeviceRef { get; set; }
        public string StatusText => IsConnected ? "Connected" : "Disconnected";
        public Visibility CanDisconnect => IsConnected ? Visibility.Visible : Visibility.Collapsed;
    }

    public class DeviceSetting { public bool AutoConnect { get; set; } }
}
