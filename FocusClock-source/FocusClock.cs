using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Focus Clock")]
[assembly: System.Reflection.AssemblyVersion("1.1.1.0")]
[assembly: System.Reflection.AssemblyDescription("专注 · Windows 番茄时钟")]

namespace FocusClock {
 public class Preferences {
  public string MainTheme="dark", FloatTheme="frost", Language="zh";
  public bool MainMechanical=false, FloatMechanical=false, LightDigits=false;
  public int MainTransparency=4, FloatTransparency=45, Size=120;
  public int FocusMinutes=25, ShortMinutes=5, LongMinutes=15;
  public bool Sound=true;
  public double FloatX=Double.NaN, FloatY=Double.NaN;
  public void Normalize(){ Size=Math.Max(100,Math.Min(200,Size));MainTransparency=Clamp(MainTransparency,0,100);FloatTransparency=Clamp(FloatTransparency,0,100);FocusMinutes=Clamp(FocusMinutes,1,180);ShortMinutes=Clamp(ShortMinutes,1,60);LongMinutes=Clamp(LongMinutes,1,120); if(!Valid(MainTheme))MainTheme="dark";if(!Valid(FloatTheme))FloatTheme="frost"; }
  static int Clamp(int v,int a,int b){return Math.Max(a,Math.Min(b,v));}
  static bool Valid(string v){return v=="dark"||v=="light"||v=="transparent"||v=="frost";}
 }
 public class TimerModel {
  public int Mode; public double Remaining=1500,Duration=1500; public bool Running,Started;
  DateTime end;
  public string Name {get{return new[]{Program.T("专注"),Program.T("短休息"),Program.T("长休息")}[Mode];}}
  public void Select(int mode,Preferences p){Mode=mode;Duration=60*(mode==0?p.FocusMinutes:mode==1?p.ShortMinutes:p.LongMinutes);Reset();}
  public void Reset(){Remaining=Duration;Running=false;Started=false;}
  public void Toggle(DateTime now){Tick(now);if(Running){Running=false;return;}if(Remaining<=0)Remaining=Duration;Started=true;Running=true;end=now.AddSeconds(Remaining);}
  public bool Tick(DateTime now){if(!Running)return false;Remaining=Math.Max(0,(end-now).TotalSeconds);if(Remaining<=0){Running=false;return true;}return false;}
  public string TimeText {get{int t=(int)Math.Ceiling(Remaining);return (t/60).ToString("00")+":"+(t%60).ToString("00");}}
  public string Status {get{if(Program.P.Language=="en")return Remaining<=0?"Done":Running?"Running":Started?"Paused":"Ready";return Remaining<=0?Program.T("已完成"):Running?Name+Program.T("中"):Started?Program.T("已暂停"):Program.T("准备")+Name;}}
 }
 public class Program {
  public static string T(string text){return P.Language=="en"?Localization.Translate(text):text;}
  public static Preferences P=new Preferences(); public static TimerModel Clock=new TimerModel();
  public static ClockWindow MainWindow, Floating; static SettingsWindow settings; static Forms.NotifyIcon tray; static DispatcherTimer ticker; static bool exiting,verify;
  static string config=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusClock","settings.xml");
  [STAThread] public static void Main(string[] args){
   if(args.Length>0 && args[0]=="--self-test"){SelfTest(args.Length>1?args[1]:"self-test.txt");return;}
   verify=args.Length>0&&args[0]=="--verify";
   bool owned;using(var mutex=new System.Threading.Mutex(true,"Local\\FocusClock.V1",out owned)){
    if(!owned&&!verify){MessageBox.Show(Program.T("番茄时钟已在运行。请从系统托盘打开。"),Program.T("专注"));return;}
    var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
    app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(Program.T("发生错误：")+e.Exception.Message,Program.T("专注"));e.Handled=true;};
    if(!verify)Load();Clock.Select(0,P);
    MainWindow=new ClockWindow(false); Floating=new ClockWindow(true);
    if(verify){Verify(args[1]);return;}
    tray=new Forms.NotifyIcon();tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);tray.Text=Program.T("专注 · 番茄时钟");tray.Visible=true;
    var menu=new Forms.ContextMenuStrip();menu.Items.Add(Program.T("打开主窗口"),null,(s,e)=>ShowMain());menu.Items.Add(Program.T("显示悬浮窗"),null,(s,e)=>ShowFloat());menu.Items.Add(Program.T("暂停 / 继续"),null,(s,e)=>Toggle());menu.Items.Add(Program.T("设置"),null,(s,e)=>Settings());menu.Items.Add(Program.T("退出"),null,(s,e)=>Exit());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowMain();tray.BalloonTipClicked+=(s,e)=>ShowMain();
    ticker=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(50)};ticker.Tick+=(s,e)=>{if(Clock.Tick(DateTime.UtcNow)){if(P.Sound)System.Media.SystemSounds.Asterisk.Play();tray.ShowBalloonTip(5000,Clock.Name+(P.Language=="en"?" complete":"完成"),Program.T("本轮计时已结束。可从主窗口选择下一阶段。"),Forms.ToolTipIcon.Info);}MainWindow.Face.InvalidateVisual();Floating.Face.InvalidateVisual();};ticker.Start();
    MainWindow.Show();app.Run();tray.Dispose();
   }
  }
  static void Load(){try{if(File.Exists(config)){using(var f=File.OpenRead(config))P=(Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(f);P.Normalize();P.FloatMechanical=P.MainMechanical;}}catch{P=new Preferences();}}
  public static void Save(){if(verify)return;try{Directory.CreateDirectory(Path.GetDirectoryName(config));string tmp=config+".tmp";using(var f=File.Create(tmp))new XmlSerializer(typeof(Preferences)).Serialize(f,P);if(File.Exists(config))File.Replace(tmp,config,null);else File.Move(tmp,config);}catch(Exception e){if(tray!=null)tray.ShowBalloonTip(3000,Program.T("设置未保存"),e.Message,Forms.ToolTipIcon.Warning);}}
  public static void SetShape(bool mechanical){P.MainMechanical=mechanical;P.FloatMechanical=mechanical;Refresh();}
  public static void Refresh(){P.Normalize();MainWindow.UpdateAppearance();Floating.UpdateAppearance();Save();}
  public static void Toggle(){Clock.Toggle(DateTime.UtcNow);RefreshFaces();}
  public static void Reset(){Clock.Reset();RefreshFaces();}
  public static void Select(int mode){Clock.Select(mode,P);RefreshFaces();}
  static void RefreshFaces(){MainWindow.Face.InvalidateVisual();Floating.Face.InvalidateVisual();}
  public static void ShowMain(){if(settings!=null){settings.Commit();settings.Hide();}MainWindow.Show();MainWindow.WindowState=WindowState.Normal;MainWindow.Activate();Floating.Hide();}
  public static void ShowFloat(){if(settings!=null)settings.Commit();Floating.UpdateAppearance();Floating.Show();Floating.Activate();MainWindow.Hide();if(settings!=null)settings.Hide();}
  public static void Settings(){if(settings==null){settings=new SettingsWindow();settings.Closed+=(s,e)=>{settings=null;if(!exiting&&!verify)ShowFloat();};}settings.Show();settings.WindowState=WindowState.Normal;settings.Activate();MainWindow.Hide();Floating.Hide();}
  public static void LanguageChanged(){Refresh();if(tray!=null){string[] labels={Program.T("打开主窗口"),Program.T("显示悬浮窗"),Program.T("暂停 / 继续"),Program.T("设置"),Program.T("退出")};for(int i=0;i<labels.Length;i++)tray.ContextMenuStrip.Items[i].Text=T(labels[i]);tray.Text=T(Program.T("专注 · 番茄时钟"));}MainWindow.RefreshMenu();Floating.RefreshMenu();}
  public static void Exit(){if(exiting)return;exiting=true;Save();if(ticker!=null)ticker.Stop();if(tray!=null)tray.Visible=false;Application.Current.Shutdown();}
  public static void SelfTest(string path){try{var p=new Preferences();var m=new TimerModel();var t=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);m.Select(0,p);Check(m.TimeText=="25:00","initial");m.Toggle(t);m.Tick(t.AddSeconds(1));Check(m.TimeText=="24:59","elapsed");m.Toggle(t.AddSeconds(10));Check(!m.Running&&m.Remaining==1490,"pause");m.Tick(t.AddHours(1));Check(m.Remaining==1490,"pause stable");m.Toggle(t.AddHours(1));Check(m.Tick(t.AddHours(2))&&!m.Running&&m.Remaining==0,"sleep catch-up");Check(!m.Tick(t.AddHours(3)),"notify once");m.Toggle(t.AddHours(3));Check(m.Running&&m.Remaining==1500,"restart");m.Select(1,p);Check(m.TimeText=="05:00"&&!m.Running,"short");m.Select(2,p);Check(m.TimeText=="15:00","long");m.Reset();Check(!m.Started,"reset");p.Size=999;p.MainTransparency=-5;p.Normalize();Check(p.Size==200&&p.MainTransparency==0,"bounds");var x=new XmlSerializer(typeof(Preferences));using(var w=new StringWriter()){x.Serialize(w,p);using(var r=new StringReader(w.ToString()))Check(((Preferences)x.Deserialize(r)).Size==200,"persistence");}File.WriteAllText(path,"PASS: initial, elapsed, pause, resume, sleep catch-up, single completion, restart, phases, reset, settings bounds, settings serialization.");}catch(Exception e){File.WriteAllText(path,"FAIL: "+e);Environment.ExitCode=1;}}
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
  static void Verify(string folder){Directory.CreateDirectory(folder);try{foreach(bool mech in new[]{false,true})foreach(string theme in new[]{"dark","light","transparent","frost"}){P.MainMechanical=mech;P.FloatMechanical=mech;P.MainTheme=P.FloatTheme=theme;P.MainTransparency=P.FloatTransparency=theme=="transparent"?100:theme=="frost"?45:4;P.Size=120;Refresh();Snapshot(MainWindow,Path.Combine(folder,"MainWindow-"+(mech?"rect":"ring")+"-"+theme+".png"));foreach(int size in new[]{100,120,200}){P.Size=size;Floating.UpdateAppearance();Snapshot(Floating,Path.Combine(folder,"float-"+(mech?"rect":"ring")+"-"+theme+"-"+size+".png"));}}VerifyNavigation(folder);File.WriteAllText(Path.Combine(folder,"render-check.txt"),"PASS: 8 MainWindow-window and 24 floating-window renders; shapes, themes and size limits.");}catch(Exception e){File.WriteAllText(Path.Combine(folder,"render-check.txt"),"FAIL: "+e);Environment.ExitCode=1;}Application.Current.Shutdown();}
  static void VerifyNavigation(string folder){
   Clock.Select(0,P);Clock.Toggle(DateTime.UtcNow);double remaining=Clock.Remaining;
   ShowFloat();Check(Floating.IsVisible&&!MainWindow.IsVisible,"floating visibility");
   foreach(bool mechanical in new[]{false,true}){P.FloatMechanical=mechanical;P.Size=100;Floating.UpdateAppearance();Snapshot(Floating,Path.Combine(folder,"double-click-"+(mechanical?"rectangle":"ring")+".png"));Floating.Face.NavigateDoubleClick(Floating.Face.DigitCenter);Check(MainWindow.IsVisible&&!Floating.IsVisible&&Clock.Running&&Clock.Remaining==remaining,"floating digits return to main without resetting");MainWindow.Face.NavigateDoubleClick(new Point(180,240));Check(Floating.IsVisible&&!MainWindow.IsVisible&&Clock.Running&&Clock.Remaining==remaining,"main returns to floating without resetting");}
   Settings();Check(settings.IsVisible&&!Floating.IsVisible,"settings visibility");Check(Clock.Running&&Clock.Remaining==remaining,"settings preserves clock");
   settings.ChooseShape(0,true);Check(P.MainMechanical&&P.FloatMechanical&&settings.ShapesMatch(true),"main shape syncs float and both selectors");settings.ChooseShape(1,false);Check(!P.MainMechanical&&!P.FloatMechanical&&settings.ShapesMatch(false)&&Clock.Running&&Clock.Remaining==remaining,"float shape syncs main without resetting timer");
   settings.Complete();Check(MainWindow.IsVisible&&!Floating.IsVisible&&!settings.IsVisible&&Clock.Running&&Clock.Remaining==remaining,"Done returns to main without resetting timer");Settings();
   P.Language="en";LanguageChanged();settings.Build();Check(Clock.Running&&Clock.Remaining==remaining,"language preserves clock");Check(Clock.Name=="Focus"&&Clock.Status=="Running","English timer labels");Check(T("形状")=="Shape"&&T("设置")=="Settings","English settings labels");
   P.MainMechanical=false;P.MainTheme="dark";P.MainTransparency=4;Refresh();Snapshot(MainWindow,Path.Combine(folder,"english-main.png"));
   P.Size=100;P.FloatMechanical=false;Floating.UpdateAppearance();Snapshot(Floating,Path.Combine(folder,"english-floating-100.png"));
   settings.Width=480;settings.Height=900;settings.UpdateLayout();SnapshotElement(settings,480,900,Path.Combine(folder,"english-settings.png"));
   ShowFloat();Check(Floating.IsVisible&&!settings.IsVisible&&Clock.Running,"return preserves clock");
   P.Language="zh";LanguageChanged();Check(Clock.Name=="专注","Chinese restored");Settings();settings.Build();SnapshotElement(settings,480,900,Path.Combine(folder,"chinese-settings.png"));ShowFloat();
   Clock.Tick(DateTime.UtcNow.AddSeconds(2));Check(Clock.Running&&Clock.Remaining<remaining,"clock advances after navigation");
   var x=new XmlSerializer(typeof(Preferences));P.Language="en";using(var w=new StringWriter()){x.Serialize(w,P);using(var r=new StringReader(w.ToString()))Check(((Preferences)x.Deserialize(r)).Language=="en","language persistence");}
   File.WriteAllText(Path.Combine(folder,"navigation-check.txt"),"PASS: settings / floating navigation, uninterrupted running timer, English / Chinese labels, language serialization, live window visibility.");
  }
  static void SnapshotElement(FrameworkElement element,int width,int height,string path){element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));element.UpdateLayout();var bmp=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bmp.Render(element);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(path))enc.Save(f);}
  static void Snapshot(ClockWindow w,string path){w.Face.Measure(new Size(w.Width,w.Height));w.Face.Arrange(new Rect(0,0,w.Width,w.Height));w.Face.UpdateLayout();var bmp=new RenderTargetBitmap((int)w.Width,(int)w.Height,96,96,PixelFormats.Pbgra32);bmp.Render(w.Face);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(path))enc.Save(f);}
 }
 public class ClockWindow:Window {
  public ClockFace Face; public bool IsFloating;
  public ClockWindow(bool floating){IsFloating=floating;Title=floating?Program.T("专注 · 悬浮窗"):Program.T("专注 · Focus Clock");WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=floating;ShowInTaskbar=!floating;Face=new ClockFace(this);Content=Face;UpdateAppearance();if(floating){Rect r=SystemParameters.WorkArea;Left=Double.IsNaN(Program.P.FloatX)?r.Right-Width-32:Program.P.FloatX;Top=Double.IsNaN(Program.P.FloatY)?r.Top+80:Program.P.FloatY;ClampPosition();}else WindowStartupLocation=WindowStartupLocation.CenterScreen;
   Closing+=(s,e)=>{if(Application.Current!=null&&Application.Current.ShutdownMode==ShutdownMode.OnExplicitShutdown){e.Cancel=true;Hide();}};
   PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Space){Program.Toggle();e.Handled=true;}else if(e.Key==Key.R){Program.Reset();e.Handled=true;}else if(e.Key==Key.Escape){if(IsFloating)Program.ShowMain();else Program.ShowFloat();}else if(e.Key==Key.F2)Program.Settings();};
   RefreshMenu();
  }
  public void RefreshMenu(){Title=IsFloating?Program.T("专注 · 悬浮窗"):Program.T("专注 · Focus Clock");var cm=new ContextMenu();Add(cm,Program.T("返回主窗口"),Program.ShowMain);Add(cm,Program.T("切换悬浮窗"),Program.ShowFloat);Add(cm,Program.T("暂停 / 继续    Space"),Program.Toggle);Add(cm,Program.T("重置    R"),Program.Reset);Add(cm,Program.T("设置    F2"),Program.Settings);Add(cm,Program.T("退出"),Program.Exit);ContextMenu=cm;
  }
  static void Add(ContextMenu m,string text,Action a){var i=new MenuItem{Header=text};i.Click+=(s,e)=>a();m.Items.Add(i);}
  public void UpdateAppearance(){Width=IsFloating?Program.P.Size:360;Height=IsFloating?(Program.P.FloatMechanical?Math.Max(100,Program.P.Size*.76):Program.P.Size):480;Face.InvalidateVisual();}
  public void ClampPosition(){var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)Left,(int)Top));var area=screen.WorkingArea;var source=PresentationSource.FromVisual(this);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;Left=Math.Max(area.Left/scale,Math.Min(Left,area.Right/scale-Width));Top=Math.Max(area.Top/scale,Math.Min(Top,area.Bottom/scale-Height));}
  public void Drag(){try{DragMove();ClampPosition();if(IsFloating){Program.P.FloatX=Left;Program.P.FloatY=Top;Program.Save();}}catch(InvalidOperationException){}}
 }
 public class ClockFace:FrameworkElement {
  readonly ClockWindow owner; bool hover; readonly List<Hit> hits=new List<Hit>(); string previous="";DateTime flipped=DateTime.MinValue;
  class Hit {public Rect R; public Action A;public Hit(Rect r,Action a){R=r;A=a;}}
  Rect digitBounds; Point pressPoint; bool dragPending;
  public ClockFace(ClockWindow w){owner=w;Focusable=true;
   MouseEnter+=(s,e)=>{hover=true;InvalidateVisual();};MouseLeave+=(s,e)=>{hover=false;InvalidateVisual();};
   MouseLeftButtonDown+=(s,e)=>{Focus();Point p=e.GetPosition(this);foreach(var h in hits)if(h.R.Contains(p)){if(e.ClickCount==1)h.A();e.Handled=true;return;}if(e.ClickCount==2){dragPending=false;NavigateDoubleClick(p);e.Handled=true;return;}pressPoint=p;dragPending=true;e.Handled=true;};
   MouseLeftButtonUp+=(s,e)=>dragPending=false;
   MouseMove+=(s,e)=>{Point p=e.GetPosition(this);if(dragPending&&e.LeftButton==MouseButtonState.Pressed&&(Math.Abs(p.X-pressPoint.X)>SystemParameters.MinimumHorizontalDragDistance||Math.Abs(p.Y-pressPoint.Y)>SystemParameters.MinimumVerticalDragDistance)){dragPending=false;owner.Drag();return;}Cursor=owner.IsFloating&&digitBounds.Contains(p)?Cursors.Hand:Cursors.SizeAll;foreach(var h in hits)if(h.R.Contains(p)){Cursor=Cursors.Hand;break;}};
  }
  public Point DigitCenter {get{return new Point(digitBounds.X+digitBounds.Width/2,digitBounds.Y+digitBounds.Height/2);}}
  public void NavigateDoubleClick(Point p){if(owner.IsFloating){if(digitBounds.Contains(p))Program.ShowMain();}else Program.ShowFloat();}
  static SolidColorBrush B(string s){return (SolidColorBrush)new BrushConverter().ConvertFromString(s);}
  static Brush Alpha(string s,double a){Color c=((SolidColorBrush)B(s)).Color;c.A=(byte)Math.Max(0,Math.Min(255,255*a));return new SolidColorBrush(c);}
  static void Text(DrawingContext d,string s,double x,double y,double size,Brush ink,bool center,string family){var f=new FormattedText(s,System.Globalization.CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(family),size,ink);d.DrawText(f,new Point(center?x-f.Width/2:x,y));}
  void Button(DrawingContext d,string s,Rect r,Brush bg,Brush ink,Action a,double font){d.DrawRoundedRectangle(bg,null,r,8,8);Text(d,s,r.X+r.Width/2,r.Y+(r.Height-font*1.35)/2,font,ink,true,"Segoe UI");hits.Add(new Hit(r,a));}
  protected override void OnRender(DrawingContext d){base.OnRender(d);hits.Clear();var p=Program.P;var m=Program.Clock;bool mini=owner.IsFloating;bool mech=mini?p.FloatMechanical:p.MainMechanical;string theme=mini?p.FloatTheme:p.MainTheme;bool dark=theme=="dark";if(mini&&mech)dark=p.LightDigits;string fg=dark?"#ECEEF0":"#434A52";Brush ink=B(fg),sub=B(dark?"#B4BAC3":"#5C6570");string surface=dark?"#191B21":"#F9FBFF";double transparency=(mini?p.FloatTransparency:p.MainTransparency)/100.0;
   Brush phase=m.Mode==0?ink:B(m.Mode==1?(dark?"#9FAD9E":"#526C60"):(dark?"#9FAEBC":"#53697E"));double w=ActualWidth,h=ActualHeight;if(w<1||h<1)return;
   // A nearly transparent hit surface permits dragging clear windows without click-through.
   d.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(1,0,0,0)),null,new Rect(0,0,w,h),mini&&!mech?w/2:12,mini&&!mech?w/2:12);
   if(!(mini&&mech)&&theme!="transparent"){
    var rect=new Rect(.5,.5,w-1,h-1);Brush bg=Alpha(surface,1-transparency);d.DrawRoundedRectangle(bg,new Pen(Alpha(dark?"#FFFFFF":"#718090",.12),1),rect,mini&&!mech?w/2:mech?12:28,mini&&!mech?w/2:mech?12:28);
    if(theme=="frost"){var glass=new LinearGradientBrush();glass.StartPoint=new Point(0,0);glass.EndPoint=new Point(1,1);glass.GradientStops.Add(new GradientStop(Color.FromArgb(28,255,255,255),0));glass.GradientStops.Add(new GradientStop(Color.FromArgb(3,230,235,245),1));d.DrawRoundedRectangle(glass,null,rect,mini&&!mech?w/2:mech?12:28,mini&&!mech?w/2:mech?12:28);}
   }
   if(!mini){Text(d,mech?"FOCUS · TIMER":Program.T("专注 · FOCUS"),24,24,14,ink,false,mech?"Georgia":"Segoe UI");Button(d,"↗",new Rect(234,16,30,30),Brushes.Transparent,ink,Program.ShowFloat,18);Button(d,"⚙",new Rect(270,16,30,30),Brushes.Transparent,ink,Program.Settings,17);Button(d,"×",new Rect(306,16,30,30),Brushes.Transparent,ink,()=>owner.Hide(),20);
    string[] names={Program.T("专注"),Program.T("短休息"),Program.T("长休息")};for(int i=0;i<3;i++){int mode=i;Button(d,names[i],new Rect(24+i*104,76,100,34),i==m.Mode?Alpha(fg,.13):Brushes.Transparent,i==m.Mode?ink:sub,()=>Program.Select(mode),12);}
   }
   double cx=w/2,cy=mini?h/2:244;double diam=mini?w-10:218;
   if(!mech){double r=diam/2;d.DrawEllipse(null,new Pen(Alpha(fg,.10),mini?1.4:2),new Point(cx,cy),r,r);double ratio=Math.Max(0,Math.Min(1,m.Remaining/m.Duration));var pen=new Pen(m.Mode==0?B(dark?"#B5BBC1":"#747A80"):phase,mini?1.5:2);pen.StartLineCap=pen.EndLineCap=PenLineCap.Round;if(ratio>=.99999)d.DrawEllipse(null,pen,new Point(cx,cy),r,r);else if(ratio>0){double angle=ratio*2*Math.PI;var geo=new StreamGeometry();using(var c=geo.Open()){c.BeginFigure(new Point(cx,cy-r),false,false);c.ArcTo(new Point(cx+r*Math.Sin(angle),cy-r*Math.Cos(angle)),new Size(r,r),0,ratio>.5,SweepDirection.Clockwise,true,false);}d.DrawGeometry(null,pen,geo);}
   }
   string time=m.TimeText;if(previous!=time){previous=time;flipped=DateTime.UtcNow;}
   double fs=mini?w*(mech?.30:.23):mech?50:52;double ty=mini?(mech?7:cy-fs*.70-(hover?12:0)):cy-40;
   digitBounds=new Rect(cx-(mini?w*.48:130),ty-3,mini?w*.96:260,fs*1.4);
   if(mech){double bw=mini?w-8:246,bh=mini?fs*1.3:80;double by=mini?ty-2:ty-6;var rr=new Rect(cx-bw/2,by,bw,bh);d.DrawRoundedRectangle(Alpha(surface,mini?.55:.65),new Pen(Alpha(fg,.15),1),rr,mini?5:8,mini?5:8);d.DrawLine(new Pen(Alpha(fg,.22),1),new Point(rr.Left+1,rr.Top+rr.Height/2),new Point(rr.Right-1,rr.Top+rr.Height/2));
    double elapsed=(DateTime.UtcNow-flipped).TotalMilliseconds;double scale=elapsed<180&&m.Running? .82+.18*Math.Min(1,elapsed/180):1;d.PushTransform(new ScaleTransform(1,scale,cx,by+bh/2));Text(d,time,cx,ty,fs,phase,true,"Consolas");d.Pop();
   }else Text(d,time,cx,ty,fs,phase,true,"Segoe UI Light");
   if(!mini||mech||!hover)Text(d,m.Status,cx,mini?(mech?ty+fs*1.3+4:cy+fs*.58):mech?cy+49:cy+30,mini?11:12,sub,true,"Segoe UI");
   Brush wash=Alpha(fg,.10);if(mini){if(hover||IsKeyboardFocusWithin){double by=mech?h-29:cy+9;Button(d,m.Running?"Ⅱ":"▶",new Rect(cx-32,by,29,25),wash,ink,Program.Toggle,13);Button(d,"↺",new Rect(cx+3,by,29,25),wash,ink,Program.Reset,17);}}else{Button(d,m.Remaining<=0?Program.T("重新开始"):m.Running?Program.T("暂停"):m.Started?Program.T("继续"):Program.P.Language=="en"?"Start":Program.T("开始")+m.Name,new Rect(78,376,140,43),wash,ink,Program.Toggle,14);Button(d,"↺",new Rect(232,376,45,43),wash,ink,Program.Reset,22);Text(d,Program.T("一次，只做一件事"),180,445,11,sub,true,"Segoe UI");}
  }
 }
 public class SettingsWindow:Window {
  StackPanel panel; bool initializing=true, syncingShapes;readonly List<Action> commits=new List<Action>();readonly List<ComboBox> shapeSelectors=new List<ComboBox>();
  public void Commit(){foreach(var commit in commits)commit();}
  public void ChooseShape(int source,bool mechanical){shapeSelectors[source].SelectedIndex=mechanical?1:0;}
  public bool ShapesMatch(bool mechanical){return shapeSelectors.TrueForAll(c=>c.SelectedIndex==(mechanical?1:0));}
  public void Complete(){Commit();Program.ShowMain();}
  public void Build(){initializing=true;commits.Clear();shapeSelectors.Clear();
  Title=Program.T("专注 · 设置");Width=430;Height=690;MinWidth=400;MinHeight=520;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=new SolidColorBrush(Color.FromRgb(245,246,248));panel=new StackPanel{Margin=new Thickness(24)};Content=new ScrollViewer{Background=Background,Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
   var language=new ComboBox{ItemsSource=new[]{"简体中文","English"},SelectedIndex=Program.P.Language=="en"?1:0};Row(Program.T("语言 / Language")).Children.Add(language);language.SelectionChanged+=(s,e)=>{if(!initializing){Commit();Program.P.Language=language.SelectedIndex==1?"en":"zh";Program.LanguageChanged();Dispatcher.BeginInvoke(new Action(Build));}};
   Note(Program.T("双击空白处返回悬浮窗，计时不中断。"));
   Heading(Program.T("主窗口"));Shape(Program.T("形状"),Program.P.MainMechanical,v=>Program.P.MainMechanical=v);Theme(Program.T("主题"),Program.P.MainTheme,v=>{Program.P.MainTheme=v;});Range(Program.T("背景透明度"),Program.P.MainTransparency,0,100,"%",v=>Program.P.MainTransparency=v);
   Heading(Program.T("悬浮窗 · 始终置顶"));Shape(Program.T("形状"),Program.P.FloatMechanical,v=>Program.P.FloatMechanical=v);Theme(Program.T("圆环主题"),Program.P.FloatTheme,v=>Program.P.FloatTheme=v);Range(Program.T("圆环背景透明度"),Program.P.FloatTransparency,0,100,"%",v=>Program.P.FloatTransparency=v);Range(Program.T("尺寸 / 矩形宽度"),Program.P.Size,100,200," px",v=>Program.P.Size=v);var ink=new CheckBox{Content=Program.T("矩形使用浅色数字 / 深色底板"),IsChecked=Program.P.LightDigits,Margin=new Thickness(0,8,0,4)};ink.Click+=(s,e)=>{Program.P.LightDigits=ink.IsChecked==true;Program.Refresh();};panel.Children.Add(ink);Note(Program.T("矩形无外层背景；数字底板固定为 45% 透明度。"));
   Heading(Program.T("计时"));Minutes(Program.T("专注"),Program.P.FocusMinutes,180,v=>Program.P.FocusMinutes=v);Minutes(Program.T("短休息"),Program.P.ShortMinutes,60,v=>Program.P.ShortMinutes=v);Minutes(Program.T("长休息"),Program.P.LongMinutes,120,v=>Program.P.LongMinutes=v);Note(Program.T("时长修改在下一次选择阶段时生效。完成后手动选择下一阶段。"));var sound=new CheckBox{Content=Program.T("完成时播放提示音"),IsChecked=Program.P.Sound,Margin=new Thickness(0,8,0,8)};sound.Click+=(s,e)=>{Program.P.Sound=sound.IsChecked==true;Program.Save();};panel.Children.Add(sound);
   Note(Program.T("空格：暂停 / 继续    R：重置    F2：设置\n拖动空白处移动；双击悬浮数字进入主窗口；右键打开菜单。\n磨砂主题为半透明玻璃外观，不模糊桌面内容。"));var done=new Button{Content=Program.T("完成"),Padding=new Thickness(24,8,24,8),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,0,0)};done.Click+=(s,e)=>Complete();panel.Children.Add(done);initializing=false;
  }
  public SettingsWindow(){Build();Closing+=(s,e)=>Commit();PreviewMouseDown+=(s,e)=>{if(e.ChangedButton!=MouseButton.Left||e.ClickCount!=2)return;DependencyObject node=e.OriginalSource as DependencyObject;while(node!=null&&node!=this){if(node is System.Windows.Controls.Primitives.ButtonBase||node is System.Windows.Controls.Primitives.TextBoxBase||node is System.Windows.Controls.Primitives.RangeBase||node is ComboBox)return;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}e.Handled=true;Commit();Program.ShowFloat();};}
  void Heading(string title){panel.Children.Add(new TextBlock{Text=title,FontSize=16,FontWeight=FontWeights.Medium,Margin=new Thickness(0,16,0,8)});}
  void Note(string text){panel.Children.Add(new TextBlock{Text=text,FontSize=12,Foreground=Brushes.DimGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,4)});}
  DockPanel Row(string name){var row=new DockPanel{Margin=new Thickness(0,5,0,5)};row.Children.Add(new TextBlock{Text=name,Width=170,VerticalAlignment=VerticalAlignment.Center});panel.Children.Add(row);return row;}
  void Shape(string label,bool value,Action<bool> set){var combo=new ComboBox{ItemsSource=new[]{Program.T("圆环 · 极简 iOS"),Program.T("倒角矩形 · 机械钟")},SelectedIndex=value?1:0};shapeSelectors.Add(combo);Row(label).Children.Add(combo);combo.SelectionChanged+=(s,e)=>{if(!initializing&&!syncingShapes){bool mechanical=combo.SelectedIndex==1;syncingShapes=true;try{Program.SetShape(mechanical);foreach(var selector in shapeSelectors)selector.SelectedIndex=mechanical?1:0;}finally{syncingShapes=false;}}};}
  void Theme(string label,string value,Action<string> set){var keys=new[]{"dark","light","transparent","frost"};var combo=new ComboBox{ItemsSource=new[]{Program.T("深色"),Program.T("浅色"),Program.T("透明"),Program.T("磨砂半透明")},SelectedIndex=Array.IndexOf(keys,value)};Row(label).Children.Add(combo);combo.SelectionChanged+=(s,e)=>{if(!initializing){set(keys[combo.SelectedIndex]);Program.Refresh();}};}
  void Range(string name,int value,int min,int max,string unit,Action<int> set){var row=Row(name);var wrap=new StackPanel();var label=new TextBlock{Text=value+unit,HorizontalAlignment=HorizontalAlignment.Right};var slider=new Slider{Minimum=min,Maximum=max,Value=value,TickFrequency=1,IsSnapToTickEnabled=true};wrap.Children.Add(label);wrap.Children.Add(slider);row.Children.Add(wrap);slider.ValueChanged+=(s,e)=>{label.Text=((int)slider.Value)+unit;if(!initializing){set((int)slider.Value);Program.Refresh();}};}
  void Minutes(string name,int value,int max,Action<int> set){var box=new TextBox{Text=value.ToString(),Width=70,HorizontalAlignment=HorizontalAlignment.Left};Row(name+Program.T("（分钟）")).Children.Add(box);Action commit=()=>{int n;if(Int32.TryParse(box.Text,out n)&&n>=1&&n<=max){set(n);box.ClearValue(TextBox.BorderBrushProperty);Program.Save();}else{box.BorderBrush=Brushes.IndianRed;box.ToolTip=Program.T("请输入 1–")+max+Program.T(" 的整数");box.Text=value.ToString();}};commits.Add(commit);box.LostFocus+=(s,e)=>commit();}
 }
}
