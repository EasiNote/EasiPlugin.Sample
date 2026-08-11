---
name: easinote-native-plugin-development
description: 创建、修改、排查、调试和打包希沃白板 EasiNote 5 原生插件的开发指南。只要用户提到希沃白板插件、EasiPlugin、dotnetCampus.EasiPlugin.Sdk、EasiNote 原生插件、EN.EditingBoardApi、BoardEditMenuItem、HeadToolBarItem、学科工具、IUIItemManager、Container.GetAsync、SafeWindow、Cvte.EasiUI、GurnetUI，或需要安全退出、重启 EasiNote、扩展备课/授课/云课件界面，就应使用此技能；即使用户没有明确要求使用技能，也应在项目搭建、生命周期、宿主调试、UI 扩展、WPF 视觉接入、多语言、页面与元素操作和插件安装包任务中触发。
compatibility: Windows 10 或更高版本；Visual Studio 2022 17.5.2 或更高版本；.NET 6 SDK；已安装并至少启动过一次希沃白板 EasiNote 5。宿主内部 API 和行为可能随版本变化。
---

# 希沃白板原生插件开发

使用 `dotnetCampus.EasiPlugin.Sdk` 创建加载到希沃白板 EasiNote 5 进程内的 .NET 6/WPF 原生插件。插件可调用宿主类型和成员，但实现前必须先识别运行端、真实就绪条件、UI 线程要求和目标宿主版本。

## 开始前

1. 确认任务属于进程内原生插件，而不是配置插件或 Web 插件。
2. 读取现有 `.csproj`、插件入口、相关 UI Item、资源和调试配置。
3. 检查项目目标框架和 SDK 版本；除非用户要求，不升级 TFM、SDK 或宿主依赖。
4. 区分公开 SDK/API 契约与从特定宿主版本观察到的内部行为。内部类型或调用链必须经目标版本验证。
5. 只修改完成任务所需的文件，并在修改后构建；具备宿主环境时再做实机验证。

## 按任务读取参考资料

仅加载当前任务需要的文档：

- 新建项目、修复 `.csproj`、配置宿主调试或理解安装产物：读取 [references/project-setup.md](references/project-setup.md)。
- 判断 Cloud/Shell、服务就绪、`GetAsync<T>`、Dispatcher、Ready 或重复注册问题：读取 [references/lifecycle-and-container.md](references/lifecycle-and-container.md)。
- 操作页面、元素、右键菜单、多语言、Loading、埋点、导出或转换事件：读取 [references/api-recipes.md](references/api-recipes.md)。
- 添加 `HeadToolBarItem`、顶部工具栏或“学科工具”下拉入口，处理 Key、图标和 Tooltip：读取 [references/head-toolbar-and-subject-tools.md](references/head-toolbar-and-subject-tools.md)。
- 创建与宿主一致的 WPF 窗口，选择 `SafeWindow`、EasiUI/GurnetUI 样式或语义资源：读取 [references/wpf-visual-integration.md](references/wpf-visual-integration.md)。
- 组件变更后安全退出并重启 EasiNote：读取 [references/safe-host-restart.md](references/safe-host-restart.md)。
- 用户要求完整代码，或任务需要组合入口、服务等待、UI 注册、多语言和业务逻辑：读取 [references/complete-examples.md](references/complete-examples.md)，并同时读取相关机制文档。

## 核心概念

- **Cloud**：云课件列表等云端界面进程形态。
- **Shell**：备课或授课界面进程形态。
- **Edit**：Shell 中的备课模式。
- **Display**：Shell 中的授课模式。
- **Paint**：承载课件的画板。
- **Slide**：课件页面。
- **Element**：页面上的文本、形状、表格、组合等元素。
- **原生插件**：程序集由 EasiNote 进程加载，可调用宿主 API。

使用 `EN.CommandOptions.IsCloud` 区分 Cloud 与 Shell。`IsCloud == false` 只表示 Shell；只有业务确实需要时，再通过 `EN.App.CurrentMode` 区分 Edit 与 Display。

## 标准决策流程

### 1. 识别运行范围

先明确功能运行在 Cloud、Shell Edit、Shell Display，还是两端分别执行不同逻辑。在 `OnRunningAsync` 中做 Cloud/Shell 职责分流，不跨端访问服务、ViewModel、画板或 UI。

自动扫描插件程序集不等于进程隔离。Shell 专属 UI Item 若依赖 Shell 服务或资源，应在 Shell 分支显式注册。

### 2. 找到真实前置条件

不要把“插件已加载”“应用 Ready”和“目标服务已注册”视为同一件事。

如果后续工作依赖某个容器服务，优先直接等待：

```csharp
var service = await Container.Current
    .GetAsync<TService>()
    .ConfigureAwait(false);
```

不要机械叠加固定延迟、`EN.App.Ready`、轮询、自定义状态机或 `Interlocked`。只有已从调用语义确认存在额外前置条件、重复调用或并发重入时，才增加对应机制。

### 3. 分开处理服务就绪与 UI 线程

`GetAsync<T>` 解决服务何时可用，不保证后续代码位于 WPF UI 线程。正确顺序通常是：

1. 在异步方法中等待目标服务；
2. 服务可用后切换到 `Application.Current.Dispatcher`；
3. 在 UI 线程创建 WPF 对象、注册资源、修改 `Lang.Sources` 和注册 UI Item。

不要为了“保证时机”在 Dispatcher 中同步调用 `Container.Current.Get<T>()`。

### 4. 注册 UI 扩展

通过 `IUIItemManager.Append` 注册与场景匹配的 UI Item：

- `BoardEditMenuItem` + `UIItemPurposes.BoardEditMenu`：备课画板右键菜单；
- `HeadToolBarItem` + `UIItemPurposes.HeadToolBar`：备课顶部工具栏体系及“学科工具”下拉；
- 元素编辑菜单使用对应的 Element Edit purpose。

`[UIItem]` 自动扫描和 `Append` 动态注册二选一，不能同时使用。Key 必须稳定且唯一；`Predicate` 必须表达真实业务条件，不要复制占位逻辑。

### 5. 添加资源与多语言

界面可见字符串优先通过 `Lang.Sources` 注册，并使用宿主实际读取的语言键。WPF 图片或样式资源必须在使用它们的 UI Item 或窗口实例化前可用。

宿主资源、语言键和内部样式可能存在版本差异。遇到资源不存在时，应检查目标版本和同类宿主界面的实际用法，不虚构兼容键。

### 6. 操作页面和元素

根据模式选择 API：

- 备课当前页：`EN.EditingBoardApi.CurrentSlide`；
- 备课全部页面：`EN.EditingBoardApi.Slides`；
- 授课当前页：`EN.DisplayingBoardApi.CurrentSlide`。

使用 `OfType<T>()` 筛选元素并显式处理未找到的情况。组合元素可能包含内部 `Elements`；是否递归应依据实际数据模型。修改坐标前区分 Slide 坐标与元素内部坐标。

### 7. 处理异步、取消和异常

耗时工作异步执行并传递 `CancellationToken`。不要在 UI 线程执行长时间 CPU 或 I/O 操作，不要 fire-and-forget 后丢失异常。

如果宿主入口只能使用同步返回或 `async void` 回调，应建立明确的异常观察路径，避免未处理异常终止宿主进程。

### 8. 最小化宿主依赖

通过 `<UseEasiNote>` 控制自动引用范围：`none`、`api`、`core`、`most`、`all`。选择满足编译的最小级别。仅缺少单个宿主程序集时，优先使用 SDK 的 `EasiNoteReference`，不要直接扩大到 `all`。

### 9. 验证与交付

1. 还原并构建项目。
2. 使用本机实际 `EasiNote.exe` 配置调试，不提交开发机版本目录。
3. 分别验证目标 Cloud、Edit 或 Display 场景。
4. 验证 UI Item 没有重复注册、资源可解析、多语言正确、异常不会导致宿主崩溃。
5. 对版本相关内部行为进行目标版本实机验证。
6. 检查 SDK 生成的 exe、zip、enp 或 enpx 产物，不打包凭据、内部地址或本机绝对路径。

## 代码质量要求

- 遵循现有项目命名、可见性、Nullable 和格式约定。
- 对外部输入早期校验；不吞异常，不使用空 `catch`。
- 不添加没有真实调用语义依据的并发状态字段、延迟或抽象层。
- 用户可见字符串使用现有本地化机制。
- 埋点不得包含令牌、课件正文、学生信息或其他敏感数据。
- 宿主内部 API 不稳定；以当前安装版本、实际引用和编译结果为准。

## 完成检查

- [ ] 已识别 Cloud、Shell Edit 或 Shell Display 的真实运行范围。
- [ ] 已用真实依赖表达就绪条件，没有机械叠加 Ready、延迟或状态机。
- [ ] 容器等待不在 UI 线程执行，WPF 操作已切换 Dispatcher。
- [ ] UI Item 注册方式唯一，Key、资源和语言键稳定且匹配。
- [ ] 页面、元素、组合和坐标系已处理空值与边界。
- [ ] 异步异常可见，取消令牌传递到底层。
- [ ] 宿主引用范围满足最小依赖原则。
- [ ] 已构建，并在可用时完成目标宿主版本实机验证。
- [ ] 交付内容不依赖本技能外的本地仓库资料或开发机路径。
