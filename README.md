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

For now, the platform binaries (or in the case of ServicePulse, web assets) are embedded directly in this repository and must be updated manually when new versions of the platform tools are released.

### Updating ServiceControl & Monitoring

First, update local binaries:

1. Install the updated version of ServiceControl.
2. Using ServiceControl Management, update local ServiceControl and Monitoring instances to the latest versions.

Next, update the repository binaries:

1. Copy the ServiceControl binaries to the repository.
    * Usually found in `C:\Program Files (x86)\Particular Software\Particular.ServiceControl`.
    * Copy to `src\Particular.PlatformSample\platform\servicecontrol\servicecontrol-instance`.
    * Do not copy the `.diagnostics` directory. Remove it from the repository if present.
2. Copy the ServiceControl.Monitoring bianries to the repository. 
    * Usually found in `C:\Program Files (x86)\Particular Software\Particular.Monitoring`.
    * Copy to `src\Particular.PlatformSample\platform\servicecontrol\monitoring-instance`.
    * Do not copy the `.diagnostics` directory. Remove it from the repository if present.

Lastly, check the configuration files for structural changes:

* The config files in [src/Particular.PlatformSample/configs](https://github.com/Particular/Particular.PlatformSample/tree/master/src/Particular.PlatformSample/configs) are embedded resources that use simple string replacement to replace values such as `{ServiceControlPort}` with the correct values.
* Compare the **ServiceControl.exe.config** and **Servicecontrol.Monitoring.exe.config** files to the newly updated sources ensuring the structure is the same, given that `TransportType` will be configured for the Learning Transport. Consult with ServiceControl maintainers if necessary.

When finished, commit the changes to a branch and raise a pull request against master.

### Updating ServicePulse

1. Install the updated ServicePulse locally.
2. Locate the binary path, usually `C:\Program Files (x86)\Particular Software\ServicePulse`, and open a PowerShell window there.
3. Locate the destination directory `<RepoRoot>\src\Particular.PlatformSample\platform\servicepulse` and delete everyting inside it. 
4. Run the following command to extract the ServicePulse web assets directly into the repository folder:
    ```
    PS> .\ServicePulse.Host.exe --extract --outPath="<DestinationDirectory>"
    ```
5. The [src/Particular.PlatformSample/configs/app.constants.js](https://github.com/Particular/Particular.PlatformSample/blob/master/src/Particular.PlatformSample/configs/app.constants.js) must be updated to match the version number in [src/Particular.PlatformSample/platform/servicepulse/js/app.constants.js](https://github.com/Particular/Particular.PlatformSample/blob/master/src/Particular.PlatformSample/platform/servicepulse/js/app.constants.js) or the ServicePulse version will not display correctly.
    * If the structure of the two config files differs in any way, consult with ServicePulse maintainers.

When finished, commit the changes to a branch and raise a pull request against master.
