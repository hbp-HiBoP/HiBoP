<# Network-free tests of the actual orchestration entry point, with GitHub and
   host state replaced by fixtures. All filesystem mutations stay in testRoot. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$updater = Join-Path $PSScriptRoot 'Update-NativePlugins.ps1'
. $updater -ValidateOnly
$realRoot = $repositoryRoot
$template = Get-Content $configurationPath -Raw
$mainText = Get-Content $updater -Raw
$main = [scriptblock]::Create($mainText.Substring($mainText.IndexOf('$resumeConfiguration = $null')))
$realInstall = ${function:Install-Payload}
$realAssertInstalled = ${function:Assert-InstalledPackages}
$realUnityClosed = ${function:Assert-UnityClosed}
$tests = [Collections.Generic.List[string]]::new()
$root = Join-Path $realRoot ('.test-results/native-android-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root | Out-Null
function Check([bool]$Condition, [string]$Name) {
    if (!$Condition) { throw "FAILED: $Name" }
    $tests.Add($Name)
}
function Reject([scriptblock]$Action, [string]$Pattern, [string]$Name) {
    $failure = $null
    try { & $Action | Out-Null } catch { $failure = $_ }
    Check ($null -ne $failure -and $failure.ToString() -match $Pattern) "$Name (expected $Pattern; actual $failure)"
}
function Save-Json($Value, [string]$Path) { $Value | ConvertTo-Json -Depth 15 | Set-Content $Path -Encoding utf8 }
function New-Fixture([string]$Directory) {
    $configuration = $template | ConvertFrom-Json -AsHashtable
    New-Item -ItemType Directory "$Directory/Tools" -Force | Out-Null
    Set-Content "$Directory/Tools/NativePlugins.json" $template
    foreach ($library in $configuration.libraries) {
        foreach ($target in $library.targets) {
            $packageDir = "$Directory/packages/$($library.name)/$($target.platform)"
            New-Item -ItemType Directory $packageDir -Force | Out-Null
            $payload = if ($target.platform -eq 'MacOS') { "$($target.payload)/Contents/MacOS/lib$($library.name)" } else { $target.payload }
            $path = "$packageDir/$payload"
            New-Item -ItemType Directory (Split-Path -Parent $path) -Force | Out-Null
            $bytes = [byte[]](0..63)
            if ($target.platform -eq 'Android') { $bytes[0]=127; $bytes[1]=69; $bytes[2]=76; $bytes[3]=70; $bytes[4]=2; $bytes[5]=1; $bytes[16]=3; $bytes[17]=0; $bytes[18]=183; $bytes[19]=0 }
            [IO.File]::WriteAllBytes($path, $bytes)
            $manifest = @{ repository=$library.manifestRepository; commit=('a'*40); configuration='Release'; platform=$target.platform; architecture=$target.architecture
                files=@(@{path=$payload;sizeBytes=$bytes.Length;sha256=(Get-FileHash $path).Hash.ToLowerInvariant()}) }
            if ($target.platform -eq 'Android') {
                $manifest.sourceMode='git-archive'; $manifest.sourceTree='b'*40
                $manifest.android=@{ndkVersion='27.2.12479018';api=32;abi='arm64-v8a';stl='c++_static';pageSize=16384}
                $manifest.publicExportCount=$(if ($library.name -eq 'hbp_math') {17} else {226}); $manifest.runtimeDependencies=@('libc.so','libdl.so','libm.so'); $manifest.buildTools=@()
            }
            Save-Json $manifest "$packageDir/artifact-manifest.json"
            if ($target.platform -eq 'MacOS') {
                & tar -czf "$packageDir/package.tar.gz" -C $packageDir $target.payload artifact-manifest.json
                if ($LASTEXITCODE -ne 0) { throw 'Fixture tar failed.' }
            }
            $destination = "$Directory/$($target.destination)"
            New-Item -ItemType Directory (Split-Path -Parent $destination) -Force | Out-Null
            Set-Content "$destination.meta" 'original-meta'
            if ($target.platform -ne 'Android') {
                if ($target.platform -eq 'MacOS') {
                    New-Item -ItemType Directory "$destination/Contents/MacOS" -Force | Out-Null
                    $destination += "/Contents/MacOS/lib$($library.name)"
                }
                Set-Content $destination 'original-payload'
            }
        }
    }
    Set-Content "$Directory/Tools/NativePlugins.lock.json" 'original-lock'
}
function Snapshot([string]$Directory) {
    return @(Get-ChildItem "$Directory/Assets", "$Directory/Tools/NativePlugins.lock.json" -File -Recurse | Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($Directory.Length) + ':' + (Get-FileHash $_.FullName).Hash }) -join "`n"
}
function Scenario([string]$Mode) {
    $repositoryRoot = Join-Path $root $Mode
    New-Fixture $repositoryRoot
    $configurationPath = "$repositoryRoot/Tools/NativePlugins.json"
    $lockFilePath = "$repositoryRoot/Tools/NativePlugins.lock.json"
    $workingRoot = "$repositoryRoot/.native-plugin-update"
    $configuration = Get-NativePluginConfiguration
    $before = Snapshot $repositoryRoot
    $Resume = $null; $ValidateOnly = $false; $AndroidPackage = $null
    $calls = [Collections.Generic.List[string]]::new()
    $dispatched = @{}
    function Assert-GitHubReady {}
    function Assert-InstallTargetsClean {}
    function Assert-ExpectedInstallDiff {}
    function Get-CimInstance {
        if ($Mode -eq 'resume-unity-open') { return @{ CommandLine = $repositoryRoot } }
    }
    function Assert-UnityClosed { & $realUnityClosed }
    function Invoke-GitHub([string[]]$Arguments) {
        $calls.Add($Arguments -join ' ')
        if ($Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'view') { return "request_id:`nsource_sha:`ninputs.source_sha`n  android:" }
        if ($Arguments[0] -eq 'api' -and $Arguments[1] -match '/commits/master$') { return 'a'*40 }
        if ($Arguments[0] -eq 'workflow' -and $Arguments[1] -eq 'run') {
            if ('platform=all' -notin $Arguments -or ('source_sha=' + ('a'*40)) -notin $Arguments) { throw 'Dispatch did not pin all platforms to exact source.' }
            $repo = $Arguments[[array]::IndexOf($Arguments,'-R')+1]
            $request = @($Arguments | Where-Object { $_ -like 'request_id=*' })[0].Substring(11)
            $dispatched[$repo]=$request
            return ''
        }
        if ($Arguments[0] -eq 'run' -and $Arguments[1] -eq 'list') {
            $repo=$Arguments[[array]::IndexOf($Arguments,'-R')+1]
            if (!$dispatched.ContainsKey($repo)) { return '[]' }
            return ConvertTo-Json -InputObject @(@{databaseId=123;displayTitle="native - $($dispatched[$repo])";url='https://example.invalid/run';status='completed';conclusion='success'})
        }
        if ($Arguments[0] -eq 'run' -and $Arguments[1] -eq 'view') {
            return @{status='completed';conclusion=$(if ($Mode -eq 'workflow-failure') {'failure'} else {'success'});url='https://example.invalid/run'} | ConvertTo-Json
        }
        if ($Arguments[0] -eq 'api' -and $Arguments[1] -match 'repos/[^/]+/([^/]+)/actions/runs/123/artifacts') {
            $name=$Matches[1]
            $items=@(@{name="$name-windows-x64-123";expired=$false},@{name="$name-linux-x64-ubuntu22-123";expired=$false},@{name="$name-macos-arm64-123";expired=$false})
            if ($name -in @('hbp_core','hbp_math') -and !($Mode -eq 'missing-android' -and $name -eq 'hbp_core') -and !($Mode -eq 'missing-math-android' -and $name -eq 'hbp_math')) { $items+=@{name="$name-android-arm64-123";expired=$false} }
            return @{artifacts=$items} | ConvertTo-Json -Depth 5
        }
        if ($Arguments[0] -eq 'run' -and $Arguments[1] -eq 'download') {
            $repo=$Arguments[[array]::IndexOf($Arguments,'-R')+1]; $name=($repo -split '/')[-1]
            $destination=$Arguments[[array]::IndexOf($Arguments,'-D')+1]; $platform=Split-Path -Leaf $destination
            $source="$repositoryRoot/packages/$name/$platform"
            if ($platform -eq 'macos') { Copy-Item "$source/package.tar.gz" $destination }
            else { Copy-Item "$source/*" $destination -Recurse }
            if ($platform -eq 'android' -and (($Mode -eq 'corrupt-android' -and $name -eq 'hbp_core') -or ($Mode -eq 'corrupt-math-android' -and $name -eq 'hbp_math'))) { Add-Content "$destination/lib$name.so" 'corrupt' }
            return ''
        }
        throw "Unexpected GitHub fixture call: $($Arguments -join ' ')"
    }
    function Install-Payload([hashtable]$Package, [string]$RequestId) {
        & $realInstall -Package $Package -RequestId $RequestId
        if ($Mode -eq 'install-failure' -and $Package.platform -eq 'Android' -and $Package.library -eq 'hbp_math') { throw 'Injected failure after Android install.' }
    }
    function Assert-InstalledPackages([hashtable[]]$Packages) {
        & $realAssertInstalled -Packages $Packages
        if ($Mode -eq 'lock-failure') { throw 'Injected failure after lock write.' }
    }
    if ($Mode -like 'resume-*') {
        $Resume='interrupted'; $requestDirectory="$workingRoot/$Resume"
        New-Item -ItemType Directory $requestDirectory -Force | Out-Null
        if ($Mode -like 'resume-legacy-*') {
            foreach ($entry in $configuration.libraries) {
                if ($entry.name -eq 'hbp_math' -or $Mode -eq 'resume-legacy-9') {
                    $entry.targets = @($entry.targets | Where-Object platform -ne 'Android')
                }
            }
            Save-Json $configuration $configurationPath
        }
        $state=New-OrchestrationState -Configuration $configuration -RequestId $Resume
        Backup-CurrentInstall -Configuration $configuration -BackupRoot "$requestDirectory/backup"
        $state.phase='installing'; Write-State $state "$requestDirectory/state.json"
        if ($Mode -like 'resume-legacy-*') { Set-Content $configurationPath $template }
        $destination="$repositoryRoot/Assets/Plugins/Native/Windows/x86_64/hbp_core.dll"
        Move-Item $destination "$destination.native-previous-$Resume"
        $interrupted=Snapshot $repositoryRoot
        # Stop after successful recovery, before any new remote work.
        function Assert-WorkflowSupportsOrchestration { throw 'Recovery completed (fixture).' }
        $pattern=if ($Mode -eq 'resume-unity-open') {'Close the Unity Editor'} elseif ($Mode -like 'resume-legacy-*') {'NativePlugins.json changed'} else {'Recovery completed'}
        Reject { . $main } $pattern $Mode
        $expected=if ($Mode -eq 'resume-unity-open') {$interrupted} else {$before}
        Check ((Snapshot $repositoryRoot) -eq $expected) "$Mode preserves/restores exact bytes"
        return
    }
    if ($Mode -eq 'success') {
        . $main
        $lock=Get-Content $lockFilePath -Raw | ConvertFrom-Json -AsHashtable
        Check (@($lock.libraries | ForEach-Object { $_.artifacts }).Count -eq 11) 'Eleven artifacts installed and pinned'
        Check (@($calls | Where-Object { $_ -like 'workflow run *' }).Count -eq 3) 'Exactly three workflow dispatches'
        Check (@($lock.libraries | Where-Object { $_.commit -ne ('a'*40) }).Count -eq 0) 'All manifests use dispatched source commit'
        Check (@(Get-ChildItem "$repositoryRoot/Assets" -Recurse -Filter '*.meta' | Where-Object { (Get-Content $_.FullName -Raw).Trim() -ne 'original-meta' }).Count -eq 0) 'Unity metadata preserved'
        $completed=$state.requestId; $Resume=$completed; $beforeResume=Snapshot $repositoryRoot
        . $main
        Check ((Snapshot $repositoryRoot) -eq $beforeResume) 'Completed resume is idempotent'
    }
    else {
        $patterns=@{'workflow-failure'='workflow failed';'missing-android'='exactly 4 artifacts';'missing-math-android'='exactly 4 artifacts';'corrupt-math-android'='size mismatch';'corrupt-android'='size mismatch';'install-failure'='Injected failure';'lock-failure'='Injected failure'}
        Reject { . $main } $patterns[$Mode] $Mode
        Check ((Snapshot $repositoryRoot) -eq $before) "$Mode leaves all plugins and lock unchanged"
    }
}
foreach ($mode in @('success','workflow-failure','missing-android','missing-math-android','corrupt-android','corrupt-math-android','install-failure','lock-failure','resume-missing-destination','resume-unity-open','resume-legacy-9','resume-legacy-10')) { Scenario $mode }

function Test-LocalImport([string]$LibraryName, [bool]$FailLockWrite, [bool]$ExistingAndroid) {
    $repositoryRoot = Join-Path $root "local-$LibraryName-$FailLockWrite-$ExistingAndroid"
    New-Fixture $repositoryRoot
    $configurationPath = "$repositoryRoot/Tools/NativePlugins.json"
    $lockFilePath = "$repositoryRoot/Tools/NativePlugins.lock.json"
    $workingRoot = "$repositoryRoot/.native-plugin-update"
    $configuration = Get-NativePluginConfiguration
    $oldLock = @{schemaVersion=1;libraries=@($configuration.libraries | ForEach-Object {
        $lib=$_
        @{name=$lib.name;repository=$lib.repository;commit=('a'*40);runId=17;runUrl='https://example.invalid/desktop';artifacts=@(
            $lib.targets | Where-Object platform -ne 'Android' | ForEach-Object {
                $target=$_; $relative=if ($target.platform -eq 'MacOS') {"Contents/MacOS/lib$($lib.name)"} else {''}
                $path=if ($relative) {"$repositoryRoot/$($target.destination)/$relative"} else {"$repositoryRoot/$($target.destination)"}
                @{platform=$target.platform;architecture=$target.architecture;destination=$target.destination;files=@(@{relativePath=$relative;sizeBytes=(Get-Item $path).Length;sha256=(Get-FileHash $path).Hash.ToLowerInvariant()})}
            })}
    })}
    foreach ($entry in $oldLock.libraries) {
        if ($entry.name -ne $LibraryName) { $entry.commit = 'd'*40 }
    }
    if ($ExistingAndroid) {
        $selected = @($oldLock.libraries | Where-Object name -eq $LibraryName)[0]
        $target = @($configuration.libraries | Where-Object name -eq $LibraryName)[0].targets | Where-Object platform -eq 'Android'
        $existingPath = "$repositoryRoot/$($target.destination)"
        Set-Content $existingPath 'previous-android-payload'
        $selected.artifacts += @{platform='Android';architecture='arm64';destination=$target.destination;files=@(@{relativePath='';sizeBytes=(Get-Item $existingPath).Length;sha256=(Get-FileHash $existingPath).Hash.ToLowerInvariant()})}
    }
    Save-Json $oldLock $lockFilePath
    $before=Snapshot $repositoryRoot
    function Get-CimInstance {}
    function Set-Content {
        [CmdletBinding()]
        param($Path, $LiteralPath, [Parameter(ValueFromPipeline)]$Value, $Encoding)
        process {
            if ($FailLockWrite -and $Path -eq $lockFilePath) { throw 'Injected local lock write failure.' }
            Microsoft.PowerShell.Management\Set-Content @PSBoundParameters
        }
    }
    $localManifestPath = "$repositoryRoot/packages/$LibraryName/Android/artifact-manifest.json"
    $localManifest = Get-Content $localManifestPath -Raw | ConvertFrom-Json -AsHashtable
    $savedRepository = $localManifest.repository
    $localManifest.repository = 'EEGFormat'
    Save-Json $localManifest $localManifestPath
    Reject { Install-LocalAndroidPackage -PackageDirectory "$repositoryRoot/packages/$LibraryName/Android" -Configuration $configuration } 'supports only hbp_core or hbp_math' "$LibraryName unsupported local repository"
    Check ((Snapshot $repositoryRoot) -eq $before) 'Unsupported repository leaves all installed bytes and lock unchanged'
    $localManifest.repository = $savedRepository
    $localManifest.commit = 'c'*40
    Save-Json $localManifest $localManifestPath
    Reject { Install-LocalAndroidPackage -PackageDirectory "$repositoryRoot/packages/$LibraryName/Android" -Configuration $configuration } 'does not match requested commit' "$LibraryName local source mismatch"
    Check ((Snapshot $repositoryRoot) -eq $before) "$LibraryName mismatch preserves installed bytes and lock"
    $localManifest.commit = 'a'*40
    Save-Json $localManifest $localManifestPath
    if ($FailLockWrite) {
        Reject { Install-LocalAndroidPackage -PackageDirectory "$repositoryRoot/packages/$LibraryName/Android" -Configuration $configuration } 'local lock write failure' 'Local import lock failure'
        Check ((Snapshot $repositoryRoot) -eq $before) 'Local lock failure restores Desktop, absent Android and original lock'
    }
    else {
        Install-LocalAndroidPackage -PackageDirectory "$repositoryRoot/packages/$LibraryName/Android" -Configuration $configuration
        $newLock=Get-Content $lockFilePath -Raw | ConvertFrom-Json -AsHashtable
        $core=@($newLock.libraries | Where-Object name -eq $LibraryName)[0]
        $android=@($core.artifacts | Where-Object platform -eq 'Android')[0]
        Check ($android.origin -eq 'local' -and $null -eq $android.runId -and $null -eq $android.runUrl) 'Local package never inherits Desktop GitHub run provenance'
        $core.artifacts=@($core.artifacts | Where-Object platform -ne 'Android')
        $oldSelected = @($oldLock.libraries | Where-Object name -eq $LibraryName)[0]
        $oldSelected.artifacts = @($oldSelected.artifacts | Where-Object platform -ne 'Android')
        # Compare parsed structures, ignoring JSON whitespace and property order.
        foreach ($index in 0..2) {
            Check (($newLock.libraries[$index] | ConvertTo-Json -Depth 12 -Compress) -eq ($oldLock.libraries[$index] | ConvertTo-Json -Depth 12 -Compress)) "Local import preserves Desktop lock entry $index"
        }
        Check ($core.artifacts.Count -eq 3) 'Local import keeps all Desktop artifacts'
    }
}
foreach ($libraryName in @('hbp_core','hbp_math')) {
    foreach ($existing in @($false,$true)) {
        Test-LocalImport $libraryName $false $existing
        Test-LocalImport $libraryName $true $existing
    }
}

# Manifest and artifact-selection negative cases use an isolated valid fixture.
$fixture="$root/validation"; New-Fixture $fixture
$config=$template | ConvertFrom-Json -AsHashtable
foreach ($libraryName in @('hbp_core','hbp_math')) {
$core=@($config.libraries | Where-Object name -eq $libraryName)[0]
$path="$fixture/packages/$libraryName/Android/artifact-manifest.json"
$original=Get-Content $path -Raw
foreach ($case in @('commit','architecture','configuration','repository','ndk','api','abi','stl','dependencies','hash')) {
    $value=$original | ConvertFrom-Json -AsHashtable
    switch ($case) {
        commit {$value.commit='c'*40}; architecture {$value.architecture='x86_64'}; configuration {$value.configuration='Debug'}
        repository {$value.repository='wrong'}; ndk {$value.android.ndkVersion='wrong'}; api {$value.android.api=21}
        abi {$value.android.abi='x86_64'}; stl {$value.android.stl='c++_shared'}; dependencies {$value.runtimeDependencies=@('libc++_shared.so')}
        hash {$value.files[0].sha256='0'*64}
    }
    Save-Json $value $path
    Reject { Test-ArtifactManifest -ManifestPath $path -Library $core -RepositoryState @{sourceSha=('a'*40)} } 'match|Unexpected|Release|SHA-256' "Manifest rejects $case"
}
Set-Content $path $original
$items=@(@{name="$libraryName-windows-x64-123";expired=$false},@{name="$libraryName-linux-x64-ubuntu22-123";expired=$false},@{name="$libraryName-macos-arm64-123";expired=$false},@{name="$libraryName-android-arm64-123";expired=$false})
$items[3].expired=$true
Reject { Select-RunArtifacts -LibraryName $libraryName -RunId 123 -Artifacts $items } 'expired' 'Expired Android artifact rejected'
$items[3].expired=$false
Reject { Select-RunArtifacts -LibraryName $libraryName -RunId 123 -Artifacts ($items + $items[3]) } 'exactly 4' 'Duplicate Android artifact rejected'
$items[3].name="$libraryName-android-arm64-999"
Reject { Select-RunArtifacts -LibraryName $libraryName -RunId 123 -Artifacts $items } 'one Android artifact' 'Wrong run artifact rejected'
}
Save-Json @{result='Passed';count=$tests.Count;tests=$tests;fixtureRoot=$root} "$root/results.json"
Write-Host "Android updater tests passed: $($tests.Count). Evidence: $root/results.json"
