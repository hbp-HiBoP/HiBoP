<#
.SYNOPSIS
    Automatically configures ADB over Wi-Fi for a Meta Quest.

.DESCRIPTION
    This script tries, in order, to:
      1. reuse an existing ADB Wi-Fi connection;
      2. connect directly to -IpAddress, if provided;
      3. discover an ADB target via mDNS;
      4. if requested with -ScanSubnet, look for a Quest listening on the ADB port
         on local IPv4 /24 subnets;
      5. otherwise, use a Quest connected over USB to:
           - detect its Wi-Fi IPv4 address;
           - run "adb tcpip <port>";
           - establish "adb connect <ip>:<port>";
           - verify the connection with a shell command.

    No IP address cache is created.

    The script prefers a standalone ADB installation (PATH / Android SDK) and can use
    the ADB bundled with Unity as a last resort.

    Compatible with Windows PowerShell 5.1+ and PowerShell 7+.

.EXAMPLE
    .\Connect-QuestAdbWifi.ps1

    Normal case: the Quest is connected over USB. The script configures ADB Wi-Fi.

.EXAMPLE
    .\Connect-QuestAdbWifi.ps1 -IpAddress 192.168.1.18

    First tries to reconnect directly without a cable.

.EXAMPLE
    .\Connect-QuestAdbWifi.ps1 -ScanSubnet

    If no USB cable is connected and no target is found otherwise, looks for an ADB
    service on port 5555 on local IPv4 /24 networks.

.EXAMPLE
    .\Connect-QuestAdbWifi.ps1 -RestartAdbServer

    Restarts the local ADB server before starting.

.EXAMPLE
    .\Connect-QuestAdbWifi.ps1 -AdbPath "C:\Android\Sdk\platform-tools\adb.exe"

    Forces the use of a specific adb.exe.

.NOTES
    First use on a new computer:
      - Developer Mode must be enabled on the Quest;
      - the Quest must be connected over USB;
      - the USB debugging RSA key must be accepted manually in the headset.
#>

[CmdletBinding()]
param(
    # Path to adb.exe OR to the platform-tools directory.
    [string]$AdbPath,

    # USB ADB serial to use if multiple devices are connected.
    [string]$Serial,

    # Known Quest IP address. Optional; no cache is used.
    [string]$IpAddress,

    # Port used by "adb tcpip".
    [ValidateRange(1, 65535)]
    [int]$Port = 5555,

    # Number of ADB TCP connection attempts.
    [ValidateRange(1, 30)]
    [int]$ConnectRetries = 8,

    # Delay between attempts.
    [ValidateRange(100, 10000)]
    [int]$RetryDelayMs = 1000,

    # Maximum time to wait for an authorized USB device.
    [ValidateRange(1, 600)]
    [int]$UsbWaitSeconds = 30,

    # Explicitly restarts the local ADB server.
    [switch]$RestartAdbServer,

    # If no target is found, scans local IPv4 /24 subnets on the selected port.
    # Disabled by default to avoid an unnecessary network scan.
    [switch]$ScanSubnet,

    # Does not use the ADB bundled with Unity as a fallback.
    [switch]$NoUnityFallback,

    # Does not attempt discovery with "adb mdns services".
    [switch]$NoMdns,

    # Displays less output.
    [switch]$Quiet
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Display / output helpers
# ---------------------------------------------------------------------------

function Write-Step {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host "[Quest ADB] $Message"
    }
}

function Write-Ok {
    param([string]$Message)
    Write-Host "[Quest ADB] OK: $Message"
}

function Write-Warn {
    param([string]$Message)
    Write-Warning "[Quest ADB] $Message"
}

function Stop-Script {
    param(
        [string]$Message,
        [int]$Code = 1
    )
    Write-Error "[Quest ADB] $Message"
    exit $Code
}

# ---------------------------------------------------------------------------
# ADB invocation
# ---------------------------------------------------------------------------

$script:Adb = $null

function Invoke-Adb {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Arguments,

        [switch]$AllowFailure
    )

    $output = @(& $script:Adb @Arguments 2>&1)
    $code = $LASTEXITCODE

    if (($code -ne 0) -and (-not $AllowFailure)) {
        $text = ($output -join [Environment]::NewLine).Trim()
        throw "ADB failed (exit code $code): adb $($Arguments -join ' ')`n$text"
    }

    [pscustomobject]@{
        ExitCode = $code
        Output   = $output
        Text     = ($output -join [Environment]::NewLine).Trim()
    }
}

# ---------------------------------------------------------------------------
# adb.exe resolution
# ---------------------------------------------------------------------------

function Resolve-AdbExecutable {
    param([string]$RequestedPath)

    $candidates = New-Object System.Collections.Generic.List[string]

    if ($RequestedPath) {
        if (Test-Path -LiteralPath $RequestedPath -PathType Container) {
            $candidates.Add((Join-Path $RequestedPath "adb.exe"))
        } else {
            $candidates.Add($RequestedPath)
        }
    }

    try {
        $cmd = Get-Command adb.exe -ErrorAction Stop
        if ($cmd.Source) {
            $candidates.Add($cmd.Source)
        }
    } catch {}

    foreach ($root in @($env:ANDROID_SDK_ROOT, $env:ANDROID_HOME)) {
        if ($root) {
            $candidates.Add((Join-Path $root "platform-tools\adb.exe"))
        }
    }

    $candidates.Add("C:\Android\Sdk\platform-tools\adb.exe")

    if ($env:LOCALAPPDATA) {
        $candidates.Add((Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"))
    }

    if (-not $NoUnityFallback) {
        $unityEditors = "C:\Program Files\Unity\Hub\Editor"
        if (Test-Path -LiteralPath $unityEditors -PathType Container) {
            $unityAdbs = Get-ChildItem -LiteralPath $unityEditors -Directory -ErrorAction SilentlyContinue |
                ForEach-Object {
                    Join-Path $_.FullName "Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
                } |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
                Sort-Object -Descending

            foreach ($candidate in $unityAdbs) {
                $candidates.Add($candidate)
            }
        }
    }

    $seen = @{}
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }

        try {
            $full = [System.IO.Path]::GetFullPath(
                [Environment]::ExpandEnvironmentVariables($candidate)
            )
        } catch {
            continue
        }

        if ($seen.ContainsKey($full.ToLowerInvariant())) { continue }
        $seen[$full.ToLowerInvariant()] = $true

        if (Test-Path -LiteralPath $full -PathType Leaf) {
            return $full
        }
    }

    return $null
}

# ---------------------------------------------------------------------------
# Device parsing / identification
# ---------------------------------------------------------------------------

function Get-AdbDevices {
    $result = Invoke-Adb -Arguments @("devices", "-l") -AllowFailure
    if ($result.ExitCode -ne 0) {
        return @()
    }

    $devices = @()

    foreach ($line in $result.Output) {
        $s = [string]$line
        if ([string]::IsNullOrWhiteSpace($s)) { continue }
        if ($s -match '^List of devices attached') { continue }
        if ($s -match '^\* daemon') { continue }

        if ($s -match '^(\S+)\s+(device|offline|unauthorized|no permissions)(?:\s+(.*))?$') {
            $serialValue = $matches[1]
            $stateValue  = $matches[2]
            $details     = ""
            if ($matches.Count -ge 4) { $details = $matches[3] }

            $isTcp = $serialValue -match '^\d{1,3}(?:\.\d{1,3}){3}:\d+$'

            $devices += [pscustomobject]@{
                Serial = $serialValue
                State  = $stateValue
                IsTcp  = $isTcp
                Details = $details
            }
        }
    }

    return @($devices)
}

function Get-DeviceProperties {
    param([string]$DeviceSerial)

    $manufacturer = ""
    $model = ""
    $product = ""

    $r = Invoke-Adb -Arguments @("-s", $DeviceSerial, "shell", "getprop", "ro.product.manufacturer") -AllowFailure
    if ($r.ExitCode -eq 0) { $manufacturer = $r.Text.Trim() }

    $r = Invoke-Adb -Arguments @("-s", $DeviceSerial, "shell", "getprop", "ro.product.model") -AllowFailure
    if ($r.ExitCode -eq 0) { $model = $r.Text.Trim() }

    $r = Invoke-Adb -Arguments @("-s", $DeviceSerial, "shell", "getprop", "ro.product.name") -AllowFailure
    if ($r.ExitCode -eq 0) { $product = $r.Text.Trim() }

    [pscustomobject]@{
        Manufacturer = $manufacturer
        Model        = $model
        Product      = $product
    }
}

function Test-IsQuest {
    param([string]$DeviceSerial)

    $props = Get-DeviceProperties -DeviceSerial $DeviceSerial
    $joined = "$($props.Manufacturer) $($props.Model) $($props.Product)"

    [pscustomobject]@{
        IsQuest = ($joined -match '(?i)\b(meta|oculus|quest)\b')
        Properties = $props
    }
}

function Format-DeviceName {
    param(
        [string]$DeviceSerial,
        $Properties
    )

    $parts = @()
    if ($Properties.Manufacturer) { $parts += $Properties.Manufacturer }
    if ($Properties.Model) { $parts += $Properties.Model }

    if ($parts.Count -gt 0) {
        return "$DeviceSerial ($($parts -join ' '))"
    }

    return $DeviceSerial
}

# ---------------------------------------------------------------------------
# ADB TCP connection verification
# ---------------------------------------------------------------------------

function Test-AdbTcpEndpoint {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Endpoint,

        [switch]$RequireQuest
    )

    $devices = Get-AdbDevices
    $device = $devices | Where-Object {
        $_.Serial -eq $Endpoint -and $_.State -eq "device"
    } | Select-Object -First 1

    if (-not $device) {
        return $null
    }

    $shell = Invoke-Adb -Arguments @("-s", $Endpoint, "shell", "echo", "QUEST_ADB_WIFI_OK") -AllowFailure
    if ($shell.ExitCode -ne 0 -or $shell.Text -notmatch "QUEST_ADB_WIFI_OK") {
        return $null
    }

    $questInfo = Test-IsQuest -DeviceSerial $Endpoint

    if ($RequireQuest -and (-not $questInfo.IsQuest)) {
        return $null
    }

    return [pscustomobject]@{
        Endpoint   = $Endpoint
        IsQuest    = $questInfo.IsQuest
        Properties = $questInfo.Properties
    }
}

function Connect-AdbEndpoint {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Endpoint,

        [switch]$RequireQuest
    )

    for ($attempt = 1; $attempt -le $ConnectRetries; $attempt++) {
        Write-Step "Connecting to $Endpoint (attempt $attempt/$ConnectRetries)..."

        $connect = Invoke-Adb -Arguments @("connect", $Endpoint) -AllowFailure

        if ($connect.Text) {
            Write-Verbose $connect.Text
        }

        Start-Sleep -Milliseconds $RetryDelayMs

        $verified = Test-AdbTcpEndpoint -Endpoint $Endpoint -RequireQuest:$RequireQuest
        if ($verified) {
            return $verified
        }
    }

    return $null
}

# ---------------------------------------------------------------------------
# Wi-Fi IPv4 address of the USB device
# ---------------------------------------------------------------------------

function Get-QuestWifiIp {
    param([string]$UsbSerial)

    # Most accurate method: wlan0 address.
    $r = Invoke-Adb -Arguments @(
        "-s", $UsbSerial, "shell",
        "ip", "-o", "-4", "addr", "show", "dev", "wlan0"
    ) -AllowFailure

    if ($r.ExitCode -eq 0 -and $r.Text -match '\binet\s+(\d{1,3}(?:\.\d{1,3}){3})/') {
        return $matches[1]
    }

    # Fallback compatible with the method generally documented by Meta.
    $r = Invoke-Adb -Arguments @("-s", $UsbSerial, "shell", "ip", "route") -AllowFailure
    if ($r.ExitCode -eq 0 -and $r.Text -match '\bsrc\s+(\d{1,3}(?:\.\d{1,3}){3})\b') {
        return $matches[1]
    }

    return $null
}

# ---------------------------------------------------------------------------
# USB device selection
# ---------------------------------------------------------------------------

function Wait-ForUsbQuest {
    param(
        [string]$RequestedSerial,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastUnauthorized = @()
    $lastOffline = @()

    do {
        $devices = Get-AdbDevices
        $usb = @($devices | Where-Object { -not $_.IsTcp })

        if ($RequestedSerial) {
            $specific = $usb | Where-Object { $_.Serial -eq $RequestedSerial } | Select-Object -First 1

            if ($specific) {
                if ($specific.State -eq "unauthorized") {
                    $lastUnauthorized = @($specific)
                } elseif ($specific.State -eq "offline") {
                    $lastOffline = @($specific)
                } elseif ($specific.State -eq "device") {
                    return $specific
                }
            }
        } else {
            $lastUnauthorized = @($usb | Where-Object { $_.State -eq "unauthorized" })
            $lastOffline = @($usb | Where-Object { $_.State -eq "offline" })
            $authorized = @($usb | Where-Object { $_.State -eq "device" })

            if ($authorized.Count -eq 1) {
                return $authorized[0]
            }

            if ($authorized.Count -gt 1) {
                # Tries to automatically select the only Quest.
                $quests = @()
                foreach ($device in $authorized) {
                    $q = Test-IsQuest -DeviceSerial $device.Serial
                    if ($q.IsQuest) {
                        $quests += [pscustomobject]@{
                            Device = $device
                            Properties = $q.Properties
                        }
                    }
                }

                if ($quests.Count -eq 1) {
                    return $quests[0].Device
                }

                if ($quests.Count -gt 1) {
                    $names = $quests | ForEach-Object {
                        Format-DeviceName -DeviceSerial $_.Device.Serial -Properties $_.Properties
                    }
                    Stop-Script -Code 5 -Message (
                        "Multiple authorized USB Meta Quest devices were detected. " +
                        "Run again with -Serial <serial>. Devices: " +
                        ($names -join ", ")
                    )
                }

                $serials = $authorized | ForEach-Object { $_.Serial }
                Stop-Script -Code 5 -Message (
                    "Multiple USB ADB devices were detected and no unique Quest can be selected. " +
                    "Run again with -Serial <serial>. Devices: " +
                    ($serials -join ", ")
                )
            }
        }

        Start-Sleep -Milliseconds 500
    }
    while ((Get-Date) -lt $deadline)

    if ($lastUnauthorized.Count -gt 0) {
        Stop-Script -Code 4 -Message @"
The Quest was detected over USB but is not authorized for ADB.

In the headset:
  1. accept the "Allow USB debugging" prompt;
  2. preferably select "Always allow from this computer";
  3. run the script again.

On a new computer, this RSA authorization is intentionally manual.
"@
    }

    if ($lastOffline.Count -gt 0) {
        Stop-Script -Code 4 -Message (
            "The USB ADB device is 'offline'. Unlock or wake the Quest, " +
            "reconnect the cable if necessary, and run the script again."
        )
    }

    return $null
}

# ---------------------------------------------------------------------------
# mDNS discovery
# ---------------------------------------------------------------------------

function Try-MdnsDiscovery {
    if ($NoMdns) {
        return $null
    }

    Write-Step "Searching for ADB services via mDNS..."

    $mdns = Invoke-Adb -Arguments @("mdns", "services") -AllowFailure
    if ($mdns.ExitCode -ne 0 -or -not $mdns.Text) {
        return $null
    }

    $endpoints = New-Object System.Collections.Generic.List[string]

    foreach ($line in $mdns.Output) {
        $s = [string]$line

        # adb mdns output may contain addresses in several formats.
        # Collect every IPv4:port occurrence.
        $matchesFound = [regex]::Matches(
            $s,
            '(?<!\d)(\d{1,3}(?:\.\d{1,3}){3}):(\d{1,5})(?!\d)'
        )

        foreach ($m in $matchesFound) {
            $endpoint = "$($m.Groups[1].Value):$($m.Groups[2].Value)"
            if (-not $endpoints.Contains($endpoint)) {
                $endpoints.Add($endpoint)
            }
        }
    }

    foreach ($endpoint in $endpoints) {
        $result = Connect-AdbEndpoint -Endpoint $endpoint -RequireQuest
        if ($result) {
            return $result
        }
    }

    return $null
}

# ---------------------------------------------------------------------------
# Optional /24 network scan
# ---------------------------------------------------------------------------

function Get-LocalIpv4Prefixes24 {
    $prefixes = New-Object System.Collections.Generic.HashSet[string]

    try {
        $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop |
            Where-Object {
                $_.IPAddress -notmatch '^127\.' -and
                $_.IPAddress -notmatch '^169\.254\.' -and
                $_.AddressState -ne 'Duplicate'
            }

        foreach ($a in $addresses) {
            if ($a.IPAddress -match '^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}$') {
                [void]$prefixes.Add("$($matches[1]).$($matches[2]).$($matches[3])")
            }
        }
    } catch {
        # .NET fallback if Get-NetIPAddress is unavailable.
        $hostEntry = [System.Net.Dns]::GetHostEntry([System.Net.Dns]::GetHostName())
        foreach ($addr in $hostEntry.AddressList) {
            if ($addr.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork) {
                $ip = $addr.IPAddressToString
                if ($ip -match '^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}$' -and
                    $ip -notmatch '^127\.' -and
                    $ip -notmatch '^169\.254\.') {
                    [void]$prefixes.Add("$($matches[1]).$($matches[2]).$($matches[3])")
                }
            }
        }
    }

    return @($prefixes)
}

function Test-TcpPortFast {
    param(
        [string]$HostName,
        [int]$TcpPort,
        [int]$TimeoutMs = 120
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $iar = $client.BeginConnect($HostName, $TcpPort, $null, $null)
        if (-not $iar.AsyncWaitHandle.WaitOne($TimeoutMs, $false)) {
            return $false
        }

        $client.EndConnect($iar)
        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Try-SubnetScan {
    if (-not $ScanSubnet) {
        return $null
    }

    $prefixes = @(Get-LocalIpv4Prefixes24)
    if ($prefixes.Count -eq 0) {
        Write-Warn "Unable to determine a local IPv4 subnet to scan."
        return $null
    }

    Write-Warn (
        "Network scan requested: looking for a TCP service on port $Port " +
        "on $($prefixes.Count) local /24 subnet(s)."
    )

    foreach ($prefix in $prefixes) {
        Write-Step "Scanning $prefix.0/24 on port $Port..."

        for ($i = 1; $i -le 254; $i++) {
            $ip = "$prefix.$i"

            if (Test-TcpPortFast -HostName $ip -TcpPort $Port) {
                Write-Step "Port $Port is open on $ip; verifying ADB/Quest..."
                $endpoint = "${ip}:$Port"

                $result = Connect-AdbEndpoint -Endpoint $endpoint -RequireQuest
                if ($result) {
                    return $result
                }
            }
        }
    }

    return $null
}

# ---------------------------------------------------------------------------
# Main program
# ---------------------------------------------------------------------------

try {
    Write-Step "Resolving ADB..."

    $script:Adb = Resolve-AdbExecutable -RequestedPath $AdbPath

    if (-not $script:Adb) {
        Stop-Script -Code 2 -Message @"
adb.exe could not be found.

Install Android SDK Platform-Tools and add its directory to PATH,
or run again with:
  -AdbPath "C:\path\to\platform-tools\adb.exe"
"@
    }

    $version = Invoke-Adb -Arguments @("version") -AllowFailure
    if ($version.ExitCode -ne 0) {
        Stop-Script -Code 2 -Message "adb.exe was found but could not be executed: $script:Adb"
    }

    Write-Step "ADB : $script:Adb"
    if (-not $Quiet) {
        $versionLine = ($version.Output | Where-Object { $_ -match '^Version ' } | Select-Object -First 1)
        if ($versionLine) {
            Write-Step ([string]$versionLine)
        }
    }

    if ($script:Adb -match '(?i)\\Unity\\Hub\\Editor\\') {
        Write-Warn (
            "The script is using the ADB bundled with Unity. This works, " +
            "but a standalone Android Platform-Tools installation in PATH is preferable for a stable Codex environment."
        )
    }

    if ($RestartAdbServer) {
        Write-Step "Restarting the ADB server..."
        [void](Invoke-Adb -Arguments @("kill-server") -AllowFailure)
    }

    [void](Invoke-Adb -Arguments @("start-server") -AllowFailure)

    # 1) Is a Quest TCP connection already active?
    Write-Step "Checking existing ADB connections..."
    $devices = Get-AdbDevices
    $tcpDevices = @($devices | Where-Object { $_.IsTcp -and $_.State -eq "device" })

    foreach ($tcpDevice in $tcpDevices) {
        $verified = Test-AdbTcpEndpoint -Endpoint $tcpDevice.Serial -RequireQuest
        if ($verified) {
            $name = Format-DeviceName -DeviceSerial $verified.Endpoint -Properties $verified.Properties
            Write-Ok "Quest is already connected over Wi-Fi: $name"
            exit 0
        }
    }

    # 2) Explicitly provided IP address.
    if ($IpAddress) {
        if ($IpAddress -notmatch '^\d{1,3}(?:\.\d{1,3}){3}$') {
            Stop-Script -Code 1 -Message "Invalid IPv4 address: $IpAddress"
        }

        $endpoint = "${IpAddress}:$Port"
        Write-Step "Attempting a direct connection to $endpoint..."

        $direct = Connect-AdbEndpoint -Endpoint $endpoint -RequireQuest
        if ($direct) {
            $name = Format-DeviceName -DeviceSerial $direct.Endpoint -Properties $direct.Properties
            Write-Ok "Quest connected over Wi-Fi: $name"
            exit 0
        }

        Write-Warn "The direct connection to $endpoint failed."
    }

    # 3) mDNS.
    $mdnsResult = Try-MdnsDiscovery
    if ($mdnsResult) {
        $name = Format-DeviceName -DeviceSerial $mdnsResult.Endpoint -Properties $mdnsResult.Properties
        Write-Ok "Quest discovered and connected via mDNS: $name"
        exit 0
    }

    # 4) Look for an authorized USB Quest.
    Write-Step "Looking for a Quest connected over USB..."
    $usbDevice = Wait-ForUsbQuest -RequestedSerial $Serial -TimeoutSeconds $UsbWaitSeconds

    if (-not $usbDevice) {
        # 5) Optional /24 scan, useful after a PC reboot if the Quest is still listening on 5555
        # and the user does not want to cache its IP address.
        $scanResult = Try-SubnetScan
        if ($scanResult) {
            $name = Format-DeviceName -DeviceSerial $scanResult.Endpoint -Properties $scanResult.Properties
            Write-Ok "Quest found on the network and connected: $name"
            exit 0
        }

        $extra = ""
        if (-not $ScanSubnet) {
            $extra = @"

You can also try:
  .\Connect-QuestAdbWifi.ps1 -IpAddress <QUEST_IP>
or:
  .\Connect-QuestAdbWifi.ps1 -ScanSubnet
if the Quest is already listening on port $Port.
"@
        }

        Stop-Script -Code 3 -Message @"
No authorized USB Quest was found.

If the Quest has restarted, connect it over USB: "adb tcpip" mode must be
enabled again after some headset restarts.

Also check that:
  - Developer Mode is enabled on the Quest;
  - the USB cable supports data transfer (not charging only);
  - the "USB debugging" authorization was accepted in the headset.
$extra
"@
    }

    $questCheck = Test-IsQuest -DeviceSerial $usbDevice.Serial
    if (-not $questCheck.IsQuest) {
        $label = Format-DeviceName -DeviceSerial $usbDevice.Serial -Properties $questCheck.Properties
        if ($Serial) {
            Write-Warn "The selected device is not clearly identified as a Meta Quest: $label"
        } else {
            Write-Warn (
                "Only one USB ADB device is present, but it is not clearly identified as a Meta Quest: $label"
            )
        }
    } else {
        $label = Format-DeviceName -DeviceSerial $usbDevice.Serial -Properties $questCheck.Properties
        Write-Step "USB Quest detected: $label"
    }

    # Retrieve the Wi-Fi IP address.
    Write-Step "Detecting the Quest's Wi-Fi IPv4 address..."
    $questIp = Get-QuestWifiIp -UsbSerial $usbDevice.Serial

    if (-not $questIp) {
        Stop-Script -Code 6 -Message @"
Unable to determine the Quest's Wi-Fi IPv4 address.

Make sure the headset is connected to Wi-Fi, then run the script again.
"@
    }

    Write-Step "Quest Wi-Fi address: $questIp"
    $endpoint = "${questIp}:$Port"

    # Enable TCP/IP.
    Write-Step "Enabling ADB TCP/IP on port $Port..."
    $tcpip = Invoke-Adb -Arguments @("-s", $usbDevice.Serial, "tcpip", "$Port") -AllowFailure

    if ($tcpip.ExitCode -ne 0 -or $tcpip.Text -notmatch '(?i)(restarting|TCP mode|port)') {
        Stop-Script -Code 7 -Message (
            "Unable to enable ADB TCP/IP.`n" +
            "ADB output:`n$($tcpip.Text)"
        )
    }

    # adbd restarts, so allow time for it to come back online.
    Start-Sleep -Milliseconds 1200

    # Connect / verify.
    $connected = Connect-AdbEndpoint -Endpoint $endpoint -RequireQuest

    if (-not $connected) {
        $portTest = $null
        try {
            $portTest = Test-NetConnection -ComputerName $questIp -Port $Port -WarningAction SilentlyContinue
        } catch {}

        $networkHint = ""
        if ($portTest -and $portTest.TcpTestSucceeded) {
            $networkHint = @"

TCP port $Port responds from this PC, but ADB cannot establish a session.
Try the following troubleshooting steps:
  adb kill-server
  adb start-server
then run this script again.
"@
        } elseif ($portTest) {
            $networkHint = @"

TCP port $Port does not respond from this PC.
Make sure the PC and Quest are on the same local network, and check the
firewall, VPN, and Wi-Fi isolation settings.
"@
        }

        Stop-Script -Code 8 -Message @"
ADB TCP/IP was enabled, but the connection to $endpoint could not be verified.
$networkHint
"@
    }

    $name = Format-DeviceName -DeviceSerial $connected.Endpoint -Properties $connected.Properties

    Write-Host ""
    Write-Ok "ADB over Wi-Fi is operational."
    Write-Host "  Endpoint : $endpoint"
    Write-Host "  Device   : $name"
    Write-Host "  ADB      : $script:Adb"
    Write-Host ""
    Write-Host "You can now disconnect the USB cable."
    Write-Host "To verify later: adb devices"
    Write-Host ""

    exit 0
}
catch {
    Stop-Script -Code 1 -Message $_.Exception.Message
}
