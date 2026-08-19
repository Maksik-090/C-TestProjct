using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Contracts.Interfaces;
using Contracts.Models;

namespace Plugins.Command.Tasks;

public class ShellCommandTask : ITask
{
    public string Id => "shell-command";

    public string Name => "Выполнение Shell-команды";

    public string Description =>
        "Запуск Shell-команды с отслеживанием вывода и завершения.";

    public IReadOnlyList<TaskParameter> Parameters =>
        new List<TaskParameter>
        {
            new TaskParameter
            {
                Name = "command",
                DisplayName = "Команда",
                Description = "Например: ping localhost",
                IsRequired = true
            },
            new TaskParameter
            {
                Name = "arguments",
                DisplayName = "Аргументы",
                Description = "Аргументы командной строки",
                IsRequired = false
            },
            new TaskParameter
            {
                Name = "workingDirectory",
                DisplayName = "Рабочая папка",
                Description = "Необязательная рабочая папка",
                IsRequired = false
            }
        };

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        IProgress<double>? progress = null)
    {
        if (!parameters.TryGetValue("command", out var command) ||
            string.IsNullOrWhiteSpace(command))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана команда."
            };
        }

        parameters.TryGetValue("arguments", out var arguments);
        parameters.TryGetValue("workingDirectory", out var workingDirectory);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,

                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };

            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (output)
                    {
                        output.Add(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (errors)
                    {
                        errors.Add(e.Data);
                    }
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Процесс завершиться между проверкой и Kill().
                }
            });

            await process.WaitForExitAsync(cancellationToken);

            var exitCode = process.ExitCode;

            return new TaskResult
            {
                IsSuccess = exitCode == 0,

                Message =
                    $"Команда завершена. Код выхода: {exitCode}",

                Data = new
                {
                    ExitCode = exitCode,
                    Output = output,
                    Errors = errors
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Выполнение команды было отменено."
            };
        }
        catch (Exception ex)
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Ошибка выполнения команды: {ex.Message}"
            };
        }
    }
}