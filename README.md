# Azure-Commitlog-Check

[English](README.md) | [中文](README.zh-CN.md)

A tool for checking whether commit messages in Azure DevOps (TFS) Pull Requests comply with standards, especially suitable for PRs using squash commit method.

## Key Features

- Connect to Azure DevOps (TFS) services
- Retrieve detailed information about specific PRs
- Check if PR titles conform to commit message standards
- Support custom commit message validation with regex patterns
- **New Feature**: Automatically detect the PR of the current branch without manual PR ID specification

## Tech Stack

- .NET 8.0
- Azure DevOps API
- System.CommandLine (command-line parsing)

## Quick Start

1. Clone the repository
2. Build the project
   ```powershell
   cd Azure-Commitlog-Check
   dotnet build
   ```
3. Run the check
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
   ```

## Detailed Documentation

For detailed instructions and CI integration guides, please refer to:

- [User Guide](Azure-Commitlog-Check/README.md)
- [CI Integration Guide](Azure-Commitlog-Check/CI_INTEGRATION_GUIDE.md)

## License

This project is licensed under the [MIT](LICENSE) License
