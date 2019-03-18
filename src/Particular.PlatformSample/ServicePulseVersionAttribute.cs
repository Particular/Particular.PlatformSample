using System;

[AttributeUsage(AttributeTargets.Assembly)]
class ServicePulseVersionAttribute : Attribute
{
    public string Version { get; }

    public ServicePulseVersionAttribute(string version)
    {
        Version = version;
    }
}