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

ServiceControl, ServiceControl.Monitoring, and ServicePulse are all NuGet package dependencies of this project. Each of these publishes a PlatformSample specific NuGet with each release:

`Particular.PlatformSample.ServiceControl`
`Particular.PlatformSample.ServiceControl.Monitoring`
`Particular.PlatformSample.ServicePulse`

To update each just update the `Version` attribute in the appropriate `PackageReference` located in the `Particular.PlatformSample.csproj` file.

### Configuration Files

The config files in [src/Particular.PlatformSample/configs](https://github.com/Particular/Particular.PlatformSample/tree/master/src/Particular.PlatformSample/configs) are embedded resources that use simple string replacement to replace values such as `{ServiceControlPort}` with the correct values.

In the event that one of the platform applications makes a structural change to the configuration files, the embedded parameterized version will need to be manually updated as well. Each of the application tools now includes an Approval test that will alert updaters in the event they have changed one of these files to prompt them to update this project.


To update the files compare the embedded resource file with to the newly updated sources ensuring the structure is the same. For `ServiceControl.exe.config` and `ServiceControl.Monitoring.exe.config` ensure the `TransportType` remains configured for the Learning Transport.

When finished, commit the changes to a branch and raise a pull request against master. 

## Deploying

As we don't care about patching older releases, the Platform Sample uses a simplified version of Release Flow that, in most cases, does not require `release-X.Y` branches.

* Builds on the master branch will, by default, create an alpha version of the next minor.
* To create a production release, label the master branch with the full version number, and trigger a build.
* All normal releases (most often updating the platform tools versions) should increment the minor.
  * A major version need only be released in the event of a breaking change in the API.
  * Patch releases generally should be avoided. If one is necessary, the release branch would need to be created from the point of the labeled minor, and the patch released from there.
* Once a release is built on master, promote to Deploy, which sends it to Octopus and MyGet.
* Deploy to NuGet by promoting in Octopus.

### Why not use version ranges / wildcard dependency?

Projects using packages.config have control over whether to resolve the **highest matching** or **lowest matching** dependency. However this feature has not yet been added for `PackageReference` in SDK style project files in Visual Studio 2017 and higher.

If, for example, Particular.PlatformSample were to reference Particular.PlatformSample.ServiceControl (>= 4.0.0 && < 5.0.0), then on a project using the platform sample NuGet would resolve the **lowest matching** version within that range. The effect would be that updated Particular.PlatformSample.ServiceControl would not automatically be deployed in projects unless the end user explicitly updated that package to latest.

For that reason, a new minor release of Particular.PlatformSample must (unfortunately) be released every time one of the underlying packages is updated

The NuGet client [has an open issue to allow users to determine package resolution strategy](https://github.com/nuget/home/issues/5553), which is [now unblocked](https://github.com/nuget/home/issues/5553#issuecomment-511509174) but is not yet prioritized. (Previously it was waiting on a [packages lockfile feature](https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies) that has now been released in NuGet 4.9 / Visual Studio 2017 15.9.)

However, even once the issue above is addressed, new versions of Particular.PlatformSample must _still_ be shipped in order to support users running Visual Studio 2017 or 2019 that contain the old NuGet client.
