---
name: ista-patch-compat-check
description: Check patch compatibility against a new ISTA version. Use when the user wants to verify if existing patches still work, identify broken patches, or generate a compatibility report for a new ISTA release.
license: GPL-3.0-or-later
metadata:
  author: TautCony
  version: "1.1"
---

# ista-patch-compat-check

## 目标
核查 ISTA-Patcher 项目中每一个 patch 对 `ref/` 目录下新版 ISTA C# 源码的适用性。
对每个 patch 判断：目标类型/方法是否仍然存在、签名描述是否仍可匹配、方法体逻辑是否仍与 patch 的修改假设相符。
若 patch 失效，输出具体失效原因、源码变更内容对比、修复建议和修改计划，并将所有结果记录为结构化 markdown 报告。

---

## 背景知识

### 推荐脚本

优先使用仓库内脚本做首轮初筛，而不是从零开始拼临时命令：

```bash
python3 scripts/check_patch_compat.py
python3 scripts/check_patch_compat.py --ref-version 4.58.12
python3 scripts/check_patch_compat.py --ref-path ref/4.58.12-project
```

脚本会输出两份产物：
- `patch-compat-summary-<version>.json`：结构化中间结果，便于继续筛选和二次分析
- `patch-compat-report-<version>-first-pass.md`：首轮 markdown 报告

> 该脚本是**首轮自动核查**。对 IL 指令级 patch 和复杂重写 patch，仍需人工复核源码逻辑或已编译 IL。

### Patch 文件分布

patch 实现分散在以下文件中，**`Patch.cs` 是编排器而非 patch 实现文件**：

| 文件 | 内容 |
|------|------|
| `src/ISTAlter/Core/PatchUtils.cs` | 核心 license / 安全 / 完整性 patch（~18 个）|
| `src/ISTAlter/Core/PatchUtils.Optional.cs` | 可选功能 patch（~27 个），通过命令行开关启用 |
| `src/ISTAlter/Core/PatchUtils.Toyota.cs` | Toyota 专项 patch（3 个）|
| `src/ISTAlter/Core/CustomPatchUtils.cs` | 从配置文件加载的自定义 patch |
| `src/ISTAlter/Core/PatchUtils.Base.cs` | 通用 helper，不含具体 patch 逻辑 |

### 版本过滤

每个 patch 方法可携带 `[FromVersion("x.xx")]` 和 `[UntilVersion("x.xx")]` attribute。
**在核查前，必须先确认当前 ref 版本号**（路径格式为 `ref/<版本号>-project/`，如 `ref/4.58.12-project/`），然后过滤掉不适用于该版本的 patch，只核查在该版本范围内应被应用的 patch。

### 目标匹配机制（dnlib）

patch 通过以下方式定位目标：
1. **类型全名精确匹配**：`module.GetType("Full.Namespace.TypeName")`
2. **方法签名描述字符串匹配**：格式为 `"(ParamType1,ParamType2)ReturnType"`，由 `DescriptionOf()` 生成
3. **IL 指令级匹配**（部分复杂 patch）：`FindInstruction(opcode, operandName)` 在方法体内定位特定调用指令

> **注意**：ref/ 目录包含的是 **C# 源码**，不是编译后的 IL。核查时需通过阅读 C# 源码推断编译后的方法签名和 IL 结构，而非直接比对 IL。

### 四类失效模式

| 失效类型 | 描述 |
|----------|------|
| **类型缺失/重命名** | 目标类型全名已变更或类型被删除 |
| **方法签名不匹配** | 参数类型、返回类型或方法名变更，导致签名描述字符串无法匹配 |
| **IL 指令模式变化** | 方法体逻辑重构，`FindInstruction` 找不到预期的操作数/调用目标 |
| **方法已删除** | 目标方法在新版中不存在 |

---

## 步骤流程

### 建议执行顺序

1. 先运行 `python3 scripts/check_patch_compat.py` 生成首轮 JSON 和 markdown。
2. 根据脚本输出中 `⚠️ review` 和 `❌ invalid` 的 patch 进入人工复核。
3. 人工复核完成后，再整理为最终报告，状态收敛到 `✅ 有效 / ⚠️ 部分失效 / ❌ 失效`。

### 第一步：确定 ref 版本与适用 patch 集

1. 读取 `ref/` 目录，提取版本号（如 `4.58.12`）。
2. 枚举上述四个 patch 文件中所有 patch 方法。
3. 根据 `[FromVersion]`/`[UntilVersion]` attribute 过滤，得到**当前版本应应用的 patch 列表**。
4. 对每个 patch，记录：
   - patch 方法名
   - 目标程序集（DLL/EXE 名称）
   - 目标类型全名
   - 目标方法名与签名描述
   - patch 操作类型（返回常量 / 清空方法体 / IL 指令替换 / XML 字符串修改等）

### 第二步：在 ref 源码中定位目标

1. 在 `ref/<version>-project/` 下，按目标类型全名（命名空间 + 类名）搜索对应 `.cs` 文件。
2. 确认目标类型是否存在；若不存在，记录"**类型缺失**"。
3. 在目标类型中查找目标方法名；若不存在，记录"**方法缺失**"。
4. 提取方法签名（参数类型、返回类型）与方法体逻辑，供下一步比对。

### 第三步：比对 Patch 有效性

"有效"的判断标准是**目标可被成功定位且 patch 修改假设仍成立**，而非 patch 代码与 ref 代码相同（两者本就不同，patch 的目的是修改 ref 代码）。

针对不同 patch 操作类型，采用对应的有效性判断标准：

| patch 操作类型 | 有效性判断标准 |
|---------------|---------------|
| 返回常量（ReturnTrue/False 等） | 目标类型 + 方法签名描述与源码一致即有效 |
| 清空方法体 | 同上 |
| IL 指令级替换 | 还需确认方法体中预期的调用目标/操作数在新版中仍然存在 |
| XML/字符串注入 | 确认方法体中相关字符串构造逻辑未被重构 |

判断结果为三种：
- ✅ **有效**：目标存在，签名匹配，方法体逻辑符合 patch 假设
- ⚠️ **部分失效**：目标存在但方法体逻辑变化，patch 可能仍能注入但效果不确定
- ❌ **失效**：属于四类失效模式之一

### 第四步：输出变更内容与修复计划

对每个 ⚠️ 或 ❌ 的 patch，输出：

- **变更描述**：新版源码与 patch 假设的具体差异（类型名、方法签名、方法体关键逻辑）
- **失效原因**：对应四类失效模式中的哪一类
- **修复建议**：针对失效类型给出具体修复思路，例如：
  - 类型重命名 → 更新 patch 中的类型全名字符串
  - 方法签名变更 → 更新签名描述字符串
  - IL 指令模式变化 → 重新分析新版方法体，调整 `FindInstruction` 目标
- **修改计划**：列出具体修改步骤，标注是否需要人工介入

### 第五步：生成 Markdown 报告

输出文件路径：`patch-compat-report-<version>.md`（保存在项目根目录或 `openspec/` 下）

报告结构：

```markdown
# Patch 兼容性核查报告

- **ISTA 版本**：x.xx.xx
- **核查时间**：YYYY-MM-DD
- **总计**：N 个 patch，✅ X 有效，⚠️ Y 部分失效，❌ Z 失效

---

## [patch 分类，如 License / Optional / Toyota]

### PatchXxx

| 项目 | 内容 |
|------|------|
| 目标程序集 | xxx.dll |
| 目标类型 | Full.Namespace.TypeName |
| 目标方法 | MethodName(ParamType)ReturnType |
| 状态 | ✅ 有效 / ⚠️ 部分失效 / ❌ 失效 |
| 失效原因 | （如适用）|
| 变更描述 | （如适用）|
| 修复建议 | （如适用）|
| 修改计划 | （如适用）|
```

---

## 分支与决策点

- `[FromVersion]`/`[UntilVersion]` 范围之外的 patch：跳过，不纳入报告（或单独列出"跳过"分区）
- 目标文件/类型不存在：记录"目标缺失"，建议人工确认是否为类型重命名
- IL 指令级 patch 且无法从 C# 源码推断新版 IL 结构：在报告中标注"需借助 ILSpy/dnSpy 对已编译程序集进行验证"
- `CustomPatchUtils` 中的自定义 patch：从配置文件读取目标，同样按以上流程核查

## 补充注意事项

- **签名归一化不能偷懒**：至少要统一处理 primitive alias、`Nullable<T>` 与 `T?`、构造函数 `.ctor`、`get_` / `set_` 映射到属性、以及带命名空间和不带命名空间的类型写法。否则会把 `BrandName?` 和 `System.Nullable` 这类等价签名误判为不匹配。
- **先按 target 粒度判断，再回收成 patch 粒度**：像 `PatchConfigSettings` 这种 patch 方法会命中多个成员，可能出现部分成员仍有效、部分成员已删除。不能只看 patch 方法整体是否“有一个目标匹配”。
- **ref 树很大，避免反复交互式全文 grep**：优先一次性建立 `.cs` 文件索引，再做类型名和成员名匹配。仓库脚本已经按这个思路实现，后续复用脚本即可。
- **同一类型常在多个 ref 项目里重复出现**：如果 patch 带有 `LibraryName`，优先用程序集名过滤匹配文件；只有缺失 `LibraryName` 时，才退回到共享实现的代表性核查。
- **复杂 patch 的 `sig_match` 不等于最终有效**：IL 指令级 patch、字符串/XML 注入 patch、以及手写 `method.ReplaceWith(...)` 的 patch，签名匹配后仍要看方法体逻辑是否保留原假设。
- **目标缺失时继续追调用链，不要立刻止步**：4.58.12 中已经出现 `MainWindowViewModel.CheckExpirationDate()` 下沉到 `MainWindowViewModelService.CheckExpirationDate()`，Toyota 相关类型也转移到 `IndustrialCustomerManager` / `IIndustrialCustomerWorker` 抽象层。遇到“方法消失”时，要继续查 service、interface、manager 和 worker 抽象。
- **参数化 patch 也算 patch 定义**：返回 `Func<ModuleDefMD, int>` 的 patch 方法，例如 market language 和 RSA 参数替换，虽然运行时需要额外参数才会启用，但兼容性核查时仍应纳入 patch 清单，并标注为 conditional。
- **首轮脚本报告允许出现 `⚠️ review`**：这是刻意保守，不是脚本失败。最终报告才需要把这些项收敛成 `✅`、`⚠️` 或 `❌`。

---

## 完成标准

- 所有在当前 ref 版本范围内的 patch 均有核查记录
- 失效 patch 有明确的失效类型、变更描述、修复建议
- 报告以 markdown 文件形式保存，结构清晰，可直接用于后续修复工作

---

## 示例触发语句

- "检查所有 patch 在 ref/ 下新 ISTA 代码中的适用性"
- "生成 patch 兼容性 markdown 报告"
- "列出失效 patch 及修复建议"
- "核查 4.58 版本的 patch 兼容性"

## 可扩展建议

- 对失效 patch 自动生成更新后的签名描述字符串草案
- 增加对 patch 依赖关系的分析（某 patch 依赖另一 patch 的结果）
- 输出可直接用于 openspec `tasks.md` 的任务清单
