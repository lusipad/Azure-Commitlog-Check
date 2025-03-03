# PR Commit Checker CI集成指南

本文档提供了将PR Commit Checker工具集成到CI环境的详细说明。

## 集成方式选择

有三种方式可以将PR Commit Checker集成到CI流程中：

1. **使用Azure Pipelines YAML** - 直接在您的管道配置中运行检查
2. **使用Azure DevOps扩展** - 创建并安装自定义任务扩展
3. **作为构建任务运行** - 将工具作为预编译的可执行文件运行

以下是每种方式的详细说明：

## 1. 使用Azure Pipelines YAML（推荐）

这种方法最为简单，只需在您的项目中添加提供的`azure-pipelines.yml`文件，或将相关步骤合并到您现有的管道中。

### 步骤：

1. 将`azure-pipelines.yml`文件添加到您的代码仓库中
2. 在Azure DevOps中创建一个基于此YAML文件的新管道
3. 确保管道有足够的权限访问Azure DevOps API

在管道设置中，您需要：
- 勾选"允许脚本访问OAuth令牌"选项（在Agent job设置的"Additional options"下）

### 示例集成：

```yaml
# 在现有管道中添加PR检查阶段
stages:
- stage: CheckPR
  condition: and(succeeded(), eq(variables['Build.Reason'], 'PullRequest'))
  jobs:
  - job: ValidateCommitMessage
    steps:
    - task: PowerShell@2
      displayName: 'Check PR Commit Message'
      inputs:
        targetType: 'inline'
        script: |
          $token = "$(System.AccessToken)"
          $organization = "$(System.TeamFoundationCollectionUri)"
          $project = "$(System.TeamProject)"
          
          # 替换为您的PR Commit Checker工具路径
          $toolPath = "$(Pipeline.Workspace)/path/to/PRCommitChecker.exe"
          
          # 使用自动检测功能，无需手动传递PR ID
          $result = & $toolPath --url "$organization" --token "$token" --project "$project" --auto-detect
          
          if ($LASTEXITCODE -ne 0) {
            Write-Host "##vso[task.logissue type=error]PR commit message does not meet standards."
            Write-Host "##vso[task.complete result=Failed;]"
          }
      env:
        SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

## 2. 创建Azure DevOps扩展

如果您希望为团队提供更直观的体验，可以将工具打包为Azure DevOps扩展。

### 步骤：

1. 构建工具：
   ```powershell
   dotnet publish -c Release -r win-x64 -o ./bin/extension/task
   ```

2. 复制`extension`文件夹中的文件到发布目录

3. 安装TFS跨平台命令行界面(tfx-cli)：
   ```powershell
   npm install -g tfx-cli
   ```

4. 创建扩展包：
   ```powershell
   cd ./bin/extension
   tfx extension create --manifest-globs vss-extension.json
   ```

5. 在Azure DevOps Marketplace中发布扩展，或在您的组织中安装私有扩展

### 使用扩展：

安装扩展后，您可以在管道中添加"PR Commit Message Checker"任务：

```yaml
steps:
- task: PRCommitChecker@1
  displayName: 'Check PR Commit Message'
  inputs:
    pattern: '^(feat|fix|docs)(\(.+\))?: .{1,50}'
    autoDetect: true  # 启用自动检测功能
```

## 3. 作为构建任务运行

如果您不想创建扩展，也可以将预编译的工具放置在共享位置，然后在CI过程中运行它。

### 步骤：

1. 构建工具：
   ```powershell
   dotnet publish -c Release -r win-x64 -o ./bin/Release/PRCommitChecker
   ```

2. 复制生成的可执行文件到共享位置

3. 在您的CI脚本中调用工具：

```powershell
# 使用PowerShell脚本调用工具
$toolPath = "\\shared\path\to\PRCommitChecker.exe"

# 使用自动检测功能
& $toolPath --url "https://dev.azure.com/your-org" --token "$(System.AccessToken)" --project "your-project" --auto-detect

if ($LASTEXITCODE -ne 0) {
    Write-Error "PR commit message does not meet standards."
    exit 1
}
```

## 在CI环境中使用自动检测功能的注意事项

当在CI环境中使用自动检测功能时，需要注意以下几点：

1. **CI代理需要访问Git仓库**：确保CI代理可以执行Git命令并能访问仓库历史记录。

2. **仓库检出深度**：如果使用浅克隆（shallow clone），请确保足够的历史记录被检出，以便工具能正确获取分支信息。在Azure Pipelines中，可以通过以下设置增加检出深度：

   ```yaml
   - checkout: self
     fetchDepth: 0  # 完整检出所有历史记录
   ```

3. **环境变量**：CI环境通常已经设置了Azure DevOps相关的环境变量，您可以直接使用这些变量：

   ```powershell
   $organization = $env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI
   $project = $env:SYSTEM_TEAMPROJECT
   $repository = $env:BUILD_REPOSITORY_NAME
   ```

4. **权限问题**：确保使用的PAT令牌或系统访问令牌有足够的权限查询PR信息。

## 将检查结果作为PR策略

您可以将commit消息检查设置为PR合并策略的一部分，确保只有符合规范的PR才能被合并。

在Azure DevOps中：

1. 转到项目设置 > Repositories > 您的仓库 > Policies > Branch policies
2. 选择需要保护的分支（例如main或master）
3. 添加新的Build policy
4. 选择包含PR Commit检查的构建管道
5. 确保选中"Required"选项
6. 保存策略

这样，每个PR在合并前都必须通过commit消息规范检查。

## 退出代码处理

工具现在返回更详细的退出代码，您可以在CI脚本中根据不同的退出代码进行不同的处理：

```powershell
# 根据退出代码进行不同处理
$exitCode = $LASTEXITCODE
switch ($exitCode) {
    0 { 
        Write-Host "Commit message 符合规范标准"
    }
    1 { 
        Write-Error "Commit message 不符合规范标准"
        exit 1
    }
    2 { 
        Write-Error "找不到指定的PR"
        exit 1
    }
    3 { 
        Write-Warning "PR没有提交记录"
        # 您可以决定这种情况是否应该导致构建失败
    }
    4 { 
        Write-Error "访问Azure DevOps API时发生错误"
        exit 1
    }
    5 { 
        Write-Error "找不到Git命令"
        exit 1
    }
    6 { 
        Write-Error "没有为当前分支找到活跃的PR"
        exit 1
    }
    default { 
        Write-Error "未知错误，退出代码: $exitCode"
        exit 1
    }
}
```

## 自定义规范

您可以通过修改正则表达式模式来自定义commit消息规范。默认模式检查常见的语义化提交格式：

```
^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}
```

如果您有不同的规范需求，例如包含JIRA工单号，可以相应修改模式：

```
^(PROJ-\d+):\s.{5,50}
```

在CI配置中通过`--pattern`参数（或扩展的pattern输入）指定您的自定义模式。
