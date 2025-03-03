# 版本发布指南

本项目使用GitHub Flow工作流程和自动化发布流程。以下是发布新版本的步骤：

## 准备发布

1. 确保所有需要包含在发布中的更改已经合并到`master`分支
2. 在本地更新`master`分支：
   ```powershell
   git checkout master
   git pull
   ```

## 更新版本号

1. 在`Azure-Commitlog-Check/Azure-Commitlog-Check.csproj`文件中更新版本号：
   ```xml
   <Version>x.y.z</Version>
   ```
   其中：
   - x: 主版本号（不兼容的API更改）
   - y: 次版本号（向后兼容的功能添加）
   - z: 修订号（向后兼容的问题修复）

2. 提交版本更新：
   ```powershell
   git add Azure-Commitlog-Check/Azure-Commitlog-Check.csproj
   git commit -m "chore: 更新版本到 x.y.z"
   git push
   ```

## 创建发布标签

1. 创建版本标签：
   ```powershell
   git tag -a vx.y.z -m "发布版本 x.y.z"
   ```

2. 推送标签到GitHub：
   ```powershell
   git push origin vx.y.z
   ```

3. 推送标签后，GitHub Actions将自动：
   - 构建项目
   - 为Windows、Linux和macOS创建发布包
   - 创建GitHub Release
   - 上传构建好的发布包

## 检查发布状态

1. 在GitHub仓库中，导航到`Actions`标签查看工作流程运行状态
2. 成功完成后，在`Releases`部分可以找到新创建的发布

## 发布后

1. 更新项目文档，如有必要
2. 通知用户新版本已经发布
3. 收集用户反馈

## 版本命名规范

我们遵循[语义化版本 2.0.0](https://semver.org/lang/zh-CN/)规范：

- **主版本号**：当你做了不兼容的 API 更改
- **次版本号**：当你做了向下兼容的功能性新增
- **修订号**：当你做了向下兼容的问题修正
