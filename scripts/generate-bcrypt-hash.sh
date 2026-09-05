#!/usr/bin/env bash
# =============================================================================
#  generate-bcrypt-hash.sh — emits a BCrypt hash compatible with the app's
#  BcryptPasswordHasher (work factor 12).
#
#  Usage:
#    ./generate-bcrypt-hash.sh 'YourPasswordHere'
#
#  Copy the printed hash and paste it into admin-operations.sql where the
#  placeholder '$2a$12$REPLACE_WITH_BCRYPT_HASH' appears.
#
#  Requires: .NET 8 SDK (or a machine that can `dotnet script` / `dotnet run`).
#  The script spins up an ephemeral .NET csx file to hash — this way the hash
#  is guaranteed compatible with the same BCrypt.Net-Next library the app uses,
#  and no third-party website ever sees the password.
# =============================================================================
set -euo pipefail

if [ "$#" -ne 1 ] || [ -z "${1-}" ]; then
  echo "Usage: $0 'PasswordToHash'" >&2
  exit 1
fi

PASSWORD="$1"

WORKDIR=$(mktemp -d)
trap 'rm -rf "$WORKDIR"' EXIT

cat > "$WORKDIR/Program.cs" <<'CS'
using System;
using BCrypt.Net;

var pw = Environment.GetEnvironmentVariable("PP_PW") ?? string.Empty;
if (string.IsNullOrEmpty(pw))
{
    Console.Error.WriteLine("PP_PW environment variable is empty.");
    Environment.Exit(1);
}
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(pw, 12));
CS

cat > "$WORKDIR/bcrypt.csproj" <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>BcryptHelper</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
PROJ

# Run silently; only the hash goes to stdout.
PP_PW="$PASSWORD" DOTNET_ROLL_FORWARD=Major \
  dotnet run --project "$WORKDIR" --configuration Release 2>/dev/null
