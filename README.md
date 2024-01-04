# EasiPlugin.Sample

希沃白板插件示例

## 开发环境

### VisualStudio

VisualStudio 2022 17.5.2 及更高版本

**注：如遇编译相关问题，优先看本地开发环境的 VisualStudio 版本，低于 VisualStudio 2022 17.5.2 版本基本无法构建通过**

### NuGet 配置

由于当前 `dotnetCampus.EasiPlugin.Sdk` 包的新版本依然还是 alpha 预览模式，没有发布到 NuGet.Org 上，因此需要配置本地的 NuGet 包源为 `packages` 文件夹

或者将 `packages` 文件夹下的 `dotnetcampus.easiplugin.sdk.2.0.0-alpha304.nupkg` 文件导入到自己本地包源或私有的 NuGet 服务器上

默认已通过 `NuGet.config` 文件夹配置指定了本地包源为 `packages` 文件夹，但是否生效取决于 Visual Studio 的心情，如发现还原 `dotnetCampus.EasiPlugin.Sdk` 包版本失败，还请自行配置好本地 NuGet 包源

### EasiNote 希沃白板

请到 https://easinote.seewo.com/ 下载安装最新版本希沃白板，安装时需要安装到默认的 C 盘

安装完成之后，需要先启动一次希沃白板，再进行插件开发

### 系统

Win10 及更高版本

## 开发前置技术

- .NET 6
- WPF

## 术语表

### EasiPlugin

即希沃白板插件的意思。其中 `dotnetCampus.EasiPlugin.Sdk` 为 NuGet 包，提供插件 SDK 负载

希沃白板插件当前有三个不同的形态：

- 配置插件：这是最为广泛的希沃白板插件形态。技术原理为修改计算机的各项配置，和希沃白板的各项配置内容，作为开关的存在，打开或修改希沃白板的隐藏开关，从而决定希沃白板不同的功能展现
- 原生插件：这是定制性最强的插件，通过 .NET 插件机制，作为插件程序集注入到希沃白板内。当前可使用整个希沃白板公开的类型和成员。有着无限的开发想象空间
- Web 插件：支持嵌入网页的形式，通过第三方网页提供的功能。限制极大，但比较独立。技术原理为希沃白板内使用 CEF 浏览器打开给定的网址。文档请参阅 [EasiNote.ClientWebApi.Documentation](https://github.com/EasiNote/EasiNote.ClientWebApi.Documentation)

本仓库为 `原生插件` 的示例

### 云课件

即希沃白板 Cloud 端。与之对应的还有希沃白板 Shell 端。希沃白板 EasiNote.exe 将会根据不同的启动命令行参数以及环境变量等配置决定进程启动形态，启动形态可分化为 Cloud 端和 Shell 端

希沃白板 Cloud 端是云课件界面，提供备课模式下所见界面，即包含云课件列表的界面

希沃白板 Shell 端是备课授课界面，其中授课界面可分为带课件的授课和不带课件的希沃大板黑板 IWB 模式

无论是 Cloud 端或 Shell 端，都会在进程启动时加载插件，但是 Cloud 端和 Shell 端执行初始化的模块不同。如插件是针对备课制作的，可只在 Shell 端执行。针对 Cloud 端和 Shell 端执行不同的插件逻辑，可通过 `EN.CommandOptions.IsCloud` 属性进行判断，如以下代码示例

```csharp
            if (EN.CommandOptions.IsCloud)
            {
                // 云课件逻辑
            }
            else
            {
                // Shell 逻辑
            }
```

### Paint

画板，这是 Shell 端的概念。希沃白板的课件是在 Paint 画板上承载的。希沃白板属于白板类软件，白板类软件的核心之一就是画板

画板是一个抽象的概念，不同的团队有着不同的叫法

在希沃白板里面的定义是 Paint 是核心的画板，在画板上书写梦想

对应在现实世界的抽象对应就是一个美术支起来的画板，可以在画板上放入许多纸张进行绘制，可以用许多不同类型的笔进行画画，可以使用一些小工具辅助画画。画完之后，可以将画的内容保存，画错了可以进行撤销重做

从界面上可以认为放在备课主编辑界面，以及授课的黑板就是 Paint 画板的功能。在画板上面允许有多个页面，页面就是 Slide 的概念。在备课授课的一个个 Slide 页面上，可以存放着很多个不同类型的 Element 元素。元素是一个基础抽象概念，在希沃白板里面有着许多类型的元素，比如 TextElement 文本元素，比如 GroupElement 组合元素等等

#### Slide 页面

备课和授课都有页面的概念，希沃白板的页面概念和 PPT 的幻灯片页面是相同的概念。页面坐标系是指使用页面左上角作为坐标原点的坐标系，放入到页面里面的元素都会使用页面坐标系，具体元素内部将使用元素自身的坐标系

#### Element 元素

元素是一个基础抽象概念，指的是放在 Slide 页面上的元素，比如文本元素、形状元素、表格元素等等。其中组合元素是最特殊的元素，组合元素是多个元素的组合

元素在页面的具体坐标取决于元素本身的变换模型，不同的元素类型可以有不同的变换模型。对于大部分元素来说，在无视旋转的前提下，可以通过元素的 Bounds 属性修改元素的坐标和尺寸，如以下代码示例

```csharp
element.Bounds = new Rect(position, size);
```

### 插件安装包

构建希沃白板插件项目，将会在插件 SDK 的辅助下生成插件安装包。插件安装包有以下不同形态，可选某一形态进行分发

- exe 包：这是独立的 exe 安装包，可以直接分发给到用户端，双击运行即可安装插件。这是最常见的分发文件。但是此 exe 安装包采用 .NET Framework 框架，要求用户端 .NET Framework 环境
- zip 包：包含插件的所有文件，可用于给杀毒软件报备
- enp 包：用于发布到希沃白板应用中心的包，将由希沃白板应用中心托管安装。仅仅只是将 zip 包后缀名改为 enp 而已
- enpx 包：属于 zip 包格式，内部存放 .NET 6 框架的 exe 独立安装包。目的是为了解决 .NET 6 框架依赖发布的独立安装包的 .NET Runtime 环境依赖问题。由希沃白板 AppHost 进程托管启动

以上安装包将在构建插件项目之后，存放在 `bin\[Debug|Release]` 文件夹下

### 多语言

希沃白板提供国际化多语言支持，对于界面可见内容，推荐使用多语言配置

添加多语言项示例

```csharp
            // 确保在主 UI 线程执行。因为多语言许多是和界面相关的
            Lang.Sources.Add(new DictionaryLanguageSource
            {
                [new CultureInfo("zh-CHS")] = new Dictionary<string, string>()
                {
                    { "Lang.BoardEditContextMenu.EasiPluginSampleElementTool", "演示工具" },
                },
            });
```

获取多语言示例

```csharp
var text = Lang.Get("Lang.BoardEditContextMenu.EasiPluginSampleElementTool");
```

### 埋点上报

上报用户行为数据到希沃后台，可用于希沃后台数据分析，无实际业务功能，数据不对外公开。上报用户行为时，还请规避上报用户敏感数据

上报示例代码

```csharp
SafeEN.Collection.ReportEvent(EventId.SampleEventId, "额外的内容");
```

## 开发入门

请参考本示例代码进行开发

进入 Program.cs 文件。此文件里面包含插件入口。在 OnRunningAsync 方法里编写插件运行初始化代码

## 开发示例

### 获取 Shell 端的备课页面

获取备课当前页面可以使用以下代码

```csharp
            var currentSlide = EN.EditingBoardApi.CurrentSlide;
```

获取备课下所有页面可以使用以下代码

```csharp
            var slides = EN.EditingBoardApi.Slides;
```

获取授课下的当前页面可以使用以下代码

```csharp
            var currentSlide = EN.DisplayingBoardApi.CurrentSlide;
```

### 获取页面里面的文本元素

页面里面的元素存放在 Slide 类型的 Elements 属性里面，只获取文本元素可使用 `Slide.Elements.OfType<TextElement>()` 的方式获取，如此即可获取到 Slide 页面里面的所有文本元素

如需只取当前页面的首个文本元素，可以使用以下代码

```csharp
            var currentSlide = EN.EditingBoardApi.CurrentSlide;

            // 文本元素对应的是 TextElement 类型
            var firstTextElement = currentSlide.Elements.OfType<TextElement>().FirstOrDefault();
```

### 获取文本的字符相对于页面的坐标

在获取到 TextElement 对象之后，可以使用 GetRunBoundsByDocumentOffset 方法获取给定字符序号的相对于文本的字符范围，如以下代码所示

```csharp
                var charCount = firstTextElement.TextEditor.CharCount;
                for (int i = 0; i < charCount; i++)
                {
                    // 获取每个字符相对于文本元素的坐标
                    var bounds = firstTextElement.TextEditor.GetRunBoundsByDocumentOffset(i);

                    // 通过 WPF 的坐标系转换方法可以转换为页面坐标系
                    var charTopLeftInSlide = firstTextElement.TextEditor.TranslatePoint(bounds.TopLeft,currentSlide);
                }
```

以上代码可在 `EasiPluginSampleElementToolMenuItem.GetTextBounds` 找到可执行例子

获取文本的字符范围时，要求当前文本框已布局渲染完成。如当前文本正在被编辑，可以通过 `TextEditor.RenderCompleted` 事件等待文本布局渲染完成

### 获取组合内的文本元素

文本元素可以放入到组合里面，想要获取组合内的元素可使用如下代码方式获取

```csharp
            foreach (var groupElement in currentSlide.Elements.OfType<GroupElement>())
            {
                foreach (var textElement in groupElement.Elements.OfType<TextElement>())
                {
                    // 这是获取组合内元素的例子
                }
            }
```

### 退出 EasiNote 进程

```csharp
             ShellFlowHelper.EnsureShutdown("退出希沃白板5的原因");
```

### 导出 Enbx 为本地文件

```csharp
                 var storageModel = await EN.CurrentBoardApi.GetStorageModelAsync();
                 string filePath = "你所要导出的路径";
                 Container.Current.Get<IEnbxStorageProvider>().ExportEnb(storageModel.Model, filePath);
```


### 监听 PPTX 转换 ENBX 完成事件


```csharp
            var converter = await Container.Current.GetAsync<IPptxToEnbxConverter>();
            if (converter is PptxToEnbxConverter pptxToEnbxConverter)
            {
                // 订阅Pptx导入成功后的事件
                pptxToEnbxConverter.PptxToEnbxConverted += PptxToEnbxConverter_PptxToEnbxConverted;
            }

        private static void PptxToEnbxConverter_PptxToEnbxConverted(object? sender, PptxToEnbxConvertedEventArgs e)
        {
            // 处理Pptx导入成功后的逻辑
        }
```

### 修改存储创建对象逻辑

创建自定义 `InterfaceCreator.GetInterfaceInstance<ISaveInfoProvider>()` 元素，通过此方法调用注册创建逻辑

### 开发工具栏

备课工具栏

```csharp
    // [LangExport("选择窗格")]
    // [UIItem(UIItemPurposes.HeadToolBar)]
    public class EditorSelectionElementHeaderToolBarItem : HeadToolBarItem
    {
        public EditorSelectionElementHeaderToolBarItem()
        {
            Key = "EditorSelectionElementHeaderToolBarItem";
            Type = UIItemTypes.Subject;

            ImageSourceKey = Key;
            ImageWidth = 20;
            ImageHeight = 20;
            SortHint = 251;

            SetValue(TextProperty, "EditorSelectionElementHeaderToolBarItem");

            Predicate = e => false;

            Command = new DelegateCommand(() =>
            {
                var selectionElementWindow = new SelectionElementWindow()
                {
                    WindowStyle = WindowStyle.ToolWindow,
                    Title = "选择窗格",
                    Width = 200,
                    Owner = Window.GetWindow(EN.EditingBoardApi.Board)
                };

                SafeEN.Collection.ReportEvent(EventId.OpenEditorSelectionElementWindow, string.Empty);
                
                selectionElementWindow.Show();
            });

            ResourceHelper.TryAddResource(ImageSourceKey, new DrawingImage()
            {
                Drawing = new GeometryDrawing
                (
                    brush: Brushes.Black,
                    pen: null,
                    geometry: Geometry.Parse(
                        "M8.61,6.00333333333333 C8.45333333333333,6.00333333333333 8.3,6.04333333333333 8.16,6.12333333333333 C7.88333333333333,6.28 7.71,6.57666666666667 7.70666666666667,6.89666666666667 L7.70666666666667,26.4233333333333 C7.70333333333333,26.7466666666667 7.88,27.0466666666667 8.16333333333333,27.2066666666667 8.39333333333333,27.3433333333333 8.67666666666667,27.3666666666667 8.93,27.27 L14.03,19.6033333333333 18.75,27.7733333333333 C18.8466666666667,27.9366666666667 19.0033333333333,28.0533333333333 19.1833333333333,28.1 19.3633333333333,28.1533333333333 19.5566666666667,28.1266666666667 19.7166666666667,28.03 L20.9533333333333,27.32 C21.1166666666667,27.2266666666667 21.2333333333333,27.0733333333333 21.2833333333333,26.8933333333333 21.33,26.71 21.3066666666667,26.5166666666667 21.21,26.3533333333333 L16.3533333333333,17.9833333333333 26.0666666666667,17.3566666666667 C26.3,17.1733333333333 26.4233333333333,16.8833333333333 26.4,16.5866666666667 C26.3766666666667,16.2933333333333 26.2066666666667,16.03 25.9466666666667,15.8833333333333 L9.05666666666667,6.12 C8.92,6.04 8.76333333333333,6.00333333333333 8.61,6.00333333333333 Z M8.63,4.57333333333333 C9.03,4.57333333333333 9.43,4.67666666666667 9.79,4.88333333333333 L26.68,14.6466666666667 C27.4,15.06 27.8466666666667,15.8266666666667 27.8466666666667,16.66 C27.8466666666667,17.49 27.4,18.26 26.68,18.6733333333333 L26.5366666666667,18.7566666666667 18.7566666666667,19.2566666666667 22.4466666666667,25.6566666666667 C22.73,26.1466666666667 22.8066666666667,26.7266666666667 22.66,27.2766666666667 C22.5133333333333,27.8233333333333 22.1566666666667,28.2866666666667 21.6666666666667,28.57 L20.4366666666667,29.2833333333333 C20.11,29.4633333333333 19.7433333333333,29.56 19.37,29.56 19.1833333333333,29.5566666666667 18.9966666666667,29.5333333333333 18.8133333333333,29.4866666666667 18.2666666666667,29.34 17.8033333333333,28.9833333333333 17.52,28.4933333333333 L13.9633333333333,22.3 9.93333333333333,28.3566666666667 9.79,28.4366666666667 C9.07,28.85 8.18666666666667,28.8466666666667 7.46666666666667,28.4333333333333 C6.75,28.02 6.30666666666667,27.2533333333333 6.30666666666667,26.4233333333333 L6.30666666666667,6.89666666666667 C6.30666666666667,6.06666666666667 6.75,5.3 7.46666666666667,4.88666666666667 C7.82666666666667,4.68 8.22666666666667,4.57333333333333 8.63,4.57333333333333 Z")
                ),
            });
        }
    }

    public static class ResourceHelper
    {
        public static bool TryAddResource(string key, object resource)
        {
            if (Application.Current.Resources[key] is null)
            {
                Application.Current.Resources[key] = resource;

                return true;
            }

            return false;
        }
    }
```

备课工具栏多语言，可以通过 LangExport 特性设置多语言，但更推荐的是在主线程执行以下代码添加多语言

```csharp
                Lang.Sources.Add(new DictionaryLanguageSource
                {
                    [new CultureInfo("zh-CHS")] = new Dictionary<string, string>()
                    {
                        { "Lang.ToolTip.Insert.EditorSelectionElementHeaderToolBarItem.Title", "选择窗格" },
                        { "Lang.ToolTip.Insert.EditorSelectionElementHeaderToolBarItem.Text", "打开元素选择窗格" },
                        { "Lang.HeadToolBar.EditorSelectionElementHeaderToolBarItem", "选择窗格" }
                    },
                });
```

将 EditorSelectionElementHeaderToolBarItem 替换为自己的 Key 的值

加上对应的注入，注入之前需要确保 IUIItemManager 已经注册到容器里面

```csharp
        private void ExportUIItems()
        {
            // 添加 UI 扩展。
            var manager = Container.Current.Get<IUIItemManager>();
            manager.Append(c => new LatexItem(), new UIItemAttribute(UIItemPurposes.HeadToolBar));
            manager.Append(c => new EditLatexElementMenuItem(), new UIItemAttribute(UIItemPurposes.ElementEditMenu));
        }
```

修改的执行代码如下

```csharp
        protected override async Task OnRunningAsync()
        {
            if (EN.CommandOptions.IsCloud)
            {

            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // 等待各项初始化完成
                var manager = Container.Current.Get<IUIItemManager>();
                manager.Append(c => new ExportAsImageMenuItem(), new UIItemAttribute(UIItemPurposes.ElementEditMenu));

                // 插件默认在后台线程执行，必须切换到主 UI 线程才能添加多语言
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 为 UI 扩展添加语言项。
                    Lang.Sources.Add(new DictionaryLanguageSource
                    {
                        [Lang.Current] = new Dictionary<string, string>()
                        {
                            { "Lang.BoardEditContextMenu.ExportAsImage", "导出为图片" },
                        },
                    });
                });
            }
        }
    }
```

### 寻找界面控件

使用 FindDecendents 方法

### 获取 EN 安装路径

读取以下注册表项即可

```
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Seewo\EasiNote5
```


## 插件机制

### 加上更多的引用

默认的插件只引用希沃白板的大多数功能的程序集，可以通过以下配置设置插件引用希沃白板的程序集

```xml
  <PropertyGroup>

    <!-- 引用 EasiNote 的默认级别：
          - none: 无任何 EasiNote 依赖
          - api: 仅 API 和 API 的依赖
          - core: 包含大多数功能的程序集（不含扩展功能）
          - most: 绝大多数程序集（包含扩展功能）
          - all: 所有会加载到 EasiNote 进程的托管程序集 -->
    <UseEasiNote>most</UseEasiNote>

  </PropertyGroup>
```

以上配置是编写到 csproj 项目文件里面

如果只是想单独加上某个 DLL 的引用，可以使用如下代码加上，以下示例加上 Cvte.Windows.Media.Imaging.Effect.dll 程序集

```xml
  <Target Name="IncludeEn" BeforeTargets="_ENSdkReferenceDlls">
    <ItemGroup>
      <EasiNoteReference Include="Cvte.Windows.Media.Imaging.Effect.dll"></EasiNoteReference>
    </ItemGroup>
  </Target>
```




## 插件群

QQ群：619366360

如有使用问题，可到插件群反馈