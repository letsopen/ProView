---
description: "ProView 特性实现任务列表（按用户故事组织）"
---

# 任务列表：ProView 专业极简图片查看器

**输入**：设计文档目录 `specs/001-proview/`  
**前置**：[`plan.md`](./plan.md)、[`spec.md`](./spec.md)；可选 [`research.md`](./research.md)、[`data-model.md`](./data-model.md)、[`contracts/`](./contracts/)、[`quickstart.md`](./quickstart.md)

**测试**：规格未要求 TDD；本列表**不包含**单独测试任务，验收以规格 SC 与 [`quickstart.md`](./quickstart.md) 冒烟为准。

**组织方式**：按用户故事分阶段，便于独立实现与独立演示。

## 格式说明：`[ID] [P?] [Story?] 描述（含文件路径）`

- **[P]**：可与同阶段其他 [P] 任务并行（不同文件、无未完成依赖）。
- **[Story]**：仅用户故事阶段使用 `[US1]`、`[US2]`、`[US3]`。
- 路径以仓库根为基准，与 [`plan.md`](./plan.md) 中 `src/ProView/` 结构一致。

---

## Phase 1：Setup（共享基础设施）

**目的**：初始化 UWP 工程与清单骨架。

- [x] T001 使用 Visual Studio 2019 在 `src/ProView/` 创建「空白应用(通用 Windows)」，生成 `src/ProView/ProView.csproj`、`src/ProView/App.xaml`、`src/ProView/App.xaml.cs`、`src/ProView/Package.appxmanifest`，并在仓库根目录创建/更新解决方案文件以包含该项目（如 `ProView.sln` 引用 `src/ProView/ProView.csproj`）。
- [x] T002 编辑 `src/ProView/Package.appxmanifest`：将最低版本对齐 Windows **19041+**；声明图片文件类型关联；按需配置 `broadFileSystemAccess` 等与 [`contracts/app-integration.md`](./contracts/app-integration.md) 一致的权限说明占位。

---

## Phase 2：Foundational（阻塞性基础）

**目的**：完成激活链路、解码与目录枚举、主界面骨架；**未完成前不得进入用户故事实现**。

**⚠️ 关键**：本阶段结束后，才具备实现各用户故事的公共能力。

- [x] T003 在 `src/ProView/App.xaml.cs` 中实现 `OnFileActivated` / `OnLaunched`，解析首个 `StorageFile` 并导航至 `src/ProView/Views/MainPage.xaml`（无文件时的行为与规格边界一致）。
- [x] T004 [P] 新建 `src/ProView/Services/FolderEnumerationService.cs`，实现当前文件所在目录的图片文件枚举、扩展名过滤与文件名排序，供后续索引导航使用。
- [x] T005 [P] 新建 `src/ProView/Services/ImageDecodeService.cs`，仅使用 `Windows.Graphics.Imaging`（WIC）异步解码为 `SoftwareBitmap`，满足 FR-001/FR-002，禁止第三方图像库。
- [x] T006 新建 `src/ProView/ViewModels/MainViewModel.cs`，持有会话状态（文件列表、当前索引、解码结果句柄），组合调用 `FolderEnumerationService` 与 `ImageDecodeService`。
- [x] T007 新建 `src/ProView/Views/MainPage.xaml` 与 `src/ProView/Views/MainPage.xaml.cs`：极简布局（如 `Image` 或等效呈现控件）、设置 `DataContext`、保证键盘焦点可用于方向键（无相册/侧栏/设置 UI）。

**检查点**：自「打开方式」启动后能进入主页面并具备绑定 ViewModel 的入口（即使尚未完成 US1 全部交互）。

---

## Phase 3：User Story 1 — 打开并浏览同目录图片（优先级：P1）🎯 MVP

**目标**：打开图片、同目录方向键切换、首尾循环；界面仅查看器 chrome。

**独立测试**：含多张图片的文件夹中，通过方向键切换并验证末张→首张、首张→末张循环；无相册/库 UI。

### User Story 1 实现任务

- [x] T008 [US1] 在 `src/ProView/ViewModels/MainViewModel.cs` 中实现从激活文件加载首张图：调用 `ImageDecodeService` 异步解码、在 UI 线程更新位图属性；解码失败时按规格失败安全（提示或占位，不崩溃）。
- [x] T009 [US1] 在 `src/ProView/Views/MainPage.xaml.cs`（或等效输入层）处理 `KeyDown`，映射左右方向键至 ViewModel 的上一张/下一张命令，并确保焦点策略与规格一致。
- [x] T010 [US1] 在 `src/ProView/ViewModels/MainViewModel.cs` 中实现索引增减与**首尾循环**逻辑，切换时重新解码新文件并释放上一帧 `SoftwareBitmap` 引用（与 data-model 中 `ImageSession` 一致）。

**检查点**：User Story 1 可独立演示为 MVP（打开 + 循环导航）。

---

## Phase 4：User Story 2 — 以光标为锚点的滚轮缩放（优先级：P2）

**目标**：滚轮缩放、指针为锚点、平滑连续缩放（非唯一离散档位）。

**独立测试**：固定单张图，验证滚轮放大/缩小与指针下像素相对静止（锚点）；动画或插值满足「平滑」主观标准。

### User Story 2 实现任务

- [x] T011 [US2] 在 `src/ProView/Views/MainPage.xaml` 中为图像视图配置变换容器（如 `ScrollViewer` + `CompositeTransform` 或等效），并处理 `PointerWheelChanged`（或兼容的指针滚轮事件），将增量传递给视图模型或代码隐藏。
- [x] T012 [US2] 在 `src/ProView/ViewModels/MainViewModel.cs` 或 `src/ProView/Views/MainPage.xaml.cs` 中实现锚点缩放数学（指针坐标 → 图像空间）、连续缩放因子更新与 FR-005 要求的平滑表现（避免仅离散档位）。

**检查点**：User Story 2 在 US1 已通基础上可单独做交互回归（可先禁用导航仅测缩放）。

---

## Phase 5：User Story 3 — 无状态与资源释放（优先级：P3）

**目标**：无临时媒体文件、无索引库；空闲 CPU 0%；退出释放资源。

**独立测试**：监视应用数据/临时目录无新增浏览缓存；任务管理器静态显示时 CPU≈0%；多次开关会话无未释放句柄（在合理范围内）。

### User Story 3 实现任务

- [x] T013 [US3] 在 `src/ProView/ViewModels/MainViewModel.cs` 中统一 `SoftwareBitmap` 释放路径（导航、重置、卸载），避免重复持有大图帧。
- [x] T014 [US3] 在 `src/ProView/App.xaml.cs` 的挂起/退出逻辑中释放会话与视图模型资源；复核 `Services/` 与全应用无写入临时图片或建立索引数据库（满足 FR-009）。

**检查点**：与规格 User Story 3 验收场景对齐。

---

## Phase 6：Polish 与横切事项

**目的**：发布配置、包体积、规格 SC 冒烟。

- [x] T015 [P] 在 `src/ProView/ProView.csproj` 与生成配置中启用 **Release** / **.NET Native**，产出 MSIX/APPX 并验证体积 **≤5MB**（NFR-001/SC-002），必要时裁剪语言资源。
- [x] T016 [P] 按 [`specs/001-proview/quickstart.md`](./quickstart.md) 与 [`specs/001-proview/spec.md`](./spec.md) 中 SC-001～SC-005 执行手动冒烟，记录结果（含 19041+ 与 Windows 11 若可测）。

---

## 依赖关系与执行顺序

### 阶段依赖

| 阶段 | 依赖 |
|------|------|
| Phase 1 Setup | 无 |
| Phase 2 Foundational | Phase 1 完成 |
| Phase 3～5 User Stories | Phase 2 完成 |
| Phase 6 Polish | Phase 3～5 目标功能均达到可发布状态 |

### 用户故事依赖

| 故事 | 依赖 |
|------|------|
| US1（P1） | 仅依赖 Phase 2 |
| US2（P2） | 依赖 Phase 2；与 US1 集成但缩放可单独测试单图 |
| US3（P3） | 依赖 Phase 2；与 US1/US2 叠加，侧重释放与无磁盘副作用 |

### 用户故事完成顺序建议

`US1 → US2 → US3`（优先级顺序）；US2/US3 在人力充足时可在 Phase 2 后与 US1 部分并行，但以不破坏 MVP 验收为准。

---

## 并行执行示例

**Phase 2（在 T003 完成后）**

```text
并行：T004「src/ProView/Services/FolderEnumerationService.cs」
并行：T005「src/ProView/Services/ImageDecodeService.cs」
随后串行：T006 → T007
```

**Phase 6**

```text
并行：T015（Release 包体积验证）
并行：T016（quickstart + SC 冒烟文档）
```

---

## 实现策略

### MVP 优先（仅 User Story 1）

1. 完成 Phase 1 + Phase 2。  
2. 完成 Phase 3（US1）。  
3. **停止并验收**：独立运行 [`spec.md`](./spec.md) 中 User Story 1 的验收场景。  
4. 再进入 US2、US3。

### 增量交付

每完成一个故事，保持可安装包可演示，避免破坏前一故事的可测试性。

---

## 任务统计（生成时快照）

| 项 | 数量 |
|----|------|
| 任务总数 | 16 |
| Phase 1 | 2 |
| Phase 2 | 5 |
| US1 | 3 |
| US2 | 2 |
| US3 | 2 |
| Polish | 2 |
| 含 [P] 可并行 | T004, T005, T015, T016 |

**独立测试摘要**：US1 — 多图文件夹循环导航；US2 — 单图锚点滚轮缩放；US3 — 无缓存文件与资源释放/空载 CPU。

**建议 MVP 范围**：Phase 1 + Phase 2 + Phase 3（User Story 1）。

**格式校验**：本文件全部任务均使用 `- [ ]`、`Txxx` 编号，用户故事任务含 `[USn]`，含 [P] 的条目已标注；描述中含明确文件路径。
