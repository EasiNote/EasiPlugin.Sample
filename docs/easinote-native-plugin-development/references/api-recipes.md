# 常用 API 配方

在实现菜单、工具栏、多语言、页面与元素操作、导出、转换事件或宿主服务调用时读取本文。示例展示 API 形态，不应照抄业务名称、埋点 ID、文案或可见条件。

除非段落明确称为“完整骨架”，本文代码均是局部 API 片段，假设调用方已经声明相关变量并引用对应命名空间。占位类型和方法（如 `MyToolWindow`、`RunWorkAsync`、`EventIds`）需要替换为插件自己的实现。

## 目录

- [Cloud 与 Shell 分流](#cloud-与-shell-分流)
- [获取页面](#获取页面)
- [筛选和遍历元素](#筛选文本元素)
- [元素范围与字符坐标](#修改元素范围)
- [注册备课右键菜单](#注册备课右键菜单)
- [注册顶部工具栏](#注册顶部工具栏)
- [添加多语言](#添加多语言)
- [显示 WPF 工具窗口](#显示-wpf-工具窗口)
- [执行带 Loading 的耗时任务](#执行带-loading-的耗时任务)
- [埋点、导出和转换](#埋点上报)
- [退出与宿主扩展点](#退出-easinote)
- [常见失败模式](#常见失败模式)

## Cloud 与 Shell 分流

```csharp
if (EN.CommandOptions.IsCloud)
{
    // 云课件界面逻辑
}
else
{
    // 备课或授课 Shell 逻辑
}
```

只在对应端访问该端 API。备课与授课都属于 Shell，但应继续根据功能入口和可用 API 区分场景。

## 获取页面

```csharp
var editingSlide = EN.EditingBoardApi.CurrentSlide;
var editingSlides = EN.EditingBoardApi.Slides;
var displayingSlide = EN.DisplayingBoardApi.CurrentSlide;
```

- `EditingBoardApi`：备课编辑画板；
- `DisplayingBoardApi`：授课显示画板；
- `CurrentSlide`：当前页；
- `Slides`：备课全部页面。

访问页面前确保当前运行模式正确且画板已经就绪。

## 筛选文本元素

```csharp
using Cvte.Paint.Features.Elements.Texts;

var firstTextElement = currentSlide.Elements
    .OfType<TextElement>()
    .FirstOrDefault();

if (firstTextElement is null)
{
    return;
}
```

需要处理所有文本元素时直接遍历。不要调用 `First()` 假定页面一定存在目标元素。

## 遍历组合元素

```csharp
using Cvte.Paint.Features.Elements;
using Cvte.Paint.Features.Elements.Texts;

foreach (var groupElement in currentSlide.Elements.OfType<GroupElement>())
{
    foreach (var textElement in groupElement.Elements.OfType<TextElement>())
    {
        // 处理组合内的文本元素
    }
}
```

如果产品允许嵌套组合，确认宿主数据模型是否需要递归遍历。不要在未验证模型行为时擅自递归并修改同一元素多次。

## 修改元素范围

大部分元素在忽略旋转等复杂变换时，可以通过 `Bounds` 设置页面中的位置和尺寸：

```csharp
var position = new Point(100, 120);
var size = new Size(400, 200);
element.Bounds = new Rect(position, size);
```

修改前保留业务需要的尺寸、旋转和其他变换信息。不要把元素内部坐标直接写成页面坐标。

## 获取文本字符在页面中的位置

```csharp
int charCount = textElement.TextEditor.CharCount;

for (int index = 0; index < charCount; index++)
{
    Rect boundsInTextElement = textElement.TextEditor
        .GetRunBoundsByDocumentOffset(index);

    Point topLeftInSlide = textElement.TextEditor
        .TranslatePoint(boundsInTextElement.TopLeft, currentSlide);

    // 使用页面坐标 topLeftInSlide
}
```

字符范围依赖文本已经完成布局和渲染。文本正在编辑或刚修改内容时，等待 `TextEditor.RenderCompleted` 后再读取范围。事件处理完成后取消订阅，避免重复执行和对象泄漏。

## 注册备课右键菜单

```csharp
using Cvte.EasiNote;
using Cvte.Windows.Input;
using dotnetCampus.EasiPlugins;

internal sealed class MyBoardMenuItem : BoardEditMenuItem
{
    public MyBoardMenuItem()
    {
        Key = nameof(MyBoardMenuItem);
        SortHint = 50;
        Command = new DelegateCommand(Execute);
        Predicate = selectedElements => selectedElements.Count > 0;
    }

    private static void Execute()
    {
        // 执行业务操作
    }
}
```

注册：

```csharp
var manager = Container.Current.Get<IUIItemManager>();
manager.Append(
    _ => new MyBoardMenuItem(),
    new UIItemAttribute(UIItemPurposes.BoardEditMenu));
```

`Predicate` 的入参表示当前选择上下文。根据真实功能决定是要求无选择、单选、特定元素类型还是任意多选。

Key 应在目标 UI purpose 的有效范围内保持稳定且不与其他项冲突。对应语言键可去掉类型名中的 `MenuItem` 后缀，例如 `MyBoardMenuItem` 对应 `Lang.BoardEditContextMenu.MyBoard`；最终规则以当前宿主读取逻辑为准。

## 注册顶部工具栏

```csharp
internal sealed class MyHeadToolBarItem : HeadToolBarItem
{
    public MyHeadToolBarItem()
    {
        Key = nameof(MyHeadToolBarItem);
        Type = UIItemTypes.Subject;
        ImageSourceKey = Key;
        ImageWidth = 20;
        ImageHeight = 20;
        SortHint = 250;
        SetValue(TextProperty, Key);
        Predicate = _ => true;
        Command = new DelegateCommand(Execute);
    }

    private static void Execute()
    {
    }
}
```

注册：

```csharp
manager.Append(
    _ => new MyHeadToolBarItem(),
    new UIItemAttribute(UIItemPurposes.HeadToolBar));
```

图标资源必须在 UI Item 解析前可用。资源 Key 与 `ImageSourceKey` 保持一致，添加前检查是否已经存在，避免覆盖宿主或其他插件资源。

## 添加多语言

```csharp
using System.Globalization;
using System.Windows;
using Cvte.Windows.Localization;

await Application.Current.Dispatcher.InvokeAsync(() =>
{
    Lang.Sources.Add(new DictionaryLanguageSource
    {
        [new CultureInfo("zh-CHS")] = new Dictionary<string, string>
        {
            ["Lang.BoardEditContextMenu.MyBoard"] = "我的工具"
        }
    });
});
```

读取：

```csharp
string text = Lang.Get("Lang.BoardEditContextMenu.MyBoard");
```

常见 UI Item 语言键形态：

```text
Lang.BoardEditContextMenu.<ItemKey>
Lang.HeadToolBar.<ItemKey>
Lang.ToolTip.Insert.<ItemKey>.Title
Lang.ToolTip.Insert.<ItemKey>.Text
```

具体键由 UI Item 类型和宿主读取逻辑决定。添加工具栏项时通常同时提供标题、提示文字和工具栏显示名称。

不要并发修改语言源。若初始化方法不能变为异步，应保存 Dispatcher 操作并在可控位置观察异常，而不是用 `_ =` 永久忽略。

## 显示 WPF 工具窗口

```csharp
var window = new MyToolWindow
{
    WindowStyle = WindowStyle.ToolWindow,
    Title = Lang.Get("Lang.MyPlugin.WindowTitle"),
    Width = 320,
    Owner = Window.GetWindow(EN.EditingBoardApi.Board)
};

window.Show();
```

在 UI 线程创建和显示窗口。非模态窗口要管理重复打开、Owner、关闭事件和资源释放；模态窗口应设置 Owner，并根据结果处理业务。

## 执行带 Loading 的耗时任务

已知 2.1.1-alpha.3 示例中的 Loading API 接受异步委托、显示文本和 `CancellationTokenSource`。以下是调用形态片段，不保证所有 SDK 版本都返回可等待任务：

```csharp
EN.Notification.DoWithLoadingAsync(
    async () =>
    {
        try
        {
            await RunWorkAsync(cancellationTokenSource.Token);
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    },
    Lang.Get("Lang.MyPlugin.Working"),
    cancellationTokenSource);
```

`cancellationTokenSource` 应由调用方在注册操作前创建，并且在异步委托完成前保持存活。实际 SDK 的返回类型和可等待方式可能随版本变化，应先查看当前包签名：若返回 `Task`，应等待并处理异常；若不返回可等待任务，异步委托必须自行记录或呈现异常，并在 `finally` 中释放资源。底层工作必须观察取消令牌，取消请求本身不会强制终止任意任务。

## 埋点上报

```csharp
SafeEN.Collection.ReportEvent(EventIds.OpenTool, string.Empty);
```

事件 ID 应稳定、可识别并集中定义。附加内容只发送完成分析所需的最少信息，禁止上报：

- 密码、Token、Cookie 或连接字符串；
- 学生、教师或组织的个人敏感信息；
- 课件正文、文件内容或完整本地路径；
- 未经用户预期的设备指纹或环境信息。

## 导出当前课件为本地 ENBX

```csharp
var storageModel = await EN.CurrentBoardApi.GetStorageModelAsync();
Container.Current
    .Get<IEnbxStorageProvider>()
    .ExportEnb(storageModel.Model, filePath);
```

导出前验证 `filePath`，确保目标目录存在且用户有写入权限。若导出 API 是同步 I/O，不要在 UI 线程执行大文件导出。不要覆盖用户文件，除非用户明确确认。

## 监听 PPTX 转 ENBX 完成

```csharp
var converter = await Container.Current.GetAsync<IPptxToEnbxConverter>();

if (converter is PptxToEnbxConverter concreteConverter)
{
    concreteConverter.PptxToEnbxConverted += OnPptxToEnbxConverted;
}
```

```csharp
private static void OnPptxToEnbxConverted(
    object? sender,
    PptxToEnbxConvertedEventArgs e)
{
    // 处理转换完成
}
```

如果插件或功能存在停止生命周期，应在停止时取消订阅。依赖具体实现类型意味着更强版本耦合；若当前 SDK 提供接口事件，优先订阅接口。

## 退出 EasiNote

```csharp
ShellFlowHelper.EnsureShutdown("插件请求退出的原因");
```

这是破坏性操作，只在用户明确触发、业务确实要求退出时调用。原因应便于日志排查，但不得包含敏感数据。不要把退出作为错误恢复的默认手段。

## 自定义宿主对象创建

某些宿主扩展点可通过以下形式获取或触发注册的创建逻辑：

```csharp
InterfaceCreator.GetInterfaceInstance<ISaveInfoProvider>();
```

只使用当前宿主版本已验证的接口。修改全局创建逻辑前查找所有调用点和生命周期影响，避免影响其他插件或宿主默认行为。

## 查找界面控件

宿主视觉树辅助方法可能提供 `FindDecendents`（名称以当前版本实际 API 为准）查找后代控件。使用前：

1. 优先寻找公开且稳定的宿主 API；
2. 限定根节点和目标类型，避免扫描整个视觉树；
3. 处理控件尚未加载或模板尚未应用；
4. 不依赖易变化的显示文本和视觉层级作为唯一定位条件。

## 常见失败模式

- 插件加载时立刻访问未就绪服务；
- Cloud 端调用备课 `EditingBoardApi`；
- 在后台线程修改 `Lang.Sources` 或 WPF 控件；
- 每次 Ready 或窗口打开都重复 `Append` UI Item；
- 直接复制示例 `Predicate`，导致菜单永远隐藏或错误显示；
- 读取字符范围时文本尚未布局；
- 忽略组合内元素；
- 把元素内部坐标当作页面坐标；
- 使用空字符串或敏感信息作为不受控埋点内容；
- 依赖具体实现类或 `UseEasiNote=all`，却没有记录版本耦合；
- 使用 `async void` 启动耗时任务而不处理异常。
