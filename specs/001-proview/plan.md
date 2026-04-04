# 实现计划：ProView 专业极简图片查看器

**分支**：`001-proview` | **日期**：2026-04-04 | **规格**：[spec.md](./spec.md)  
**输入**：特性规格说明 [`spec.md`](./spec.md)

**说明**：本文件由 `/speckit.plan` 工作流生成；Phase 0/1 产出见同目录 `research.md`、`data-model.md`、`quickstart.md` 与 `contracts/`。

## 摘要

交付一款 **Windows UWP** 单机图片查看器：仅用 **WIC/系统编解码器** 解码，**异步**加载以保障大图冷启动；**方向键**在同目录内循环切换；**滚轮**以指针为锚点平滑缩放；**无**相册/编辑/设置、无临时文件与索引。实现上采用 **C# / XAML**，目标 **.NET Native**，包形态 **MSIX/APPX**，并声明 **宽泛文件访问** 以枚举打开文件所在目录。

详细技术决议见 [research.md](./research.md)。

## Technical Context

**Language/Version**: C#（UWP；目标 .NET Native；最低 SDK 对应 Windows 10 19041+）  
**Primary Dependencies**: 仅平台与 WinRT/UWP API（`Windows.Graphics.Imaging`、`Windows.Storage`、可选 `Microsoft.Graphics.Canvas` 仅当不引入非 WIC 解码链时评估；禁止第三方图像编解码库）  
**Storage**: N/A（无应用侧媒体数据库；仅内存中持有当前解码位图与目录列表）  
**Testing**: MSTest 或 xUnit（UWP 测试项目）；核心逻辑可抽离为可测试类库时辅以单元测试；手动验收规格中的 SC 条目  
**Target Platform**: Windows 10（内部版本 19041+）、Windows 11（x64/ARM64 以 VS 发布配置为准）  
**Project Type**: desktop-app（UWP 应用商店/侧载）  
**Performance Goals**: 冷启动至首帧主观「秒开」；静态显示时空载 CPU 0%；4K 场景内存接近系统解码底线  
**Constraints**: MSIX/APPX ≤5MB；Visual Studio 2019；禁止第三方图像库；清单能力含 broadFileSystemAccess 或等效策略以支持同级目录访问  
**Scale/Scope**: 单窗口极简 UI；单一代码库；无后端

## Constitution Check

*GATE：Phase 0 研究前必须通过；Phase 1 设计后须再核对。*

| 宪章项 | 计划中的对应与结论 |
|--------|-------------------|
| 秒开 + UWP 异步加载 | 采用 `StorageFile` + `BitmapDecoder`/`SoftwareBitmap` 异步路径；UI 线程不阻塞解码（见 research.md） |
| 仅 WIC、无第三方图像库 | 解码统一走 `Windows.Graphics.Imaging`（WIC 封装）；不引用 Skia、ImageSharp 等 |
| 方向键导航 + 首尾循环 | ViewModel 维护有序文件列表与当前索引；边界取模或显式分支 |
| 滚轮锚点 + 平滑缩放 | `CompositeTransform` / `Matrix3x2` 或 Canvas 缩放，以指针映射到图像坐标为锚点；动画或连续插值满足 FR-005 |
| 无临时文件、无索引 | 不写入缓存目录；目录列表仅内存；退出释放 `SoftwareBitmap` 引用 |
| 非目标：管理/编辑/设置 | 不实现相册、编辑、设置页、复杂右键 |
| 包 ≤5MB、0% 空载 CPU、19041+ | 发布配置裁剪语言包与符号；空闲不挂计时器；MinVersion 锁定 19041 |
| VS2019 + C# + XAML + .NET Native | 项目模板与生成配置按宪章固定 |
| 宽泛文件访问 | Package.appxmanifest 声明 broadFileSystemAccess，并文档化隐私说明（contracts） |

**闸门结论**：无违宪项；无需填写「Complexity Tracking」表。

**Phase 1 后复检**：结构与依赖仍满足上表；若引入任何 NuGet，须逐项证明非图像解码依赖。

## Project Structure

### 文档（本特性）

```text
specs/001-proview/
├── plan.md              # 本文件（/speckit.plan）
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/           # Phase 1（集成与清单约定）
└── tasks.md             # Phase 2（/speckit.tasks，非本命令生成）
```

### 源代码（仓库根目录，建议）

仓库当前无工程文件；实现阶段建议采用 **单解决方案 + 单 UWP 应用项目**（后续可按需增加可测试类库）。

```text
src/
└── ProView/
    ├── ProView.csproj
    ├── Package.appxmanifest
    ├── App.xaml
    ├── App.xaml.cs
    ├── Views/
    │   └── MainPage.xaml
    ├── ViewModels/
    │   └── MainViewModel.cs（或 Shell）
    └── Services/
        ├── ImageDecodeService.cs    # WIC/BitmapDecoder 封装
        └── FolderEnumerationService.cs  # 目录内图片列表与排序

tests/
└── ProView.Tests/                 # 可选；UWP 单元测试项目
```

**结构决策**：单一 UWP 应用承载 UI 与业务；解码与目录枚举放入 `Services/` 便于测试与职责分离；不引入前后端拆分或多项目，除非包体积或测试需求迫使抽离类库。

## Complexity Tracking

> 无宪章违规需辩解；本表留空。

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## 生成的工件（Phase 0 / Phase 1）

| 工件 | 路径 | 说明 |
|------|------|------|
| 研究决议 | [research.md](./research.md) | 技术选型、备选方案与风险 |
| 数据模型 | [data-model.md](./data-model.md) | 会话状态与目录枚举概念 |
| 快速开始 | [quickstart.md](./quickstart.md) | VS2019 创建工程与本地运行 |
| 约定 | [contracts/](./contracts/) | 激活、清单与文件关联约定 |

## 后续步骤

- 运行 `/speckit.tasks`：将本计划拆为可执行 `tasks.md`。
- 实现时优先打通：激活 → 单图解码显示 → 目录枚举与方向键 → 滚轮缩放与锚点。
