# PRCommitChecker

一个用于检查Azure DevOps(TFS)中Pull Request的commit-log是否符合规范的工具，特别适用于使用squash提交方式的PR。

## 主要功能

- 连接到Azure DevOps(TFS)服务
- 获取指定PR的详细信息
- 检查PR标题是否符合commit message规范
- 支持自定义commit message规范的正则表达式模式
- 自动检测当前分支的PR，无需手动指定PR ID

## 技术栈

- .NET 8.0
- Azure DevOps API 
- System.CommandLine (命令行解析)

## 快速开始

1. 克隆仓库
2. 构建项目
   ```powershell
   cd PRCommitChecker
   dotnet build
   ```
3. 运行检查
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
   ```

## 详细文档

详细的使用说明和CI集成指南，请参考：

- [使用说明](PRCommitChecker/README.md)
- [CI集成指南](PRCommitChecker/CI_INTEGRATION_GUIDE.md)

## 许可证

本项目采用 [MIT](LICENSE) 许可证
