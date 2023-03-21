# EasiPlugin.Sample

希沃白板插件示例

## 开发环境

### VisualStudio

VisualStudio 2022 17.5.2 及更高版本

**注：如遇编译相关问题，优先看本地开发环境的 VisualStudio 版本，低于 VisualStudio 2022 17.5.2 版本基本无法构建通过**

### NuGet 配置

由于当前 `dotnetCampus.EasiPlugin.Sdk` 包的新版本依然还是 alpha 预览模式，没有发布到 NuGet.Org 上，因此需要配置本地的 NuGet 包源为 `packages` 文件夹

或者将 `packages` 文件夹下的 `dotnetcampus.easiplugin.sdk.2.0.0-alpha109.nupkg` 文件导入到自己本地包源或私有的 NuGet 服务器上

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

进入 Program.cs 文件。此文件里面包含插件入口。在 OnRunning 方法里编写插件运行初始化代码

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

## 插件群

QQ群：619366360

如有使用问题，可到插件群反馈