# PR Commit Log 规范检查工具

这是一个用于检查Azure DevOps(TFS)中Pull Request的commit-log是否符合规范的工具，特别适用于使用squash提交方式的PR。

## 功能特点

- 连接到Azure DevOps(TFS)服务
- 获取指定PR的详细信息
- 检查PR标题是否符合commit message规范
- 支持自定义commit message规范的正则表达式模式
- **新功能**: 自动检测当前分支的PR，无需手动指定PR ID

## 前提条件

- .NET 8.0或更高版本
- Azure DevOps个人访问令牌(PAT)，需要有读取PR的权限
- Git命令行工具（用于自动检测PR时）

## 安装

```powershell
dotnet build
```

## 使用方法

### 方法1: 指定PR ID

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --pr-id 123
```

### 方法2: 自动检测当前分支的PR

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
```

如果工具无法自动确定仓库名称，您可以手动指定：

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect --repository "your-repository"
```

### 命令行参数

- `--url`: Azure DevOps服务器URL，例如`https://dev.azure.com/organization`
- `--token`: Azure DevOps个人访问令牌(PAT)
- `--project`: Azure DevOps项目名称
- `--pr-id`: (可选) Pull Request ID。如果不指定，必须使用`--auto-detect`
- `--auto-detect`: (可选) 自动检测当前分支的PR ID
- `--repository`: (可选) 当使用`--auto-detect`且无法自动确定仓库名称时需要指定
- `--pattern`: (可选) commit log规范的正则表达式模式，默认为`^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}`
- `--quiet`: (可选) 安静模式，减少输出信息，只显示最终结果

## 自动检测PR ID的工作原理

自动检测功能通过以下步骤工作：

1. 获取当前Git分支名称
2. 确定仓库名称（从Git远程URL或目录名称）
3. 查询Azure DevOps API查找当前分支的活跃PR
4. 如果找到匹配的PR，自动检查其commit message

注意：此功能要求：
- 工具在Git仓库目录内运行
- 存在远程仓库配置
- 当前分支已经创建了PR

## 默认Commit规范

默认的commit规范遵循以下格式：
```
<类型>(<范围>): <描述>
```

其中：
- 类型：必须是以下之一：feat, fix, docs, style, refactor, perf, test, chore
- 范围：可选，表示影响的模块或组件
- 描述：提交的简短描述，不超过50个字符

示例：
- `feat(auth): 添加用户登录功能`
- `fix(api): 修复数据不一致问题`
- `docs: 更新README文档`

## 自定义规范

可以通过`--pattern`参数自定义commit规范的正则表达式模式：

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect --pattern "^(JIRA-\d+|feat|fix):.+"
```

## 退出代码

工具会返回以下退出代码：

- 0: 成功（commit message符合规范）
- 1: 不符合commit规范
- 2: 找不到指定的PR
- 3: PR没有提交记录
- 4: API错误
- 5: 找不到Git命令（自动检测时）
- 6: 没有为当前分支找到活跃的PR

## 许可证

MIT
