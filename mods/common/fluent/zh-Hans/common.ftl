## Common
button-continue = 继续
button-back = 返回
button-quit = 退出

## Version and player defaults
label-development-version = 开发版
default-player-name = 指挥官
label-skirmish-server-name = 遭遇战

options-lobby-slot =
    .slot = 位置
    .open = 空位
    .closed = 已关闭
    .bots = 电脑
    .bots-disabled = 电脑已禁用

## System info prompt
label-system-info-row = { $label }：{ $value }
label-system-info-id = 匿名 ID
label-system-info-platform = 操作系统
label-system-info-os = 系统版本
label-system-info-architecture = 架构
label-system-info-runtime = .NET 运行时
label-system-info-opengl = OpenGL 版本
label-system-info-window-size = 窗口尺寸
label-system-info-window-scale = 窗口缩放
label-system-info-ui-scale = 界面缩放
label-system-info-system-language = 系统语言

label-unrevealed-terrain = 未探索区域

label-paused = 已暂停

menu-ingame =
    .leave = 离开
    .abort = 中止任务
    .restart = 重新开始
    .surrender = 投降
    .load-game = 载入游戏
    .save-game = 保存游戏
    .music = 音乐
    .settings = 设置
    .return-to-map = 返回地图
    .resume = 继续
    .save-map = 保存地图
    .exit-map = 退出地图编辑器

dialog-leave-mission =
    .title = 离开任务
    .prompt = 离开本局并返回菜单？
    .confirm = 离开
    .cancel = 留下

dialog-restart-mission =
    .title = 重新开始
    .prompt = 确定要重新开始吗？
    .confirm = 重新开始
    .cancel = 留下

dialog-surrender =
    .title = 投降
    .prompt = 确定要投降吗？
    .confirm = 投降
    .cancel = 留下

options-shroud-selector =
    .all-players = 所有玩家
    .disable-shroud = 禁用迷雾
    .other = 其他

options-observer-stats =
    .none = 信息：无
    .basic = 基础
    .economy = 经济
    .production = 生产
    .support-powers = 支援技能
    .combat = 战斗
    .army = 军队
    .earnings-graph = 收入（图表）
    .army-graph = 军队（图表）

label-win-state-won = 胜利
label-win-state-lost = 失败

options-display-mode =
    .windowed = 窗口化
    .legacy-fullscreen = 全屏（旧式）
    .fullscreen = 全屏

label-video-display-index = 显示器 { $number }

options-status-bars =
    .standard = 标准
    .show-on-damage = 受伤时显示
    .always-show = 始终显示

options-target-lines =
    .automatic = 自动
    .manual = 手动
    .disabled = 已禁用

checkbox-frame-limiter = 启用帧率限制（{ $fps } FPS）

auto-save-interval =
    .disabled = 已禁用
    .options =
        { $seconds ->
            [one] { $seconds } 秒
           *[other] { $seconds } 秒
        }
    .minute-options =
        { $minutes ->
            [one] { $minutes } 分钟
           *[other] { $minutes } 分钟
        }

auto-save-max-file-number = { $saves } 个存档

options-mouse-scroll-type =
    .disabled = 已禁用
    .standard = 标准
    .inverted = 反转
    .joystick = 摇杆

hotkey-context-any = 任意

options-game-speed =
    .slowest = 最慢
    .slower = 较慢
    .normal = 普通
    .fast = 快
    .faster = 较快
    .fastest = 最快

## Server orders
options-control-scheme =
    .classic = 经典
    .modern = 现代
    .otherrts = 其他 RTS

options-camera =
    .close = 近
    .medium = 中
    .far = 远
    .furthest = 最远
