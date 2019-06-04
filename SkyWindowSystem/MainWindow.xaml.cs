using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;    // 手动添加
using System.IO.Ports; // 手动添加
using System.Threading;  // 手动添加
using System.Windows.Media.Media3D; // 手动添加

namespace SkyWindowSystem
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public class Quat//四元数
    {
        public static double x;
        public static double y;
        public static double z;
        public static double w;
    }
    
    //信息存储结构类
    public class MyMessage
    {
        //值类数据
        public static byte power_level;//电池电压
        public static short  temperature_now;//当前温度
        public static int  HMC5883_value;//指南针数据                                    
        public static byte power_control;//电量管理是否开启
        public static byte dip_angle_x;//卫星倾角X
        public static byte dip_angle_y;//卫星倾角y
        public static byte temp_control;//温控是否开启
        public static byte wheel_control;//飞轮是否开启
        public static byte magnetic_control;//磁矩棒是否开启
        public static byte magnetic_x;//磁矩棒x方向
        public static byte magnetic_y;//磁矩棒y方向
        public static double Longitude;//经度值
        public static double Latitude;//纬度值
        public static bool zhongkong;//中控
        public static int jiaozheng_dir;//显示角度矫正
        public static bool zhengorfan = true; //飞轮转向
    }
    //信息存储结构类
    public class MyMessageOld
    {
        //值类数据
        public static int HMC5883_value;//指南针数据                                    
        public static byte dip_angle_x;//卫星倾角X
        public static byte dip_angle_y;//卫星倾角y    
    }

    public partial class MainWindow : Window
    {
        private int TemAngleOld = 0; //温度历史值
        private int TemAngleNow= 0; //当前温度

        // 目标星A状态（初始化false）
        // 定义：如果本派心跳不等于前拍心跳，则目标星A工作正常
        public bool bTargetSatAstate = false;
        //---------------------------------------------------
        // 端口数据定义
        //---------------------------------------------------
        // 端口基本信息（list[0] 端口名；list[1] 端口波特率）
        List<string> lststr_comminfo = new List<string>();
     
        // 波特率
        string strBaudRate;
        // 定义辅助线程（接收）
        Thread recvThread;
        // 定义辅助线程（状态检查）
        Thread stateThread;
        // 定义端口
        SerialPort spPort = new SerialPort();

        public string[] portsName; // 可用串口数组
        public bool qidong=false; // 是否点击启动按钮

        public MainWindow()
        {
            InitializeComponent();
            DrawScale();

            MyMessage.zhongkong = true;
            MyMessage.power_level = 10;
            MyMessage.jiaozheng_dir = 180;
           //↓↓↓↓↓↓↓↓↓可用串口下拉控件↓↓↓↓↓↓↓↓↓
           portsName = SerialPort.GetPortNames();//获取可用串口
            if (portsName.Length > 0)//ports.Length > 0说明有串口可用
            {
                AvailableComCbobox.Items.Clear();
                for (int i = 0; i < portsName.Length; i++)
                {
                    AvailableComCbobox.Items.Add(portsName[i]);
                }
                AvailableComCbobox.SelectedValue = portsName[0];//默认选第1个串口
            }
            else//未检测到串口
            {
                MessageBox.Show("无可用串口");
            }
            // ↑↑↑↑↑↑↑↑可用串口下拉控件↑↑↑↑↑↑↑↑↑
            // 保存端口信息

            strBaudRate = "38400";//通讯波特率
        }

        /// <summary>
        /// 画表盘的刻度
        /// </summary>
        private void DrawScale()
        {
            for (int i = 30; i <= 150; i += 5)
            {
                //添加刻度线
                Line lineScale = new Line();

                if (i % 20== 0)
                {
                    lineScale.X1 = 100 - 82 * Math.Cos(i * Math.PI / 180);
                    lineScale.Y1 = 100 - 82 * Math.Sin(i * Math.PI / 180);
                    lineScale.Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0));//使用红色的线
                    lineScale.StrokeThickness = 3;//线宽为5
                    //添加刻度值
                    TextBlock txtScale = new TextBlock();
                    txtScale.Text = ((i-60)/2).ToString();
                    txtScale.FontSize = 12;
                   if (i < 90)//对坐标值进行一定的修正
                    {
                        Canvas.SetLeft(txtScale, 100 - 83 * Math.Cos(i * Math.PI / 180));
                    }
                    else
                    {
                        Canvas.SetLeft(txtScale, 90 - 70 * Math.Cos(i * Math.PI / 180));
                      
                    }
                    Canvas.SetTop(txtScale, 95 - 75 * Math.Sin(i * Math.PI / 180));
                    this.gaugeCanvas.Children.Add(txtScale);
                }
                else
                {
                    lineScale.X1 = 100 - 85 * Math.Cos(i * Math.PI / 180);
                    lineScale.Y1 = 100 - 85 * Math.Sin(i * Math.PI / 180);
                    lineScale.Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0));
                    lineScale.StrokeThickness = 2;
                }
                lineScale.X2 = 100 - 93 * Math.Cos(i * Math.PI / 180);
                lineScale.Y2 = 100 - 93 * Math.Sin(i * Math.PI / 180);
                this.gaugeCanvas.Children.Add(lineScale);
            }
        }

        // ----------------------------------------
        // 名称：CommInit
        // 用途：初始化COM口
        // 输入：无
        // 输出：无
        // 其它：尝试连接COM口5次，失败则退出
        // ----------------------------------------
        private bool CommInit()
        {
            int nTry; // 尝试次数
            string strErrMsg = ""; // 系统错误信息

            // 设置端口名称
            spPort.PortName = AvailableComCbobox.Text;
            // 设置波特率
            spPort.BaudRate = Convert.ToInt32(strBaudRate);
            // 设置数据位
            spPort.DataBits = 8;
            // 设置校验位（不进行校验，否则出错）
            spPort.Parity = Parity.None;
            // 设置停止位
            spPort.StopBits = StopBits.One;

            // 尝试连接系统的次数为5次
            for (nTry = 0; nTry < 5; nTry++)
            {
                try
                {
                    // 打开串口
                    spPort.Open();
                }
                catch (Exception e)
                {
                    // 拷贝最后一次异常的信息
                    strErrMsg = e.ToString();
                    continue;
                }
                // 连接成功或者到达5次尝试
                break;
            }// for

            // 5次尝试失败
            if (spPort.IsOpen == false && nTry == 5)
            {
                MessageBox.Show(strErrMsg, "提示：端口打开失败 -- 详细信息");
                return false;
            }
            else // 5次之内尝试成功
            {
                return true;
            }
        }
        // CommInit
        // ----------------------------------------
        // 名称：AppTerminate
        // 用途：终止应用程序
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        public void AppTerminate()
        {
            //----------------------------------
            //线程活动时需要先关闭线程
            //----------------------------------
            if(qidong==true)
            {
                // 关闭接收线程
                if (recvThread.IsAlive == true)
                {
                    // 终止线程
                    recvThread.Abort();
                    // 等待线程关闭
                    recvThread.Join();
                }
          
            // 关闭UI更新线程
            if (stateThread.IsAlive == true)
            {
                // 终止线程
                stateThread.Abort();
                // 等待线程关闭
                stateThread.Join();
            }
            
                // 关闭串口
                if (spPort.IsOpen)
                {
                    // 关闭串口
                    spPort.Close();
                }
            }
            // 需要等待辅助线程资源先行释放再关闭主线程
            this.Close();
        }

        // ----------------------------------------
        // 名称：serialPortRecieve
        // 用途：线程处理函数
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        void serialPortRecieve()
        {
            try
            {
                // 先清理缓冲区
                spPort.DiscardInBuffer();
                // 线程终止前应该保持线程运行状态
                while (true)
                {
                    // 利用休眠等待缓冲区数据填充完全（重要）
                    System.Threading.Thread.Sleep(20);
                    // 获取当前读缓冲区中的字节数
                    int nBytesNum = spPort.BytesToRead;
                    // 定义接收数据池大小
                    byte[] byReadBuf = new byte[nBytesNum];
                    // 读取数据
                    spPort.Read(byReadBuf, 0, nBytesNum);
                    // 转码
                    if (nBytesNum > 6)
                    {
                        if (byReadBuf[0] == 0xaa && byReadBuf[1] == 0x55)
                        {
                            bTargetSatAstate = true;
                            switch (byReadBuf[2])
                            {
                                case 0:
                                    MyMessage.power_level = byReadBuf[3];
                                    MyMessage.power_control = byReadBuf[4];
                                    break;
                                case 2:
                                    MyMessage.temperature_now =(short)((byReadBuf[3]<<8)+ byReadBuf[4]);
                                    //  if (MyMessage.temperature_now > 0x8000)
                                    //  MyMessage.temperature_now = 0x8000 - MyMessage.temperature_now;
                                    MyMessage.temp_control = byReadBuf[5];
                                    break;
                                case 4:
                                    MyMessage.HMC5883_value = (byReadBuf[3] << 8) + byReadBuf[4];
                                    MyMessage.dip_angle_x = byReadBuf[5];
                                    MyMessage.dip_angle_y = byReadBuf[6];
                                    break;
                                case 6:
                                    MyMessage.wheel_control = byReadBuf[3];
                                    MyMessage.magnetic_control= byReadBuf[4];
                                    break;
                                case 8:
                                    MyMessage.Longitude = byReadBuf[3]*100+byReadBuf[4];
                                    MyMessage.Latitude = byReadBuf[5]*100+byReadBuf[6];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                   // string strRecv = Encoding.GetEncoding("GB2312").GetString(byReadBuf);
                   // 更新数据缓冲区（仅将原始数据更新到缓冲区，不做其它处理）
                }// while
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString(), "线程终止调试信息");
            }

            // 线程结束
            return;
        }// serialPortRecieve

        // ----------------------------------------
        // 名称：stateCheck
        // 用途：状态检查线程处理函数
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        void stateCheck()
        {
            //-----------------
            // 常量
            //-----------------
            // 定义颜色
            Color color_green = (Color)ColorConverter.ConvertFromString("#FF43EF43");
            Color color_red   = (Color)ColorConverter.ConvertFromString("#FFE72350");
            Color color_yellow = (Color)ColorConverter.ConvertFromString("Yellow");
           
            //-----------------
            // 状态
            //-----------------
            
            // 星载软件版本
            string strSatSoftwareVersion = "0";
            //  三维图形数据
            //  double dbAngle = 35; // 角度
            //  double dbAxisX = 0;  // X轴
            //  double dbAxisY = 0;  // Y轴
            //  double dbAxisZ = 1;  // Z轴
            //  经纬度（单位°，保留一位小数，注意需要添加方向）
            double dbLongitude = 0.01; // 经度值
            double dbLatitude = 0.01; // 纬度值
            // 姿态参数和姿态误差
            // 姿态参数
            double dbAnglePitch = 0; // 俯仰角
            double dbAngleYaw = 0;   // 偏航角
            double dbAngleRoll =0;   // 滚转角
            double dbVelocityPitch = 0; // 俯仰角速率
            double dbVelocityYaw = 0;   // 偏航角速率
            double dbVelocityRoll = 0;  // 滚转角速率
            // 误差参数
            double dbErrAnglePitch = 0; // 俯仰角误差
            double dbErrAngleYaw = 0;   // 偏航角误差
            double dbErrAngleRoll = 0;  // 滚转角误差
            double dbErrVelocityPitch = 0;// 俯仰角速率误差
            double dbErrVelocityRoll = 0; // 滚转角速率误差
            // 目标星温度（单位°C）
            double dbTemperature = 0.0; // 目标星温度
            // 星载电池电量
            int nElecQuantity = 0; // 星载电池电量
            // 指令状态
            bool bFreewheelCmd = false;  // 飞轮指令
            bool bThermalctrlCmd = true; // 热控指令
            bool bMagnetbarCmd = false;  // 磁力矩棒指令
            bool bPowerctrlCmd = true;   // 电源监控指令

            while (true)
            {
                // 100ms刷新一次，大致与下位机速率一致，降低计算机资源消耗
                System.Threading.Thread.Sleep(100);
                // 解码（TBD）
                //按钮状态改变
                if (MyMessage.power_control>0)//电源管理是否开启
                    bPowerctrlCmd = true;
                else
                    bPowerctrlCmd = false;
                if (MyMessage.temp_control > 0)//温控是否开启
                    bThermalctrlCmd = true;
                else
                    bThermalctrlCmd = false;
                if (MyMessage.wheel_control > 0)//飞轮是否开启
                    bFreewheelCmd = true;
                else
                    bFreewheelCmd = false;
                if (MyMessage.magnetic_control > 0)//磁力矩是否开启
                    bMagnetbarCmd = true;
                else
                    bMagnetbarCmd = false;

                nElecQuantity = MyMessage.power_level*10;//电量值
                dbTemperature = MyMessage.temperature_now*0.1;//温度值

                dbAnglePitch = MyMessage.dip_angle_x;//俯仰角
                dbVelocityPitch = MyMessage.dip_angle_x - MyMessageOld.dip_angle_x;//俯仰角速率
                if (dbVelocityPitch > 20)
                    dbVelocityPitch = 20;
                else if (dbVelocityPitch < -20)
                    dbVelocityPitch = -20;
                dbAngleRoll = MyMessage.dip_angle_y;//滚转角
                dbVelocityRoll = MyMessage.dip_angle_y - MyMessageOld.dip_angle_y;//滚转角速率

                if (dbVelocityRoll > 20)
                    dbVelocityRoll = 20;
                else if (dbVelocityRoll < -20)
                    dbVelocityRoll = -20;

                if (dbAnglePitch > 128)
                    dbAnglePitch = dbAnglePitch - 254;
                if (dbAngleRoll > 128)
                    dbAngleRoll = dbAngleRoll - 254;

                dbAngleYaw = -(MyMessage.HMC5883_value+ MyMessageOld.HMC5883_value)*0.5 + MyMessage.jiaozheng_dir;//偏航角
                if (dbAngleYaw > 180)
                    dbAngleYaw = dbAngleYaw - 360;
                else if (dbAngleYaw < -180)
                    dbAngleYaw = dbAngleYaw + 360;

                dbVelocityYaw = MyMessage.HMC5883_value - MyMessageOld.HMC5883_value;//偏航角速率

                dbErrAnglePitch = -dbAnglePitch;//俯仰角误差
                dbErrAngleRoll = -dbAngleRoll;//滚转脚误差
                dbErrAngleYaw = MyMessage.HMC5883_value;//偏航角误差
                if (dbErrAngleYaw > 180)
                    dbErrAngleYaw = dbErrAngleYaw - 360;

                dbErrVelocityPitch = -dbVelocityPitch;
                dbErrVelocityRoll = -dbVelocityRoll;
                dbErrVelocityRoll = -dbVelocityYaw;

                MyMessageOld.HMC5883_value = MyMessage.HMC5883_value;
                MyMessageOld.dip_angle_x = MyMessage.dip_angle_x;
                MyMessageOld.dip_angle_y = MyMessage.dip_angle_y;

                dbLongitude = MyMessage.Longitude*0.01;
                dbLatitude = MyMessage.Latitude*0.01;

                if (MyMessage.power_level < 3)
                {
                    if (MyMessage.zhongkong == true)
                    {
                        MessageBox.Show("电量过低，控制器关闭，请充电");
                        MyMessage.zhongkong = false;
                    }
                }

                //3维状态显示
                Quat.w = dbAngleYaw;
                Quat.x = 0;
                Quat.y = 0;
                Quat.z = 1;
                // 检查所有控件数据并更新控件状态
                //-----------------
                // 本地通信端口
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    if (spPort.IsOpen)
                    {
                        // 更新端口号信息
                       // serial_port_name.Content = spPort.PortName;
                        // 更新波特率信息
                        serial_port_baudrate.Content = "38400";
                        // 更新通信状态
                        communication_state.Content = "已连接";
                        communication_state.Foreground = new SolidColorBrush(color_green);
                    }
                    else
                    {
                        // 更新端口号信息
                     //   serial_port_name.Content = "未知";
                        // 更新波特率信息
                        serial_port_baudrate.Content =  "未知";
                        // 更新通信状态
                        communication_state.Content = "断开";
                        communication_state.Foreground = new SolidColorBrush(color_red);
                    }
                }));
                //-----------------
                // 星载软件版本
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    // 每一拍初始化为0，如果没有数据继续送来，则版本号恢复“未知”
                    if(strSatSoftwareVersion == "0")
                    {
                        target_version.Content = "1.0";
                    }
                    else
                    {
                        target_version.Content = strSatSoftwareVersion;
                    }
                }));
                //-----------------
                // 目标星A状态
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    if(bTargetSatAstate == true)
                    {
                        // 目标星A状态信息
                        satA_state.Content = "通讯正常";
                        satA_state.Foreground = new SolidColorBrush(color_green);
                    }
                    else
                    {
                        // 目标星A状态信息
                        satA_state.Content = "通讯中断";
                        satA_state.Foreground = new SolidColorBrush(color_red);
                    }
                }));

                bTargetSatAstate = false;
                //-----------------
                // 三维视图数据（注意，标签不需要单独设定，已经与三维视图数据绑定）
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    Vector3D vec = new Vector3D();
                    rotate.Angle = Quat.w;
                    vec.X = Quat.x;
                    vec.Y = Quat.y;
                    vec.Z = Quat.z;

                    rotate.Axis = vec;
                }));
               
                //-----------------
                // 姿态参数和误差参数
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    // 姿态参数
                    angle_pitch.Content = dbAnglePitch.ToString();
                    angle_yaw.Content = dbAngleYaw.ToString();
                    angle_roll.Content = dbAngleRoll.ToString();
                    velocity_pitch.Content = dbVelocityPitch.ToString();
                    velocity_yaw.Content = dbVelocityYaw.ToString();
                    velocity_roll.Content = dbVelocityRoll.ToString();
                    
                }));
                //-----------------
                // 目标星温度（注意，标签不需要单独设定，已经与进度条绑定）
                //-----------------
                 Dispatcher.Invoke(new Action(() =>
                {
                    RotateTransform rt = new RotateTransform();
                    rt.CenterX = 100;
                    rt.CenterY = 90;

                    this.indicatorPin.RenderTransform = rt;
                    TemAngleOld = TemAngleNow;
                    TemAngleNow = (int)(dbTemperature - 15)*2+5;

                    double timeAnimation = Math.Abs(TemAngleOld - TemAngleNow) * 8;
                    DoubleAnimation da = new DoubleAnimation(TemAngleOld, TemAngleNow, new Duration(TimeSpan.FromMilliseconds(timeAnimation)));
                    da.AccelerationRatio = 1;
                    rt.BeginAnimation(RotateTransform.AngleProperty, da);
                    this.currentValueTxtBlock.Text =dbTemperature.ToString("f1");
                }));
                //-----------------
                // 星载电池电量（注意，标签不需要单独设定，已经与进度条绑定）
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    // 未连接时置电量为0，连接时按照接收的数据来置
                    pb_elec_quantity.ChargingProgress = nElecQuantity;

                    // 显示方式
                    if(nElecQuantity >= 30) // 电量大于30%使用绿色
                    {
                        pb_elec_quantity.Foreground = new SolidColorBrush(color_green);
                    }
                    else if(nElecQuantity >10 && nElecQuantity < 30) // 电量在10%~30%使用黄色
                    {
                        pb_elec_quantity.Foreground = new SolidColorBrush(color_yellow);
                    }
                    else // 电量小于10%使用红色
                    {
                        pb_elec_quantity.Foreground = new SolidColorBrush(color_red);
                    }
                }));
                //-----------------
                // 子系统指令状态
                //-----------------
                Dispatcher.Invoke(new Action(() =>
                {
                    // 飞轮
                    if(bFreewheelCmd == true)
                    {
                        freewheel_cmd.Content = "飞轮开";
                        freewheel_cmd.Background = new SolidColorBrush(color_green);
                    }
                    else
                    {
                        freewheel_cmd.Content = "飞轮关";
                        freewheel_cmd.Background = new SolidColorBrush(color_red);  
                    }
                    // 热控
                    if (bThermalctrlCmd == true)
                    {
                        thermalctrl_cmd.Content = "温控开";
                        thermalctrl_cmd.Background = new SolidColorBrush(color_green);
                        this.aim_temper.Content = "40°C";
                    }
                    else
                    {
                        thermalctrl_cmd.Content = "温控关";
                        thermalctrl_cmd.Background = new SolidColorBrush(color_red);
                        this.aim_temper.Content = "NA";
                    }
                   
                    // 磁力矩棒
                    if (bMagnetbarCmd == true)
                    {
                        magnetbar_cmd.Content = "磁控开";
                        magnetbar_cmd.Background = new SolidColorBrush(color_green);
                    }
                    else
                    {
                        magnetbar_cmd.Content = "磁控关";
                        magnetbar_cmd.Background = new SolidColorBrush(color_red);
                    }
                   
                    // 电源监控
                    if (bPowerctrlCmd == true)
                    {
                        powerctrl_cmd.Content = "电控开";
                        powerctrl_cmd.Background = new SolidColorBrush(color_green);
                    }
                    else
                    {
                        powerctrl_cmd.Content = "电控关";
                        powerctrl_cmd.Background = new SolidColorBrush(color_red);
                    }
                }));

            }
        }

        //-------------------------------------------------------
        // 控件事件
        //-------------------------------------------------------
        // ----------------------------------------
        // 名称：Grid_MouseLeftButtonDown
        // 用途：移动窗口
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
       // private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
       // {
        //    this.DragMove();
       // }

        // ----------------------------------------
        // 名称：btnCloseWindow
        // 用途：终止应用程序
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        private void btnCloseWindow(object sender, RoutedEventArgs e)
        {
            // 终止应用程序
            AppTerminate();
        }// btnCloseWindow

        // ----------------------------------------
        // 名称：btnMaxWindow
        // 用途：最大化窗口
        // 输入：无
        // 输出：无
        // 其它：禁用
        // ----------------------------------------
        private void btnMaxWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        // ----------------------------------------
        // 名称：btnMinWindow
        // 用途：最小化窗口
        // 输入：无
        // 输出：无
        // 其它：禁用
        // ----------------------------------------
        private void btnMinWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // ----------------------------------------
        // 名称：btnStopSatRunning
        // 用途：发送终止星载软件运行指令
        // 输入：无
        // 输出：无
        // 其它：禁用
        // ----------------------------------------
        private void btnStopSatRunning(object sender, RoutedEventArgs e)
        {
            // 终止星载计算机软件运行
            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x06;
            sendata[3] = 0x00;
            sendata[4] = 0x00;
            sendata[5] = 0x00;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }

        // ----------------------------------------
        // 名称：btnOn__Cmd * 6
        // 用途：发送卫星子系统开关指令
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        // 电源监控开关
        private void btnOnPowerctrlCmd(object sender, RoutedEventArgs e)
        {
            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x00;
            if (MyMessage.power_control == 0)
                sendata[3] = 0x01;
            else
                sendata[3] = 0x00;
            sendata[4] = 0x01;
            sendata[5] = 0x01;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }
        // 飞轮开关
        private void btnOnFreewheelCmd(object sender, RoutedEventArgs e)
        {

            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x01;
            if (MyMessage.wheel_control == 0)
                sendata[3] = 0x01;
            else
                sendata[3] = 0x00;
            sendata[4] = 0x01;
            sendata[5] = 0x01;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }
        // 温控开关
        private void btnOnThermalctrlCmd(object sender, RoutedEventArgs e)
        {
            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x02;
            if(MyMessage.temp_control == 0)
                sendata[3] = 0x01;
            else
                sendata[3] = 0x00;
            sendata[4] = 0x01;
            sendata[5] = 0x01;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }
       
        // 磁力矩棒开关
        private void btnOnMagnetbarCmd(object sender, RoutedEventArgs e)
        {
            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x03;
            if (MyMessage.magnetic_control == 0)
                sendata[3] = 0x01;
            else
                sendata[3] = 0x00;
            sendata[4] = 0x01;
            sendata[5] = 0x01;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }
          
        // ----------------------------------------
        // 名称：btnOnSendCtrlCmd
        // 用途：发送卫星控制指令
        // 输入：无
        // 输出：无
        // 其它：无
        // ----------------------------------------
        private void btnOnSendCtrlCmd(object sender, RoutedEventArgs e)
        {
            byte[] sendata = new byte[6];

            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x04;
            if(MyMessage.wheel_control>0)
            {
                // 发送控制指令
                short zhuan_su = Convert.ToInt16(wheel_speed_obj.Text);


                if (MyMessage.zhengorfan == true)
                        sendata[3] = (byte)(zhuan_su + 100);
                    else
                        sendata[3] = (byte)(100 - zhuan_su);
                    sendata[4] = 0x00;
                    sendata[5] = 0x00;
                    spPort.Write(sendata, 0, 6);//发送数据
                    spPort.DiscardOutBuffer();//清空发送缓冲区
              
            }
            else
                MessageBox.Show("飞轮处于关闭状态中");

        }
        //启动按钮
        //点击启动程序运行
        private void startover_Click(object sender, RoutedEventArgs e)
        {
            if (qidong == false)
            {
                // STEP2：初始化串口
                if (CommInit() == false)
                {
                    // 终止应用程序
                    this.Close();
                    // 正常返回（此处不能删除，否则线程资源不能正确释放）
                    return;
                }

                // STEP3：初始化接收线程
                recvThread = new Thread(serialPortRecieve);
                // 修改线程名称
                recvThread.Name = "recvThread";
                // 启动数据接收辅助线程
                recvThread.Start();

                // STEP4：初始化状态检查线程
                stateThread = new Thread(stateCheck);
                // 修改线程名称
                stateThread.Name = "stateCheck";
                // 启动数据接收辅助线程
                stateThread.Start();

                qidong = true;
                startover.Content = "建立成功";
                //使按钮可用
                freewheel_cmd.IsEnabled = true;
                thermalctrl_cmd.IsEnabled = true;
                magnetbar_cmd.IsEnabled = true;
                powerctrl_cmd.IsEnabled = true;
            }
        }
        //矫正按钮
        private void Rectify_dir_Click(object sender, RoutedEventArgs e)
        {
            MyMessage.jiaozheng_dir = MyMessage.HMC5883_value;
        }

        private void ZhengOrFan_Click(object sender, RoutedEventArgs e)
        {
            if (MyMessage.zhengorfan == true)
            {
                MyMessage.zhengorfan = false;
                ZhengOrFan.Content = "反转";
            }
            else
            {
                MyMessage.zhengorfan = true;
                ZhengOrFan.Content = "正转";
            }
               
        }

        private void wheel_stop1_Click(object sender, RoutedEventArgs e)
        {
            byte[] sendata = new byte[6];
            sendata[0] = 0xaa;
            sendata[1] = 0x55;
            sendata[2] = 0x04;
            sendata[3] = 0x64;
            sendata[4] = 0x00;
            sendata[5] = 0x00;
            sliderSchedule.Value = 0;
            spPort.Write(sendata, 0, 6);//发送数据
            spPort.DiscardOutBuffer();//清空发送缓冲区
        }
    }
}
