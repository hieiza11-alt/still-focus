# Still · 静刻

**少一点干扰，多一点专注。**  
*A little less noise. A little more focus.*

Still 是一款轻量的 Windows 番茄时钟。简洁的主窗口，安静的桌面悬浮钟，让时间保持可见，也让注意力留在眼前。

## 功能

- **三种计时阶段**：专注、短休息、长休息，时长可调整。
- **两种时钟样式**：极简圆环与复古机械矩形，主窗口和悬浮窗同步切换。
- **桌面悬浮钟**：始终置顶，可拖动，尺寸支持 100–200。
- **独立外观设置**：主窗口与圆环悬浮窗分别支持深色、浅色、透明及半透明玻璃外观。
- **轻量矩形模式**：没有外圈与外层背景，仅保留半透明数字底板。
- **中英文界面**：随时切换，自动保存语言选择。
- **本地运行**：免安装，无需账号，不联网。

## 开始使用

1. 在 Releases 中下载发布包并解压。
2. 双击 `FocusClock.exe`。
3. 选择计时阶段，点击开始。

默认时长为专注 **25 分钟**、短休息 **5 分钟**、长休息 **15 分钟**。

目前可执行文件名为 `FocusClock.exe`。

## 操作方式

| 操作 | 效果 |
| --- | --- |
| 点击主窗口右上角 ↗ | 切换到悬浮窗 |
| 双击悬浮窗数字 | 返回主窗口 |
| 双击主窗口非按钮区域 | 返回悬浮窗 |
| 双击设置空白处 | 返回悬浮窗 |
| 点击设置“完成” | 返回主窗口 |
| 悬停悬浮窗 | 显示暂停／继续和重置 |
| 拖动空白区域 | 移动窗口 |
| 右键 | 打开菜单 |
| `Space` | 开始／暂停／继续 |
| `R` | 重置当前计时 |
| `F2` | 打开设置 |

切换窗口、形状或语言不会重置正在进行的计时。

主窗口的关闭按钮会将程序收起到系统托盘。完全退出时，请使用右键菜单或托盘菜单中的“退出”。

## 外观

专注采用中性灰白色，短休息采用柔和绿色，长休息采用柔和蓝色。

圆环随剩余时间逐渐减少。矩形模式使用机械数字窗，悬浮数字底板保持 45% 透明度。

磨砂主题目前采用半透明玻璃外观，不模糊桌面后方内容。

## 计时与设置

- 完成后提供声音和系统通知，不自动切换下一阶段。
- 睡眠期间仍按实际经过的时间计时，唤醒后校正。
- 退出程序后不继续计时，重新打开进入准备状态。
- 设置保存在 `%LOCALAPPDATA%\FocusClock\settings.xml`。
- 不添加开机启动，也不自动创建桌面快捷方式。

## 运行环境

- Windows 11
- .NET Framework 4.x 桌面运行环境

窗口尺寸使用 Windows 逻辑像素，实际显示大小随系统缩放设置变化。

## 从源码构建

源码位于 `FocusClock-source`，使用 C# 与 WPF 编写。

在 Windows PowerShell 中运行：

```powershell
.\FocusClock-source\build.ps1
```

构建脚本使用系统 .NET Framework C# 编译器，输出 `FocusClock.exe`。

## 验证范围

计时逻辑、窗口切换后的计时连续性、中英文显示和形状同步已有自动检查。
跨显示器混合 DPI、系统通知实际弹出和双击手势的完整桌面回归仍待进一步验证。

## English

Still is a quiet, minimal Pomodoro timer for Windows.

Choose a clean progress ring or a mechanical-style rectangular clock, keep a small timer above your desktop, and switch between English and Simplified Chinese. Window and appearance changes keep your current timer running.

Download a release, extract it, and launch `FocusClock.exe`. No installation, account, or network connection is required.
