# Azure-Commitlog-Check

[English](README.md) | [中文](README.zh-CN.md)

一个用于检查Azure DevOps(TFS)中Pull Request的commit-log是否符合规范的工具，特别适用于使用squash提交方式的PR。

## 主要功能

- 连接到Azure DevOps(TFS)服务
- 获取指定PR的详细信息
- 检查PR标题是否符合commit message规范
- 支持自定义commit message规范的正则表达式模式
- **新功能**: 自动检测当前分支的PR，无需手动指定PR ID

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
3. 运行检查
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
   ```

## 详细文档

详细的使用说明和CI集成指南，请参考：

- [使用说明](Azure-Commitlog-Check/README.md)
- [CI集成指南](Azure-Commitlog-Check/CI_INTEGRATION_GUIDE.md)
- [发布指南](RELEASE.md)

## GitHub Flow和自动发布

本项目使用GitHub Flow工作流和GitHub Actions实现自动发布：

- **持续集成**：在向main分支提交Pull Request时自动运行构建和测试
- **自动发布**：当推送版本标签（如v1.0.0）时，GitHub Actions会自动：
  - 构建项目
  - 为Windows、Linux和macOS创建发布包
  - 将打包好的文件发布到GitHub Release

关于如何发布新版本，请查看[发布指南](RELEASE.md)了解详情。

## 许可证

本项目采用 [MIT](LICENSE) 许可证
