# 贡献指南

感谢你为 ReinforcementGate 提交改进。这个项目是面向 SCP: Secret Laboratory Dedicated Server 的 LabAPI 插件，贡献应尽量保持范围清晰、行为可验证。

## 开始之前

- 先搜索已有 Issue，避免重复提交。
- 行为变更请先说明动机、影响范围和兼容的 LabAPI/SCP:SL 版本。
- 不要提交服务器私有配置、SteamID、日志中的令牌或任何游戏程序集。

## 本地构建

项目目标框架是 .NET Framework 4.8，真实服务端构建需要设置 `SL_REFERENCES`，指向 SCP:SL Dedicated Server 的 `SCPSL_Data/Managed` 目录。

```powershell
$env:SL_REFERENCES = 'D:\path\to\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed'
dotnet build ReinforcementGate.sln -c Release --no-restore
dotnet test tests\ReinforcementGate.Tests\ReinforcementGate.Tests.csproj -c Release --no-restore
```

如果没有真实服务端程序集，请在 PR 中说明环境限制；不要把临时 DLL 桩或游戏程序集提交到仓库。

## 修改规范

- 逻辑变更先补失败测试，再实现修复。
- 保持 `RespawnEvents` 权限语义、波类型映射和状态优先级不变，除非 PR 明确说明。
- 通知默认保持关闭；自定义 BC/CASSIE 文案应通过配置完成。
- 不回溯已经发生的支援，不处理已经生成的玩家，也不主动创建支援波次。
- 保持 README、配置示例和公共 API 文档与实现同步。

## Pull Request 清单

- [ ] 已说明改动内容、动机和兼容版本。
- [ ] 已添加或更新相关测试。
- [ ] 已运行 Release 构建和测试，或说明无法运行的原因。
- [ ] `git diff --check` 通过。
- [ ] 未提交凭据、服务器配置、游戏程序集或构建输出。
- [ ] 如涉及配置/API/命令，已同步更新 README。

## 提交信息

提交信息使用简短、可读的动词开头，例如：

- `feat: expose reinforcement state API`
- `fix: align command with LabAPI 1.1.7`
- `docs: add contribution guide`

## 许可证

本仓库原创代码采用 MIT License。提交代码即表示你有权按该许可证授权所提交的内容。
