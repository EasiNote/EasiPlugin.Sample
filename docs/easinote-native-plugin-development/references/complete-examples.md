# 完整开发示例

本文提供一个端到端项目示例，以及可接入该项目的功能扩展示例。在新建插件、要求“给出完整示例”或需要把多个宿主 API 串成执行流程时读取本文。

示例基线为 .NET 6、WPF、`dotnetCampus.EasiPlugin.Sdk` 2.1.1-alpha.3 和 EasiNote 5。宿主 API 可能随版本变化；复制代码后应使用当前项目实际还原的 SDK 和目标 EasiNote 版本编译验证。

## 目录

- [端到端示例：备课右键菜单插件](#端到端示例备课右键菜单插件)
- [扩展示例：获取文本字符的页面坐标](#扩展示例获取文本字符的页面坐标)
- [扩展示例：注册备课顶部工具栏](#扩展示例注册备课顶部工具栏)
- [扩展示例：执行带取消的耗时任务](#扩展示例执行带取消的耗时任务)
- [版本绑定示例：监听 PPTX 转换完成](#版本绑定示例监听-pptx-转换完成)

## 端到端示例：备课右键菜单插件

此示例在 Shell 端等待 `IUIItemManager` 可用，随后在 UI 线程添加中文语言项并注册备课画板右键菜单。点击菜单后读取当前页面顶层文本元素数量，通过 WPF 消息框向用户显示结果，并上报不含敏感信息的数量埋点。

文件清单：

```text
TextElementCounterPlugin.csproj
Program.cs
CountTextElementsMenuItem.cs
EventIds.cs
Properties/launchSettings.json
```

项目文件、三个 C# 文件可直接组成插件主体。`launchSettings.json` 使用 [project-setup.md](project-setup.md) 中的模板，并把 `executablePath` 替换为目标机器实际路径。

### 项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <Product>页面文本统计插件</Product>
    <Description>在备课右键菜单中显示当前页面的文本元素数量。</Description>
    <UseEasiNote>all</UseEasiNote>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="dotnetCampus.EasiPlugin.Sdk" Version="2.1.1-alpha.3" />
  </ItemGroup>
</Project>
```

示例使用 `all` 以减少初次理解引用范围的干扰。实际项目应逐步降低到满足编译的最小 `UseEasiNote` 级别。

### Program.cs

```csharp
using System.Globalization;
using System.Windows;
using Cvte.Composition;
using Cvte.EasiNote;
using Cvte.Windows.Localization;

namespace TextElementCounterPlugin;

internal sealed class Program : dotnetCampus.EasiPlugins.EasiPlugin
{
    protected override Task OnRunningAsync()
    {
        return EN.CommandOptions.IsCloud
            ? Task.CompletedTask
            : StartShellAsync();
    }

    private static async Task StartShellAsync()
    {
        var manager = await Container.Current
            .GetAsync<IUIItemManager>()
            .ConfigureAwait(false);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RegisterLanguages();
            RegisterMenuItem(manager);
        });
    }

    private static void RegisterMenuItem(IUIItemManager manager)
    {
        manager.Append(
            _ => new CountTextElementsMenuItem(),
            new UIItemAttribute(UIItemPurposes.BoardEditMenu));
    }

    private static void RegisterLanguages()
    {
        Lang.Sources.Add(new DictionaryLanguageSource
        {
            [new CultureInfo("zh-CHS")] = new Dictionary<string, string>
            {
                ["Lang.BoardEditContextMenu.CountTextElements"] = "统计文本元素",
                ["Lang.TextElementCounter.Result"] = "当前页面有 {0} 个顶层文本元素。"
            }
        });
    }
}
```

这段入口代码不在 Cloud 端注册备课菜单。Shell 初始化直接等待真实依赖 `IUIItemManager`，再切换到 Dispatcher 注册语言和菜单；不额外叠加 Ready、固定延迟或无依据的并发状态机。宿主会观察 `OnRunningAsync` 返回的任务；若目标 SDK 的调用语义不同，应按项目现有日志和错误提示方式观察初始化异常。

### CountTextElementsMenuItem.cs

```csharp
using System.Linq;
using System.Globalization;
using System.Windows;
using Cvte.EasiNote;
using Cvte.Paint.Features.Elements.Texts;
using Cvte.Windows.Input;
using Cvte.Windows.Localization;
using dotnetCampus.EasiPlugins;

namespace TextElementCounterPlugin;

internal sealed class CountTextElementsMenuItem : BoardEditMenuItem
{
    public CountTextElementsMenuItem()
    {
        Key = nameof(CountTextElementsMenuItem);
        SortHint = 50;
        Predicate = selectedElements => selectedElements.Count == 0;
        Command = new DelegateCommand(Execute);
    }

    private static void Execute()
    {
        var currentSlide = EN.EditingBoardApi.CurrentSlide;
        int textElementCount = currentSlide.Elements
            .OfType<TextElement>()
            .Count();

        string message = string.Format(
            CultureInfo.CurrentCulture,
            Lang.Get("Lang.TextElementCounter.Result"),
            textElementCount);

        MessageBox.Show(
            message,
            Lang.Get("Lang.BoardEditContextMenu.CountTextElements"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        SafeEN.Collection.ReportEvent(
            EventIds.CountTextElements,
            textElementCount.ToString());
    }
}
```

此示例明确统计当前页面的顶层文本元素，不包含组合内部元素。若业务要求递归统计组合，应使用 [api-recipes.md](api-recipes.md) 的组合元素配方扩展。埋点只上报数量，不应包含文本内容、课件名称、文件路径或用户信息。

### EventIds.cs

```csharp
namespace TextElementCounterPlugin;

internal static class EventIds
{
    public const string CountTextElements = "TextElementCounter.CountTextElements";
}
```

### 语言键对应关系

菜单类型名为 `CountTextElementsMenuItem`，去掉 `MenuItem` 后得到 `CountTextElements`，因此语言键使用：

```text
Lang.BoardEditContextMenu.CountTextElements
```

如果当前宿主版本采用不同的语言键推导规则，应以实际 UI Item 读取逻辑为准。

## 扩展示例：获取文本字符的页面坐标

以下服务获取指定页面中指定文本元素的全部字符左上角页面坐标。调用前必须确保文本已经完成布局。把此文件加入端到端示例项目后，可从菜单命令或其他 UI Item 调用。

```csharp
using System.Windows;
using Cvte.Paint.Features.Elements.Texts;

namespace TextElementCounterPlugin;

internal static class TextBoundsService
{
    public static IReadOnlyList<Point> GetCharacterPositions(
        UIElement coordinateRoot,
        TextElement textElement)
    {
        int charCount = textElement.TextEditor.CharCount;
        var positions = new List<Point>(charCount);

        for (int index = 0; index < charCount; index++)
        {
            Rect boundsInTextElement = textElement.TextEditor
                .GetRunBoundsByDocumentOffset(index);

            Point positionInSlide = textElement.TextEditor.TranslatePoint(
                boundsInTextElement.TopLeft,
                coordinateRoot);

            positions.Add(positionInSlide);
        }

        return positions;
    }
}
```

如果文本正在编辑或刚刚修改，等待 `TextEditor.RenderCompleted` 再调用，并在首次处理完成后取消订阅：

```csharp
private void WaitForTextRender(
    UIElement coordinateRoot,
    TextElement textElement)
{
    textElement.TextEditor.RenderCompleted += OnRenderCompleted;

    void OnRenderCompleted(object? sender, EventArgs e)
    {
        textElement.TextEditor.RenderCompleted -= OnRenderCompleted;
        IReadOnlyList<Point> positions =
            TextBoundsService.GetCharacterPositions(
                coordinateRoot,
                textElement);

        // 使用 positions 执行业务逻辑。
    }
}
```

事件签名以当前宿主版本为准。若编译器提示参数类型不同，按实际事件委托调整，不能通过动态调用绕过类型检查。

## 扩展示例：注册备课顶部工具栏

以下示例创建一个顶部工具栏项，使用 WPF `DrawingImage` 注册简单图标资源。它是接入端到端示例 `StartShellAsync` 的扩展代码，不是独立项目；先等待 `IUIItemManager`，再在同一个 UI Dispatcher 操作中依次注册资源、多语言和工具栏项。完整 Key 与“学科工具”分流规则见 [head-toolbar-and-subject-tools.md](head-toolbar-and-subject-tools.md)。

### OpenToolWindowHeaderItem.cs

```csharp
using System.Windows;
using Cvte.EasiNote;
using Cvte.Windows.Input;
using Cvte.Windows.Localization;

namespace TextElementCounterPlugin;

internal sealed class OpenToolWindowHeaderItem : HeadToolBarItem
{
    internal const string ItemKey = "HeadToolBar.TextElementTool";
    internal const string ImageResourceKey =
        "Image.ToolBar.TextElementTool.TabUI";

    public OpenToolWindowHeaderItem()
    {
        Key = ItemKey;
        Type = UIItemTypes.Applications;
        ImageSourceKey = ImageResourceKey;
        ImageWidth = 20;
        ImageHeight = 20;
        SortHint = double.MaxValue;
        SetValue(TextProperty, Lang.Get("Lang.HeadToolBar.TextElementTool"));
        Command = new DelegateCommand(ShowWindow);
    }

    private static void ShowWindow()
    {
        var window = new TextElementToolWindow
        {
            WindowStyle = WindowStyle.ToolWindow,
            Title = "文本元素工具",
            Width = 320,
            Height = 240,
            Owner = Window.GetWindow(EN.EditingBoardApi.Board)
        };

        window.Show();
    }
}
```

`TextElementToolWindow` 是插件自己的 WPF Window。实际项目应通过多语言资源设置标题，不要硬编码界面文案。

### TextElementToolWindow.cs

```csharp
using System.Windows;
using System.Windows.Controls;

namespace TextElementCounterPlugin;

internal sealed class TextElementToolWindow : Window
{
    public TextElementToolWindow()
    {
        Content = new TextBlock
        {
            Margin = new Thickness(16),
            Text = "在此放置文本元素工具界面。",
            TextWrapping = TextWrapping.Wrap
        };
    }
}
```

### 注册工具栏、图标和语言

将以下调用接入 `StartShellAsync`。如果同一个入口还注册右键菜单，可复用已经获取的 `IUIItemManager`：

```csharp
private static async Task RegisterHeadToolBarItemAsync()
{
    var manager = await Container.Current
        .GetAsync<IUIItemManager>()
        .ConfigureAwait(false);

    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        RegisterHeadToolBarIcon();
        RegisterHeadToolBarLanguages();
        manager.Append(
            _ => new OpenToolWindowHeaderItem(),
            new UIItemAttribute(UIItemPurposes.HeadToolBar));
    });
}

private static void RegisterHeadToolBarIcon()
{
    if (Application.Current.Resources.Contains(
            OpenToolWindowHeaderItem.ImageResourceKey))
    {
        return;
    }

    var icon = new DrawingImage
    {
        Drawing = new GeometryDrawing(
            Brushes.Black,
            null,
            Geometry.Parse(
                "M3,2 H17 V18 H3 Z M6,6 H14 V8 H6 Z M6,10 H14 V12 H6 Z"))
    };

    icon.Freeze();
    Application.Current.Resources[
        OpenToolWindowHeaderItem.ImageResourceKey] = icon;
}

private static void RegisterHeadToolBarLanguages()
{
    Lang.Sources.Add(new DictionaryLanguageSource
    {
        [new CultureInfo("zh-CHS")] = new Dictionary<string, string>
        {
            ["Lang.HeadToolBar.TextElementTool"] = "文本元素工具",
            ["Lang.ToolTip.Insert.TextElementTool.Title"] = "文本元素工具",
            ["Lang.ToolTip.Insert.TextElementTool.Text"] =
                "打开文本元素工具窗口"
        }
    });
}
```

## 扩展示例：执行带取消的耗时任务

以下示例展示命令如何持有 `CancellationTokenSource`，把取消令牌传给底层循环，并在完成后释放资源。`DoWithLoadingAsync` 的返回类型随 SDK 版本而异，因此这里按已知调用形态展示，不假定它一定可以 `await`。

```csharp
using System.Threading;
using Cvte.EasiNote;

private static void StartLongRunningWork()
{
    var cancellationTokenSource = new CancellationTokenSource();

    EN.Notification.DoWithLoadingAsync(
        async () =>
        {
            try
            {
                await ProcessSlidesAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // 用户取消属于预期路径，可按产品需要提示。
            }
            catch (Exception exception)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        exception.Message,
                        "处理课件失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        },
        "正在处理课件……",
        cancellationTokenSource);
}

private static async Task ProcessSlidesAsync(CancellationToken cancellationToken)
{
    var slides = EN.EditingBoardApi.Slides;

    foreach (var slide in slides)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        int elementCount = slide.Elements.Count;
        _ = elementCount;
    }
}
```

示例中的最小循环只读取页面元素数量，用于展示取消检查位置。替换为真实业务时，应继续向所有支持取消的 I/O 或异步 API 传递令牌。对于不支持取消的宿主 API，只能在调用前后检查取消，不能宣称已经强制中断正在执行的宿主操作。界面文案应在正式插件中移入语言源。

如果当前 SDK 的 `DoWithLoadingAsync` 返回 `Task`，应在可等待的调用链中等待它并统一处理异常。如果不返回 `Task`，异步委托内部必须捕获、记录或向用户呈现非取消异常，避免异常逃逸到宿主进程。

## 版本绑定示例：监听 PPTX 转换完成

以下组件在启动时订阅 PPTX 转 ENBX 完成事件，并提供显式停止方法取消订阅。该示例依赖具体实现类型，不保证跨 EasiNote 版本编译；只有在当前 SDK 与宿主中确认这些类型和事件签名后才使用。

```csharp
using Cvte.Composition;

namespace TextElementCounterPlugin;

internal sealed class PptxConversionObserver
{
    private PptxToEnbxConverter? _converter;

    public async Task StartAsync()
    {
        if (_converter is not null)
        {
            return;
        }

        var converter = await Container.Current.GetAsync<IPptxToEnbxConverter>();

        if (converter is not PptxToEnbxConverter concreteConverter)
        {
            return;
        }

        _converter = concreteConverter;
        _converter.PptxToEnbxConverted += OnPptxToEnbxConverted;
    }

    public void Stop()
    {
        if (_converter is null)
        {
            return;
        }

        _converter.PptxToEnbxConverted -= OnPptxToEnbxConverted;
        _converter = null;
    }

    private static void OnPptxToEnbxConverted(
        object? sender,
        PptxToEnbxConvertedEventArgs e)
    {
        // 根据事件参数处理转换完成后的业务。
    }
}
```

此示例依赖具体实现类型，版本耦合较强。如果当前 SDK 在 `IPptxToEnbxConverter` 接口上直接公开事件，应优先保存接口并订阅接口事件。

## 组合示例时的检查清单

- 修改命名空间、产品名称、语言键和埋点 ID，避免不同插件冲突；
- 根据功能选择 `BoardEditMenu`、`HeadToolBar` 或其他准确的 UI purpose；
- 不把所有示例无条件塞入同一个插件，只组合任务实际需要的部分；
- 所有 UI 和语言资源操作在 WPF UI 线程完成；
- 所有宿主事件都保存订阅对象，并在适当生命周期取消订阅；
- 编译后在 Cloud、备课和授课目标模式中分别验证实际分支；
- 根据编译结果缩小 `UseEasiNote`，不要长期依赖 `all`；
- 分发前移除调试文案、测试埋点和任何本机绝对路径。
