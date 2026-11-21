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

在 fork 的 **Settings → Actions → General** 里把 Workflow permissions 设为
**Read and write permissions**，否则同步工作流无法建 PR/issue。

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

当 #22517（或等价的语言选择基础设施）被上游合并时：

1. `git fetch upstream`
2. 确认我们的核心提交哪些已被上游覆盖（`git range-diff upstream/bleed solar-core solar`）
3. 丢弃核心补丁，只保留翻译数据：

```bash
git rebase --onto upstream/bleed solar-core solar
```

`solar-core` 之后的提交（翻译、字体、弹窗）会重放到新的 `upstream/bleed` 上。
4. 若上游的字段名/语言注册方式与我们的不同，按上游实现调整 `mod.yaml` 与
   `FluentLanguages`，翻译 `.ftl` 本身不用动。
5. 编译、启动、切换语言测试通过后推送 `solar`。

`Upstream Localization Watch` 工作流每天检查 #22517/#22531/#22629 是否被合并，
一旦合并会自动开 issue（label `solar-migration`），提醒执行上面的步骤。

> 我们的策略是“只同步 `upstream/bleed`，不直接合并上述 PR 的 head 分支”。
> 这样上游合并后，历史是干净的：我们只是把自己的数据 rebase 到上游实现上，
> 不会产生重复的语言选择实现。

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
