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
        private DateTime lockTime;
        private DateTime unlockTime;
        private readonly TimeSpan longDuration = TimeSpan.FromMinutes(Properties.Settings.Default.LongDuration);
        private readonly TimeSpan shortDuration = TimeSpan.FromMinutes(Properties.Settings.Default.ShortDuration);
        private TimeSpan _countdown;
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
            TimeSpan gap = DateTime.UtcNow - lockTime;
            //解锁间隔时间过短、计时尚未临近结束：继续之前未完的计时
            if (gap < shortDuration && Countdown > shortDuration) { }
            //解锁间隔时间果断、计时已经（临近）结束：短时间计时
            else if (gap < shortDuration && Countdown < shortDuration)
            {
                Countdown = shortDuration;
            }
            //其余情况重新计时：长时间计时
            else
            {
                UnlockTime = DateTime.UtcNow;
                Countdown = longDuration;
            }
            _timer.Start();
        }
        void LockDo()
        {
            _timer.Stop();
            lockTime = DateTime.UtcNow;
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
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            UnlockDo();
        }
    }
}
