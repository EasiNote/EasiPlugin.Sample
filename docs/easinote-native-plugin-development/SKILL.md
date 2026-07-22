---
name: easinote-native-plugin-development
description: 创建、修改、排查和打包希沃白板 EasiNote 5 原生插件的开发指南。只要用户提到希沃白板插件、EasiPlugin、dotnetCampus.EasiPlugin.Sdk、EasiNote 原生插件、EN.EditingBoardApi、BoardEditMenuItem、IUIItemManager，或希望用 .NET 6/WPF 扩展希沃白板备课、授课、云课件界面，就应使用此技能；即使用户没有明确说“使用技能”，也应在项目搭建、插件入口、宿主调试、UI 扩展、多语言、页面与元素操作、插件安装包等任务中触发。
compatibility: Windows 10 或更高版本；Visual Studio 2022 17.5.2 或更高版本，推荐 Visual Studio 2026；.NET 6 SDK；已按默认位置安装并至少启动过一次希沃白板 EasiNote 5。
---

# 希沃白板原生插件开发

使用 `dotnetCampus.EasiPlugin.Sdk` 创建加载到希沃白板 EasiNote 5 进程内的 .NET 6 原生插件。插件可以调用宿主公开的类型和成员，因此修改前先识别运行端、初始化时序、UI 线程要求和宿主版本。

## 开始前

1. 确认任务属于原生插件，而不是修改系统配置的“配置插件”或嵌入网页的 Web 插件。
2. 检查目标机器已安装 EasiNote 5，并至少成功启动过一次。
3. 检查项目目标为 `net6.0-windows`，使用 WPF；除非现有项目需要，否则不要升级目标框架。
4. 读取现有 `.csproj`、插件入口、相关 UI Item 和调试配置，再进行最小修改。
5. 如果需要新建项目或调整调试配置，读取 [references/project-setup.md](references/project-setup.md)。
6. 如果需要操作页面、元素、菜单、多语言、埋点、导出或宿主服务，读取 [references/api-recipes.md](references/api-recipes.md)。

## 核心概念

- **Cloud 端**：云课件列表等云端界面。
- **Shell 端**：备课和授课进程形态。
- **Paint**：承载课件的画板。
- **Slide**：课件页面，概念类似 PPT 幻灯片。
- **Element**：页面上的文本、形状、表格、组合等元素。
- **原生插件**：程序集由 EasiNote 进程加载，可调用宿主 API。

使用 `EN.CommandOptions.IsCloud` 区分 Cloud 与 Shell。备课功能通常只在 Shell 端注册；不要假定 Cloud 和 Shell 执行相同的初始化模块。

## 标准工作流

### 1. 识别插件运行范围

先明确功能运行在以下哪一类环境：

- Cloud 云课件界面；
- Shell 备课模式；
- Shell 授课模式；
- Cloud 与 Shell 都运行，但执行不同逻辑。

在插件入口的 `OnRunningAsync` 中分流。不要在未确认运行端时访问只属于备课或授课的 API。

### 2. 等待宿主就绪

插件被加载不等于宿主服务和界面已经就绪。在 Shell 端：

- 先订阅 `EN.App.Ready`，再重新检查 `EN.App.IsReady`，避免状态在检查与订阅之间变化而丢失事件；
- 立即就绪路径和事件路径必须进入同一个一次性初始化门；
- 确认就绪后立即取消订阅，并确保初始化只执行一次；
- 仅对确实需要等待后续模块的功能增加短延迟，不要用固定延迟替代明确的就绪事件。

避免重复注册菜单、事件或服务。初始化方法应具备清晰的一次性调用路径。

### 3. 注册 UI 扩展

通过 `Container.Current.Get<IUIItemManager>()` 获取 UI 项管理器，再使用 `Append` 注册对应的 UI Item。

选择与场景匹配的基类和 `UIItemPurposes`，例如：

- `BoardEditMenuItem` + `UIItemPurposes.BoardEditMenu`：备课画板右键菜单；
- `HeadToolBarItem` + `UIItemPurposes.HeadToolBar`：备课顶部工具栏；
- 元素编辑菜单使用对应的 Element Edit purpose。

为 UI Item 设置稳定且唯一的 Key、排序、命令和可见条件。`Predicate` 必须表达真实可用条件，不要照抄示例中的占位逻辑。

### 4. 遵守 UI 线程要求

插件初始化默认可能运行在后台线程。以下操作应切换到 WPF UI Dispatcher：

- 修改 `Lang.Sources`；
- 创建、显示或修改 WPF 窗口和控件；
- 执行明确要求 UI 线程的宿主界面 API。

优先等待 Dispatcher 操作完成；不要无故丢弃返回的任务。若现有宿主回调只能使用 `async void`，在回调内部捕获并处理异步异常，避免进程因未处理异常崩溃。

### 5. 添加多语言

界面可见字符串优先通过 `Lang.Sources` 注册，并通过 `Lang.Get` 或 UI Item 的语言键读取。语言键应稳定、唯一，并与 UI Item 类型或 Key 保持一致。

至少提供任务要求的语言。添加语言源时使用正确的 `CultureInfo`，并在 UI 线程执行。不要把仓库名、示例类名或测试文案带进最终插件。

### 6. 操作页面和元素

根据运行模式选择 API：

- 备课当前页：`EN.EditingBoardApi.CurrentSlide`；
- 备课全部页面：`EN.EditingBoardApi.Slides`；
- 授课当前页：`EN.DisplayingBoardApi.CurrentSlide`。

元素保存在 `Slide.Elements` 中。使用 `OfType<T>()` 筛选类型，并显式处理找不到元素的情况。组合元素需要继续遍历其内部 `Elements`。

修改元素坐标前区分页面坐标系和元素内部坐标系。大多数元素可通过 `Bounds` 调整位置和大小；字符范围等内部坐标可用 WPF 坐标转换方法转换到 Slide。

### 7. 处理耗时任务

耗时操作使用异步 API，并把取消令牌传到底层。需要阻止用户误操作时，可使用宿主 Loading 通知，但要确保：

- 异常不会被静默吞掉；
- 取消令牌传递到底层，底层操作在循环和阶段边界观察取消请求；
- 不在 UI 线程执行长时间 CPU 或 I/O 操作；
- 不使用无所有者、永不释放的 `CancellationTokenSource`。

### 8. 最小化宿主依赖

通过 `<UseEasiNote>` 控制自动引用范围：`none`、`api`、`core`、`most`、`all`。选择满足编译的最小级别；只有确有需要时才提高引用范围。

若仅缺少单个宿主程序集，优先用 SDK 的 `EasiNoteReference` 项单独声明，而不是直接扩大到 `all`。

### 9. 编译、调试和交付

1. 还原 NuGet 包并构建项目。
2. 使用指向本机实际 `EasiNote.exe` 的启动配置调试 Cloud、备课或授课模式。
3. 验证插件只注册一次，菜单条件正确，多语言显示正常，异常不会使宿主崩溃。
4. 在 `bin/Debug` 或 `bin/Release` 检查 SDK 生成的安装产物。
5. 根据分发场景选择 exe、zip、enp 或 enpx；不要把开发机绝对路径、凭据或内部地址打入技能、源码或安装包。

## 代码质量要求

- 遵循现有项目的命名、命名空间和格式；新项目启用 Nullable。
- 公共 API 添加 XML 文档；非必要类型和成员使用最小可见性。
- 对外部入参做早期校验，异步方法传递 `CancellationToken`。
- 不吞异常，不使用空 `catch`，不把宿主异常伪装成成功。
- 埋点不得包含密码、令牌、课件正文、学生信息或其他敏感数据。
- 不硬编码具体 EasiNote 版本目录；调试配置必须让使用者替换为本机实际安装路径。
- SDK 和宿主内部 API 可能随版本变化；遇到类型或成员不存在时，以当前安装版本和编译器结果为准，不要虚构兼容 API。

## 完成检查

- [ ] 项目为 `net6.0-windows`，WPF 配置正确。
- [ ] NuGet SDK 可还原，版本与目标宿主兼容。
- [ ] Cloud/Shell 分支正确，没有跨端误用 API。
- [ ] 宿主就绪事件只处理一次并取消订阅。
- [ ] UI 扩展没有重复注册，`Predicate` 符合业务条件。
- [ ] 多语言和 WPF 操作在 UI 线程执行。
- [ ] 页面、元素和组合元素均处理空值与坐标系。
- [ ] 耗时操作可取消，异常可见且不会静默失败。
- [ ] 已构建，并在目标 EasiNote 模式中实际验证。
- [ ] 交付文件不依赖本技能之外的仓库资料或本机路径。
