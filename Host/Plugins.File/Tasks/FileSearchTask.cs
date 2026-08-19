using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Contracts.Interfaces;
using Contracts.Models;

namespace Plugins.File.Tasks;

public class FileSearchTask : ITask
{
    public string Id => "file-search";

    public string Name => "Поиск файлов";

    public string Description =>
        "Поиск файлов в указанной папке по заданной маске.";

    public IReadOnlyList<TaskParameter> Parameters =>
        new List<TaskParameter>
        {
            new TaskParameter
            {
                Name = "mask",
                DisplayName = "Маска файлов",
                Description = "Например: *.txt",
                IsRequired = true
            },

            new TaskParameter
            {
                Name = "directory",
                DisplayName = "Папка",
                Description = "Путь к папке для поиска",
                IsRequired = true
            }
        };

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        IProgress<double>? progress = null,
        IProgress<string>? log = null)
    {

        if (!parameters.TryGetValue("mask", out var mask) ||
            string.IsNullOrWhiteSpace(mask))
        {
            log?.Report("Не указана маска файлов.");

            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана маска файлов."
            };
        }

        if (!parameters.TryGetValue("directory", out var directory) ||
            string.IsNullOrWhiteSpace(directory))
        {
            log?.Report("Не указана папка для поиска.");

            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана папка для поиска."
            };
        }

        if (!Directory.Exists(directory))
        {
            log?.Report($"Папка не существует: {directory}");

            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Папка не существует: {directory}"
            };
        }

        try
        {
            log?.Report($"Начат поиск файлов.");
            log?.Report($"Маска: {mask}");
            log?.Report($"Папка: {directory}");

            progress?.Report(0);

            cancellationToken.ThrowIfCancellationRequested();

            log?.Report("Поиск файлов в директории...");

            var files = await Task.Run(
                () => Directory
                    .EnumerateFiles(
                        directory,
                        mask,
                        SearchOption.AllDirectories)
                    .ToList(),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            log?.Report($"Поиск завершён. Найдено файлов: {files.Count}");

            if (files.Count == 0)
            {
                progress?.Report(100);
                log?.Report("Подходящих файлов не найдено.");

                return new TaskResult
                {
                    IsSuccess = true,
                    Message = "Файлы не найдены.",
                    Data = files
                };
            }

            for (int i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string file = files[i];
                log?.Report($"Найден файл: {file}");

                double percentage =
                    (i + 1) * 100.0 / files.Count;

                progress?.Report(percentage);

                await Task.Yield();
            }

            progress?.Report(100);
            log?.Report("Задача успешно завершена.");

            return new TaskResult
            {
                IsSuccess = true,
                Message = $"Найдено файлов: {files.Count}",
                Data = files
            };
        }
        catch (OperationCanceledException)
        {
            log?.Report("Поиск был отменён.");

            return new TaskResult
            {
                IsSuccess = false,
                Message = "Поиск был отменён."
            };
        }
        catch (Exception ex)
        {
            log?.Report($"Ошибка поиска: {ex.Message}");

            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Ошибка поиска: {ex.Message}"
            };
        }
    }
}