# Unity引擎战术RPG启发式角色智能体模板
Unity 版本: 6000.0.30f1

中文视频链接：https://www.bilibili.com/video/BV11PXfBuEJp/?spm_id_from=333.1387.homepage.video_card.click

## 介绍
本项目是修复了毕设中存在的漏洞与代理决策延迟。但未测试在极端条件下（例如大量技能与超过20个单位的情况下保持肉眼无法察觉的延迟）。本项目作为策略角色扮演游戏启发式智能体模板，
实现了基于网格战术行动的智能评分代理——其行为表现基于项目游戏机制设计。在设计上贴近4款游戏：
《最终幻想战略版》、《神界原罪2》《皇家骑士团重生》及《三角战略》。由于项目编写之初考量了恶魔城的设计理念，所以出现了部分与TPRG/SRPG存在差异的设计。
此项目被实现在3维网格数据上，但地图在设计上仍需完善，目前只是支持int 数值的y轴高度，所有其余在设计上也是如此。
同时值得注意的是地图的数据需要使用本项目内置的简陋地图编辑器配合tile工具进行配置同时生成地图数据文件。
## Tactics Map Editor - Not fully Encapsulate (战术地图编辑器）
<img width="400" height="651" alt="image" src="https://github.com/user-attachments/assets/8fba9c06-d0e4-4896-ac71-ab76189dc841" />

该项目利用了通过定制地图编辑工具生成的预制JSON地图数据，用于加载特定地图并释放多余的地图。

## 角色战术时间轴
这是一种游戏机制，用于管理和确定角色行动的回合顺序。

<img width="940" height="900" alt="image" src="https://github.com/user-attachments/assets/c0a4de30-c9fa-40c3-b66f-44d1525c027b" />

上述图像为CT时间线的逻辑流程图。相较于常规速度决定时间线的机制，该机制引入了新要素：CT疲劳惩罚。角色行动顺序的排序完全取决于角色速度，直至所有角色行动序列分配完毕。
因此，CT时间线的设计允许角色在每次排序中拥有多个行动序列。最终结果以CT回合呈现，每个回合包含多个角色的行动轮次。
主要表现计算流程如下：

<img width="570" height="712" alt="image" src="https://github.com/user-attachments/assets/3edcb173-d05c-43f4-93c0-36c4d550092f" />

## 技能机制
为确保战术多样性，技能机制的设计很大程度上决定了战术AI。目前项目上完成了治疗技能，伤害技能与原创抛射物技能。同时允许MP消耗与非MP消耗技能。
### 投射物与非投射物
技能可能被沿其飞行路径布置的地形特征或单位阻挡。这包括墙壁、地形起伏，以及根据技能碰撞逻辑判定为友军或敌军的单位。
### 技能范围
技能范围与战术瞄准镜机制相关联。在定义技能范围和遮挡范围时，实际范围将体现战术范围的特性：既从角色原点延伸直到可及范围，又从角色原点开始执行遮挡计算。通过这种方式实现技能范围的限制。
### 技能条件
技能条件代表施放技能的要求。在技能设计中，该条件被设定为消耗MP点数，但根据游戏设计，这并非唯一可接受的条件。

## 战术RPG代理
本项目智能代理的设计原则追求去中心化、回合制战术，与项目默认的战斗机制高度契合。该系统将与地图空间、团队、角色、技能及战斗系统深度集成。TRPG智能代理被设计为具备空间感知、角色识别、技能判断及团队协作能力的智能体，而非单纯基于规则运作。

## 评分评估
智能体的决策受不同评估机制的约束，其中某些评估会根据智能体当前状态被抑制。在当前智能体评估中，可区分出三种独立的评估原则，包括纯技能施放动作的评估、移动技能施放动作或纯移动动作的评估。除上述三种评估外，定向评估必定在最后执行。

<img width="544" height="834" alt="image" src="https://github.com/user-attachments/assets/e76d3a85-fe88-4a10-9616-e0c8f4838537" />

## 规则
规则是评估项的子类组合，也是最关键且最符合逻辑的呈现形式。规则也可嵌套为其他规则的子项，尽管这些规则并不严格禁止子项与父项之和超过父项的总分。然而，当前所有规则的制定都限制了子类规则的得分不得超过父类别的总分。
### 目标规则
目标规则基于智能体评估哪个单位最适合最近距离作战。目标始终根据决策者当前的前线指数和目标值进行调整。目标可能包括敌方单位和队友单位。
### 移动目标规则
移动目标规则旨在寻找通往当前目标角色周边区域最合适、最短且最安全的路线。根据角色前线指数，若指数越高，决策者对危险路径的敏感度越低。
### 技能伤害规则
本规则用于计算当前情境下最适宜的技能能力。该规则采用最低消耗与最高产出公式的计算方法。
### 技能治疗规则
此规则是技能评估的另一种变体，专门适用于治疗技能。该划分背后的核心原则是赋予智能体更直观且易于调试的行为倾向。例如，智能体可能表现出更倾向于采取攻击性行动，而非使用治疗技能。
### 风险移动规则
一种确定最安全站位规则的方法，通过评估队友与敌人的位置来保障单位安全，同时防止单位偏离团队过远。
### 风险移动伤害规则
风险移动规则的变体，主要整合伤害规则的技能选择机制，同时确保所选移动位置的安全性。
### 风险移动治疗规则
风险移动规则的变体，主要整合治疗规则的技能选择机制，同时确保所选移动位置的安全性。
### 致命打击规则
该规则通常作为伤害规则的子类运作，仅将消灭目标的能力视为额外计分要素。
### 背部防御规则
参考地形与角色的范围调整合适的朝向。

## 杂项
### 战术角色编辑器
战术角色编辑器是组合了角色数据定义生成与AI调试的工具，此项目允许查看AI行为的积分，从而帮助进行扩展与调试
<img width="1002" height="955" alt="image" src="https://github.com/user-attachments/assets/b392616b-8b63-4dbd-900d-e4f1a34b1dd6" />
<img width="1000" height="962" alt="image" src="https://github.com/user-attachments/assets/f9fc7447-8bbd-4f1f-a14d-5b7a20f65afb" />
<img width="1001" height="952" alt="image" src="https://github.com/user-attachments/assets/f2e4e4c6-2dbd-4090-9086-f05e8ecddef1" />
### 技能编辑器
技能编辑器可帮助直观的进行技能与特性的配置，目前支持投射物与非投射物，但无特效。

## 优化
### 代理抉择
由于代理的条件与计算复杂所以需要在单帧内消耗大量的计算机性能，并且Job System上难以对其分配，项目中虽完成了Job 寻路算法，但是还没有非常有价值的应用方向，所以AI的抉择优化上只实施了Coroutine的分帧计算。同时在表现上避免了主线程卡顿。

# Unity Engine Tactical RPG Heuristic Agent Template
Unity Version: 6000.0.30f1

## Introduction

This project addresses the bugs and decision-making delays present in the original thesis project. However, it has not been tested under extreme conditions (such as maintaining imperceptible latency when dealing with a large number of skills and more than 20 units). Serving as a template for heuristic agents in tactical role-playing games,
this project implements an intelligent scoring agent for grid-based tactical actions—its behavior is based on the game mechanics designed for the project. The design draws inspiration from four games:
*Final Fantasy Tactics*, *Divinity: Original Sin 2*, *Tactic Orge Reborn*, and *Triangle Strategy*. Since the project was initially developed with the design philosophy of *Castlevania* in mind, some design elements differ from those of TPRGs and SRPGs.
This project is implemented on a 3D grid, but the map design still requires refinement; currently, it only supports y-axis heights as integer values, and the same applies to all other design aspects.
It is also worth noting that map data must be configured using the project’s built-in rudimentary map editor in conjunction with tile tools to generate map data files.

## Tactics Map Editor - Not fully Encapsulate (Tactics Map Editor)
<img width="400" height="651" alt="image" src="https://github.com/user-attachments/assets/8fba9c06-d0e4-4896-ac71-ab76189dc841" />

This project utilises pre-generated JSON map data created using a custom map editing tool to load specific maps and unload unnecessary ones.

## Character Tactic Turn Order
This is a game mechanic used to manage and determine the order in which characters take their turns.

<img width="940" height="900" alt="image" src="https://github.com/user-attachments/assets/c0a4de30-c9fa-40c3-b66f-44d1525c027b" />

The image above shows a flowchart of the CT timeline. Compared to the conventional speed-based timeline mechanism, this mechanism introduces a new element: CT fatigue penalty. The order of character actions is determined entirely by character speed until all character action sequences have been assigned.
Consequently, the CT timeline design allows characters to have multiple action sequences in each sorting round. The final result is presented as a CT round, with each round containing multiple character action rounds.
The main calculation process is as follows:

<img width="570" height="712" alt="image" src="https://github.com/user-attachments/assets/3edcb173-d05c-43f4-93c0-36c4d550092f" />

## Skill Mechanics
To ensure tactical diversity, the design of skill mechanics largely determines the tactical AI. Currently, the project has implemented healing skills, damage skills, and original projectile skills. Both MP-consuming and non-MP-consuming skills are supported.
### Projectiles and Non-Projectiles
Skills may be blocked by terrain features or units along their flight path. This includes walls, terrain elevation changes, and units identified as friendly or enemy based on skill collision logic.
### Skill Range
Skill range is linked to the tactical scope mechanism. When defining skill range and obstruction range, the actual range reflects the characteristics of the tactical range: it extends from the character’s origin to the reachable area, and obstruction calculations begin from the character’s origin. This approach enforces the limitations of the skill range.
### Skill Conditions
Skill conditions represent the requirements for casting a skill. In skill design, this condition is set as MP consumption; however, depending on game design, this is not the only acceptable condition.

<img width="544" height="834" alt="image" src="https://github.com/user-attachments/assets/e76d3a85-fe88-4a10-9616-e0c8f4838537" />

## Rules
Rules are combinations of subcategories of evaluation criteria, and they represent the most critical and logical form of presentation. Rules can also be nested as sub-items of other rules, although these rules do not strictly prohibit the sum of sub-items and parent items from exceeding the parent’s total score. However, current rule design requires that the score of a subcategory rule must not exceed the total score of its parent category.
### Target Rule
The Target Rule is based on the agent’s assessment of which unit is best suited for close-range combat. The target is always adjusted according to the decision-maker’s current Frontline Index and target value. Targets may include enemy units and ally units.
### Movement Target Rule
The Movement Target Rule aims to find the most suitable, shortest, and safest route to the area surrounding the current target character. Based on the character’s Frontline Index, the higher the index, the lower the decision-maker’s sensitivity to dangerous paths.
### Skill Damage Rules
These rules are used to calculate the most appropriate skill abilities for the current situation. The calculation method follows the formula of minimum cost and maximum output.
### Skill Healing Rules
This rule is another variant of skill evaluation, specifically designed for healing skills. The core principle behind this distinction is to give the AI more intuitive and easily tunable behavioral tendencies. For example, an agent may exhibit a greater tendency to take offensive actions rather than use healing skills.
### Risk Movement Rule
A method for determining the safest positioning rule, ensuring unit safety by evaluating the positions of teammates and enemies, while preventing units from straying too far from the team.
### Risk Movement Damage Rule
A variant of the Risk Movement Rule that primarily integrates the skill selection mechanism from the Damage Rule while ensuring the safety of the selected movement position.
### Risky Movement Healing Rules
A variant of the Risky Movement Rules that primarily integrates the skill selection mechanism from the Healing Rules while ensuring the safety of the selected movement location.
### Critical Strike Rules
These rules typically operate as a subcategory of the Damage Rules, treating the ability to eliminate targets solely as an additional scoring factor.
### Back Defense Rules
Adjust orientation appropriately based on terrain and character range.

## Sundries
### Tactical Character Editor
The Tactical Character Editor is a tool that combines character data definition and AI debugging. This feature allows you to view the state of AI behavior, thereby aiding in expansion and debugging.
<img width="1002" height="955" alt="image" src="https://github.com/user-attachments/assets/b392616b-8b63-4dbd-900d-e4f1a34b1dd6" />
<img width="1000" height="962" alt="image" src="https://github.com/user-attachments/assets/f9fc7447-8bbd-4f1f-a14d-5b7a20f65afb" />
<img width="1001" height="952" alt="image" src="https://github.com/user-attachments/assets/f2e4e4c6-2dbd-4090-9086-f05e8ecddef1" />

### Skill Editor
The Skill Editor allows for intuitive configuration of skills and traits. It currently supports projectiles and non-projectiles, but does not include special effects.

## Optimization
### Agent Decision-Making
Due to the complexity of agent conditions and calculations, a significant amount of computational power is consumed per frame, and it is difficult to allocate these tasks within the Job System. Although a pathfinding algorithm for Jobs has been implemented in the project, it has not yet found a particularly valuable application. Therefore, for AI decision-making optimization, we have only implemented frame-based calculations using coroutines. This approach also prevents stuttering on the main thread.
