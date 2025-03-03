# Azure-Commitlog-Check

[English](README.md) | [中文](README.zh-CN.md)

一个用于检查 Azure DevOps(TFS) 中 Pull Request 的 commit-log 是否符合规范的工具，特别适用于使用 squash 提交方式的 PR。

## 主要功能

- 连接到 Azure DevOps(TFS) 服务
- 获取指定 PR 的详细信息
- 检查 PR 标题是否符合 commit message 规范
- 支持自定义 commit message 规范的正则表达式模式
- **新功能**: 自动检测当前分支的 PR，无需手动指定 PR ID

## 技术栈

- .NET 8.0
- Azure DevOps API 
- System.CommandLine (命令行解析)

## 快速开始

1. 克隆仓库
2. 构建项目
   ```powershell
   cd Azure-Commitlog-Check
   dotnet build
   ```
3. 运行检查（两种方式）:
   
   a. 自动检测方式（推荐）:
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
   ```
   
   b. 手动指定 PR ID 方式：
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --pr-id 12345
   ```

## 命令参数说明

| 参数        | 描述                             | 是否必须 |
|------------|----------------------------------|---------|
| --url      | Azure DevOps 服务器 URL            | 是      |
| --token    | 个人访问令牌 (PAT)                | 是      |
| --project  | Azure DevOps 项目名称             | 是      |
| --pr-id    | PR ID（如使用--auto-detect 则可选）| 否     |
| --repository | 仓库名称（自动检测时可选）       | 否      |
| --auto-detect | 自动检测当前分支的 PR           | 否      |
| --pattern  | 验证用的正则表达式模式            | 否      |
| --quiet    | 安静模式，仅输出最终结果          | 否      |

## 正则表达式自定义与 PR 合并消息处理

### 默认正则表达式

工具默认使用以下正则表达式验证提交信息：
```
^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}
```

这个表达式要求提交信息符合以下格式：
- 必须以类型开头：feat, fix, docs, style, refactor, perf, test, chore
- 可选的作用域，如 feat(login)
- 冒号和空格后跟 1-50 字符的描述

### 自定义正则表达式

您可以使用`--pattern`参数自定义正则表达式：

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --pr-id 12345 --pattern "^(feat|fix|custom)(\(.+\))?: .+"
```

### 处理自动合并PR消息

如果服务器自动生成的合并消息（如"已合并 PR 123: 标题内容"）与您的提交规范不符，您可以使用宽松的正则表达式来解决：

```powershell
# 允许标准格式或自动合并PR格式
dotnet run -- --auto-detect --pattern "^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .+|^已合并\s+PR\s+\d+:.*"
```

或排除特定的PR检查（仅在特殊情况下使用）：

```powershell
# 如果PR标题匹配自动合并格式则跳过检查
if ($prTitle -match "^已合并\s+PR\s+\d+:") { exit 0 } else { azcommitcheck --auto-detect }
```

## 退出代码

| 代码 | 含义                  | 典型场景                    |
|------|----------------------|----------------------------|
| 0    | 成功                 | 所有提交符合规范            |
| 1    | 无效的提交信息       | 提交信息格式不正确          |
| 2    | PR 不存在             | 指定的 PR ID 不存在           |
| 3    | 无提交记录           | PR 中没有提交记录            |
| 4    | API 错误              | 网络问题/令牌过期           |
| 5    | Git 未安装            | 缺少 Git 环境变量             |
| 6    | 分支无关联 PR         | 分支未与任何 PR 关联          |

## GitHub Flow 和自动发布

本项目使用 GitHub Flow 工作流和 GitHub Actions 实现自动发布：

- **持续集成**：在向 master 分支提交 Pull Request 时自动运行构建和测试
- **自动发布**：当推送版本标签（如 v1.0.0）时，GitHub Actions 会自动：
  - 构建项目
  - 为 Windows、Linux 和 macOS 创建发布包
  - 将打包好的文件发布到 GitHub Release

## 许可证

本项目使用[MIT](LICENSE)许可证
