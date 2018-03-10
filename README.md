# Firestone
Hearthstone lobby emulator written in C#

Developed in Visual Studio 2017. Targets .NET Core 2.0.

### Pre-requisites

- .NET Core 2.0 (https://github.com/dotnet/core)
- Google Protobuf (installed if needed via the build instructions below)
- [Optional] An SSL certificate (created if needed via the build instructions below)
- `.proto` files describing the Hearthstone lobby server protocol (see below)
- [Windows] PowerShell 3 or later
- [Linux] OpenSSL
- A modified Hearthstone client (see below)

### Build

First clone the repository:

```
git clone https://github.com/HearthCode/Firestone
cd Firestone
```

Run the pre-build setup:

**Windows:**

```
powershell -file setup.ps1 -protopath <PATH_TO_YOUR_PROTO_DIRECTORY> -domain <FQDN>
```

**Linux:** Not implemented yet.

Replace `<PATH_TO_YOUR_PROTO_DIRECTORY>` with the name of a directory containing your Hearthstone server proto files. If no proto path is specified, any existing protos in the solution's `protos` folder will be re-used instead.

Replace `<FQDN>` with the fully-qualified domain name of your server. This is used for SSL certificate generation. If not specified, uses `localhost`.

This will download Google Protobuf if needed, create the needed directory structure, copy and compile your `.proto` files into C# source code, and generate an SSL certificate called `Firestone.pfx` in the Firestone project directory.

If `Firestone.pfx` already exists, the existing certificate will be re-used. If you have a pre-existing certificate you would like to use, overwrite `Firestone.pfx` with your own `.pfx` file.

Your `.proto` files must use the `proto3` syntax and include `option csharp_namespace=...` for each package using all-lowercase namespaces, with no nested folders. These are not provided in the repository - you must obtain them yourself.

Build the application:

```
dotnet restore -r <RID>
dotnet publish -c Release -r <RID>
```

Replace `<RID>` with a resource identifier string from https://docs.microsoft.com/en-us/dotnet/core/rid-catalog

### Configuration

Edit `firestone-conf.json` to configure the behaviour of the server.

Edit `log4net.config.xml` to configure logging behaviour.

### Usage

Simply start the `Firestone` binary/executable to start the server.

Connections using the official Hearthstone client require two steps to prepare the client:

1. Add or edit `client.config` in the Hearthstone root folder so that it looks like this:

```
[Config]
Version = 3

[Aurora]
Version.Source = "product"
Backend = BattleNetCSharp
Env.Override = 1
Env = localhost
Port = 1119

[Localization]
Locale = enUS

[Application]
Mode = Public
```

Replace the settings for `Env` and `Port` to match those of your Firestone server.

2. Modify the Hearthstone client code so that validation of the server-side SSL certificate is removed or always passes. How to do this is beyond the scope of this document.


## FAQ

**Q. Why are you making a Hearthstone server?**

A few reasons:

- Why do people climb Mt Everest? To prove it can be done.
- For fun!
- For education in a range of technologies while working on something we're passionate about (Hearthstone!)
- To mess around making new cards and game modes
- To write AIs to compete with each other and humans without disturbing real players or the official servers
- Blizzard, notice me senpai, give me a job :)

Things we are not interested in:

- Profit
- Ladder bots. AIs for Firestone will be deliberately incompatible with the official servers
- Evading the need to buy packs. We have bought tens of thousands of packs on the official servers

**Q. Who is the target demographic?**

Developers and those with an interest in AI and/or game design.

This project is not aimed at people who just want to play Hearthstone. If you want to do that, play the real game.

**Q. Why is this project public?**

The other Hearthstone servers we are aware of are all in private repositories. This has led to project stagnation and we would like to keep an active developer community.

**Q. What is the project maintainer's background?**

- Professional Hearthstone player in the Nordic region (2015-2017)
- Worked at NDS (now a part of Cisco) as a black hat for pay TV smart card security

**Q. Does this server do anything useful?**

Not yet. We just started on it.

**Q. Can you tell me how to get the `.proto` files needed to compile the server?**

No.

**Q. Can you tell me how to modify the Hearthstone client so it can connect to Firestone?**

No.

**Q. Is this server illegal? Does it break Blizzard's ToS?**

We're not lawyers. The server contains no copyright infringing elements. It is written from the ground up, and does not interact with the official servers, does not use any code from the Hearthstone client and is not a derivative work.

**Q. Does using this server break Blizzard's ToS?**

That's for Blizzard to decide. Use at your own risk.

**Q. Can I run a copy of this server and monetize it?**

Only if you want to be met with fire and fury like the world has never seen.

**Q. How can I join the developer community?**

We use a private Discord server to collaborate on development. Access will be granted on an invite-only basis. Show us you have something to offer.
