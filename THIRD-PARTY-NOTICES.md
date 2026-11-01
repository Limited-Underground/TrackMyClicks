# Third-party components

Original TrackMyClicks source is licensed under Apache License 2.0; see LICENSE and NOTICE. There are no third-party NuGet package references or vendored libraries in this source set.

The application uses .NET 10, WPF and Windows Forms from Microsoft. The .NET runtime is primarily MIT-licensed and includes additional third-party components. Any binary package that bundles a runtime must retain the exact runtime distribution's LICENSE.txt and ThirdPartyNotices.txt and include its host/shared files without stripping notices. Do not replace third-party notices with the TrackMyClicks license. Runtime source and license information: https://github.com/dotnet/runtime and https://github.com/dotnet/wpf.

SQLite calls use winsqlite3.dll supplied by Windows. That operating-system component, user32.dll and kernel32.dll are not redistributed by this source repository. A Windows 11 installation is required. SQLite's upstream public-domain information is at https://www.sqlite.org/copyright.html; this does not relicense the Windows operating-system component.

Review exact dependency versions, notices, vulnerabilities and an SBOM again for each distributed binary. The preserved private V0.2/V0.3 evaluation packages are not public releases and are not included in this repository.
