# WPF 视觉接入

在 EasiNote 进程内创建 WPF 窗口、对话框或设置页，并希望复用宿主控件、颜色、样式和设计令牌时读取本文。

宿主资源名称和装载顺序属于版本相关实现。本文列出的键来自已验证版本；目标版本中不存在时，应查找相近宿主窗口和实际资源字典，不要自行猜测替代键。

## 宿主资源环境

已验证的 Shell 版本会向 `Application.Resources` 合并以下核心资源：

1. `/Cvte.GurnetUI.EasiNote;component/Resources.xaml`
2. `/Cvte.EasiUI;component/Shared.xaml`
3. `/Cvte.EasiUI;component/Main.xaml`
4. `/EasiNote.Resources;component/Main.Shell.xaml`

这些资源大致提供：

- `Cvte.GurnetUI.EasiNote`：新版 Gurnet 风格控件、窗口和设计令牌；
- `Cvte.EasiUI/Shared.xaml`：传统 EasiUI 语义颜色；
- `Cvte.EasiUI/Main.xaml`：公共图片、阴影和基础控件样式；
- `EasiNote.Resources/Main.Shell.xaml`：白板业务层工具栏、选择控件和属性面板资源。

插件 UI 在宿主 WPF `Application` 中运行时，通常可以通过 `{StaticResource ...}` 或 `{DynamicResource ...}` 使用已装载资源，不需要复制宿主资源字典。

插件独立启动、单元测试或设计器中不一定存在这些资源。实现前明确窗口是否只面向宿主运行环境。若窗口需要独立预览，应提供独立设计时策略，而不是把宿主完整资源复制进插件。

## 控件与窗口体系

### SafeWindow

常见宿主窗口基类：

```text
Cvte.Windows.Controls.SafeWindow
```

插件窗口沿用 `SafeWindow`，通常可与宿主窗口生命周期和异常保护方式保持一致。使用前检查项目现有引用和同类宿主窗口，不要仅凭类型名推断所有行为。

### 传统 EasiUI

常见资源和附加属性：

- `Style.ShadowDialog`：带宿主阴影和标题区域的对话框样式；
- `Style.AccentButton`：主题色主按钮；
- `Style.DefaultComboBox`：标准下拉框；
- `Style.DefaultTextBox`：标准文本框；
- `Cvte.EasiUI.Helper.ControlsHelper`：圆角、状态颜色和图标等附加属性。

小型设置窗口可优先参考宿主中的 `SafeWindow + Style.ShadowDialog` 用法。

### GurnetUI

较新的界面可能使用：

- `GurnetContentDialog`；
- `Style.GurnetContentDialog`；
- `GurnetWindowHelper.IsModalWindow`；
- `GurnetWindowHelper.IsHeightAdaptive`。

新旧两套资源可能并存。内容对话框可优先查找宿主中相近的 `GurnetContentDialog`；已有传统窗口不要仅为“更新”而混入另一套视觉体系。

## 先找同类宿主界面

设计插件窗口前，优先查找一个尺寸、交互和用途接近的宿主窗口，确认：

- 使用的窗口基类；
- 标题区和关闭行为；
- 控件样式键；
- 间距、字号和按钮尺寸；
- 模态或非模态行为；
- Owner、置顶和多屏策略；
- DPI 与缩放表现。

不要根据单个资源名称推断整套视觉规范。

## 语义颜色

传统 EasiUI 已验证版本中的常见语义资源包括：

- `Brush.Accent.Normal`：默认强调色；
- `Brush.Accent.Light`：悬停强调色；
- `Brush.Accent.Dark`：按下强调色；
- `Brush.Text.Accent.Normal`：强调文本；
- `Brush.BorderBrush.Accent.Normal`：强调边框；
- `Brush.Text.Dark`：标题和主要正文；
- `Brush.Text.Normal`：普通说明文字；
- `Brush.Text.Faint`：次要提示；
- `Brush.BorderBrush.Lighter`：浅边框；
- `Brush.Background.Lightest`：浅表面；
- `Brush.Background.Deep05`：弱分隔线。

新版 Gurnet 资源可能提供：

- `brush-brand-normal`；
- `brush-text-gray-primary`；
- `brush-text-gray-secondary`；
- `brush-fill-secondary`；
- `brush-fill-tertiary`；
- `brush-border-secondary`；
- `border-radius-medium`。

同一个局部界面尽量统一使用一套命名体系：

- 控件样式来自 EasiUI 时，优先配合 `Brush.*`；
- 使用 Gurnet 控件时，优先配合 Gurnet 设计令牌。

不要大量硬编码绿色、灰色、边框和圆角值。语义资源可以跟随宿主主题或定制版本变化。

## 典型设置窗口层级

小型设置对话框可参考以下范围，最终以同类宿主窗口为准：

1. 标题区约 30px 高，标题字号约 12px；
2. 内容区左右留白约 20–24px；
3. 主标题约 16–18px，字段标签约 12–14px；
4. 输入控件常见高度约 28–32px，字段间距约 16px；
5. 底部操作区约 48–56px，顶部使用浅分隔线；
6. 主操作按钮常见宽度约 80–96px，并使用宿主强调按钮样式。

避免：

- 每个字段标签都使用大字号；
- 控件紧贴窗口边缘；
- 主按钮无依据拉伸到整个窗口宽度；
- 依赖系统默认 Button、ComboBox、CheckBox 外观；
- 同时混用大量硬编码颜色和两套设计令牌。

## StaticResource 与 DynamicResource

选择原则：

- 资源在窗口加载时必定存在，且运行期不需要替换：使用 `StaticResource`；
- 主题可能运行时替换，或资源需要支持后加载覆盖：使用 `DynamicResource`；
- 宿主已有样式混合使用两者时，参考目标资源本身和附近窗口的实际写法。

不要为了“更灵活”将所有引用都改成 `DynamicResource`，也不要在资源装载顺序不确定时盲目使用 `StaticResource`。

## 只依赖公共资源

插件通用窗口应依赖公共资源，如 EasiUI 和 GurnetUI 的基础样式。不要引用某个业务页面私有的图片、模板或局部 `ResourceDictionary`，否则会产生：

- 不必要的业务程序集耦合；
- 不同宿主版本中的资源缺失；
- 页面资源未加载时的解析失败；
- 插件独立维护困难。

必须使用业务资源时，应确认该资源属于目标功能的稳定公开契约；否则将需要的视觉资产放入插件自己的资源，并使用唯一 Key。

## UI 线程与资源注册

创建窗口、控件、`BitmapImage`、`DrawingImage`、ResourceDictionary 或修改 `Application.Resources` 时，遵循项目和 WPF 的线程要求。

通常先在后台等待宿主服务，再在 Dispatcher 中创建和注册 WPF 对象：

```csharp
await Application.Current.Dispatcher.InvokeAsync(() =>
{
    RegisterPluginResources();
    ShowWindow();
});
```

不要把容器服务等待放进 Dispatcher。生命周期细节见 [lifecycle-and-container.md](lifecycle-and-container.md)。

可冻结且注册后不再修改的 `Freezable` 资源应调用 `Freeze()`，减少线程亲和和运行期开销。

## DPI 与清晰度

窗口或控件可使用：

```xml
SnapsToDevicePixels="True"
UseLayoutRounding="True"
```

这有助于减少非整数像素边界导致的模糊，但不能替代实际 DPI 测试。至少检查：

- 100%、125%、150% 缩放；
- 多显示器间移动；
- 文本裁切和按钮最小尺寸；
- 图标缩放和线条清晰度；
- 阴影、圆角与窗口边缘。

## 常见错误

### 复制整个宿主资源字典

问题：引入大量内部资源和版本耦合，还可能覆盖宿主键。

处理：直接使用已装载公共资源，插件自有资源使用唯一 Key。

### 硬编码宿主绿色

问题：悬停、按下、禁用和定制主题无法自动适配。

处理：使用语义 Brush 或现有按钮样式。

### 混用 EasiUI 与 Gurnet 令牌

问题：局部视觉层级和交互状态不一致。

处理：根据目标控件体系选择一套主体系。

### 假设设计器拥有宿主资源

问题：设计器或独立运行时报找不到资源。

处理：明确窗口只在宿主运行，或提供隔离的设计时资源策略。

### 引用业务页面私有样式

问题：资源装载顺序和程序集依赖不稳定。

处理：选择公共样式或插件自有资源。

## 实施检查清单

- [ ] 已确认窗口只在宿主运行，还是需要独立设计时支持。
- [ ] 已找到一个同类宿主窗口作为实现基准。
- [ ] 使用合适的 `SafeWindow`、EasiUI 或 Gurnet 控件体系。
- [ ] 主要颜色、文本和边框使用语义资源。
- [ ] 没有依赖业务页面私有资源。
- [ ] `StaticResource` 与 `DynamicResource` 的选择符合装载和主题需求。
- [ ] WPF 对象和资源在正确线程创建。
- [ ] 可冻结资源已冻结。
- [ ] 已构建，并在目标宿主版本验证资源解析。
- [ ] 已检查常用 DPI、多屏、阴影和图标清晰度。
