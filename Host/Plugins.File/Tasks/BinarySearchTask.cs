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
        IProgress<double>? progress = null,
        IProgress<string>? log = null)
    {
        if (!parameters.TryGetValue(
                "sequence",
                out var sequence) ||
            string.IsNullOrEmpty(sequence))
        {
            log?.Report(
                "Не указана искомая последовательность.");

            return new TaskResult
            {
                IsSuccess = false,
                Message =
                    "Не указана искомая последовательность."
            };
        }

        if (!parameters.TryGetValue(
                "file",
                out var file) ||
            string.IsNullOrWhiteSpace(file))
        {
            log?.Report("Не указан файл.");

            return new TaskResult
            {
                IsSuccess = false,
                Message = "Не указан файл."
            };
        }

        if (!System.IO.File.Exists(file))
        {
            log?.Report(
                $"Файл не существует: {file}");

            return new TaskResult
            {
                IsSuccess = false,
                Message =
                    $"Файл не существует: {file}"
            };
        }

        try
        {
            log?.Report(
                "Начат поиск последовательности.");

            log?.Report(
                $"Файл: {file}");

            log?.Report(
                $"Искомая последовательность: \"{sequence}\"");


            byte[] pattern =
                System.Text.Encoding.ASCII.GetBytes(sequence);


            log?.Report(
                $"Размер искомой последовательности: {pattern.Length} байт.");

            progress?.Report(0);

            var positions = await Task.Run(
                () => FindOccurrences(
                    file,
                    pattern,
                    cancellationToken,
                    progress,
                    log),
                cancellationToken);


            log?.Report(
                $"Поиск завершён.");

            log?.Report(
                $"Найдено вхождений: {positions.Count}");

            progress?.Report(100);


            return new TaskResult
            {
                IsSuccess = true,

                Message =
                    $"Найдено вхождений: {positions.Count}",

                Data = positions
            };
        }
        catch (OperationCanceledException)
        {
            log?.Report(
                "Поиск был отменён.");

            return new TaskResult
            {
                IsSuccess = false,
                Message =
                    "Поиск был отменён."
            };
        }
        catch (Exception ex)
        {
            log?.Report(
                $"Ошибка поиска: {ex.Message}");

            return new TaskResult
            {
                IsSuccess = false,
                Message =
                    $"Ошибка поиска: {ex.Message}"
            };
        }
    }

    private static List<long> FindOccurrences(
        string file,
        byte[] pattern,
        CancellationToken cancellationToken,
        IProgress<double>? progress,
        IProgress<string>? log)
    {
        var positions = new List<long>();


        if (pattern.Length == 0)
        {
            return positions;
        }


        const int bufferSize =
            1024 * 1024;


        using var stream =
            new System.IO.FileStream(
                file,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.Read,
                bufferSize,
                useAsync: false);


        var buffer =
            new byte[bufferSize];


        long filePosition = 0;

        int bytesRead;


        log?.Report(
            $"Размер файла: {stream.Length:N0} байт.");

        log?.Report(
            "Начато чтение файла блоками по 1 МБ.");


        while ((bytesRead =
            stream.Read(
                buffer,
                0,
                buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (
                int i = 0;
                i <= bytesRead - pattern.Length;
                i++)
            {
                bool match = true;


                for (
                    int j = 0;
                    j < pattern.Length;
                    j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }


                if (match)
                {
                    long position =
                        filePosition + i;

                    positions.Add(position);

                    log?.Report(
                        $"Найдено вхождение. Позиция: {position}");
                }
            }


            filePosition += bytesRead;

            if (stream.Length > 0)
            {
                double percentage =
                    (double)filePosition /
                    stream.Length *
                    100;


                progress?.Report(
                    percentage);
            }
        }
        return positions;
    }
}