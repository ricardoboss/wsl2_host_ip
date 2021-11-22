using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

const string hostsFile = "C:\\Windows\\System32\\drivers\\etc\\hosts";
const string hostEntryName = "wsl2.host";

var wslIp = (
    from ni in NetworkInterface.GetAllNetworkInterfaces()
    where ni.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet
    where ni.Name == "vEthernet (WSL)"
        from ip in ni.GetIPProperties().UnicastAddresses
        where ip.Address.AddressFamily == AddressFamily.InterNetwork
        select ip.Address.ToString()
).FirstOrDefault();

if (wslIp == null)
    return;

var entryExists = false;
var lines = File.ReadLines(hostsFile).ToArray();
for (var i = 0; i < lines.Length; i++)
{
    if (!lines[i].Contains(hostEntryName))
        continue;

    lines[i] = wslIp + "\t" + hostEntryName;
    entryExists = true;
    break;
}

var entries = lines.ToList();
if (!entryExists)
{
    entries.Add(wslIp + "\t" + hostEntryName);
}

try
{
    File.WriteAllLines(hostsFile, entries);
}
catch (UnauthorizedAccessException)
{
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("LoggingConsoleApp.Program", LogLevel.Warning)
            .AddEventLog();
    });

    var logger = loggerFactory.CreateLogger("WSL2_Host_Ip");
    logger.LogError("Access denied when updating hosts file.");
}
