using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BreakTimeCS.ViewModels
{
    internal class MainViewModel :INotifyPropertyChanged
    {
        //Inotify的实现
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged(string propName)
        {
            if ((this.PropertyChanged != null))
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        //数据结构
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();
        private readonly DispatcherTimer _timer = new();
        private TimeSpan _countdown;
        private DateTime unlockTime;
        public DateTime UnlockTime
        {
            get => unlockTime.ToLocalTime();
            set
            {
                if (unlockTime.ToLocalTime() != value)
                {
                    unlockTime = value.ToUniversalTime();
                    this.NotifyPropertyChanged(nameof(UnlockTime));
                }
            }
        }
        public TimeSpan Countdown
        {
            get => _countdown;
            set
            {
                if (_countdown != value)
                {
                    _countdown = value;
                    this.NotifyPropertyChanged(nameof(Countdown));
                }
            }
        }
        //事件
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                UnlockDo();
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                LockDo();
            }
        }
        //方法
        void UnlockDo()
        {
            UnlockTime = DateTime.UtcNow;
            _countdown = TimeSpan.FromMinutes(Properties.Settings.Default.Duration);
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        void LockDo()
        {
            _timer.Stop();
        }
        void Timer_Tick(object? sender, EventArgs e)
        {
            Countdown -= _timer.Interval;
            switch (Countdown.TotalSeconds)
            {
                case 0:
                    LockWorkStation();
                    break;
            }
        }
        //构造函数
        public MainViewModel()
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            UnlockDo();
        }
    }
}
