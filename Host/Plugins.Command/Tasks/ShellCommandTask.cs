using Contracts.Interfaces;
using Contracts.Models;
using System.Diagnostics;
using System.Text;

namespace Plugins.Command.Tasks;

public class ShellCommandTask : ITask
{
    public string Id => "shell-command";
    public string Name => "Выполнение Shell-команды";
    public string Description =>"Запуск Shell-команды с отслеживанием вывода и завершения.";

    public IReadOnlyList<TaskParameter> Parameters =>
        new List<TaskParameter>
        {
            new TaskParameter
            {
                Name = "command",
                DisplayName = "Команда",
                Description = "Например: ping",
                IsRequired = true
            },

            new TaskParameter
            {
                Name = "arguments",
                DisplayName = "Аргументы",
                Description = "Например: localhost -n 10",
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
        IProgress<double>? progress = null,
        IProgress<string>? log = null)
    {
        if (!parameters.TryGetValue("command",out var command) || string.IsNullOrWhiteSpace(command))
        {
            log?.Report("Не указана команда.");
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана команда."
            };
        }

        parameters.TryGetValue("arguments",out var arguments);

        parameters.TryGetValue("workingDirectory",out var workingDirectory);


        try
        {
            log?.Report("Подготовка к запуску Shell-команды.");
            log?.Report($"Команда: {command}");
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                log?.Report(
                    $"Аргументы: {arguments}");
            }
            string actualWorkingDirectory =string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory;
            log?.Report($"Рабочая папка: {actualWorkingDirectory}");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = actualWorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                StandardErrorEncoding = Encoding.GetEncoding(866),
            };


            using var process = new Process
            {
                StartInfo = startInfo
            };

            var output =new List<string>();
            var errors =new List<string>();


            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    return;
                lock (output)
                {
                    output.Add(e.Data);
                }

                log?.Report($"[OUT] {e.Data}");
            };


            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    return;

                lock (errors)
                {
                    errors.Add(e.Data);
                }

                log?.Report($"[ERR] {e.Data}");
            };

            log?.Report("Запуск процесса...");
            progress?.Report(0);
            process.Start();

            log?.Report($"Процесс запущен. PID: {process.Id}");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            log?.Report("Получен запрос на отмену.");

                            log?.Report("Завершение процесса...");

                            process.Kill(entireProcessTree: true);
                        }
                    }
                    catch
                    {
    
                    }
                });

            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit();

            int exitCode =process.ExitCode;
            progress?.Report(100);

            log?.Report($"Процесс завершён. Код выхода: {exitCode}");

            if (exitCode == 0)
            {
                log?.Report("Команда выполнена успешно.");
            }
            else
            {
                log?.Report($"Команда завершилась с кодом {exitCode}.");
            }

            return new TaskResult
            {
                IsSuccess = exitCode == 0,

                Message =$"Команда завершена. Код выхода: {exitCode}",

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
            log?.Report("Выполнение команды было отменено.");

            progress?.Report(100);

            return new TaskResult
            {
                IsSuccess = false,
                Message ="Выполнение команды было отменено."
            };
        }
        catch (Exception ex)
        {
            log?.Report($"Ошибка выполнения команды: {ex.Message}");

            return new TaskResult
            {
                IsSuccess = false,
                Message =$"Ошибка выполнения команды: {ex.Message}"
            };
        }
    }
}