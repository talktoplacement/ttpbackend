// Global usings for CareerPlatform.Api.
//
// ImplicitUsings is enabled (see Directory.Build.props), which already provides the
// broadly-used BCL and ASP.NET Core namespaces for the Web SDK:
//   System, System.Collections.Generic, System.IO, System.Linq, System.Net.Http,
//   System.Threading, System.Threading.Tasks,
//   Microsoft.AspNetCore.Builder, Microsoft.AspNetCore.Http, Microsoft.AspNetCore.Routing,
//   Microsoft.Extensions.Configuration, Microsoft.Extensions.DependencyInjection,
//   Microsoft.Extensions.Hosting, Microsoft.Extensions.Logging.
// Re-declaring any of those here would raise duplicate-using warnings (CS0105) that CI
// treats as errors, so only namespaces NOT covered by the implicit set are added below.

global using System.Text.Json;
global using Microsoft.Extensions.Options;

// The project's own CareerPlatform.Api.Common namespace now contains types (task 2.1),
// so it is exposed as a global using across the project.
global using CareerPlatform.Api.Common;
