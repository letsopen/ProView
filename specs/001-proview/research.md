# 研究记录：ProView（Phase 0）

**特性**：`001-proview`  
**关联规格**：[spec.md](./spec.md)  
**目的**：落实 `/speckit.plan` Phase 0，消除实现层面的未定项并形成可执行决议。

---

## R1 — 图像解码栈（WIC / 禁止第三方库）

**决议**：使用 UWP 提供的 **`Windows.Graphics.Imaging.BitmapDecoder`**（及 **`SoftwareBitmap`**）从 **`IRandomAccessStream`** 解码；像素源与显示之间不引入第三方解码或重采样库。

**理由**：该 API 为 WIC 的 WinRT 封装，满足 FR-001；与「零依赖架构」宪章一致。

**备选方案**：

| 备选 | 未采纳原因 |
|------|------------|
| 直接 P/Invoke WIC COM | 复杂度高，UWP 沙箱与审批成本大；WinRT 已足够 |
| SkiaSharp / ImageSharp 等 | 明确违反宪章与 FR-001 |
| 自写 JPEG/PNG 解析 | 超出范围且不安全 |

---

## R2 — 大图异步与「秒开」

**决议**：在 **非 UI 线程**（`Task.Run` 或异步解码 API）完成从流到 `SoftwareBitmap` 的解码；解码完成后在 UI 线程置换 `Image`/`SwapChainPanel`/`CanvasControl` 的图源。首屏可先显示占位或低分辨率策略（若未来需要再扩展，当前规格未强制）。

**理由**：满足 FR-002 与 SC-001，避免同步阻塞 UI 线程导致卡顿。

**备选方案**：

| 备选 | 未采纳原因 |
|------|------------|
| 同步 `GetPixelDataAsync` 在主线程 | 大图会卡死 UI |
| 写磁盘缩略图缓存 | 违反无临时文件/无索引精神 |

---

## R3 — 同目录文件列表与排序

**决议**：使用 **`StorageFile.GetParentAsync()`** 获得目录，用 **`StorageFolder`** API 枚举文件；过滤为图片扩展名集合（实现时固定列表，如 `.jpg`、`.png`、`.bmp`、`.gif`、`.webp` 等，以 WIC 可解码为准）；按 **文件名** 排序（`NaturalSort` 可选，规格未强制）。

**理由**：满足 FR-003、FR-010；宽泛文件访问需在清单中声明（见 contracts）。

**备选方案**：

| 备选 | 未采纳原因 |
|------|------------|
| Win32 `FindFirstFile` P/Invoke | UWP 下非首选；Storage API 与权限模型一致 |

---

## R4 — 滚轮缩放与指针锚点

**决议**：在单页内使用 **`ScrollViewer` + `CompositeTransform`** 或 **`Image` 外包裹可变换容器**；滚轮 delta 映射为缩放因子；锚点通过指针在控件内坐标与当前变换矩阵反推图像空间，更新 `CenterX`/`CenterY` 或等价平移使锚点固定。

**理由**：覆盖 FR-004、FR-005；纯 XAML/Composition 可实现平滑缩放，无需游戏引擎。

**备选方案**：

| 备选 | 未采纳原因 |
|------|------------|
| Win2D（Microsoft.Graphics.Canvas） | 若仅用于 GPU 绘制且仍用 WIC 解码，可接受；但若引入额外图像管线需审查是否违反「零依赖」精神——默认优先内置 XAML 变换 |
| 离散缩放档位 | 与 FR-005 冲突 |

---

## R5 — 应用激活与单实例

**决议**：在 **`OnFileActivated`** / **`OnLaunched`** 中解析 **`IActivatedEventArgs`**，读取 **`StorageFile`** 路径并交给 ViewModel；若规格后续要求「始终单实例」，可评估 `AppInstance` API（实现阶段再定，**非阻塞**）。

**理由**：与假设中的「打开方式 / 关联」一致。

**NEEDS CLARIFICATION 状态**：无；单实例策略留作实现任务可选项。

---

## R6 — 包体积 ≤5MB

**决议**：使用 Release/.NET Native 配置；限制本地化资源数量；不嵌入大型素材；依赖项为零或极少非图像 NuGet。

**理由**：满足 NFR-001。

---

## R7 — 测试策略

**决议**：核心枚举与索引循环逻辑优先 **可测试纯类**（.NET Standard 或 UWP 类库）+ 单元测试；解码与文件 IO 以集成/手动为主。

**理由**：UWP 测试栈成熟度有限，与规格 SC 条目匹配。
