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
        IProgress<double>? progress = null)
    {
        if (!parameters.TryGetValue("mask", out var mask) ||
            string.IsNullOrWhiteSpace(mask))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана маска файлов."
            };
        }

        if (!parameters.TryGetValue("directory", out var directory) ||
            string.IsNullOrWhiteSpace(directory))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана папка для поиска."
            };
        }

        if (!Directory.Exists(directory))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Папка не существует: {directory}"
            };
        }

        try
        {
            var files = await Task.Run(
                () => Directory
                    .EnumerateFiles(
                        directory,
                        mask,
                        SearchOption.AllDirectories)
                    .ToList(),
                cancellationToken);

            return new TaskResult
            {
                IsSuccess = true,
                Message = $"Найдено файлов: {files.Count}",
                Data = files
            };
        }
        catch (OperationCanceledException)
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Поиск был отменён."
            };
        }
        catch (Exception ex)
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Ошибка поиска: {ex.Message}"
            };
        }
    }
}