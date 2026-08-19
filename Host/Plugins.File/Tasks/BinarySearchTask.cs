using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Contracts.Interfaces;
using Contracts.Models;

namespace Plugins.File.Tasks;

public class BinarySearchTask : ITask
{
    public string Id => "binary-search";
    public string Name => "Поиск последовательности в файле";
    public string Description =>
        "Поиск последовательности однобайтовых символов в бинарном файле.";
    public IReadOnlyList<TaskParameter> Parameters =>
        new List<TaskParameter>
        {
            new TaskParameter
            {
                Name = "sequence",
                DisplayName = "Искомая последовательность",
                Description = "Например: libsec",
                IsRequired = true
            },
            new TaskParameter
            {
                Name = "file",
                DisplayName = "Файл",
                Description = "Полный путь к бинарному файлу",
                IsRequired = true
            }
        };

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        IProgress<double>? progress = null)
    {
        if (!parameters.TryGetValue("sequence", out var sequence) ||
            string.IsNullOrEmpty(sequence))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указана искомая последовательность."
            };
        }

        if (!parameters.TryGetValue("file", out var file) ||
            string.IsNullOrWhiteSpace(file))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указан файл."
            };
        }

        if (!System.IO.File.Exists(file))
        {
            return new TaskResult
            {
                IsSuccess = false,
                Message = $"Файл не существует: {file}"
            };
        }

        try
        {
            byte[] pattern = System.Text.Encoding.ASCII.GetBytes(sequence);

            var positions = await Task.Run(
                () => FindOccurrences(
                    file,
                    pattern,
                    cancellationToken,
                    progress),
                cancellationToken);

            return new TaskResult
            {
                IsSuccess = true,
                Message = $"Найдено вхождений: {positions.Count}",
                Data = positions
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

    private static List<long> FindOccurrences(
        string file,
        byte[] pattern,
        CancellationToken cancellationToken,
        IProgress<double>? progress)
    {
        var positions = new List<long>();

        if (pattern.Length == 0)
        {
            return positions;
        }

        const int bufferSize = 1024 * 1024;

        using var stream = new System.IO.FileStream(
            file,
            System.IO.FileMode.Open,
            System.IO.FileAccess.Read,
            System.IO.FileShare.Read,
            bufferSize,
            useAsync: false);

        var buffer = new byte[bufferSize];
        long filePosition = 0;
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int i = 0; i <= bytesRead - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    positions.Add(filePosition + i);
                }
            }

            filePosition += bytesRead;

            if (stream.Length > 0)
            {
                double percentage =
                    (double)filePosition / stream.Length * 100;

                progress?.Report(percentage);
            }
        }

        return positions;
    }
}
