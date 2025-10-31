using System.IO.Abstractions;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MQR.Services.Instructions;
using MQR.Services.Instructions.Models.Parsers;
using MQR.Services.Instructions.Models.Queries;

namespace MQR.Services;

/// <summary>
/// Provides access to the various instruction sets we use to control the mainframe.
/// </summary>
public interface IInstructionSetProvider
{
    Task Initialise(string sourceDirectory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all logon instruction sets.
    /// These initialise a mainframe session, authenticate us, and navigate us to the right starting screen.
    /// These differ based on the data source (TOPS, TRUST, etc.)
    /// </summary>
    Task<List<InstructionSetProvider.SessionPoolInfo>> GetSessionPoolRequirements(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the query instruction set with the given identifier.
    /// </summary>
    /// <param name="requestQueryInstructionSet"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    
    Task<QueryInstructionSet> GetQueryInstructionSet(string requestQueryInstructionSet, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the logon instruction set with the given identifier.
    /// </summary>
    /// <param name="logonInstructionSetName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<LogonInstructionSet> GetLogonInstructionSet(string logonInstructionSetName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the logon instruction set with the given identifier.
    /// </summary>
    /// <param name="logonInstructionSetName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ParseInstructionSet> GetParseInstructionSet(string parseInstructionSetName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Just a simple abstraction over IOptions, which already has all the instruction sets configured.
/// </summary>
public class InstructionSetProvider(IFileSystem filesystem) : IInstructionSetProvider
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    private readonly List<LogonInstructionSet> _logons = [];
    private readonly List<QueryInstructionSet> _queries = [];
    private readonly List<ParseInstructionSet> _parsers = [];
    
    public async Task Initialise(string sourceDirectory, CancellationToken cancellationToken = default)
    {
        var subDirs = filesystem.Directory.EnumerateDirectories(sourceDirectory);

        foreach (var subDir in subDirs)
        {
            var dirName = filesystem.Path.GetFileName(subDir);
            var jsonFiles = filesystem.Directory.EnumerateFiles(subDir, "*.json");

            foreach (var jsonFile in jsonFiles.Where(f => !f.Contains("_Base")))
            {
                switch (dirName)
                {
                    case "Logon":
                        var logonSet = await GetInstructionSet<LogonInstructionSet>(jsonFile, cancellationToken);
                        if (logonSet != null)
                        {
                            _logons.Add(logonSet);
                        }
                        break;

                    case "Parse":
                        var parseSet = await GetInstructionSet<ParseInstructionSet>(jsonFile, cancellationToken);
                        if (parseSet != null)
                        {
                            _parsers.Add(parseSet);
                        }
                        break;

                    case "Query":
                        var querySet = await GetInstructionSet<QueryInstructionSet>(jsonFile, cancellationToken);
                        if (querySet != null)
                        {
                            _queries.Add(querySet);
                        }
                        break;
                }
            }
        }
    }

    public Task<List<SessionPoolInfo>> GetSessionPoolRequirements(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _logons.Select(i => new SessionPoolInfo
            {
                PoolId = i.Identifier,
                
            }).ToList()
            );
    }

    private async Task<T?> GetInstructionSet<T>(string jsonFile, CancellationToken cancellationToken = default)
    {
        if (jsonFile.EndsWith(".composed.json", StringComparison.InvariantCultureIgnoreCase))
        {
            return await JsonInstructionComposer.LoadFromHeaderAsync<T>(jsonFile, _options, cancellationToken);
        }

        return await DeserializeAsync<T>(jsonFile, cancellationToken);
    }

    private async Task<T?> DeserializeAsync<T>(string jsonFile, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(filesystem.File.OpenRead(jsonFile) , _options, cancellationToken);
        }
        catch (Exception e)
        {
            throw new SerializationException("Failed to deserialize instruction set from " + jsonFile, e);
        }
        
    }

    public Task<QueryInstructionSet> GetQueryInstructionSet(string instructionSetName, CancellationToken cancellationToken = default)
    {
        var instructionSet = _queries.SingleOrDefault(q => q.Identifier == instructionSetName);
        return instructionSet == null ? throw new KeyNotFoundException($"No query instruction set found with identifier '{instructionSetName}'.") : Task.FromResult(instructionSet);
    }
    
    public Task<LogonInstructionSet> GetLogonInstructionSet(string instructionSetName, CancellationToken cancellationToken = default)
    {
        var instructionSet = _logons.SingleOrDefault(q => q.Identifier == instructionSetName);
        return instructionSet == null ? throw new KeyNotFoundException($"No logon instruction set found with identifier '{instructionSetName}'.") : Task.FromResult(instructionSet);
    }
    
    public Task<ParseInstructionSet> GetParseInstructionSet(string instructionSetName, CancellationToken cancellationToken = default)
    {
        var instructionSet = _parsers.SingleOrDefault(q => q.Identifier == instructionSetName);
        return instructionSet == null ? throw new KeyNotFoundException($"No parse instruction set found with identifier '{instructionSetName}'.") : Task.FromResult(instructionSet);
    }

    public class SessionPoolInfo
    {
        public string PoolId { get; set; }
    }
}