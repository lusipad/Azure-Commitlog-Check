# Azure-Commitlog-Check

[English](README.md) | [中文](README.zh-CN.md)

A tool for checking whether commit messages in Azure DevOps (TFS) Pull Requests comply with standards, especially suitable for PRs using squash commit method.

## Key Features

- Connect to Azure DevOps (TFS) services
- Retrieve detailed information about specific PRs
- Check if PR titles conform to commit message standards
- Support custom commit message validation with regex patterns
- Automatically detect the PR of the current branch without manual PR ID specification

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
3. Run the check (two options):
   
   a. With auto-detection (recommended):
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --auto-detect
   ```
   
   b. With manual PR ID:
   ```powershell
   dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --pr-id 12345
   ```

## Command Parameters

| Parameter    | Description                                      | Required |
|--------------|--------------------------------------------------|----------|
| --url        | Azure DevOps server URL                          | Yes      |
| --token      | Personal Access Token (PAT)                      | Yes      |
| --project    | Azure DevOps project name                        | Yes      |
| --pr-id      | Pull Request ID (optional if --auto-detect used) | No       |
| --repository | Repository name (for auto-detection)             | No       |
| --auto-detect| Auto-detect PR of current branch                 | No       |
| --pattern    | Regex pattern for validation                     | No       |
| --quiet      | Only output final result                         | No       |

## Regular Expression Customization and PR Merge Message Handling

### Default Regular Expression

The tool uses the following regex pattern by default:
```
^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}
```

This pattern requires commit messages to follow this format:
- Must start with a type: feat, fix, docs, style, refactor, perf, test, chore
- Optional scope in parentheses, like feat(login)
- Colon and space followed by 1-50 characters description

### Customizing Regular Expression

You can customize the regex pattern using the `--pattern` parameter:

```powershell
dotnet run -- --url "https://dev.azure.com/yourorganization" --token "your-pat-token" --project "your-project" --pr-id 12345 --pattern "^(feat|fix|custom)(\(.+\))?: .+"
```

### Handling Automatic PR Merge Messages

If server auto-generated merge messages (like "Merged PR 123: Title content") don't comply with your commit standards, you can use a more permissive regex pattern:

```powershell
# Allow standard format or auto-merge PR format
dotnet run -- --auto-detect --pattern "^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .+|^Merged\s+PR\s+\d+:.*"
```

Or exclude specific PR checks (use only in special cases):

```powershell
# Skip check if PR title matches auto-merge format
if ($prTitle -match "^Merged\s+PR\s+\d+:") { exit 0 } else { azcommitcheck --auto-detect }
```

## Exit Codes

| Code | Meaning                   | Typical Scenario                    |
|------|---------------------------|-------------------------------------|
| 0    | Success                   | All commits meet standards          |
| 1    | Invalid Commit Message    | Commit message format is incorrect  |
| 2    | PR Not Found              | Specified PR ID doesn't exist       |
| 3    | No Commits                | PR has no commit records            |
| 4    | API Error                 | Network issues/token expired        |
| 5    | Git Not Found             | Missing Git environment variables   |
| 6    | No PR For Branch          | Branch not associated with any PR   |

## Documentation

### Release Guide

For release instructions, please refer to [Release Guide](RELEASE.md).

## GitHub Flow & Automated Release

This project uses GitHub Flow and automated releases through GitHub Actions:

- **Continuous Integration**: Automatically runs build and tests when Pull Requests are submitted to the master branch
- **Automated Release**: When version tags (e.g., v1.0.0) are pushed, GitHub Actions will automatically:
  - Build the project
  - Create release packages for Windows, Linux, and macOS
  - Publish a new GitHub Release with the packaged files

## License

This project is licensed under the [MIT](LICENSE) License
