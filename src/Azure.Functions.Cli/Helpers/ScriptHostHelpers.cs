﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.Diagnostics;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Description;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Functions.Cli.Helpers
{
    public static class ScriptHostHelpers
    {
        private const System.Diagnostics.TraceLevel DefaultTraceLevel = System.Diagnostics.TraceLevel.Info;
        private static bool _isHelpRunning = false;

        public static void SetIsHelpRunning()
        {
            _isHelpRunning = true;
        }

        public static FunctionMetadata GetFunctionMetadata(string functionName)
        {
            var functionErrors = new Dictionary<string, Collection<string>>();
            var functions = ScriptHost.ReadFunctionMetadata(new ScriptHostConfiguration(), new ConsoleTraceWriter(System.Diagnostics.TraceLevel.Info), null, functionErrors);
            var function = functions.FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
            if (function == null)
            {
                var error = functionErrors
                    .FirstOrDefault(f => f.Key.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                    .Value
                    ?.Aggregate(string.Empty, (a, b) => string.Join(Environment.NewLine, a, b));
                throw new FunctionNotFoundException($"Unable to get metadata for function {functionName}. Error: {error}");
            }
            else
            {
                return function;
            }
        }

        public static string GetFunctionAppRootDirectory(string startingDirectory, IEnumerable<string> searchFiles = null)
        {
            if (_isHelpRunning)
            {
                return startingDirectory;
            }

            searchFiles = searchFiles ?? new List<string> { ScriptConstants.HostMetadataFileName };

            if (searchFiles.Any(file => FileSystemHelpers.FileExists(Path.Combine(startingDirectory, file))))
            {
                return startingDirectory;
            }

            var parent = Path.GetDirectoryName(startingDirectory);

            if (parent == null)
            {
                var files = searchFiles.Aggregate((accum, file) => $"{accum}, {file}");
                throw new CliException($"Unable to find project root. Expecting to find one of {files} in project root.");
            }
            else
            {
                return GetFunctionAppRootDirectory(parent, searchFiles);
            }
        }

        internal static async Task<System.Diagnostics.TraceLevel> GetTraceLevel(string scriptPath)
        {
            var filePath = Path.Combine(scriptPath, ScriptConstants.HostMetadataFileName);
            if (!FileSystemHelpers.FileExists(filePath))
            {
                return DefaultTraceLevel;
            }

            var hostJson = JsonConvert.DeserializeObject<JObject>(await FileSystemHelpers.ReadAllTextFromFileAsync(filePath));
            var traceLevelStr = hostJson["tracing"]?["consoleLevel"]?.ToString();
            if (!string.IsNullOrEmpty(traceLevelStr) && Enum.TryParse(traceLevelStr, true, out System.Diagnostics.TraceLevel traceLevel))
            {
                return traceLevel;
            }
            else
            {
                return DefaultTraceLevel;
            }
        }
    }
}
