# Windows 版的 WSL 数据源

在“设置 → 数据源”中，`WSL Codex · sessions` 和 `WSL Claude Code · projects` 是独立的附加目录。可以通过文件夹选择器选择，或粘贴绝对路径后移开输入焦点；“重置”清除对应路径。原有的 Codex 和 Claude 目录不变，关闭相应数据源时两组目录都会停用。

将工作台配置中的 Codex home 路径追加 `sessions`，Claude home 路径追加 `projects`，例如 `\\wsl.localhost\Ubuntu-22.04\root\.codex\sessions` 和 `\\wsl.localhost\Ubuntu-22.04\root\.claude\projects`。这些路径仅作为示例，具体发行版、用户及数据目录以实际配置为准；修改工作台设置后，需要在此同步调整。历史统计也会读取对应 Codex `archived_sessions`。

程序只读会话文件，不启动或更改 WSL 服务。Windows 文件通知不可用时，既有的定期发现和轮询仍会读取变更；新会话可能需要约 10 秒才能发现。WSL 不可访问时不会读取该来源，恢复后会重新发现。原始日志可能含有敏感信息；实时上报设置与原版一致，默认关闭。

每个附加字段接收一个目录；请选择不与已有目录重叠的路径。程序会去重完全相同的路径和已有响应标识，但不保证能识别不同挂载路径或复制目录指向同一批文件的所有情况。
