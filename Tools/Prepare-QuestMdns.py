"""Restore pinned mDNS inputs and reproducibly build the small HiBoP transport adaptation."""
import hashlib
import json
import pathlib
import shutil
import subprocess
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORK = ROOT / '.artifacts/quest-ncm/vendor'
OUTPUT = ROOT / 'Assets/Plugins/Managed/QuestMdns'
PINS = {
    'mdns-source.zip': ('https://github.com/jdomnitz/net-mdns/archive/refs/tags/v0.38.zip', '2e77a2b1715e892ea8655e33adba0e69753a479df2a5c62e3e7b4e778ffb3268'),
    'dns.nupkg': ('https://api.nuget.org/v3-flatcontainer/makaretu.dns.new/3.1.2/makaretu.dns.new.3.1.2.nupkg', '6f617bdbcaeea1cc1cc0231b7f77607ceb8a76f9bf605e4735ecdaa8764c7bcd'),
    'logging.nupkg': ('https://api.nuget.org/v3-flatcontainer/common.logging/3.4.1/common.logging.3.4.1.nupkg', 'd7179b9736214022af794bfacfc675ec3a753fa28a751f311b2925eeda6a6d07'),
    'logging-core.nupkg': ('https://api.nuget.org/v3-flatcontainer/common.logging.core/3.4.1/common.logging.core.3.4.1.nupkg', '5e167886779fc6311c426e8bb3f3853d908338d16cea3fed33d08a400f03081a'),
}
LICENSES = {
    'Common.Logging-LICENSE.txt': ('https://raw.githubusercontent.com/net-commons/common-logging/master/license.txt', '1eb85fc97224598dad1852b5d6483bbcf0aa8608790dcc657a5a2a761ae9c8c6'),
    'Dns-LICENSE.txt': ('https://raw.githubusercontent.com/jdomnitz/net-dns/master/LICENSE', '37920e72b0c28aa74ad0eb9b37814a69d44614eb1991c0f1cd7e6caad735d7ad'),
}

def main():
    sdk = subprocess.check_output(['dotnet', '--version'], text=True).strip()
    if sdk != '10.0.201':
        raise RuntimeError(f'Use .NET SDK 10.0.201 to reproduce the tested binaries (found {sdk}).')
    WORK.mkdir(parents=True, exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, (url, digest) in LICENSES.items():
        license_path = OUTPUT / name
        if not license_path.exists():
            urllib.request.urlretrieve(url, license_path)
        if hashlib.sha256(license_path.read_bytes()).hexdigest() != digest:
            raise RuntimeError(f'License hash mismatch: {name}')
    for name, (url, digest) in PINS.items():
        archive = WORK / name
        if not archive.exists():
            urllib.request.urlretrieve(url, archive)
        if hashlib.sha256(archive.read_bytes()).hexdigest() != digest:
            raise RuntimeError(f'Hash mismatch: {name}')
        with zipfile.ZipFile(archive) as package:
            package.extractall(WORK / 'extracted' / (pathlib.Path(name).stem if name != 'mdns-source.zip' else 'source'))
    source = WORK / 'extracted/source/net-mdns-0.38/src'
    shutil.copyfile(ROOT / 'Tools/QuestMdns/MulticastClient.cs', source / 'MulticastClient.cs')
    shutil.copyfile(ROOT / 'Tools/QuestMdns/MdnsNativeReceiver.cs', source / 'MdnsNativeReceiver.cs')
    service = source / 'MulticastService.cs'
    content = service.read_text(encoding='utf-8-sig')
    content = content.replace('public void OnDnsMessage(object sender, UdpReceiveResult result)',
        'public void OnDnsMessage(object sender, UdpReceiveResult result)\n        { OnDnsMessage(result, 0); }\n\n        public void OnDnsMessage(object sender, MdnsPacketEventArgs packet)\n        { OnDnsMessage(packet.Result, packet.InterfaceIndex); }\n\n        private void OnDnsMessage(UdpReceiveResult result, int interfaceIndex)')
    content = content.replace('RemoteEndPoint = result.RemoteEndPoint }', 'RemoteEndPoint = result.RemoteEndPoint, InterfaceIndex = interfaceIndex }')
    # The application owns interface generations and recreation, including the portable timer fallback.
    content = content.replace('NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;', '// HiBoP owns interface monitoring.')
    content = content.replace('log.Error("FindNics failed", e);', 'throw new System.IO.IOException("Could not initialize mDNS network interfaces.", e);')
    content = content.replace('public void Start()', 'public async Task StopAsync()\n        {\n            var previous = client;\n            Stop();\n            if (previous != null) await previous.Completion.ConfigureAwait(false);\n        }\n\n        public void Start()')
    content = content.replace('public void Start()', 'public bool HasReceiverFailure => client?.HasReceiverFailure == true;\n\n        public void Start()')
    service.write_text(content, encoding='utf-8')
    event = source / 'MessageEventArgs.cs'
    event.write_text(event.read_text(encoding='utf-8-sig').replace('public Message Message', 'public int InterfaceIndex { get; set; }\n        public Message Message'), encoding='utf-8')
    dependencies = [
        ('dns', 'netstandard2.0', 'Makaretu.Dns.dll'),
        ('logging', 'netstandard1.3', 'Common.Logging.dll'),
        ('logging-core', 'netstandard1.0', 'Common.Logging.Core.dll'),
    ]
    references = ''
    for package, framework, dll in dependencies:
        candidates = list((WORK / f'extracted/{package}/lib').glob(f'*/{dll}'))
        binary = next((p for p in candidates if p.parent.name == framework), candidates[0])
        shutil.copyfile(binary, OUTPUT / dll)
        references += f'<Reference Include="{dll}"><HintPath>{OUTPUT / dll}</HintPath></Reference>'
    (source / 'HiBoP.Mdns.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><AssemblyName>Makaretu.Dns.Multicast.New</AssemblyName><LangVersion>latest</LangVersion><Deterministic>true</Deterministic><DebugType>none</DebugType><DebugSymbols>false</DebugSymbols><AssemblyVersion>0.38.0.0</AssemblyVersion><Version>0.38.0</Version></PropertyGroup><ItemGroup>' + references + '</ItemGroup></Project>', encoding='utf-8')
    subprocess.run(['dotnet', 'build', str(source / 'HiBoP.Mdns.csproj'), '-c', 'Release', '-o', str(WORK / 'built'), '--nologo'], check=True)
    shutil.copyfile(WORK / 'built/Makaretu.Dns.Multicast.New.dll', OUTPUT / 'Makaretu.Dns.Multicast.New.dll')
    shutil.copyfile(source.parent / 'LICENSE', OUTPUT / 'LICENSE.txt')
    expected = json.loads((ROOT / 'Tools/QuestMdns/ManagedPlugins.lock.json').read_text())
    for dll in OUTPUT.glob('*.dll'):
        digest = hashlib.sha256(dll.read_bytes()).hexdigest()
        if expected.get(dll.name) != digest:
            raise RuntimeError(f'Rebuilt dependency differs from its tested pin: {dll.name} ({digest})')
        print(dll.name, digest)

if __name__ == '__main__':
    main()
