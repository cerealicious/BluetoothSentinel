using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Newtonsoft.Json;

namespace BluetoothSentinel
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<DeviceItem> _devices = new();
        private const string ConfigFile = "settings.json";

        public MainWindow()
        {
            InitializeComponent();
            DeviceList.ItemsSource = _devices;
            LoadSettings();
            RefreshDevices();
        }

        private async void RefreshDevices_Click(object sender, RoutedEventArgs e) => await RefreshDevices();

        private async Task RefreshDevices()
        {
            _devices.Clear();
            var selector = BluetoothDevice.GetDeviceSelector();
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
                        Name = info.Name,
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
                    // Skip devices that fail to initialize
                    System.Diagnostics.Debug.WriteLine($"Failed to load device {info.Name}: {ex.Message}");
                }
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
                        s.Close();
                    }
                }
            };
        }

        private void PairDevice_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("ms-settings:bluetooth");
        }

        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Bluetooth Sentinel v1.0\n\nDeveloper: cerealicious\n\nPurpose: Manage Bluetooth auto-connect preferences.", "About");
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var device = _devices.FirstOrDefault(d => d.Id == id);
                device?.DeviceRef?.Close();
            }
        }

        private DeviceSetting LoadDeviceSetting(string id)
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, DeviceSetting>>(json);
                if (dict != null && dict.ContainsKey(id)) return dict[id];
            }
            return new DeviceSetting { AutoConnect = true };
        }

        private void SaveDeviceSetting(string id, bool autoConnect)
        {
            Dictionary<string, DeviceSetting> dict = File.Exists(ConfigFile) 
                ? JsonConvert.DeserializeObject<Dictionary<string, DeviceSetting>>(File.ReadAllText(ConfigFile)) 
                : new Dictionary<string, DeviceSetting>();
            
            dict[id] = new DeviceSetting { AutoConnect = autoConnect };
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(dict, Formatting.Indented));
        }

        private void LoadSettings() { /* Initial load logic if needed */ }
    }

    public class DeviceItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsConnected { get; set; }
        public bool AutoConnect { get; set; }
        public BluetoothDevice DeviceRef { get; set; }
        public string StatusText => IsConnected ? "Connected" : "Disconnected";
        public Visibility CanDisconnect => IsConnected ? Visibility.Visible : Visibility.Collapsed;
    }

    public class DeviceSetting { public bool AutoConnect { get; set; } }
}
