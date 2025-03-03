[CmdletBinding()]
param()

Trace-VstsEnteringInvocation $MyInvocation

try {
    # 获取任务输入
    $pattern = Get-VstsInput -Name 'pattern' -Default '^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .{1,50}'
    $autoDetect = Get-VstsInput -Name 'autoDetect' -AsBool -Default $true
    $repository = Get-VstsInput -Name 'repository' -Default ''
    $pullRequestId = Get-VstsInput -Name 'pullRequestId' -Default ''
    
    # 获取系统变量
    $token = $env:SYSTEM_ACCESSTOKEN
    if (-not $token) {
        Write-Host "##vso[task.logissue type=error]No access token available. Make sure to enable 'Allow scripts to access OAuth token'."
        exit 1
    }
    
    $organization = $env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI
    $project = $env:SYSTEM_TEAMPROJECT
    
    if (-not $autoDetect -and -not $pullRequestId) {
        $prId = $env:SYSTEM_PULLREQUEST_PULLREQUESTID
        if (-not $prId) {
            Write-Host "##vso[task.logissue type=warning]No PR ID specified and auto-detect is disabled. Unable to proceed."
            exit 1
        } else {
            $pullRequestId = $prId
        }
    }
    
    # 获取工具路径 - 假设工具已经被发布到代理上的特定位置
    # 对于实际使用，您可能需要将工具包含在扩展中，或从特定位置下载
    $toolPath = Join-Path -Path $PSScriptRoot -ChildPath "Azure-Commitlog-Check.exe"
    
    if (-not (Test-Path $toolPath)) {
        Write-Host "##vso[task.logissue type=error]Could not find Azure Commitlog Check tool at $toolPath"
        exit 1
    }
    
    # 构建命令行参数
    $arguments = "--url `"$organization`" --token `"$token`" --project `"$project`" --quiet"
    
    if ($autoDetect) {
        Write-Host "使用自动检测功能查找当前分支的PR"
        $arguments += " --auto-detect"
        
        if (-not [string]::IsNullOrEmpty($repository)) {
            $arguments += " --repository `"$repository`""
        } elseif (-not [string]::IsNullOrEmpty($env:BUILD_REPOSITORY_NAME)) {
            # 如果未指定仓库名称，尝试使用环境变量
            $arguments += " --repository `"$env:BUILD_REPOSITORY_NAME`""
        }
    } else {
        Write-Host "检查PR #$pullRequestId的commit message"
        $arguments += " --pr-id $pullRequestId"
    }
    
    if (-not [string]::IsNullOrEmpty($pattern)) {
        $arguments += " --pattern `"$pattern`""
    }
    
    Write-Host "执行: $toolPath $arguments"
    
    # 运行工具
    $output = & $toolPath $arguments.Split(" ")
    $exitCode = $LASTEXITCODE
    
    # 显示工具输出
    $output | ForEach-Object { Write-Host $_ }
    
    # 处理退出代码
    switch ($exitCode) {
        0 { 
            Write-Host "##vso[task.logissue type=warning]PR commit message 符合规范标准"
            Write-Host "##vso[task.complete result=Succeeded;]" 
        }
        1 { 
            Write-Host "##vso[task.logissue type=error]PR commit message 不符合规范标准"
            Write-Host "##vso[task.complete result=Failed;]"
        }
        2 { 
            Write-Host "##vso[task.logissue type=error]找不到指定的PR"
            Write-Host "##vso[task.complete result=Failed;]"
        }
        3 { 
            Write-Host "##vso[task.logissue type=warning]PR没有提交记录"
            Write-Host "##vso[task.complete result=SucceededWithIssues;]"
        }
        4 { 
            Write-Host "##vso[task.logissue type=error]访问Azure DevOps API时发生错误"
            Write-Host "##vso[task.complete result=Failed;]"
        }
        5 { 
            Write-Host "##vso[task.logissue type=error]找不到Git命令，无法自动检测PR"
            Write-Host "##vso[task.complete result=Failed;]"
        }
        6 { 
            Write-Host "##vso[task.logissue type=error]当前分支没有活跃的PR"
            Write-Host "##vso[task.complete result=Failed;]"
        }
        default { 
            Write-Host "##vso[task.logissue type=error]未知错误，退出代码: $exitCode"
            Write-Host "##vso[task.complete result=Failed;]"
        }
    }
    
    exit $exitCode
} finally {
    Trace-VstsLeavingInvocation $MyInvocation
}
