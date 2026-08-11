# 项目搭建与调试配置

在新建希沃白板原生插件项目、修复项目文件、设置宿主调试或解释安装产物时读取本文。

完成项目配置后，如需加入可运行的菜单、工具栏或元素操作，继续读取 [complete-examples.md](complete-examples.md)。

## 兼容性基线

本文以 `net6.0-windows`、`dotnetCampus.EasiPlugin.Sdk` 2.1.1-alpha.3 和 EasiNote 5 为已知示例组合。宿主内部类型、安装目录、调试参数和安装包实现可能随版本变化；公开 SDK 类型应以项目实际还原的包为准，宿主行为应在目标 EasiNote 版本中验证。

## 环境要求

- Windows 10 或更高版本；
- .NET 6 SDK；
- Visual Studio 2022 17.5.2 或更高版本，推荐 Visual Studio 2026；
- EasiNote 5 安装到默认系统位置，并至少启动成功一次；
- 能从 NuGet.org 还原 `dotnetCampus.EasiPlugin.Sdk`。

EasiNote 安装目录通常包含产品版本号，不能把某个版本目录当作所有机器的固定路径。若需要定位安装信息，可检查注册表：

```text
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Seewo\EasiNote5
```

读取注册表时处理键不存在、权限不足和安装位置变化，不要把开发机探测结果提交为通用绝对路径。

## 最小项目文件

使用 Windows Desktop SDK、.NET 6 Windows TFM 和 WPF。部分宿主 API 可能涉及 Windows Forms，因此可按项目需要启用 `UseWindowsForms`。

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <Product>插件显示名称</Product>
    <Description>插件用途说明</Description>

    <UseEasiNote>core</UseEasiNote>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="dotnetCampus.EasiPlugin.Sdk" Version="2.1.1-alpha.3" />
  </ItemGroup>
</Project>
```

`2.1.1-alpha.3` 是一份已知可用的基线示例，不代表永远是最新或与所有 EasiNote 版本兼容。创建项目时应核对 NuGet 上可用版本、用户指定版本和目标机器宿主版本；不要未经要求自动升级既有项目。

## UseEasiNote 引用级别

```xml
<UseEasiNote>most</UseEasiNote>
```

可选值：

| 值 | 含义 |
|---|---|
| `none` | 不自动引用 EasiNote 托管程序集 |
| `api` | 仅 API 及 API 的依赖 |
| `core` | 大多数核心功能程序集，不含扩展功能 |
| `most` | 绝大多数程序集，包含扩展功能 |
| `all` | 所有会加载到 EasiNote 进程的托管程序集 |

从满足编译的最小级别开始。扩大引用范围会增加插件对宿主内部实现的耦合，也更容易受到版本变化影响。

## 单独引用宿主程序集

只缺少一个宿主 DLL 时，可以在 SDK 收集引用之前加入 `EasiNoteReference`：

```xml
<Target Name="IncludeEasiNoteAssembly" BeforeTargets="_ENSdkReferenceDlls">
  <ItemGroup>
    <EasiNoteReference Include="Cvte.Windows.Media.Imaging.Effect.dll" />
  </ItemGroup>
</Target>
```

程序集名称必须来自目标机器实际安装内容。不要在无法验证时猜测 DLL 名称。

## 插件入口

入口类型继承 `dotnetCampus.EasiPlugins.EasiPlugin`，并覆盖 `OnRunningAsync`。入口先划分 Cloud 与 Shell 职责；具体初始化方法再等待它实际依赖的宿主服务：

```csharp
using Cvte.EasiNote;

namespace MyEasiPlugin;

internal sealed class Program : dotnetCampus.EasiPlugins.EasiPlugin
{
    protected override Task OnRunningAsync()
    {
        if (EN.CommandOptions.IsCloud)
        {
            _ = StartCloudAsync();
        }
        else
        {
            _ = StartShellAsync();
        }

        return Task.CompletedTask;
    }

    private static Task StartCloudAsync()
    {
        return Task.CompletedTask;
    }

    private static Task StartShellAsync()
    {
        return Task.CompletedTask;
    }
}
```

上例只展示入口形态。项目必须使用现有日志或错误提示机制观察启动任务异常，不能静默丢弃。若 Shell 功能依赖某个容器服务，直接在 `StartShellAsync` 中等待 `Container.Current.GetAsync<TService>()`；不要把 `EN.App.Ready`、固定延迟或 `Interlocked` 当作所有插件的默认模板。

只有功能真实依赖整个应用 Ready 状态，并且已经确认目标版本的事件语义时，才使用 `EN.App.Ready`。详细决策见 [lifecycle-and-container.md](lifecycle-and-container.md)。

## launchSettings.json

Visual Studio 调试时把启动命令设为本机实际的 `EasiNote.exe`。下面只展示结构，`executablePath` 必须替换，不能原样视为有效路径：

```json
{
  "profiles": {
    "调试：云课件": {
      "commandName": "Executable",
      "executablePath": "<本机 EasiNote.exe 完整路径>",
      "workingDirectory": "$(ENNetExecutableFolder)"
    },
    "调试：备课": {
      "commandName": "Executable",
      "executablePath": "<本机 EasiNote.exe 完整路径>",
      "workingDirectory": "$(ENNetExecutableFolder)",
      "commandLineArgs": "--cloud -m Edit"
    },
    "调试：授课": {
      "commandName": "Executable",
      "executablePath": "<本机 EasiNote.exe 完整路径>",
      "workingDirectory": "$(ENNetExecutableFolder)",
      "commandLineArgs": "--cloud -m Display"
    }
  }
}
```

这些参数来自已知 EasiNote 5 示例配置。`--cloud -m Edit` 和 `--cloud -m Display` 是宿主启动约定，不应仅根据参数名称推断插件进程中的 `EN.CommandOptions.IsCloud` 值；必须在目标版本实际验证 Cloud/Shell 分支和最终加载插件的进程。若目标版本的启动参数不同，保留其已验证配置。

调试前关闭不需要的 EasiNote 进程，选择与功能对应的 profile，并确认加载的是当前构建输出。

## 构建与产物

正常构建后，SDK 会在 `bin/Debug` 或 `bin/Release` 生成插件安装产物。具体可用形态取决于 SDK 版本，常见包括：

- **exe**：用户双击安装的常见独立安装包；
- **zip**：包含插件文件，可用于安全软件报备或人工检查；
- **enp**：提供给希沃白板应用中心托管的包，本质上是特定后缀的 zip；
- **enpx**：zip 格式，内部包含由 EasiNote AppHost 托管启动的 .NET 安装程序。

SDK 2.1.1 系列开始的安装包可复用 EasiNote 的 .NET 6 环境，不再以 .NET Framework 运行时作为唯一前提。仍应以实际生成产物和目标机器验证结果为准。

## 构建故障检查顺序

1. 检查 Visual Studio 与 .NET 6 SDK 是否满足要求。
2. 检查 NuGet.org 是否可访问，以及 SDK 包版本是否存在。
3. 检查 TFM 是否为 `net6.0-windows`，并启用 WPF。
4. 检查 `<UseEasiNote>` 是否覆盖所需程序集。
5. 检查 EasiNote 是否安装到目标机器，且已至少启动一次。
6. 清理后重新还原和构建。
7. 根据精确编译错误定位缺失类型或版本不兼容；不要通过复制未知来源 DLL 或关闭错误来掩盖问题。

## 发布前检查

- 使用 Release 配置构建；
- 在干净测试机器或最接近用户环境的机器安装验证；
- 验证卸载和升级路径；
- 确认安装包不包含 PDB、日志、密钥、本机路径或内部服务地址等不应分发内容；
- 保存 SDK 版本、EasiNote 版本和验证模式，便于后续排查兼容性。
