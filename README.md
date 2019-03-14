# Particular.PlatformSample

A package containing the Particular Service Platform for use in samples and tutorials. The package can be used within a sample or tutorial to demonstrate platform capabilities without requiring the user to install the platform or any other dependencies.

Includes binary versions of [ServiceControl](https://github.com/Particular/ServiceControl), [ServiceControl.Monitoring](https://github.com/Particular/ServiceControl.Monitoring), and a self-hosted version of the web assets for [ServicePulse](https://github.com/Particular/ServicePulse). All tools are configured to use the [Learning Transport](https://docs.particular.net/transports/learning/) for the project in which the package is included.


## Usage

Add the following to a console application project:

```
static void Main(string[] args)
{
    Particular.PlatformLauncher.Launch();
}
```

The package will:

1. Copy platform binaries into the project output directory.
1. Find available ports for all the platform tools.
1. Find `.learningtransport` directory and folders for logging.
1. Launch ServiceControl with modified config file.
1. Launch ServiceControl.Monitoring with modified config file.
1. Serve the ServiceControl web assets using the Kestrel HTTP server.
1. Wait for the ServiceControl API to be responsive.
1. Open a browser window pointing to the ServicePulse UI.

This can be seen directly in this repository by launching the **SmokeTest.exe**:

![SmokeTest Output](output.png)

## Options

The `PlatformLauncher.Launch()` API has very limited options available as optional parameters, which can be mixed as needed.

### Showing console output

By default the console outputs of ServiceControl and ServiceControl.Monitoring are suppressed. To view them for purposes of debugging or curiosity, use the `showPlatformToolConsoleOutput` parameter:

```
Particular.PlatformLauncher.Launch(showPlatformToolConsoleOutput: true);
```

### ServicePulse default route

Some samples benefit from opening ServicePulse to a specific view, rather than the Dashboard.

For example, to open ServicePulse to the Monitoring view:

```
Particular.PlatformLauncher.Launch(servicePulseDefaultRoute: "/monitoring");
```


## Updating

ServiceControl, ServiceControl.Monitoring, and ServicePulse are all NuGet packaged dependencies of this project. Each of these publishes a PlatformSample specific NuGet with each release:

`Particular.PlatformSample.ServiceControl`
`Particular.PlatformSample.ServiceControl.Monitoring`
`Particular.PlatformSample.ServicePulse`

To update each just update the `Version` attribute in the appropriate `PackageReference` located in the `Particular.PlatformSample.csproj` file.

### Configuration Files

The config files in [src/Particular.PlatformSample/configs](https://github.com/Particular/Particular.PlatformSample/tree/master/src/Particular.PlatformSample/configs) are embedded resources that use simple string replacement to replace values such as `{ServiceControlPort}` with the correct values.

In the event that one of the platform applications makes a structural change to the configuration files, the embedded parameterized version will need to be manually updated as well. Each of the application tools now includes an Approval test that will alert updaters in the event they have changed one of these files to prompt them to update this project.


To update the files compare the embedded resource file with to the newly updated sources ensuring the structure is the same. For `ServiceControl.exe.config` and `ServiceControl.Monitoring.exe.config` ensure the `TransportType` remains configured for the Learning Transport.

When finished, commit the changes to a branch and raise a pull request against master. 
