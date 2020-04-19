# SimpleIPCHttp
A simple IPC library. It can be used between processes on the same machine, or across networks. If you really wanted to,
you can even use this to communicate between parts of the same application.

## Installation
This package is hosted on [NuGet](https://www.nuget.org/packages/SimpleIPCHttp/).

You can install it from the command line with the following command:
```
Install-Package SimpleIPCHttp
```

## Usage
```csharp
var i1 = new IpcInterface();
var i2 = new IpcInterface(i1.PartnerPort, i1.Port);

i1.On<DummyClass>(dummyClass => { Console.WriteLine("Message Received!") });
await i2.SendMessage(new DummyClass());
```