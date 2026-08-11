# HeadToolBarItem 与“学科工具”入口

在 Shell 备课端添加顶部工具栏项或“学科工具”下拉入口，处理注册方式、Key、图标、文本、Tooltip 和用户工具栏配置时读取本文。

本文中的宿主分流和语言键推导来自已验证版本的宿主行为，可能随版本变化。实现后必须在目标 EasiNote 版本中验证。

## 扩展点

备课顶部工具栏体系使用：

```text
HeadToolBarItem
  + UIItemPurposes.HeadToolBar
  + IUIItemManager
```

一个 `HeadToolBarItem` 可能显示在顶部工具栏，也可能进入“学科工具”下拉。最终位置不仅由注册 API 决定，还受用户工具栏配置和 Item 属性影响。

## 选择一种注册方式

SDK 支持两种方式：

1. 给类型添加 `[UIItem]`，由 SDK 扫描插件程序集；
2. 在 Shell 初始化中调用 `IUIItemManager.Append(...)`。

两种方式只能选择一种，否则会产生重复 Key 或重复入口。

对于 Shell 专属入口，推荐在 Shell 分支等待管理器并动态注册：

```csharp
var manager = await Container.Current
    .GetAsync<IUIItemManager>()
    .ConfigureAwait(false);

await Application.Current.Dispatcher.InvokeAsync(() =>
{
    RegisterResources();
    RegisterLanguages();
    manager.Append(
        _ => new MyHeadToolBarItem(),
        new UIItemAttribute(UIItemPurposes.HeadToolBar));
});
```

不要在 UI 线程同步调用 `Container.Current.Get<IUIItemManager>()`。生命周期细节见 [lifecycle-and-container.md](lifecycle-and-container.md)。

## 稳定的业务标识与 Key

宿主原生备课工具普遍采用：

```text
HeadToolBar.<业务标识>
```

例如插件业务标识为 `MyPlugin`：

```text
HeadToolBar.MyPlugin
```

Key 会参与 UI Item 去重、用户工具栏配置持久化、自动化标识以及语言键推导。发布后不要随意修改，否则用户已有配置会将其视为另一个工具。

业务标识必须稳定且足够唯一，避免与宿主或其他插件碰撞。不要使用 `Tool`、`Plugin` 等过于通用的后缀作为完整标识。

## Key、图片和语言键契约

已验证宿主版本会取 Item Key 的最后一段作为业务后缀。例如：

```text
HeadToolBar.MyPlugin
```

推导后缀：

```text
MyPlugin
```

并查找以下键：

```text
Item Key:       HeadToolBar.MyPlugin
Image Key:      Image.ToolBar.MyPlugin.TabUI
Text Key:       Lang.HeadToolBar.MyPlugin
Tooltip Title:  Lang.ToolTip.Insert.MyPlugin.Title
Tooltip Text:   Lang.ToolTip.Insert.MyPlugin.Text
```

因此 Item、图片、文本和 Tooltip 应共享同一个业务标识。

即使构造函数设置了 `Text` 或 `ToolTip`，宿主控件仍可能按上述规则覆盖显示值。必须注册目标版本实际读取的语言键。

## 最小 Item 定义

```csharp
internal sealed class MyPluginHeadToolBarItem : HeadToolBarItem
{
    internal const string ItemKey = "HeadToolBar.MyPlugin";
    internal const string ImageResourceKey =
        "Image.ToolBar.MyPlugin.TabUI";

    internal MyPluginHeadToolBarItem()
    {
        Key = ItemKey;
        Type = UIItemTypes.Applications;
        SortHint = double.MaxValue;
        StyleSourceKey = "Style.ToolBarButton";
        ImageSourceKey = ImageResourceKey;
        ImageWidth = 20;
        ImageHeight = 20;
        SetValue(TextProperty, Lang.Get("Lang.HeadToolBar.MyPlugin"));
        Command = new AsyncRelayCommand(ExecuteAsync);
    }

    private static Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
```

占位命令必须替换为项目自己的实现，并按现有命令库调整类型。不要为了复制示例而引入新的命令依赖。

## 顶部工具栏与“学科工具”的分流

已验证宿主版本会加载全部 `HeadToolBarItem`，再根据用户备课工具栏配置分流：

```text
Key 在用户配置中
  → 顶部工具栏集合

Key 不在用户配置中
  → 额外工具集合
  → “学科工具”下拉
```

在用户没有自定义配置时，默认工具栏生成还会考虑 Item 属性。已验证版本中，扩展项通常需要满足以下条件才会被加入默认顶部工具栏：

- `Type` 不是 `UIItemTypes.WebResource`；
- `Predicate` 不为空；
- `Predicate` 对当前学段和学科返回 `true`。

若目标是让新入口默认留在“学科工具”下拉：

- 保持 `Predicate` 为空；
- 使用稳定、唯一的 Key；
- 不主动把该 Key 写入用户顶部工具栏配置。

不要把 `Predicate` 当作简单的“是否显示在学科工具”开关。它表达工具适用条件，并参与默认工具栏配置生成。用户已有配置也会影响最终位置。

## 下拉分组

已验证版本中的“学科工具”面板按 `IsExternalTool` 分组：

- `false`：学科工具；
- `true`：在线资源。

`bool` 默认值为 `false`，普通学科工具通常无需显式设置。

`Type = UIItemTypes.Applications` 表达应用类入口，但当前可视分组不一定按 `Type` 单独显示标题。不要仅根据枚举名称推断最终 UI，需在目标版本验证。

## 图标展示尺寸

顶部工具栏图标槽为 20×20，Item 通常设置：

```csharp
ImageWidth = 20;
ImageHeight = 20;
```

PNG 源文件可以是 40×40、64×64 或更高分辨率，由 WPF 缩放到 20×20。不要把源图片像素尺寸写入 `ImageWidth` 和 `ImageHeight`。

实机检查：

- 主体不能贴边；
- 透明留白与宿主图标接近；
- 细线缩放后仍清晰；
- 图标重心和视觉尺寸与相邻工具一致。

## 注册 PNG 图标

`ImageSourceKey` 是 `Application.Resources` 中的资源键，不是文件路径。资源必须先于 Item 创建或显示完成注册。

```csharp
private static void RegisterPngIcon(string imageResourceKey)
{
    if (Application.Current.Resources.Contains(imageResourceKey))
    {
        return;
    }

    var icon = new BitmapImage();
    icon.BeginInit();
    icon.CacheOption = BitmapCacheOption.OnLoad;
    icon.UriSource = new Uri(
        "pack://application:,,,/MyPlugin;component/Assets/Icon.png",
        UriKind.Absolute);
    icon.EndInit();
    icon.Freeze();

    Application.Current.Resources[imageResourceKey] = icon;
}
```

替换程序集名和资源路径，并确保图片的 Build Action 与项目现有资源约定一致。

## 注册几何图标

不要只向图片资源键注册裸 `Geometry`。应使用 `GeometryDrawing` 绘制，再包装成 `DrawingImage`：

```csharp
private static void RegisterGeometryIcon(string imageResourceKey)
{
    if (Application.Current.Resources.Contains(imageResourceKey))
    {
        return;
    }

    var geometry = Geometry.Parse(
        "M3,2 H17 V18 H3 Z M6,6 H14 V8 H6 Z M6,10 H14 V12 H6 Z");

    var icon = new DrawingImage
    {
        Drawing = new GeometryDrawing(
            Brushes.Black,
            null,
            geometry)
    };

    icon.Freeze();
    Application.Current.Resources[imageResourceKey] = icon;
}
```

几何坐标可直接按 20×20 画布设计。多色图标使用 `DrawingGroup` 组合多个 `GeometryDrawing`；可加入 20×20 透明矩形稳定设计边界，避免不同图形边界导致缩放比例变化。

## 注册语言

```csharp
private static void RegisterLanguages()
{
    Lang.Sources.Add(new DictionaryLanguageSource
    {
        [new CultureInfo("zh-CHS")] = new Dictionary<string, string>
        {
            ["Lang.HeadToolBar.MyPlugin"] = "工具名称",
            ["Lang.ToolTip.Insert.MyPlugin.Title"] = "工具名称",
            ["Lang.ToolTip.Insert.MyPlugin.Text"] = "工具说明",
        },
    });
}
```

使用项目和宿主已采用的 Culture 名称。若项目需要多语言，提供对应字典。语言源和 WPF 资源均应在 Dispatcher 中注册。

## 点击命令

入口注册时只建立 UI Item。业务服务可在点击时解析，避免让入口注册时序依赖非必要服务。

命令应：

- 防止用户连续点击产生不允许的并行任务；
- 观察异步异常并使用项目现有日志；
- 需要 UI 时回到 Dispatcher；
- 不复制宿主内部 IPC 或导航链，优先使用公开 API；
- 对耗时工作传递取消令牌。

## 常见错误

### 同时添加 `[UIItem]` 和调用 `Append`

结果：重复 Key、重复入口或不可预测覆盖。

### 用 Ready 加固定延迟等待 IUIItemManager

结果：重复且不准确地表达前置条件。

处理：直接等待 `GetAsync<IUIItemManager>()`。

### Item、图片和语言使用无关联命名

结果：宿主无法按业务后缀找到文本、Tooltip 或图片。

### 按源图片尺寸设置 Item 尺寸

结果：工具栏布局和相邻图标不一致。

处理：展示尺寸保持 20×20，单独优化源图清晰度。

### 引用宿主私有图片 Key

结果：不同版本可能缺少资源，也可能与宿主资源冲突。

处理：注册插件自己的唯一资源 Key。

### 随意修改已发布 Item Key

结果：用户持久化配置失效，入口被识别为新工具。

## 验证清单

- [ ] 入口仅在目标 Shell/Edit 场景注册。
- [ ] `[UIItem]` 与 `Append` 只使用一种。
- [ ] Item Key 稳定且唯一。
- [ ] Item、图片、文本和 Tooltip 共享同一业务后缀。
- [ ] 图片资源先于 Item 注册。
- [ ] 展示尺寸为 20×20，缩放效果已实机检查。
- [ ] `Predicate` 与目标默认位置一致。
- [ ] 用户已有工具栏配置场景已验证。
- [ ] 点击命令不会并行重入或泄漏异常。
- [ ] 目标 EasiNote 版本中的语言键和分流行为已验证。
