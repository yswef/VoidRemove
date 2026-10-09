using Microsoft.Extensions.Logging;
using VoidRemove.Core.Backup;
using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Core.Residual;
using VoidRemove.Core.Safety;
using VoidRemove.Core.Uninstall;
using VoidRemove.Shared.Models;

namespace VoidRemove.Core.Engine;

/// <summary>A residual that matched the program, with its verdict.</summary>
public sealed record MatchedResidual(ResidualCandidate Candidate, ResidualMatch Match);

/// <summary>
/// Orchestrates one removal end to end: optional restore point, registry backup, the
/// uninstaller itself, a bounded residual sweep, quarantine and the history report. Every
/// destructive step honours the exclusion list and the dry-run flag.
/// </summary>
public sealed class UninstallEngine
{
    private readonly IResidualScanner _scanner;
    private readonly QuarantineService _quarantine;
    private readonly RegistryBackupService _registryBackup;
    private readonly IUninstallHistoryStore _history;
    private readonly ExclusionList _exclusions;
    private readonly IProcessRunner _processRunner;
    private readonly IRestorePointService _restorePoints;
    private readonly ILogger<UninstallEngine>? _logger;

    public UninstallEngine(
        IResidualScanner scanner,
        QuarantineService quarantine,
        RegistryBackupService registryBackup,
        IUninstallHistoryStore history,
        ExclusionList exclusions,
        IProcessRunner processRunner,
        IRestorePointService restorePoints,
        ILogger<UninstallEngine>? logger = null)
    {
        _scanner = scanner;
        _quarantine = quarantine;
        _registryBackup = registryBackup;
        _history = history;
        _exclusions = exclusions;
        _processRunner = processRunner;
        _restorePoints = restorePoints;
        _logger = logger;
    }

    /// <summary>Computes the residuals a run would remove, without touching anything.</summary>
    public async Task<IReadOnlyList<MatchedResidual>> PreviewAsync(
        ProgramRecord record,
        UninstallOptions options,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ResidualCandidate> candidates = await _scanner.ScanAsync(maxConcurrency, cancellationToken);
        return SelectResiduals(record, candidates, options);
    }

    /// <summary>Runs a full removal and reports the outcome.</summary>
    public async Task<UninstallReport> RunAsync(
        ProgramRecord record,
        UninstallOptions options,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        DateTime started = DateTime.UtcNow;

        if (options.CreateRestorePoint && !options.DryRun)
        {
            await _restorePoints.CreateAsync("Before removing " + record.DisplayName, cancellationToken);
        }

        string? registryBackupPath = null;
        if (!options.DryRun)
        {
            registryBackupPath = await BackupUninstallKeyAsync(record, cancellationToken);
        }

        bool uninstallerOk = await RunUninstallerAsync(record, options, cancellationToken);

        int removed = 0;
        string? quarantineId = null;

        if (options.RemoveResiduals)
        {
            IReadOnlyList<ResidualCandidate> candidates = await _scanner.ScanAsync(maxConcurrency, cancellationToken);
            IReadOnlyList<MatchedResidual> selected = SelectResiduals(record, candidates, options);

            if (selected.Count > 0 && !options.DryRun)
            {
                QuarantineManifest manifest = await _quarantine.QuarantineAsync(
                    selected.Select(static m => m.Candidate.Path).ToList(),
                    record.DisplayName,
                    cancellationToken);

                quarantineId = manifest.Id;
                removed = manifest.Items.Count;
            }
            else if (selected.Count > 0)
            {
                removed = 0;
                quarantineId = null;
            }
        }

        UninstallReport report = new()
        {
            ProgramName = record.DisplayName,
            Publisher = record.Publisher,
            StartedUtc = started,
            FinishedUtc = DateTime.UtcNow,
            Succeeded = uninstallerOk,
            WasDryRun = options.DryRun,
            QuarantineManifestId = quarantineId,
            RegistryBackupPath = registryBackupPath,
            ResidualsRemoved = removed,
        };

        await _history.AddAsync(report, cancellationToken);
        _logger?.LogInformation("Removal of {Program} finished (dry-run={Dry}, ok={Ok}).",
            record.DisplayName, options.DryRun, uninstallerOk);

        return report;
    }

    // ------------------------------------------------------- steps

    private List<MatchedResidual> SelectResiduals(ProgramRecord record, IReadOnlyList<ResidualCandidate> candidates, UninstallOptions options)
    {
        List<MatchedResidual> selected = new();

        foreach (ResidualCandidate candidate in candidates)
        {
            if (_exclusions.IsExcluded(candidate.Path))
            {
                continue;
            }

            ResidualMatch match = ResidualMatcher.Match(record, candidate);
            if (match.IsMatch && match.Level <= options.MaxSafetyLevel)
            {
                selected.Add(new MatchedResidual(candidate, match));
            }
        }

        return selected
            .OrderBy(static m => m.Match.Level)
            .ThenByDescending(static m => m.Match.Score)
            .ToList();
    }

    private async Task<string?> BackupUninstallKeyAsync(ProgramRecord record, CancellationToken cancellationToken)
    {
        string hive = record.IsUserScope ? "HKCU" : "HKLM";
        string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + record.KeyName;

        return await _registryBackup.BackupKeyAsync(hive, keyPath, record.DisplayName, cancellationToken);
    }

    private async Task<bool> RunUninstallerAsync(ProgramRecord record, UninstallOptions options, CancellationToken cancellationToken)
    {
        if (options.DryRun)
        {
            return true;
        }

        string raw = options.PreferQuiet && !string.IsNullOrWhiteSpace(record.QuietUninstallString)
            ? record.QuietUninstallString
            : record.UninstallString;

        UninstallCommand command = UninstallStringParser.Parse(raw, record.IsWindowsInstaller);

        return command.Kind switch
        {
            UninstallKind.MsiExec when !string.IsNullOrEmpty(command.ProductCode) =>
                (await _processRunner.RunAsync(
                    "msiexec.exe",
                    UninstallStringParser.BuildMsiQuietArguments(command.ProductCode),
                    options.Timeout,
                    cancellationToken)).LooksSuccessful,

            UninstallKind.Custom =>
                (await _processRunner.RunAsync(command.Executable, command.Arguments, options.Timeout, cancellationToken)).LooksSuccessful,

            // MSIX removal goes through the package manager, wired in the Windows Apps phase.
            UninstallKind.Msix => true,

            _ => false,
        };
    }
}
