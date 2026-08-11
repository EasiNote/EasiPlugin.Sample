# 生命周期、进程形态与容器就绪

在判断 Cloud/Shell、等待宿主服务、处理 `EN.App.Ready`、Dispatcher、自动扫描或重复初始化时读取本文。

本文将公开 API 的使用原则与特定宿主版本观察到的行为分开描述。内部注册时序可能随 EasiNote 版本变化，最终以目标版本代码、运行行为和编译结果为准。

## Cloud 与 Shell 是进程形态

同一个原生插件程序集可能在不同进程形态中加载：

- Cloud：云课件列表和 Cloud 页面；
- Shell：备课或授课界面。

使用以下属性分流：

```csharp
EN.CommandOptions.IsCloud
```

入口应明确划分职责：

```csharp
protected override Task OnRunningAsync()
{
    if (EN.CommandOptions.IsCloud)
    {
        _ = StartCloudAsync();
    }
    else
    {
        _ = StartShellAsync();
    }

    return Task.CompletedTask;
}
```

上例只展示入口形态。若启动任务可能失败，调用方必须建立日志、错误提示或其他异常观察路径，不能无声丢弃任务异常。

不要在 Cloud 分支访问只属于 Shell 的画板、工具栏或窗口服务，也不要在 Shell 分支直接操作 Cloud 页面 ViewModel。

## Shell 不等于备课模式

`IsCloud == false` 只说明当前是 Shell。Shell 同时可能处于：

```csharp
EN.App.CurrentMode == AppMode.Edit
EN.App.CurrentMode == AppMode.Display
```

只有业务确实需要区分备课和授课，并且依赖服务或入口本身不能自然限定运行范围时，才额外检查模式。

例如，备课画板右键菜单本身已经由对应的 UI purpose 和服务限定范围时，不要无依据叠加模式判断。多余的生命周期条件会增加版本耦合，并可能阻止本来可用的功能初始化。

## 使用真实依赖表达就绪条件

插件开始运行时，目标宿主服务不一定已经注册。同步调用：

```csharp
Container.Current.Get<TService>()
```

可能过早，也可能把解析工作放到不合适的线程。

如果后续工作的真实前置条件就是某个服务存在，优先异步等待该服务：

```csharp
var service = await Container.Current
    .GetAsync<TService>()
    .ConfigureAwait(false);
```

`GetAsync<TService>` 表达的是“等待目标服务可获取”。在没有其他证据时，不要再机械叠加：

- 固定延迟；
- `EN.App.Ready`；
- 无限轮询；
- 自定义初始化状态机；
- `Interlocked` 防重入。

每增加一种机制，都意味着设计声称存在一种额外的生命周期或并发约束。必须先从调用方、事件语义或容器注册链确认该约束真实存在。

## 什么时候使用 EN.App.Ready

只有功能的真实前置条件是“整个应用进入 Ready 状态”，而不是某个可以单独等待的服务时，才使用 `EN.App.Ready`。

使用 Ready 时应确认目标宿主版本中的事件语义，包括：

- 事件是否可能在插件订阅前已经触发；
- 是否可能触发多次；
- 取消订阅时机；
- 初始化失败后是否允许重试；
- Ready 后目标服务是否真的同步可用。

若需要兼容“已经 Ready”和“稍后 Ready”两条路径，应先订阅事件，再检查当前状态，使两条路径进入同一初始化方法。只有事件或调用方确实可能重复进入时，才增加一次性门。

不要把 Ready 模板复制到所有插件。对只依赖 `IUIItemManager` 的注册任务，直接等待 `GetAsync<IUIItemManager>()` 通常更准确。

## 容器等待与 UI 线程是两个问题

`GetAsync<T>` 解决服务何时可用，不保证 continuation 位于 WPF UI 线程。WPF 资源、控件、窗口、`Lang.Sources` 和部分宿主 UI API 仍应通过 Dispatcher 操作。

推荐顺序：

```csharp
var manager = await Container.Current
    .GetAsync<IUIItemManager>()
    .ConfigureAwait(false);

await Application.Current.Dispatcher.InvokeAsync(() =>
{
    RegisterResources();
    RegisterLanguages();
    RegisterUIItem(manager);
});
```

顺序含义：

1. 在异步方法中等待宿主服务；
2. 服务可用后切换到 UI Dispatcher；
3. 在 UI 线程创建 WPF 对象并注册 UI 扩展。

不要为了“确保时机”将同步 `Get<T>` 移入 Dispatcher。这会把潜在等待、解析和异常放到 UI 线程，增加界面卡顿或死锁风险。

## 不要虚构并发和重入

在插件入口中增加以下代码前，必须确认底层确实可能并发或重复调用：

```csharp
private int _initializationState;
Interlocked.CompareExchange(...);
```

如果宿主实际只调用一次，虚假的防御会：

- 误导维护者理解生命周期；
- 增加失败恢复和状态转换分支；
- 掩盖真正的服务依赖；
- 让简单初始化变成不必要的并发状态机。

如果真实调用方可能重复触发，应根据业务语义选择去重位置。优先让注册方法幂等，例如先检查资源 Key，或由稳定的 UI Item Key 阻止重复；只有需要协调并发执行时才使用原子状态。

## 自动扫描不是进程隔离

SDK 可能扫描插件程序集中的 `UIItemAttribute`：

```csharp
[UIItem(UIItemPurposes.HeadToolBar)]
internal sealed class MyItem : HeadToolBarItem
{
}
```

程序集可能在 Cloud 和 Shell 都被加载，因此自动扫描不能替代进程分流。Shell 专属 UI Item 若依赖 Shell 服务、资源或初始化时序，推荐在 Shell 分支显式注册：

```csharp
var manager = await Container.Current
    .GetAsync<IUIItemManager>()
    .ConfigureAwait(false);

await Application.Current.Dispatcher.InvokeAsync(() =>
{
    manager.Append(
        _ => new MyItem(),
        new UIItemAttribute(UIItemPurposes.HeadToolBar));
});
```

不要同时使用特性扫描和 `Append`，否则同一 Key 可能注册两次。

## 异常观察

`OnRunningAsync` 的具体签名和宿主调用语义可能限制直接返回长期初始化任务。若入口启动后台异步任务，应使用项目现有日志和错误处理方式观察异常。

不要使用空 `catch`。如果初始化失败会导致功能不可用，应记录可诊断上下文；需要提示用户时，在 UI 线程使用宿主已有的提示机制。不要因为插件错误直接终止宿主进程。

## 常见错误

### 用固定延迟表示服务就绪

问题：机器性能和宿主版本变化后延迟不可靠，还会无谓拖慢快速路径。

处理：等待真实服务、事件或可验证状态。

### 在 Dispatcher 中同步解析容器服务

问题：把服务解析和潜在等待放到 UI 线程。

处理：先 `GetAsync<T>()`，再切换 Dispatcher。

### Ready、GetAsync 和状态机全部叠加

问题：重复表达同一前置条件，增加竞态和失败恢复分支。

处理：选择最接近真实依赖的一种机制；只有存在独立约束时才组合。

### 用 UIItemAttribute 隔离 Cloud 与 Shell

问题：程序集扫描发生在哪些进程中取决于 SDK 和宿主加载链，特性本身不表达进程范围。

处理：在入口进行职责分流，并在正确端动态注册。

## 检查清单

- [ ] 功能属于 Cloud、Shell，还是两端各有职责？
- [ ] Shell 功能是否真的需要区分 Edit 与 Display？
- [ ] 真正的就绪条件是哪一个服务、事件或状态？
- [ ] 是否可直接用 `GetAsync<T>` 表达？
- [ ] 是否无依据增加了 Ready、固定延迟、轮询或状态机？
- [ ] WPF 对象和资源是否在 Dispatcher 中操作？
- [ ] 是否有证据证明存在并发、重复调用或重入？
- [ ] UI Item 是否只采用一种注册方式？
- [ ] 异步初始化异常是否可被观察和诊断？
