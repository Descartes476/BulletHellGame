# DKsProject

一个基于 Unity 的 2D Bullet Hell（弹幕射击）练习项目。

当前项目包含基础的玩家移动与射击、敌人与子弹管理、HUD 展示，以及一套独立的确定性仿真核心，便于后续扩展同步、回放或更严格的游戏逻辑验证。

## 项目特点

- 2D 弹幕射击基础玩法
- 玩家、敌人、子弹的运行时管理
- HUD 分数、血量、复活信息展示
- 基于 `Fix64` 的定点数仿真核心
- 仿真状态与表现层分离
- 预留对象池与模式化射击扩展能力

## 目录结构

```text
DKsProject/
|- Assets/
|  |- BulletHell/
|  |  |- Prefabs/                 # 预制体资源
|  |  |- Scenes/                  # 项目场景
|  |  |- Scripts/
|  |  |  |- Bullet/               # 子弹与子弹管理
|  |  |  |- Core/                 # 通用基础设施（如对象池）
|  |  |  |- Enemy/                # 敌人、敌人生成与射击模式
|  |  |  |- Game/                 # 游戏全局流程管理
|  |  |  |- Player/               # 玩家控制与玩家实体
|  |  |  |- Simulation/           # 确定性仿真核心
|  |  |  |- UI/                   # HUD 与界面控制
|  |  |  |- Fix64.cs              # 定点数与定点向量实现
|  |  |  |- TestScript.cs         # 临时测试脚本
|- Packages/                      # Unity Package 配置
|- ProjectSettings/               # Unity 项目设置
|- launch_unityhub_and_windsurf.bat
```

## 主要系统说明

### 玩家系统

玩家相关逻辑主要位于：

- `Assets/BulletHell/Scripts/Player/PlayerController.cs`
- `Assets/BulletHell/Scripts/Player/PlayerBase.cs`

职责包括：

- 采样输入
- 控制移动
- 控制开火
- 管理生命值、死亡、复活与无敌状态

### 子弹系统

子弹相关逻辑主要位于：

- `Assets/BulletHell/Scripts/Bullet/Bullet.cs`
- `Assets/BulletHell/Scripts/Bullet/BulletManager.cs`
- `Assets/BulletHell/Scripts/Core/ObjectPool.cs`

职责包括：

- 子弹生成与回收
- 子弹逐帧推进
- 阵营区分（玩家 / 敌人）
- 基于对象池的复用

### 敌人系统

敌人相关逻辑主要位于：

- `Assets/BulletHell/Scripts/Enemy/EnemyBase.cs`
- `Assets/BulletHell/Scripts/Enemy/EnemySpawner.cs`
- `Assets/BulletHell/Scripts/Enemy/EnemyShooter.cs`
- `Assets/BulletHell/Scripts/Enemy/Patterns/`

职责包括：

- 敌人生成
- 敌人生命值管理
- 选择并执行不同的射击模式

### 仿真系统

仿真核心主要位于：

- `Assets/BulletHell/Scripts/Simulation/Core/SimulationDriver.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/WorldSimulator.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/PlayerSimulator.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/EnemySimulator.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/BulletSimulator.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/SimulationConfig.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/SimulationConfigAsset.cs`

这一层负责：

- 使用定点数推进世界状态
- 维护玩家、敌人、子弹的仿真快照
- 将输入转为逻辑 Tick
- 将仿真状态同步到场景中的表现对象

### Tick / Input / WorldHash 语义

为了统一仿真、回放与后续联机语义，当前项目约定：

- `WorldSnapshot.Tick` 表示当前世界本身所处的逻辑帧号
- `InputFrame.Tick` 表示该输入要作用于哪个逻辑帧
- `ReplayFrame.WorldHash` 表示该帧输入执行前的世界状态哈希

统一公式为：`World[t] + Input[t] -> World[t+1]`。

这意味着 `ReplayFrame.Tick = t` 时，记录的是 `Hash(World[t])`，这样可以保留 `Tick 0` 的初始世界状态，并使录制、回放和校验流程保持一致。

### UI 系统

UI 相关逻辑主要位于：

- `Assets/BulletHell/Scripts/UI/HUDController.cs`
- `Assets/BulletHell/Scripts/Game/GameManager.cs`

职责包括：

- 显示血量
- 显示分数
- 显示复活倒计时
- 响应游戏状态事件

## 开发环境

当前项目使用 Unity 包包括：

- `com.unity.feature.2d`
- `com.unity.textmeshpro`
- `com.unity.ugui`
- `com.unity.timeline`
- `com.unity.visualscripting`

如果你直接在 Unity 中打开项目，Unity 会根据 `Packages/manifest.json` 自动解析依赖。

## 如何打开项目

### 方式一：使用 Unity Hub

通过 Unity Hub 打开项目目录：

```text
h:\project\unityproject\DKsProject
```

### 方式二：使用仓库内脚本

项目根目录提供了一个启动脚本：

- `launch_unityhub_and_windsurf.bat`

它会尝试：

- 用 Unity Hub 打开当前项目
- 用 Windsurf 打开当前工程目录

> 注意：脚本中写死了本机路径，如果你的 Unity Hub 或 Windsurf 安装位置不同，需要自行调整批处理文件中的路径。

## 建议的开发入口

如果你准备继续开发，建议优先查看以下文件：

- `Assets/BulletHell/Scripts/Game/GameManager.cs`
- `Assets/BulletHell/Scripts/Player/PlayerController.cs`
- `Assets/BulletHell/Scripts/Player/PlayerBase.cs`
- `Assets/BulletHell/Scripts/Bullet/BulletManager.cs`
- `Assets/BulletHell/Scripts/Enemy/EnemyBase.cs`
- `Assets/BulletHell/Scripts/Simulation/Core/SimulationDriver.cs`
- `Assets/BulletHell/Scripts/Fix64.cs`

## 后续可扩展方向

- 增加更多敌人行为与弹幕模式
- 为仿真层补充更严格的状态校验
- 增加关卡流程与波次系统
- 增加暂停、结算、主菜单等完整 UI
- 为输入、仿真和表现层建立更清晰的解耦
- 扩展回放、联机同步或录像功能

## 说明

当前仓库中同时存在运行时表现逻辑与仿真逻辑，两者并行演进时，建议优先保持以下原则：

- 仿真层负责确定性状态计算
- 表现层负责 Unity 场景中的视觉同步
- 公共配置尽量统一从配置资产或集中式配置结构读取
