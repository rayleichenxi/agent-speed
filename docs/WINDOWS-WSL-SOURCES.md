# Windows 版的 WSL 数据源

在“设置 → 数据源”中，使用 Codex 和 Claude 对应的目录列表添加 WSL 目录。官方的多目录功能会同时读取 Windows 与 WSL 位置；关闭对应数据源会停用该来源的所有位置。旧维护版的 `wslCodexRoot` 和 `wslClaudeRoot` 会在首次读取时迁移到新列表，无需重新填写。

将 Codex home 路径追加 `sessions`，Claude home 路径追加 `projects`，例如 `\\wsl.localhost\Ubuntu-22.04\root\.codex\sessions` 和 `\\wsl.localhost\Ubuntu-22.04\root\.claude\projects`。实际发行版、用户及数据目录以本机配置为准；历史统计也会读取对应 Codex `archived_sessions`。

程序只读会话文件，不启动或更改 WSL 服务。Windows 文件通知不可用时，既有的定期发现和轮询仍会读取变更；新会话可能需要约 10 秒才能发现。WSL 不可访问时不会读取该来源，恢复后会重新发现。原始日志可能含有敏感信息；实时上报默认关闭。

请避免把同一批文件通过不同挂载路径或复制目录重复添加；应用会去重相同位置，但不能保证识别所有指向相同文件的别名。
