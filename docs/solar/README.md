# solar 分支维护手册

`solar` 是本地汉化长期分支：所有引擎补丁、语言包、字体和脚本都提交在这里；
`bleed` 只做上游只读镜像，永远不在上面提交。

## 1. 分支与远程

```text
OpenRA/OpenRA (upstream)  bleed
        │  fetch + merge
        ▼
你的 fork (origin)        solar          ← 日常开发/构建
        └─ solar-sync                     ← CI 自动创建的同步 PR 分支
```

约定：

- `upstream` = `git@github.com:OpenRA/OpenRA.git`（只读）
- `origin` = 你的 fork（推送 `solar`）
- `bleed` 仅用于 `git merge --ff-only upstream/bleed`
- 任何汉化工作都提交到 `solar`，不要提交到 `bleed`
- **不要直接 merge PR #22517 / #22531 / #22629 的 head 分支**；只同步 `upstream/bleed`。
  这些 PR 的上游提交要么已经被上游合并，要么会被我们自己的实现替代。

## 2. 一次性设置

```bash
git fetch --unshallow          # 必须完整历史，否则无法 merge/rebase 上游
git remote rename origin upstream
git remote add origin git@github.com:<你的账号>/OpenRA.git
git switch -c solar upstream/bleed
git branch --set-upstream-to=upstream/bleed bleed

git config rerere.enabled true
git config rerere.autoupdate true
git config merge.conflictStyle zdiff3

git push -u origin solar
```

在 fork 的 **Settings** 里还需要做两件事（否则自动同步不会按预期工作）：

1. **General → Default branch** 改为 `solar`。
   GitHub 的 `schedule` 和 `workflow_dispatch` 只在**默认分支**上读取工作流文件；
   不改的话 `solar` 上的两个 workflow 不会自动运行。
2. **Actions → General → Workflow permissions** 设为
   **Read and write permissions**，否则工作流无法创建 PR/issue。

> 上游除 `bleed` 外还有 `prep-2602`、`prep-2610` 这类发布准备分支；
> 我们的自动同步只跟踪 `upstream/bleed`，不跟踪 `prep-*`。

## 3. 日常同步

本地：

```powershell
.\tools\solar\sync.ps1          # git fetch upstream + merge upstream/bleed 到 solar
# 或线性历史（仅私有分支）：
.\tools\solar\sync.ps1 -Rebase
```

冲突只可能出现在被汉化补丁改过的引擎文件（`FluentProvider.cs`、`Manifest.cs`、
`Settings.cs`、`ModData.cs`、`MapPreview.cs`、各 `mod.yaml` 等）。
`mods/*/fluent/zh-Hans/*.ftl` 是新文件，上游不会碰，基本不会冲突。
`rerere` 会记住解法。

CI：`Solar Sync` 每周一 03:00 UTC 自动执行，行为：

1. 拉取 `upstream/bleed`；
2. 在 `solar-sync` 临时分支上 `git merge --no-ff`；
3. 冲突 → 开 issue（label `solar-sync`）并失败；
4. 成功 → `dotnet build` + Fluent key 对比 → 开 PR 到 `solar`；
5. 你 review 后合并 PR 即可。

也可以手动触发（Actions → Solar Sync → Run workflow）。

## 4. 提交结构

为了将来能干净地摘掉引擎补丁，提交按“能否单独删除”分组：

| 提交 | 内容 | 将来是否可删 |
| --- | --- | --- |
| `solar: language selection core` | `Settings.Game.Language`、`FluentProvider` 覆盖加载、`FluentLanguages`、设置面板、字体选择 | 上游合并语言基础设施后删除 |
| `solar: restart dialog` | 切换语言后的“知道了 / 立即重启”弹窗与进程重启 | 可保留或上游化 |
| `solar: zh-Hans data` | `mods/*/fluent/zh-Hans/*.ftl`、字体、各 mod.yaml 的 `FluentLanguages: zh-Hans` 行 | 永久保留 |

规则：

- 数据提交**只允许**新增 `fluent/<lang>/*.ftl`、字体文件、`mod.yaml` 里的语言注册行；
  不要碰核心 `.cs`。
- 核心补丁提交完成后打 tag，作为将来 rebase 的边界：

```bash
git tag solar-core        # 指向最后一个“核心补丁”提交
```

- 不要在 `solar` 上随手跑 `make version`（会改各 mod.yaml 的版本行，制造冲突）。

## 5. 上游合并语言基础设施之后

**当前策略：等上游真正合并后再决定。** 不预设迁移方式，也不直接 merge 上述 PR 的 head 分支；
只同步 `upstream/bleed`。`Upstream Localization Watch` 每天检查 #22517/#22531/#22629，
一旦合并会自动开 issue（label `solar-migration`），届时在两个方案中选择：

### 方案 A：迁移到上游实现（维护成本最低）

```bash
git fetch upstream
git range-diff upstream/bleed solar-core solar   # 先确认重叠范围
git rebase --onto upstream/bleed solar-core solar
```

`solar-core` 之后的提交（翻译、字体、弹窗）会重放到新的 `upstream/bleed` 上；
按上游的字段名/语言注册方式调整 `mod.yaml` 与 `FluentLanguages`，`.ftl` 本身不用动。
最后编译、启动、切换语言测试通过后推送 `solar`。

### 方案 B：保留 solar 自己的 core

不 rebase 掉核心提交，只把 `upstream/bleed` merge 进来并手动解决冲突。
适用于上游实现与我们的需求差异较大（例如重启弹窗、字体策略）的情况；
长期成本是每次上游改动 `FluentProvider`/`Manifest`/`Settings` 都要跟随适配。

无论选哪个方案，翻译数据（`mods/*/fluent/zh-Hans/*.ftl`、字体、`mod.yaml` 语言行）
都保留，不要动。

## 6. 发布

```bash
git tag -a solar-2026.11.21 -m "Chinese localization based on 4cc9a9f"
git push origin solar --tags
```

tag 名带上日期和基于的上游 commit，方便定位问题。

## 7. 检查脚本

```powershell
python tools/solar/check-fluent.py --lang zh-Hans
python tools/solar/check-fluent.py --lang zh-Hans --strict   # 发布前用
```

输出：缺失文件、缺失 key、多余 key、占位符不匹配。
`--strict` 会把缺失 key 视为失败；CI 默认非严格，只报告。

## 8. 禁止事项

- ❌ 在 `bleed` 上提交
- ❌ 直接 merge #22517/#22531/#22629 的 head 分支
- ❌ 在数据提交里改核心 `.cs`
- ❌ 对已推送的 `solar` 做 force-push（除非确认没有其他人/CI 基于它）
- ❌ 把 `bin/`、`.codegraph/`、测试日志提交进仓库
