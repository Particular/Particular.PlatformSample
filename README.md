# NOT A REAL EDIT - BUT I AM EXTERNAL!

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

1. In TeamCity, find the latest ServiceControl build on the `master` branch, download the following artifacts, and unzip:
    * zip/Particular.ServiceControl-{VERSION}.zip
    * zip/Particular.Servicecontrol.Monitoring-{VERSION}.zip
2. Update the ServiceControl binaries:
    1. In the repository, delete the contents of `src\Particular.PlatformSample\platform\servicecontrol\servicecontrol-instance`.
    2. Replace with the contents of the `ServiceControl` folder from the ServiceControl zip file.
    3. Add the contents of the `Transports/LearningTransport` folder from the ServiceControl zip file.
3. Update the Monitoring binaries:
    1. In the repository, delete the contents of  `src\Particular.PlatformSample\platform\servicecontrol\monitoring-instance`.
    2. Replace with the contents of the `ServiceControl.Monitoring` folder from the Monitoring zip file.
    3. Add the contents of the `Transports/LearningTransport` folder from the Monitoring zip file. This will include duplicates (things like Autofac, Nancy, etc.) that you can just not replace in the destination directory. Also skip 
4. Check the new configuration files for structural changes:
    * The config files in [src/Particular.PlatformSample/configs](https://github.com/Particular/Particular.PlatformSample/tree/master/src/Particular.PlatformSample/configs) are embedded resources that use simple string replacement to replace values such as `{ServiceControlPort}` with the correct values.
    * Compare the **ServiceControl.exe.config** and **Servicecontrol.Monitoring.exe.config** files to the newly updated sources ensuring the structure is the same, given that `TransportType` will be configured for the Learning Transport. Consult with ServiceControl maintainers if necessary.
5. Remove any pdb files or config files that show up as file additions in the git diff.

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
