# 安全策略

## 支持版本

当前维护 1.x 版本线。发布版本的具体兼容范围以对应 README、Release 说明和插件依赖为准。

## 报告安全问题

请不要在公开 Issue、Pull Request、讨论区或聊天记录中披露可利用的安全漏洞、完整利用步骤或敏感服务器信息。

如果仓库已开启 GitHub Private Vulnerability Reporting，请使用仓库的 [Security 页面](https://github.com/qingranawa/SCPSL-ReinforcementGate/security) 创建私密报告。GitHub 会在该功能可用时提供私密漏洞报告入口。

如果私有漏洞报告入口未开启，请通过维护者 `qingranawa` 的 [GitHub 私信](https://github.com/qingranawa) 联系维护者，并在消息中说明这是 ReinforcementGate 安全报告。

报告请尽量包含以下最小信息：

- 插件版本、SCP:SL 版本和 LabAPI 版本。
- 复现条件、必要的配置和最小复现步骤。
- 受影响的功能、权限边界或潜在影响范围。
- 便于定位问题的日志、堆栈或最小复现代码；请先移除服务器地址、密钥、令牌和玩家个人信息。

维护者会在确认报告后评估影响范围、修复方案和披露时间。请不要在报告中提交真实服务器凭据或其他秘密信息。

## 安全边界

ReinforcementGate 是运行在 SCP:SL 服务端内存中的 LabAPI 插件。它不分发游戏程序集，不保存跨回合控制状态，也不提供独立的网络服务。安全审查重点包括 Remote Admin 权限检查、公共控制 API 的调用边界、配置与模板处理、插件加载/卸载生命周期，以及通知或外部事件异常是否能改变已经提交的状态或支援决定。
