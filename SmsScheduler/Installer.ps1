
function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$installFolder = "c:\SmsServices"
$path = Get-ScriptDirectory Installer.ps1
$build_output = (Get-Item $path).parent.FullName + '\build_output\'
$go_environment = (get-item env:GO_ENVIRONMENT_NAME).Value

function InstallEndpoints
{
	if ([System.IO.Directory]::Exists($installFolder))
	{
	 [System.IO.Directory]::Delete($installFolder, 1)
	}
	[System.IO.Directory]::CreateDirectory($installFolder)

	# move all the service files that were built to the output folder
	[System.IO.Directory]::Move($build_output + 'EmailSender', $installFolder + '\EmailSender')
	[System.IO.Directory]::Move($build_output + 'SmsCoordinator', $installFolder + '\SmsCoordinator')
	[System.IO.Directory]::Move($build_output + 'SmsTracking', $installFolder + '\SmsTracking')

	$nsbHost = Join-Path $installFolder -childpath  '\EmailSender\NServiceBus.Host.exe'
	#& $nsbHost ("/install", "/serviceName:SmsEmailSender", "/displayName:Sms Email Sender", "/description:Service for sending emails from Sms Coordinator", "NServiceBus.Production")
    $argList = '/install /serviceName:SmsEmailSender /displayName:"Sms Email Sender" /description:"Service for sending emails from Sms Coordinator" NServiceBus.Production'
    #$processInfo = Start-Process -Wait -NoNewWindow -FilePath $nsbHost -ArgumentList $argList
    RunCommand $nsbHost $argList
    Start-Service SmsEmailSender -ErrorVariable error
    if (!($error -eq $null))
    {
        throw "Exception starting SmsEmailSender service: $error"
    }

	$nsbHost = Join-Path $installFolder -childpath '\SmsCoordinator\NServiceBus.Host.exe'
	$argList = '/install /serviceName:SmsCoordinator /displayName:"Sms Coordinator" /description:"Service for coordinating and sending Sms" NServiceBus.Production'
	RunCommand $nsbHost $argList
    Start-Service SmsCoordinator -ErrorVariable error
    if (!($error -eq $null))
    {
        throw "Exception starting SmsEmailSender service: $error"
    }

	$nsbHost = Join-Path $installFolder -childpath '\SmsTracking\NServiceBus.Host.exe'
	$argList = '/install /serviceName:SmsTracking displayName:"Sms Tracking"  description:"Service for tracking status of coordinated and Sms" NServiceBus.Production'
    RunCommand $nsbHost $argList
    Start-Service SmsTracking -ErrorVariable error
    if (!($error -eq $null))
    {
        throw "Exception starting SmsEmailSender service: $error"
    }
}

function InstallWeb
{
    $msDeploy = "C:\Program Files (x86)\IIS\Microsoft Web Deploy V2\msdeploy.exe"
    $webDeployPackage = Join-Path $build_output -childpath '\SmsWeb.zip'
    
    $environmentParametersFile
    if ($go_environment -ne $null)
    {
        Write-Host "Go environment set to $go_environment, copying appropriate web.config"
        $environmentConfig = $build_output + '\Configuration\' + $go_environment + '.SmsWeb.SetParameters.xml'
        $environmentParametersFile = $build_output + 'SmsWeb.SetParameters.xml'
        Copy-Item $environmentConfig $environmentParametersFile
    }
    else
    {
        Write-Host "No Go environment set in $go_environment, leaving default web.config"
    }
    
    echo $webDeployPackage
    $arg = " -verb:sync -source:package='$webDeployPackage' -dest:auto -verbose -setParamFile=""$environmentParametersFile"""
    
    echo $arg
    RunCommand $msdeploy $arg
}

function RunCommand([string]$fileName, [string]$arg)
{
    #echo "Filename: $fileName"
    #echo "Arg: $arg"
    $ps = new-object System.Diagnostics.Process
    $ps.StartInfo.Filename = $fileName
    $ps.StartInfo.Arguments = $arg
    $ps.StartInfo.RedirectStandardOutput = $True
    $ps.StartInfo.RedirectStandardError = $True
    $ps.StartInfo.UseShellExecute = $false
    #echo $ps.StartInfo
    $null = $ps.Start()
    #$null = $ps.WaitForExit()
    [string] $Out = $ps.StandardOutput.ReadToEnd();
    [string] $Err = $ps.StandardError.ReadToEnd();
    $exitCode = $ps.ExitCode
    $exitCode
    $Out
    $Err
    if (!($exitCode -eq 0))
	{
		throw ("Errors Running Command" + $fileName + $arg + "`r`n" + $Err)
	}
    return
}

function SetupInfrastructure
{
    $nserviceBusCore = Join-Path $build_output -childpath "SmsCoordinator\NServiceBus.Core.dll"
	Import-Module $nserviceBusCore
	Install-Dtc
	Install-Msmq
	Install-RavenDB
	Install-PerformanceCounters
}

function Build
{
	$clean = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /t:Clean"
	$build = $msbuild + " " + $path + "\SmsScheduler.sln /p:Configuration=Release /p:VisualStudioVersion=11.0 /t:Build"
    $webPackage = $msbuild + " " + $path + "\SmsWeb\SmsWeb.csproj /p:Configuration=Release /t:Package"

	Invoke-Expression $clean
	Invoke-Expression $build
    Invoke-Expression $webPackage
    $installFiles = Join-Path $path -childpath '\Install*.*'
    $configFiles = Join-Path $path -childpath '\Configuration\*.*'
    $configDestination = Join-Path $build_output -childpath '\Configuration'
    
    if ([System.IO.Directory]::Exists($configDestination))
	{
	 [System.IO.Directory]::Delete($configDestination, 1)
	}
	[System.IO.Directory]::CreateDirectory($configDestination)
    
    Copy-Item $installFiles $build_output
    Copy-Item $configFiles $configDestination
}

function UnitTests
{
	$nunit = $path + "\packages\NUnit.Runners.2.6.2\tools\nunit-console.exe"

	$EmailSender = Join-Path $build_output -childpath "tests\EmailSenderTests\EmailSenderTests.dll"
	$SmsCoordinatorTests = Join-Path $build_output -childpath "tests\SmsCoordinatorTests\SmsCoordinatorTests.dll"
	$SmsTrackingTests = Join-Path $build_output -childpath "tests\SmsTrackingTests\SmsTrackingTests.dll"
	$SmsWebTests = Join-Path $build_output -childpath "tests\SmsWebTests\SmsWebTests.dll"

	$xmlResultsFile = Join-Path $path -childpath "TestResult.xml"
	& $nunit /xml:$xmlResultsFile $EmailSender $SmsCoordinatorTests $SmsTrackingTests $SmsWebTests 

	[xml]$testOutput = Get-Content $xmlResultsFile
	$failureCount = $testOutput.'test-results'.failures
	if ($failureCount > 0)
	{
		throw "Tests Failure"
	}
}

function UninstallEndpoints
{
	if(Get-Service "SmsEmailSender" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsEmailSender
		"Service Exists (SmsEmailSender) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsEmailSender'"
		$service.delete()
	}

	if(Get-Service "SmsCoordinator" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsCoordinator
		"Service Exists (SmsCoordinator) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsCoordinator'"
		$service.delete()
	}

	if(Get-Service "SmsTracking" -ErrorAction SilentlyContinue)
	{
		Stop-Service SmsTracking
		"Service Exists (SmsTracking) - Uninstalling"
		$service = Get-WmiObject -Class Win32_Service -Filter "Name='SmsTracking'"
		$service.delete()
	}
}