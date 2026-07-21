---
name: easi-plugin-sample
description: 为希沃白板原生插件示例仓库提供开发指导技能，在该仓库执行插件初始化、UI 扩展、命令/多语言、页面与元素操作及打包发布时使用。
---

# EasiPlugin.Sample 开发技能

## 使用时机
当需要在当前仓库（`.NET 6`、`WPF`、`Windows Desktop`）完成或检索希沃白板原生插件实现细节时，使用此技能。

## 关键上下文
- 项目文件：`src/EasiPluginSample/EasiPluginSample.csproj`
- 入口代码：`src/EasiPluginSample/Program.cs`
- 示例文档：`README.md`

## 核心规则（执行顺序）
1. 以 `Program`/`Plugin` 入口启动：在 `OnRunningAsync` 中初始化插件行为。
2. 按场景区分逻辑：通过 `EN.CommandOptions.IsCloud` 判断 `Cloud` 与 `Shell`。
3. 注册 UI 扩展时先从 `Container.Current` 获取 `IUIItemManager`，再执行 `Append`。
4. 所有 UI 相关操作必须在主线程（通常使用 `Dispatcher.InvokeAsync`）。
5. 多语言建议通过 `Lang.Sources.Add(new DictionaryLanguageSource { ... })` 动态注入。
6. 需要更多 EasiNote API 时，依据目标依赖级别在 `csproj` 设置 `UseEasiNote`。

## 常用开发清单
- 初始化插件逻辑：覆盖/实现 `OnRunningAsync`。
- 获取当前页面与所有页面：
  - `EN.EditingBoardApi.CurrentSlide`
  - `EN.EditingBoardApi.Slides`
  - `EN.DisplayingBoardApi.CurrentSlide`
- 访问文本元素：`currentSlide.Elements.OfType<TextElement>()`
- 组合内取文本：先 `OfType<GroupElement>()`，再遍历其 `Elements`。
- 监听渲染完成：使用 `TextEditor.RenderCompleted`。
- 导出 ENBX：先取 `EN.CurrentBoardApi.GetStorageModelAsync()`，再用 `IEnbxStorageProvider.ExportEnb(...)`。
- 注册事件与回调：保持事件签名与 SDK 示例一致，避免跨线程直接更新 UI。

## 常见场景参考
- 工具栏/菜单按钮：定义 `HeadToolBarItem` 或 `Edit*MenuItem` 并设置 `Key`、`Type`、`Predicate`、`Command`。
- 图标与文案：使用 `ImageSourceKey` 与 `ResourceHelper.TryAddResource`。
- 多语言文案：按 `Lang.ToolTip...` / `Lang.HeadToolBar...` 键进行分层命名。
- 安装包形态：按 SDK 机制产出 `exe/zip/enp/enpx`。

## 约定
- 优先复用 `README.md` 示例片段，避免引入超出仓库现有范式的新结构。
- 优先使用 `System.Text.Json` 与 `HttpClient`（如涉及网络与 JSON）。
- 保持空值检测与错误边界，遵循现有项目风格。
- 若新增代码涉及公共 API，请补充 XML 文档注释并保证可空性。