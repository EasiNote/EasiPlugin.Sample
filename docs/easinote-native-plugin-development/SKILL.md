---
name: easinote-native-plugin-development
description: 基于 dotnetCampus.EasiPlugin.Sdk 开发、修改、调试和发布希沃白板 EasiNote 原生插件的工作流。用于处理 .NET 6/WPF 插件项目、EasiPlugin 入口与 EN.App.Ready 生命周期、Cloud/Shell 分流、备课与授课 API、UIItem 菜单或工具栏扩展、多语言、埋点、页面与元素操作、宿主调试配置，以及 exe/zip/enp/enpx 安装包构建问题。
---

# 希沃白板原生插件开发

## 工作原则

- 先读取仓库的 `README.md`、目标 `.csproj`、插件入口、相邻 UI 扩展和 `launchSettings.json`，再修改代码。
- 将本技能限定于原生插件。不要把配置插件、Web 插件或 ClientWebApi 的实现方式混入原生插件。
- 以当前项目可编译的 SDK 版本和 EasiNote 安装版本为准。EasiNote 的部分类型属于宿主内部 API，可能随版本变化；先搜索现有用法和符号，再编译验证，不要凭记忆猜测类型或成员。
- 保留项目现有的目标框架、SDK 包版本、可空设置和代码风格，除非用户明确要求升级。
- 复用宿主提供的 `EN`、`SafeEN`、`Container.Current`、`IUIItemManager` 和通知能力，不要为已有抽象再包装一层。
- 将界面可见文本放入多语言资源；将埋点 ID 集中管理；不要上报用户敏感数据。
- 对 WPF 控件、应用资源和语言源执行线程切换；不要从后台线程直接操作界面对象。

## 开发流程

### 1. 确认插件运行场景

先区分以下场景，再选择 API 和初始化位置：

| 场景 | 判断或入口 | 常用 API |
| --- | --- | --- |
| 云课件 Cloud 端 | `EN.CommandOptions.IsCloud` 为 `true` | Cloud 端对应服务 |
| Shell 备课 | `EN.CommandOptions.IsCloud` 为 `false`，模式为 Edit | `EN.EditingBoardApi` |
| Shell 授课 | `EN.CommandOptions.IsCloud` 为 `false`，模式为 Display | `EN.DisplayingBoardApi` |
| 宿主服务 | 等待宿主就绪后获取 | `Container.Current.Get<T>()`、`GetAsync<T>()` |
| UI 扩展 | 宿主就绪后注册 | `IUIItemManager` |

Cloud 端和 Shell 端都会加载插件，但初始化模块不同。只在需要的分支注册功能，避免访问尚未初始化的服务。

### 2. 检查项目配置

沿用现有项目配置。创建同类项目时，以以下属性为基线，并从当前仓库复制实际 SDK 版本，不要自行猜测版本：

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <Product>插件产品名</Product>
    <Description>插件描述</Description>
    <UseEasiNote>all</UseEasiNote>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="dotnetCampus.EasiPlugin.Sdk" Version="从当前仓库读取" />
  </ItemGroup>
</Project>
```

按实际依赖选择 `UseEasiNote`，并优先保留项目当前值：

- `none`：不引用 EasiNote 程序集。
- `api`：只引用 API 及其依赖。
- `core`：引用大多数核心程序集，不包含扩展功能。
- `most`：引用绝大多数程序集，包含扩展功能。
- `all`：引用宿主进程会加载的所有托管程序集。

仅缺少单个宿主 DLL 时，使用 SDK 的 `EasiNoteReference` 项，不要手工复制宿主程序集：

```xml
<Target Name="IncludeEn" BeforeTargets="_ENSdkReferenceDlls">
  <ItemGroup>
    <EasiNoteReference Include="需要的程序集.dll" />
  </ItemGroup>
</Target>
```

### 3. 实现插件入口和生命周期

让入口类型继承 `dotnetCampus.EasiPlugins.EasiPlugin`，并在 `OnRunningAsync` 中分流 Cloud 与 Shell 逻辑。

在 Shell 端遵循以下顺序：

1. 检查 `EN.App.IsReady`。
2. 已就绪时直接执行 Shell 初始化。
3. 未就绪时订阅 `EN.App.Ready`。
4. 事件触发后立即取消订阅，再执行一次初始化。
5. 只注册一次菜单、工具栏、事件和语言资源。

保持异步调用可观察：

- 在 `OnRunningAsync` 可以等待时直接返回或等待初始化任务。
- 仅允许事件处理器使用 `async void`，并在处理器内部捕获异步异常，使用项目已有日志或通知渠道报告；不要吞掉异常。
- 不要用未观察的任务代替生命周期管理。
- 将人为延迟视为可选的兼容措施；优先等待明确的 Ready 事件或服务可用条件。
- 为可取消操作传递 `CancellationToken`，及时释放 `CancellationTokenSource`。

### 4. 注册 UI 扩展

先通过容器取得 `IUIItemManager`，再按目标位置注册 UI 项：

```csharp
IUIItemManager manager = Container.Current.Get<IUIItemManager>();
manager.Append(
    _ => new CustomMenuItem(),
    new UIItemAttribute(UIItemPurposes.BoardEditMenu));
```

优先复用项目已有的 `AppendWithLang` 等辅助方法。不要重复实现同一套语言键推导和注册逻辑。

实现菜单或工具栏项时：

- 继承与目的匹配的现有 UIItem 类型，例如备课右键菜单使用 `BoardEditMenuItem`。
- 设置稳定且唯一的 `Key`；需要图标资源时让资源键与 `Key` 保持一致。
- 使用 `SortHint` 控制顺序。
- 使用 `Predicate` 表达可见或可用条件，保持判断无副作用。
- 使用宿主已有命令类型，例如 `DelegateCommand`。
- 在命令执行前校验当前页面、选区和目标元素仍然有效。
- 对耗时操作使用宿主 Loading 通知，并接入取消和异常处理。

添加备课工具栏时，使用 `UIItemPurposes.HeadToolBar`；添加元素编辑菜单时，使用 `UIItemPurposes.ElementEditMenu`；不要把不同位置的语言键规则混用。

### 5. 添加多语言和 WPF 资源

将 `Lang.Sources.Add(...)`、`Application.Current.Resources` 修改以及其他 WPF 界面操作调度到主 UI 线程。

备课右键菜单的语言键通常遵循：

```text
Lang.BoardEditContextMenu.{去掉 MenuItem 后缀的类型名}
```

备课工具栏通常需要以下语言键，其中 `{Key}` 必须替换为 UI 项的实际 Key：

```text
Lang.ToolTip.Insert.{Key}.Title
Lang.ToolTip.Insert.{Key}.Text
Lang.HeadToolBar.{Key}
```

添加语言项时：

- 使用明确的 `CultureInfo`，例如 `zh-CHS`，或按现有代码使用 `Lang.Current`。
- 复用现有 `DictionaryLanguageSource` 注册方式。
- 防止同一进程重复添加同一资源或重复订阅事件。
- 不要在业务代码中散落可见字符串；沿用项目资源组织方式。

### 6. 操作页面和元素

按运行模式选择画板 API：

- 备课当前页：`EN.EditingBoardApi.CurrentSlide`。
- 备课全部页面：`EN.EditingBoardApi.Slides`。
- 授课当前页：`EN.DisplayingBoardApi.CurrentSlide`。

通过 `Slide.Elements` 查找顶层元素，通过 `OfType<T>()` 筛选类型。需要处理组合时，同时遍历 `GroupElement.Elements`；若业务允许嵌套组合，按实际模型递归处理并避免重复访问。

处理文本元素坐标时遵循以下顺序：

1. 取得 `TextElement.TextEditor.CharCount`。
2. 使用 `GetRunBoundsByDocumentOffset(index)` 取得字符相对文本元素的范围。
3. 使用 WPF `TranslatePoint` 将坐标转换到目标 `Slide` 的页面坐标系。
4. 使用 `Bounds` 修改大多数元素的位置和尺寸。

只有在文本已完成布局渲染后读取字符范围。文本正在编辑或布局未完成时，等待 `TextEditor.RenderCompleted`，并在完成后取消订阅。不要把文本元素坐标、页面坐标和元素内部坐标直接混用。

### 7. 使用宿主服务和专项 API

通过宿主容器获取已注册服务：

```csharp
T service = Container.Current.Get<T>();
T asyncService = await Container.Current.GetAsync<T>();
```

在调用前确认对应模块已经初始化。按需沿用仓库已有模式：

- 使用 `SafeEN.Collection.ReportEvent(eventId, extra)` 上报行为事件，并过滤敏感信息。
- 使用 `EN.CurrentBoardApi.GetStorageModelAsync()` 获取当前课件存储模型，再通过已注册的存储提供器导出 ENBX。
- 获取 PPTX 转 ENBX 服务后订阅转换完成事件，并在插件不再使用时取消订阅。
- 使用宿主提供的关闭流程退出 EasiNote，不要直接终止进程。

不要把示例中的具体内部实现类型视为稳定契约；优先依赖接口，并以当前 SDK 的可编译结果为准。

### 8. 配置宿主调试

让 Visual Studio 启动 EasiNote 宿主，而不是直接运行插件输出程序集。

沿用项目 `Properties/launchSettings.json` 中的多配置结构，并分别验证：

- Cloud 云课件。
- Shell 备课 Edit。
- Shell 授课 Display。
- 必要时的托管与本机联合调试。

将 `workingDirectory` 保持为项目已有的 SDK 属性，例如 `$(ENNetExecutableFolder)`。根据本机实际安装版本更新宿主可执行文件路径，不要照抄示例中的版本号，也不要提交用户目录、私有路径、凭据或密钥。

调试前确认：

1. 在受支持的 Windows 和 Visual Studio 版本上开发。
2. 将 EasiNote 安装到受支持位置。
3. 至少正常启动一次 EasiNote，使宿主完成初始化。
4. 还原 `dotnetCampus.EasiPlugin.Sdk` 成功。

### 9. 构建和验证

修改完成后按以下顺序验证：

1. 检查修改文件的编译错误。
2. 构建整个解决方案。
3. 运行与改动相关的测试；项目没有测试时明确记录。
4. 分别启动受影响的 Cloud、备课和授课场景。
5. 验证插件只初始化一次，Ready 事件和业务事件均正确取消订阅。
6. 验证菜单或工具栏的位置、排序、Predicate、命令、多语言和图标。
7. 验证耗时操作的 Loading、取消、异常反馈和 UI 响应。
8. 验证元素操作使用正确坐标系，并覆盖无页面、无元素、组合元素和文本未渲染等边界情况。
9. 检查构建配置对应的 `bin/Debug` 或 `bin/Release` 产物。

SDK 构建可生成以下分发形态，按用户要求选择，不要手工改变内部结构：

- `.exe`：独立插件安装包。
- `.zip`：完整插件文件，可供报备或检查。
- `.enp`：供希沃白板应用中心托管的包，本质为约定扩展名的 zip。
- `.enpx`：包含 .NET 6 安装程序、由 AppHost 托管启动的 zip 格式包。

## 代码质量检查

- 遵循现有命名空间和代码风格，不为局部功能引入新架构层。
- 为新增公共 API 添加 XML 文档；保持可空引用安全。
- 对关键入参执行早期校验，使用精确异常类型。
- 不捕获并吞掉基础 `Exception`；只在异步事件边界等必要位置捕获，并记录或反馈。
- 不从后台线程访问 WPF 可视树、资源字典或多语言源。
- 不硬编码用户敏感信息、机器私有路径或埋点中的业务数据。
- 不修改目标框架、语言版本、SDK 包版本或宿主引用级别来掩盖编译问题。
- 对不熟悉的 EasiNote API 先查找现有调用、定义和实现，再通过构建确认。
