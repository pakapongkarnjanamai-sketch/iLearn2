#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$IisHost = 'ap-ntc2137-prwb',

    [string]$SiteName = 'Default Web Site',

    [pscredential]$IisCredential,

    # Optional: use only when newly-created pools must run as the fixed service account.
    # The password is accepted as a secure credential and is never written to output.
    [pscredential]$AppPoolCredential,

    [ValidateSet('inprocess', 'outofprocess', 'preserve')]
    [string]$AspNetCoreHostingModel = 'inprocess',

    [switch]$AuditOnly
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$mappings = @(
    [pscustomobject]@{ Path = '/iLearn';             Pool = 'iLearn.User';        Kind = 'AspNetCore'; Required = $true },
    [pscustomobject]@{ Path = '/iLearn/Service';     Pool = 'iLearn.Service';     Kind = 'AspNetCore'; Required = $true },
    [pscustomobject]@{ Path = '/iLearn/admin';       Pool = 'iLearn.Admin';       Kind = 'AspNetCore'; Required = $true },
    [pscustomobject]@{ Path = '/iLearn/admin-react'; Pool = 'iLearn.Admin.React'; Kind = 'Static';     Required = $true },
    [pscustomobject]@{ Path = '/iLearn/student';     Pool = 'iLearn.Static';      Kind = 'Static';     Required = $false }
)

$remoteParams = @{
    ComputerName = $IisHost
    ArgumentList = @($SiteName, $mappings, [bool]$AuditOnly, $AspNetCoreHostingModel, $AppPoolCredential)
    ScriptBlock  = {
        param(
            [string]$RemoteSiteName,
            [object[]]$RemoteMappings,
            [bool]$RemoteAuditOnly,
            [string]$RemoteHostingModel,
            [pscredential]$RemoteAppPoolCredential
        )

        Set-StrictMode -Version 3.0
        $ErrorActionPreference = 'Stop'
        Import-Module WebAdministration

        function ConvertTo-IisPath {
            param(
                [Parameter(Mandatory)][string]$Site,
                [Parameter(Mandatory)][string]$ApplicationPath
            )

            $basePath = "IIS:\Sites\$Site"
            $trimmed = $ApplicationPath.Trim('/')
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                return $basePath
            }

            return (Join-Path $basePath ($trimmed -replace '/', '\'))
        }

        function Save-XmlDocument {
            param(
                [Parameter(Mandatory)][xml]$Document,
                [Parameter(Mandatory)][string]$Path
            )

            $settings = [System.Xml.XmlWriterSettings]::new()
            $settings.Indent = $true
            $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
            $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
            try {
                $Document.Save($writer)
            }
            finally {
                $writer.Close()
            }
        }

        function Ensure-AppPool {
            param(
                [Parameter(Mandatory)][string]$PoolName,
                [Parameter(Mandatory)][bool]$ReadOnly,
                [pscredential]$Credential
            )

            $poolPath = "IIS:\AppPools\$PoolName"
            $created = $false
            if (-not (Test-Path -LiteralPath $poolPath)) {
                if ($ReadOnly) {
                    return [pscustomobject]@{
                        Created = $false
                        Exists = $false
                        State = 'Missing'
                        ManagedRuntimeVersion = ''
                        IdentityType = ''
                        UserName = ''
                    }
                }

                New-WebAppPool -Name $PoolName | Out-Null
                $created = $true
            }

            $pool = Get-Item -LiteralPath $poolPath
            if (-not $ReadOnly) {
                $pool.managedRuntimeVersion = ''
                $pool.managedPipelineMode = 'Integrated'
                $pool.startMode = 'AlwaysRunning'

                if ($Credential) {
                    $pool.processModel.identityType = 'SpecificUser'
                    $pool.processModel.userName = $Credential.UserName
                    $pool.processModel.password = $Credential.GetNetworkCredential().Password
                }

                $pool | Set-Item

                $state = (Get-WebAppPoolState -Name $PoolName).Value
                if ($state -ne 'Started') {
                    Start-WebAppPool -Name $PoolName
                }
            }

            $poolAfter = Get-Item -LiteralPath $poolPath
            $stateAfter = (Get-WebAppPoolState -Name $PoolName).Value
            return [pscustomobject]@{
                Created = $created
                Exists = $true
                State = $stateAfter
                ManagedRuntimeVersion = [string]$poolAfter.managedRuntimeVersion
                IdentityType = [string]$poolAfter.processModel.identityType
                UserName = [string]$poolAfter.processModel.userName
            }
        }

        function Set-AspNetCoreHostingModel {
            param(
                [Parameter(Mandatory)][string]$PhysicalPath,
                [Parameter(Mandatory)][string]$Model,
                [Parameter(Mandatory)][bool]$ReadOnly
            )

            if ($Model -eq 'preserve') {
                return [pscustomobject]@{ WebConfig = ''; Before = 'preserve'; After = 'preserve'; Changed = $false }
            }

            $expandedPath = [Environment]::ExpandEnvironmentVariables($PhysicalPath)
            $webConfigPath = Join-Path $expandedPath 'web.config'
            if (-not (Test-Path -LiteralPath $webConfigPath)) {
                return [pscustomobject]@{ WebConfig = $webConfigPath; Before = 'missing'; After = 'missing'; Changed = $false }
            }

            [xml]$doc = Get-Content -LiteralPath $webConfigPath
            $node = $doc.SelectSingleNode('//aspNetCore')
            if ($null -eq $node) {
                return [pscustomobject]@{ WebConfig = $webConfigPath; Before = 'no aspNetCore'; After = 'no aspNetCore'; Changed = $false }
            }

            $before = $node.GetAttribute('hostingModel')
            if ($before -ne $Model -and -not $ReadOnly) {
                $stamp = Get-Date -Format 'yyyyMMddHHmmss'
                Copy-Item -LiteralPath $webConfigPath -Destination "$webConfigPath.bak-poolsplit-$stamp" -Force
                $node.SetAttribute('hostingModel', $Model)
                Save-XmlDocument -Document $doc -Path $webConfigPath
            }

            $after = if ($before -ne $Model -and -not $ReadOnly) { $Model } else { $before }
            return [pscustomobject]@{ WebConfig = $webConfigPath; Before = $before; After = $after; Changed = ($before -ne $after) }
        }

        $results = foreach ($mapping in $RemoteMappings) {
            $iisPath = ConvertTo-IisPath -Site $RemoteSiteName -ApplicationPath $mapping.Path
            if (-not (Test-Path -LiteralPath $iisPath)) {
                if ([bool]$mapping.Required) {
                    throw "Required IIS application not found: $($mapping.Path) ($iisPath)"
                }

                [pscustomobject]@{
                    Path = $mapping.Path
                    Kind = $mapping.Kind
                    Exists = $false
                    PreviousPool = ''
                    ActualPool = ''
                    TargetPool = $mapping.Pool
                    PoolState = 'Skipped'
                    PoolCreated = $false
                    HostingModelBefore = ''
                    HostingModelAfter = ''
                    WebConfig = ''
                    AuditOnly = $RemoteAuditOnly
                }
                continue
            }

            $app = Get-ItemProperty -LiteralPath $iisPath
            $previousPool = [string]$app.applicationPool
            $poolResult = Ensure-AppPool -PoolName $mapping.Pool -ReadOnly:$RemoteAuditOnly -Credential $RemoteAppPoolCredential

            if (-not $RemoteAuditOnly -and $previousPool -ne $mapping.Pool) {
                Set-ItemProperty -LiteralPath $iisPath -Name applicationPool -Value $mapping.Pool
            }

            # Re-read the binding from IIS rather than assuming the write landed. This is the value
            # the shared-pool check groups on: grouping on TargetPool can never detect a violation
            # because the targets are hardcoded unique, so the check would be dead code.
            $actualPool = [string](Get-ItemProperty -LiteralPath $iisPath).applicationPool

            $hosting = [pscustomobject]@{ WebConfig = ''; Before = ''; After = ''; Changed = $false }
            if ($mapping.Kind -eq 'AspNetCore') {
                $hosting = Set-AspNetCoreHostingModel -PhysicalPath ([string]$app.physicalPath) -Model $RemoteHostingModel -ReadOnly:$RemoteAuditOnly
            }

            [pscustomobject]@{
                Path = $mapping.Path
                Kind = $mapping.Kind
                Exists = $true
                PreviousPool = $previousPool
                ActualPool = $actualPool
                TargetPool = $mapping.Pool
                PoolState = $poolResult.State
                PoolCreated = $poolResult.Created
                PoolManagedRuntimeVersion = $poolResult.ManagedRuntimeVersion
                PoolIdentityType = $poolResult.IdentityType
                PoolUserName = $poolResult.UserName
                HostingModelBefore = $hosting.Before
                HostingModelAfter = $hosting.After
                WebConfig = $hosting.WebConfig
                AuditOnly = $RemoteAuditOnly
            }
        }

        return $results
    }
}

if ($IisCredential) {
    $remoteParams.Credential = $IisCredential
}

if ($PSCmdlet.ShouldProcess($IisHost, "Set iLearn IIS applications to dedicated app pools (AuditOnly=$([bool]$AuditOnly))")) {
    $results = Invoke-Command @remoteParams
    $results | Sort-Object Path | Format-Table Path, Kind, PreviousPool, ActualPool, TargetPool, PoolState, PoolManagedRuntimeVersion, PoolIdentityType, PoolUserName, HostingModelBefore, HostingModelAfter, AuditOnly -AutoSize

    # Shared-pool check runs here, after the table, so an audit always shows the operator the full
    # picture before it complains. ActualPool is what IIS reports right now — in -AuditOnly that is
    # the state to report, after an apply it is post-apply verification that the writes took.
    $sharedPools = @($results |
        Where-Object { $_.Exists -and $_.Kind -eq 'AspNetCore' } |
        Group-Object ActualPool |
        Where-Object { $_.Count -gt 1 })

    if ($sharedPools.Count -gt 0) {
        $summary = ($sharedPools | ForEach-Object { "$($_.Name): $($_.Count) apps ($(($_.Group.Path) -join ', '))" }) -join '; '

        if ($AuditOnly) {
            # This is the 500.35 topology. An audit reports it; it does not fail.
            Write-Warning "Multiple ASP.NET Core apps share one app pool ($summary). This is the ASP.NET Core Module 500.35 topology — rerun this script without -AuditOnly to split them."
        }
        else {
            throw "Apply did not split the app pools: multiple ASP.NET Core apps still share one pool ($summary)."
        }
    }
    elseif (-not $AuditOnly) {
        Write-Host "App-pool split verified: no ASP.NET Core apps share a pool." -ForegroundColor DarkGray
    }
}