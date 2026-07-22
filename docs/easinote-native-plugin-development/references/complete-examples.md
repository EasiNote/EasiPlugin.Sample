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

此示例在 Shell 端等待宿主就绪，注册一个备课画板右键菜单，并在 UI 线程添加中文语言项。点击菜单后读取当前页面顶层文本元素数量，通过 WPF 消息框向用户显示结果，并上报不含敏感信息的数量埋点。

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
using System.Threading;
using System.Windows;
using Cvte.Composition;
using Cvte.EasiNote;
using Cvte.Windows.Localization;

namespace TextElementCounterPlugin;

internal sealed class Program : dotnetCampus.EasiPlugins.EasiPlugin
{
    private int _shellInitializationState;

    protected override Task OnRunningAsync()
    {
        if (EN.CommandOptions.IsCloud)
        {
            return Task.CompletedTask;
        }

        EN.App.Ready += OnAppReady;
        TryStartShell();

        return Task.CompletedTask;
    }

    private void OnAppReady(object? sender, EventArgs e)
    {
        TryStartShell();
    }

    private void TryStartShell()
    {
        if (!EN.App.IsReady)
        {
            return;
        }

        if (Interlocked.CompareExchange(
                ref _shellInitializationState,
                1,
                0) != 0)
        {
            return;
        }

        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RegisterLanguages();
                RegisterMenuItem();
            });

            Interlocked.Exchange(ref _shellInitializationState, 2);
            EN.App.Ready -= OnAppReady;
        }
        catch (Exception exception)
        {
            Interlocked.Exchange(ref _shellInitializationState, 0);
            ShowInitializationError(exception);
        }
    }

    private static void RegisterMenuItem()
    {
        var manager = Container.Current.Get<IUIItemManager>();
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
                ["Lang.TextElementCounter.Result"] = "当前页面有 {0} 个顶层文本元素。",
                ["Lang.TextElementCounter.ErrorTitle"] = "插件初始化失败"
            }
        });
    }

    private static void ShowInitializationError(Exception exception)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                exception.Message,
                "插件初始化失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }
}
```

这段入口代码先订阅 Ready 事件再检查状态，并用状态门防止并发重复初始化。语言项先于菜单注册；只有全部注册成功后才取消 Ready 订阅并进入完成状态。初始化失败会恢复可重试状态并显示错误。插件不在 Cloud 端注册备课菜单。

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

以下示例创建一个顶部工具栏项，使用 WPF `DrawingImage` 注册简单图标资源。它是接入端到端示例 `TryStartShell` 的扩展代码，不是独立项目；工具栏项、资源和多语言都应在同一个 UI Dispatcher 操作中先后注册。

### OpenToolWindowHeaderItem.cs

```csharp
using System.Windows;
using Cvte.EasiNote;
using Cvte.Windows.Input;

namespace TextElementCounterPlugin;

internal sealed class OpenToolWindowHeaderItem : HeadToolBarItem
{
    public OpenToolWindowHeaderItem()
    {
        Key = nameof(OpenToolWindowHeaderItem);
        Type = UIItemTypes.Subject;
        ImageSourceKey = Key;
        ImageWidth = 20;
        ImageHeight = 20;
        SortHint = 250;
        SetValue(TextProperty, Key);
        Predicate = _ => true;
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

### 注册工具栏和图标

```csharp
using System.Windows;
using System.Windows.Media;
using Cvte.Composition;
using Cvte.EasiNote;

private static void RegisterHeadToolBarItem()
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        const string resourceKey = nameof(OpenToolWindowHeaderItem);

        if (!Application.Current.Resources.Contains(resourceKey))
        {
            Application.Current.Resources[resourceKey] = new DrawingImage
            {
                Drawing = new GeometryDrawing(
                    Brushes.Black,
                    null,
                    Geometry.Parse("M4,4 H28 V28 H4 Z M9,10 H23 V13 H9 Z M9,17 H23 V20 H9 Z"))
            };
        }

        var manager = Container.Current.Get<IUIItemManager>();
        manager.Append(
            _ => new OpenToolWindowHeaderItem(),
            new UIItemAttribute(UIItemPurposes.HeadToolBar));
    });
}
```

为工具栏添加多语言时，按宿主版本注册对应键：

```csharp
Lang.Sources.Add(new DictionaryLanguageSource
{
    [new CultureInfo("zh-CHS")] = new Dictionary<string, string>
    {
        ["Lang.HeadToolBar.OpenToolWindowHeaderItem"] = "文本元素工具",
        ["Lang.ToolTip.Insert.OpenToolWindowHeaderItem.Title"] = "文本元素工具",
        ["Lang.ToolTip.Insert.OpenToolWindowHeaderItem.Text"] = "打开文本元素工具窗口"
    }
});
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
