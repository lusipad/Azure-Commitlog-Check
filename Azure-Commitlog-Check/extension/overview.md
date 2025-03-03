# Azure Commitlog Check

这个扩展可以帮助您验证 Azure DevOps 中的 Pull Request 提交信息是否符合规范，特别适用于使用 squash 合并方式的团队。

## 主要功能

- **自动检测 PR**：无需手动指定 PR ID，可自动检测当前分支的 PR
- **自定义验证规则**：支持使用正则表达式自定义提交信息验证规则
- **集成到 CI/CD 流程**：轻松集成到您的 Azure Pipeline 中
- **支持构建验证策略**：可用作 PR 的构建验证策略，确保所有合并的代码都符合提交信息规范

## 快速开始

1. 在您的 Azure DevOps 组织中安装此扩展
2. 在您的 Pipeline YAML 文件中添加任务：

```yaml
# azure-pipelines.yml 示例
trigger:
- main
- feature/*

pool:
  vmImage: 'windows-latest'

steps:
- checkout: self
  fetchDepth: 0  # PR 自动检测正常工作所必需
  
# 确保为此任务启用 OAuth 令牌访问
- task: AzureCommitlogCheck@1
  inputs:
    autoDetect: true  # 自动检测当前分支的 PR
    # repository: 'MyRepository'  # 可选：如果自动检测有问题，请指定
    pattern: '^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}'  # 可选：自定义模式
  env:
    SYSTEM_ACCESSTOKEN: $(System.AccessToken)  # API 访问所需
```

## 配置选项

| 输入参数 | 描述 | 默认值 |
|----------|------|--------|
| autoDetect | 自动检测当前分支的 PR | true |
| pullRequestId | 手动 PR ID（如果 autoDetect=true 则忽略） | |
| repository | 用于自动检测的仓库名称 | 从环境变量自动检测 |
| pattern | 验证用的正则表达式模式 | 标准模式 |

## 注意事项

- 确保在管道设置中启用了"允许脚本访问 OAuth 令牌"选项
- 使用自动检测 PR 功能时，需要设置 checkout 步骤的 fetchDepth 为 0

## 支持

如果您遇到任何问题或有改进建议，请在我们的[GitHub 仓库](https://github.com/lusipad/Azure-Commitlog-Check/issues)提交 issue。
