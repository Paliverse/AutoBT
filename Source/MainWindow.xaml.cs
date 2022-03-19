using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using LEDSystem.Core.Handlers;
using ModernWpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoBT
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private int seletedIndex = 0;
        private Guid[] seletedIndex_Guid;

        private TimerHandler timerHandler;
        BluetoothClient BTClient = new BluetoothClient();

        public List<BluetoothDeviceDetails> ListOfDevices = new List<BluetoothDeviceDetails>();
        public List<SavedDevice> ListOfSavedDevices = new List<SavedDevice>();

        

        public MainWindow()
        {
            InitializeComponent();

            DetailedDeviceInfoGrid.Visibility = Visibility.Collapsed;

            HomePageGrid.Visibility = Visibility.Visible;
            SettingsPageGrid.Visibility = Visibility.Hidden;
            InfoPageGrid.Visibility = Visibility.Hidden;

            MenuHomeRectangle.Opacity = 1;
            MenuSettingsRectangle.Opacity = 0;
            MenuHelpRectangle.Opacity = 0;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenhight = SystemParameters.PrimaryScreenHeight;

            this.Width = screenWidth / 1.5;
            this.Height = screenhight / 1.5;

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            // Breathing

            StartScan();

            BTClient.InquiryLength = new TimeSpan(0, 0, 0, 1);

            BluetoothDeviceInfo[] devices = BTClient.DiscoverDevices(20, true, true, false);

            UpdateList();

        }

        #region Window Scaling
        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            WindowAspectRatio.Register((Window)sender);
        }

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0d;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {

        }

        public double ScaleValue
        {
            get
            {
                return (double)GetValue(ScaleValueProperty);
            }
            set
            {
                SetValue(ScaleValueProperty, value);
            }
        }

        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            CalculateScale();
        }

        private void CalculateScale()
        {
            double xScale = ActualWidth / 1920.0d;
            double yScale = ActualHeight / 1080.0d;

            Debug.WriteLine($"actualWidth: {ActualWidth} actualHeight: {ActualHeight}");

            double value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(MainGrid, value);
        }


        #endregion

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (timerHandler != null)
            {
                timerHandler.Stop();
            }
        }

        public class SavedDevice
        {
            public string BT_DeviceName { get; set; }
            public string BT_MacAddress { get; set; }
            public Guid[] BT_Services { get; set; }
        }

        public class BluetoothDeviceDetails
        {
            public string BT_DeviceName { get; set; }
            public string BT_Connected { get; set; }
            public string BT_MacAddress { get; set; }
            public string BT_Authenticated { get; set; }
            public Guid[] BT_Services { get; set; }
        }


        private void StartScan()
        {
            timerHandler = new TimerHandler();
            timerHandler.SetInterval(5);
            timerHandler.Create(OnCreate_Breathing, OnUpdate_Breathing, OnClosed_Breathing);
        }

        public void OnCreate_Breathing()
        {
        }
        public void OnUpdate_Breathing()
        {
            

        }
        public void OnClosed_Breathing()
        {
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            var SelectedItem = ListOfDevicesFound.SelectedItem as BluetoothDeviceDetails;

            BluetoothDeviceInfo[] devices = BTClient.DiscoverDevices(20, true, true, false);
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";

            foreach (var d in devices)
            {
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                foreach (var i in ListOfSavedDevices)
                {
                    if (SelectedItem.BT_MacAddress == i.BT_MacAddress && SelectedItem.BT_MacAddress == macAd)
                    {
                        Debug.WriteLine("TRUE");
                        foreach (var item in i.BT_Services)
                        {
                            d.SetServiceState(item, false);
                        }
                        foreach (var item in i.BT_Services)
                        {
                            d.SetServiceState(item, true);
                        }
                    }
                }
            }

            UpdateList();

            BluetoothDeviceInfo[] devices2 = BTClient.DiscoverDevices(20, true, true, false);

            foreach (var d in devices2)
            {
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                if (macAd == SelectedItem.BT_MacAddress)
                {
                    for (int i = 0; i < ListOfDevices.Count; i++)
                    {
                        if (ListOfDevices[i].BT_MacAddress == SelectedItem.BT_MacAddress)
                        {
                            ListOfDevices[i] = new BluetoothDeviceDetails() { BT_DeviceName = $"{d.DeviceName}", BT_Connected = $"{d.Connected}", BT_MacAddress = $"{macAd}", BT_Authenticated = $"{d.Authenticated}", BT_Services = d.InstalledServices };
                            ListOfDevicesFound.Items.Refresh();
                            ListOfDevicesFound.SelectedIndex = seletedIndex;
                        }
                    }

                }
            }

        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            var SelectedItem = ListOfDevicesFound.SelectedItem as BluetoothDeviceDetails;

            BluetoothDeviceInfo[] devices = BTClient.DiscoverDevices(20, true, true, false);
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";

            foreach (var d in devices)
            {
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                foreach (var i in ListOfSavedDevices)
                {
                    if (SelectedItem.BT_MacAddress == i.BT_MacAddress && SelectedItem.BT_MacAddress == macAd)
                    {
                        foreach (var item in i.BT_Services)
                        {
                            d.SetServiceState(item, false);
                        }
                    }
                }
            }

            UpdateList();


            BluetoothDeviceInfo[] devices2 = BTClient.DiscoverDevices(20, true, true, false);

            foreach (var d in devices2)
            {
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                if (macAd == SelectedItem.BT_MacAddress)
                {
                    for (int i = 0; i < ListOfDevices.Count; i++)
                    {
                        if (ListOfDevices[i].BT_MacAddress == SelectedItem.BT_MacAddress)
                        {
                            ListOfDevices[i] = new BluetoothDeviceDetails() { BT_DeviceName = $"{d.DeviceName}", BT_Connected = $"{d.Connected}", BT_MacAddress = $"{macAd}", BT_Authenticated = $"{d.Authenticated}", BT_Services = d.InstalledServices };
                            ListOfDevicesFound.Items.Refresh();
                            ListOfDevicesFound.SelectedIndex = seletedIndex;
                        }
                    }

                }
            }


        }

        public static Guid ToGuid(int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        private void RunCommands(List<string> cmds, string workingDirectory = "")
        {
            string result = "";
            var process = new Process();
            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = workingDirectory;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            process.StartInfo = psi;
            process.Start();
            process.OutputDataReceived += (sender, e) => { result = e.Data; Debug.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { Debug.WriteLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (StreamWriter sw = process.StandardInput)
            {
                foreach (var cmd in cmds)
                {
                    sw.WriteLine(cmd);
                }
            }
            process.WaitForExit();
        }



        private void RefreshBTList_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();

            var SelectedItem = ListOfDevicesFound.SelectedItem as BluetoothDeviceDetails;
            BluetoothDeviceInfo[] devices2 = BTClient.DiscoverDevices(20, true, true, false);
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";

            foreach (var d in devices2)
            {
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                if (macAd == SelectedItem.BT_MacAddress)
                {
                    for (int i = 0; i < ListOfDevices.Count; i++)
                    {
                        if (ListOfDevices[i].BT_MacAddress == SelectedItem.BT_MacAddress)
                        {
                            ListOfDevices[i] = new BluetoothDeviceDetails() { BT_DeviceName = $"{d.DeviceName}", BT_Connected = $"{d.Connected}", BT_MacAddress = $"{macAd}", BT_Authenticated = $"{d.Authenticated}", BT_Services = d.InstalledServices };
                            ListOfDevicesFound.Items.Refresh();
                            ListOfDevicesFound.SelectedIndex = seletedIndex;
                        }
                    }

                }
            }
        }

        private void UpdateList()
        {
            BluetoothDeviceInfo[] devices = BTClient.DiscoverDevices(20, true, true, false);
            ListOfDevices.Clear();


            foreach (var d in devices)
            {

                var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
                var replace = "$1:$2:$3:$4:$5:$6";
                var macAd = Regex.Replace(d.DeviceAddress.ToString(), regex, replace);

                ListOfDevices.Add(new BluetoothDeviceDetails() { BT_DeviceName = $"{d.DeviceName}", BT_Connected = $"{d.Connected}", BT_MacAddress = $"{macAd}", BT_Authenticated = $"{d.Authenticated}", BT_Services = d.InstalledServices });


                if (d != null)
                {
                    if (d.Connected)
                    {
                        Debug.WriteLine("||||||||||||||||||||||||||||||||||");

                        Debug.WriteLine(d.DeviceName);
                        Debug.WriteLine(d.Connected);
                        Debug.WriteLine(d.DeviceAddress);

                        Debug.WriteLine("--------------------");
                        Guid uuid = BluetoothService.L2CapProtocol;

                        foreach (var a in d.GetServiceRecords(uuid))
                        {
                            System.UInt32 num = 0;
                            num = (uint)a[0].Value.Value;
                            Debug.WriteLine(num);
                            Debug.WriteLine(ToGuid((int)num));

                        }
                        Debug.WriteLine(d.GetVersions().Manufacturer);
                        Debug.WriteLine("--------------------");

                        foreach (var item in d.InstalledServices)
                        {
                            Debug.WriteLine(item);
                            //d.SetServiceState(item, false);

                        }
                        Debug.WriteLine("||||||||||||||||||||||||||||||||||");
                    }


                }

            }

            this.Dispatcher.InvokeIfRequired(new Action(() =>
            {
                ListOfDevicesFound.ItemsSource = ListOfDevices;
            }));
        }

        private void MenuHome_Click(object sender, RoutedEventArgs e)
        {
            HomePageGrid.Visibility = Visibility.Visible;
            SettingsPageGrid.Visibility = Visibility.Hidden;
            InfoPageGrid.Visibility = Visibility.Hidden;

            MenuHomeRectangle.Opacity = 1;
            MenuSettingsRectangle.Opacity = 0;
            MenuHelpRectangle.Opacity = 0;
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            HomePageGrid.Visibility = Visibility.Hidden;
            SettingsPageGrid.Visibility = Visibility.Visible;
            InfoPageGrid.Visibility = Visibility.Hidden;

            MenuHomeRectangle.Opacity = 0;
            MenuSettingsRectangle.Opacity = 1;
            MenuHelpRectangle.Opacity = 0;
        }

        private void MenuInfo_Click(object sender, RoutedEventArgs e)
        {
            HomePageGrid.Visibility = Visibility.Hidden;
            SettingsPageGrid.Visibility = Visibility.Hidden;
            InfoPageGrid.Visibility = Visibility.Visible;

            MenuHomeRectangle.Opacity = 0;
            MenuSettingsRectangle.Opacity = 0;
            MenuHelpRectangle.Opacity = 1;
        }

        private void ListOfDevicesFound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var SelectedItem = ListOfDevicesFound.SelectedItem as BluetoothDeviceDetails;

            if (SelectedItem != null)
            {
                DetailedDeviceInfoGrid.Visibility = Visibility.Visible;

                for (int i = 0; i < ListOfDevices.Count; i++)
                {
                    if (SelectedItem.BT_MacAddress == ListOfDevices[i].BT_MacAddress)
                    {
                        seletedIndex = i;
                    }
                }

                DetailedDeviceInfo_DeviceNameTitle.Content = SelectedItem.BT_DeviceName;
                DetailedDeviceInfo_DeviceName.Content = SelectedItem.BT_DeviceName;
                DetailedDeviceInfo_MacAddress.Content = SelectedItem.BT_MacAddress;
                DetailedDeviceInfo_IsAuthinticated.Content = SelectedItem.BT_Authenticated;
                DetailedDeviceInfo_IsConnected.Content = SelectedItem.BT_Connected;

                var servicesList = "";

                foreach (var item in SelectedItem.BT_Services)
                {
                    servicesList += $"{item}\n";
                }
                DetailedDeviceInfo_ListOfServices.Text = servicesList;

                SavedDevice d = new SavedDevice();
                d.BT_DeviceName = SelectedItem.BT_DeviceName;
                d.BT_MacAddress = SelectedItem.BT_MacAddress;
                d.BT_Services = SelectedItem.BT_Services;

                if (!ListOfSavedDevices.Any(v => v.BT_DeviceName == SelectedItem.BT_DeviceName && v.BT_MacAddress == SelectedItem.BT_MacAddress))
                {
                    DetailedDeviceInfo_SaveGuidButton.IsEnabled = true;
                    DetailedDeviceInfo_ConnectButton.IsEnabled = false;
                    DetailedDeviceInfo_DisconnectButton.IsEnabled = false;
                }
                else
                {
                    DetailedDeviceInfo_SaveGuidButton.IsEnabled = false;
                    DetailedDeviceInfo_ConnectButton.IsEnabled = true;
                    DetailedDeviceInfo_DisconnectButton.IsEnabled = true;
                }

                Debug.WriteLine(ListOfSavedDevices.Count);
            }
        }

        private void DetailedDeviceInfo_SaveGuidButton_Click(object sender, RoutedEventArgs e)
        {
            var SelectedItem = ListOfDevicesFound.SelectedItem as BluetoothDeviceDetails;

            if(!ListOfSavedDevices.Any(v => v.BT_DeviceName == SelectedItem.BT_DeviceName && v.BT_MacAddress == SelectedItem.BT_MacAddress))
            {
                ListOfSavedDevices.Add(new SavedDevice() { BT_DeviceName = SelectedItem.BT_DeviceName, BT_MacAddress = SelectedItem.BT_MacAddress, BT_Services = SelectedItem.BT_Services });
                DetailedDeviceInfo_ConnectButton.IsEnabled = true;
                DetailedDeviceInfo_DisconnectButton.IsEnabled = true;
            }
            else
            {
                Debug.WriteLine("ALREADY THERE!");
            }
        }

    }
}

internal class WindowAspectRatio
{
    private double _ratio;

    private WindowAspectRatio(Window window)
    {
        _ratio = window.Width / window.Height;
        Debug.WriteLine($"Ratio: {_ratio}");
        ((HwndSource)HwndSource.FromVisual(window)).AddHook(DragHook);
    }

    public static void Register(Window window)
    {
        new WindowAspectRatio(window);
    }

    internal enum WM
    {
        WINDOWPOSCHANGING = 0x0046,
    }

    [Flags()]
    public enum SWP
    {
        NoMove = 0x2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled)
    {


        if ((WM)msg == WM.WINDOWPOSCHANGING)
        {
            WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

            if ((position.flags & (int)SWP.NoMove) != 0 ||
                HwndSource.FromHwnd(hwnd).RootVisual == null) return IntPtr.Zero;

            position.cx = (int)(position.cy * _ratio);

            Marshal.StructureToPtr(position, lParam, true);
            handeled = true;
        }

        return IntPtr.Zero;
    }
}
