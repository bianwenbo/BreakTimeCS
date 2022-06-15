using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.WinUI.Notifications;
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
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO Dummy);
        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();
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
        private uint _idleTime;
        public uint IdleTime
        {
            get => this._idleTime;
            set
            {
                if (this._idleTime != value)
                {
                    this._idleTime = value;
                    this.NotifyPropertyChanged(nameof(IdleTime));
                }
            }
        }
        //事件
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e) //监听系统锁屏/解锁事件
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
        void UnlockDo() //解锁干什么
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
        void LockDo() //锁屏干什么
        {
            _timer.Stop();
            lockTime = DateTime.UtcNow;
        }
        void Timer_Tick(object? sender, EventArgs e) //计时器间隔事件
        {
            Countdown -= _timer.Interval;
            switch (Countdown.TotalSeconds)
            {
                case 0:
                    LockWorkStation();
                    break;
                case 10:
                    new ToastContentBuilder()
                        .AddText("请在10s后休息")
                        .AddText("站立远望、摇头晃腰！")
                        //.AddInlineImage(new Uri("ms-appdata:///local/Resources/BWB-1-Lockscreen.jpg"))
                        .Show();
                    break;
            }
            IdleTime = GetIdleTime();
        }
        static uint GetIdleTime()
        {
            LASTINPUTINFO LastUserAction = new();
            LastUserAction.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(LastUserAction);
            GetLastInputInfo(ref LastUserAction);
            return ((uint)Environment.TickCount - LastUserAction.dwTime);
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
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
