using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Azure_Commitlog_Check
{
    class Program
    {
        // 定义退出代码
        private const int SUCCESS = 0;
        private const int INVALID_COMMIT_MESSAGE = 1;
        private const int PR_NOT_FOUND = 2;
        private const int NO_COMMITS = 3;
        private const int API_ERROR = 4;
        private const int GIT_NOT_FOUND = 5;
        private const int NO_PR_FOR_BRANCH = 6;

        static async Task<int> Main(string[] args)
        {
            // 创建命令行参数
            var rootCommand = new RootCommand("检查PR的commit-log是否符合规范");

            var urlOption = new Option<string>(
                name: "--url",
                description: "Azure DevOps 服务器URL (例如: https://dev.azure.com/organization)"
            )
            { IsRequired = true };

            var tokenOption = new Option<string>(
                name: "--token",
                description: "Azure DevOps 个人访问令牌(PAT)"
            )
            { IsRequired = true };

            var projectOption = new Option<string>(
                name: "--project",
                description: "Azure DevOps 项目名称"
            )
            { IsRequired = true };

            var prIdOption = new Option<int?>(
                name: "--pr-id",
                description: "PR ID (如果不指定，将尝试自动检测当前分支的PR)"
            )
            { IsRequired = false };

            var repositoryOption = new Option<string>(
                name: "--repository",
                description: "Azure DevOps 仓库名称 (自动检测PR时需要)"
            );

            var autoDetectOption = new Option<bool>(
                name: "--auto-detect",
                description: "自动检测当前分支的PR",
                getDefaultValue: () => false
            );

            var patternOption = new Option<string>(
                name: "--pattern",
                description: "Commit log 规范的正则表达式模式",
                getDefaultValue: () => @"^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}"
            );

            var quietOption = new Option<bool>(
                name: "--quiet",
                description: "安静模式，只输出最终结果",
                getDefaultValue: () => false
            );

            rootCommand.AddOption(urlOption);
            rootCommand.AddOption(tokenOption);
            rootCommand.AddOption(projectOption);
            rootCommand.AddOption(prIdOption);
            rootCommand.AddOption(repositoryOption);
            rootCommand.AddOption(autoDetectOption);
            rootCommand.AddOption(patternOption);
            rootCommand.AddOption(quietOption);

            rootCommand.SetHandler(async (string url, string token, string project, int? prId, string repository, bool autoDetect, string pattern, bool quiet) =>
            {
                int exitCode;
                
                if (prId.HasValue)
                {
                    // 使用指定的PR ID
                    exitCode = await CheckPullRequestCommitLog(url, token, project, prId.Value, pattern, quiet);
                }
                else if (autoDetect)
                {
                    // 自动检测PR ID
                    exitCode = await AutoDetectAndCheckPR(url, token, project, repository, pattern, quiet);
                }
                else
                {
                    Console.WriteLine("错误: 必须指定PR ID (--pr-id) 或启用自动检测 (--auto-detect)");
                    exitCode = 1;
                }
                
                Environment.ExitCode = exitCode;
            }, urlOption, tokenOption, projectOption, prIdOption, repositoryOption, autoDetectOption, patternOption, quietOption);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> AutoDetectAndCheckPR(string url, string token, string project, string repository, string pattern, bool quiet)
        {
            try
            {
                if (!quiet)
                {
                    Console.WriteLine("正在自动检测当前分支的PR...");
                }

                // 检查git是否可用
                if (!IsGitAvailable())
                {
                    Console.WriteLine("错误: 无法找到git命令。请确保git已安装并添加到PATH中。");
                    return GIT_NOT_FOUND;
                }

                // 获取当前分支名称
                string branchName = GetCurrentBranchName();
                if (string.IsNullOrEmpty(branchName))
                {
                    Console.WriteLine("错误: 无法获取当前分支名称。");
                    return API_ERROR;
                }

                if (!quiet)
                {
                    Console.WriteLine($"当前分支: {branchName}");
                }

                // 如果没有提供仓库名称，尝试获取
                if (string.IsNullOrEmpty(repository))
                {
                    repository = GetRepositoryName();
                    if (string.IsNullOrEmpty(repository))
                    {
                        Console.WriteLine("错误: 无法自动获取仓库名称。请使用 --repository 参数指定仓库名称。");
                        return API_ERROR;
                    }
                    
                    if (!quiet)
                    {
                        Console.WriteLine($"检测到仓库: {repository}");
                    }
                }

                // 连接到Azure DevOps
                var credentials = new VssBasicCredential(string.Empty, token);
                var connection = new VssConnection(new Uri(url), credentials);
                var gitClient = connection.GetClient<GitHttpClient>();

                // 获取仓库ID
                var repos = await gitClient.GetRepositoriesAsync(project);
                var repo = repos.FirstOrDefault(r => r.Name.Equals(repository, StringComparison.OrdinalIgnoreCase));
                if (repo == null)
                {
                    Console.WriteLine($"错误: 在项目 '{project}' 中找不到名为 '{repository}' 的仓库。");
                    return API_ERROR;
                }

                // 查找分支对应的PR
                var prs = await gitClient.GetPullRequestsAsync(
                    repo.Id,
                    new GitPullRequestSearchCriteria
                    {
                        SourceRefName = $"refs/heads/{branchName}",
                        Status = PullRequestStatus.Active
                    }
                );

                if (prs.Count == 0)
                {
                    Console.WriteLine($"错误: 没有找到分支 '{branchName}' 的活跃PR。");
                    return NO_PR_FOR_BRANCH;
                }

                // 使用第一个匹配的PR
                var pr = prs[0];
                if (!quiet)
                {
                    Console.WriteLine($"找到PR #{pr.PullRequestId}: {pr.Title}");
                }

                // 使用找到的PR ID进行检查
                return await CheckPullRequestCommitLog(url, token, project, pr.PullRequestId, pattern, quiet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"自动检测PR失败: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
                return API_ERROR;
            }
        }

        static bool IsGitAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        static string GetCurrentBranchName()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && output != "HEAD")
                {
                    return output;
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        static string GetRepositoryName()
        {
            try
            {
                // 尝试从远程URL获取仓库名称
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "remote get-url origin",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // 尝试从URL提取仓库名称
                    // 支持以下格式:
                    // https://dev.azure.com/org/project/_git/repository
                    // git@ssh.dev.azure.com:v3/org/project/repository
                    
                    // 检查HTTPS格式
                    var httpsMatch = Regex.Match(output, @"https://.*?/_git/([^/]+)$");
                    if (httpsMatch.Success)
                    {
                        return httpsMatch.Groups[1].Value;
                    }
                    
                    // 检查SSH格式
                    var sshMatch = Regex.Match(output, @"[^/]+/([^/]+)$");
                    if (sshMatch.Success)
                    {
                        return sshMatch.Groups[1].Value;
                    }
                    
                    // 尝试获取当前目录名称作为仓库名称
                    string currentDir = Path.GetFileName(Directory.GetCurrentDirectory());
                    if (!string.IsNullOrEmpty(currentDir))
                    {
                        return currentDir;
                    }
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        static async Task<int> CheckPullRequestCommitLog(string url, string token, string project, int prId, string pattern, bool quiet)
        {
            try
            {
                if (!quiet)
                {
                    Console.WriteLine($"正在连接到 {url}...");
                    Console.WriteLine($"项目: {project}");
                    Console.WriteLine($"PR ID: {prId}");
                    Console.WriteLine($"规范模式: {pattern}");
                    Console.WriteLine();
                }

                // 创建连接
                var credentials = new VssBasicCredential(string.Empty, token);
                var connection = new VssConnection(new Uri(url), credentials);

                // 获取 Git 客户端
                var gitClient = connection.GetClient<GitHttpClient>();

                // 获取 PR 详细信息
                var pullRequest = await gitClient.GetPullRequestByIdAsync(prId);
                if (pullRequest == null)
                {
                    Console.WriteLine($"找不到 ID 为 {prId} 的 PR");
                    return PR_NOT_FOUND;
                }

                if (!quiet)
                {
                    Console.WriteLine($"PR 标题: {pullRequest.Title}");
                    Console.WriteLine($"状态: {pullRequest.Status}");
                    Console.WriteLine($"创建者: {pullRequest.CreatedBy.DisplayName}");
                    Console.WriteLine();
                }

                // 获取 PR 的提交信息
                var repository = pullRequest.Repository;
                var commits = await gitClient.GetPullRequestCommitsAsync(
                    repository.Id,
                    pullRequest.PullRequestId);

                if (commits.Count == 0)
                {
                    Console.WriteLine("此 PR 没有提交记录");
                    return NO_COMMITS;
                }

                // 对于 squash 合并，我们主要关注 PR 的标题和描述
                if (!quiet)
                {
                    Console.WriteLine("由于 PR 使用 squash 提交方式，我们将检查 PR 的标题作为 commit-log");
                }
                var commitTitle = pullRequest.Title;

                // 检查 commit 标题是否符合规范
                var regex = new Regex(pattern);
                var isValid = regex.IsMatch(commitTitle);

                Console.WriteLine($"Commit 标题: {commitTitle}");
                Console.WriteLine($"符合规范: {(isValid ? "是" : "否")}");

                if (!isValid)
                {
                    Console.WriteLine($"不符合规范模式: {pattern}");
                    Console.WriteLine("规范示例: feat(模块): 添加了新功能");
                    Console.WriteLine("支持的类型: feat, fix, docs, style, refactor, perf, test, chore");
                    return INVALID_COMMIT_MESSAGE;
                }

                return SUCCESS;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
                return API_ERROR;
            }
        }
    }
}
