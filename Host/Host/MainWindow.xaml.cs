using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Contracts.Interfaces;
using Host.Models;
using Host.Services;

namespace Host;

public partial class MainWindow : Window
{
    private readonly List<TaskItem> _tasks = new();
    private readonly ObservableCollection<ExecutionItem> _executions = new();
    private CancellationTokenSource? _currentCancellationTokenSource;
    private readonly Dictionary<string, TextBox> _parameterInputs = new();


    public MainWindow()
    {
        InitializeComponent();
        ExecutionGrid.ItemsSource = _executions;
        LoadPlugins();
    }


    // ЗАГРУЗКА ПЛАГИНОВ

    private void LoadPlugins()
    {
        try
        {
            string pluginsDirectory = System.IO.Path.Combine(AppContext.BaseDirectory,"Plugins");
            var loader = new PluginLoader();
            var plugins =loader.LoadPlugins(pluginsDirectory);
            _tasks.Clear();

            foreach (var plugin in plugins)
            {
                foreach (var task in plugin.GetTasks())
                {
                    _tasks.Add(new TaskItem(task,plugin));
                }
            }
            TasksGrid.ItemsSource = null;
            TasksGrid.ItemsSource = _tasks;
            PluginsCountText.Text = $"Плагинов: {plugins.Count}";
            TasksCountText.Text = $"Задач: {_tasks.Count}";
            StatusText.Text = "Плагины успешно загружены.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка загрузки плагинов.";


            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    // ВЫБОР ЗАДАЧИ
    private void TasksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TasksGrid.SelectedItem is not TaskItem selectedTask)
        {
            SelectedTaskText.Text = "Задача не выбрана";
            RunButton.IsEnabled = false;
            ClearParameters();
            return;
        }


        SelectedTaskText.Text =selectedTask.Name;
        RunButton.IsEnabled = true;
        BuildParameterControls(selectedTask.Task);
    }


    // СОЗДАНИЕ ПОЛЕЙ ПАРАМЕТРОВ
    private void BuildParameterControls(ITask task)
    {
        ParametersPanel.Children.Clear();
        ParametersPanel.RowDefinitions.Clear();
        _parameterInputs.Clear();

        if (task.Parameters.Count == 0)
        {
            ParametersPanel.Children.Add(new TextBlock
                {
                    Text ="Эта задача не требует параметров.",
                    Margin =new Thickness(5)
                });
            return;
        }


        int row = 0;
        foreach (var parameter in task.Parameters)
        {
            ParametersPanel.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =GridLength.Auto
                });


            // Название
            var label = new TextBlock
            {
                Text =
                    parameter.DisplayName + (parameter.IsRequired ? " *" : ""),

                VerticalAlignment = VerticalAlignment.Center,

                Margin = new Thickness(5)
            };


            Grid.SetRow(label, row);

            Grid.SetColumn(label, 0);


            ParametersPanel.Children.Add(label);


            // Поле
            var textBox = new TextBox
            {
                Margin = new Thickness(5),

                MinHeight = 26,

                Text = parameter.DefaultValue
            };


            Grid.SetRow(textBox, row);
            Grid.SetColumn(textBox, 1);


            ParametersPanel.Children.Add(textBox);


            // Описание
            var description = new TextBlock
            {
                Text =parameter.Description,

                Foreground =Brushes.Gray,

                VerticalAlignment = VerticalAlignment.Center,

                Margin = new Thickness(5)
            };


            Grid.SetRow(description,row);
            Grid.SetColumn(description,2);


            ParametersPanel.Children.Add(description);
            _parameterInputs[parameter.Name] =textBox;
            row++;
        }
    }


    // ОЧИСТКА ПАРАМЕТРОВ
    private void ClearParameters()
    {
        ParametersPanel.Children.Clear();
        ParametersPanel.RowDefinitions.Clear();
        _parameterInputs.Clear();
    }


    // ЗАПУСК ЗАДАЧИ
    private async void RunButton_Click(object sender,RoutedEventArgs e)
    {
        if (TasksGrid.SelectedItem is not TaskItem selectedTask)
        {
            return;
        }



        // Собираем параметры
        var parameters = new Dictionary<string, string>();
        foreach (var parameter in selectedTask.Task.Parameters)
        {
            if (!_parameterInputs.TryGetValue(parameter.Name,out var textBox))
            {
                continue;
            }
            string value = textBox.Text;

            if (parameter.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show($"Необходимо заполнить параметр: {parameter.DisplayName}","Параметры", MessageBoxButton.OK, MessageBoxImage.Warning);
                textBox.Focus();
                return;
            }
            parameters[parameter.Name] = value;
        }



        // Подготовка запуска


        RunButton.IsEnabled = false;
        CancelButton.IsEnabled = true;

        _currentCancellationTokenSource = new CancellationTokenSource();
        var execution = new ExecutionItem
            {
                TaskName = selectedTask.Name,
                Status = "Выполняется",
                StartTime = DateTime.Now.ToString("HH:mm:ss"),
                EndTime = "",
                Result = "",
                Progress = 0,
                Log = ""
            };


        _executions.Add(execution);
        ExecutionGrid.SelectedItem = execution;
        ExecutionGrid.ScrollIntoView(execution);
        LogTextBox.Clear();


        TaskProgressBar.Value = 0;
        TaskProgressText.Text = "0%";


        try
        {
            StatusText.Text =
                $"Выполняется: {selectedTask.Name}";

            // ПРОГРЕСС
            var progress =
                new Progress<double>(
                    value =>
                    {
                        execution.Progress = Math.Clamp(value,0,100);
                        TaskProgressBar.Value =execution.Progress;
                        TaskProgressText.Text =execution.ProgressText;
                    });

            // ЛОГ
            var log =
                new Progress<string>(message =>
                    {
                        string line =$"[{DateTime.Now:HH:mm:ss}] {message}";
                        if (string.IsNullOrEmpty(execution.Log))
                        {
                            execution.Log =line;
                        }
                        else
                        {
                            execution.Log +=Environment.NewLine + line;
                        }
                        LogTextBox.AppendText( line + Environment.NewLine);
                        LogTextBox.ScrollToEnd();
                    });


            // ВЫПОЛНЕНИЕ
            var result = await selectedTask.Task.ExecuteAsync(parameters, _currentCancellationTokenSource.Token, progress, log);



            // РЕЗУЛЬТАТ

            execution.Status =  result.IsSuccess ? "Завершено" : "Ошибка";
            execution.Result = result.Message;
            execution.EndTime = DateTime.Now.ToString("HH:mm:ss");


            if (result.IsSuccess)
            {
                execution.Progress = 100;
                TaskProgressBar.Value = 100;
                TaskProgressText.Text = "100%";
                StatusText.Text = "Задача успешно завершена.";
            }
            else
            {
                StatusText.Text ="Задача завершилась с ошибкой.";
            }
        }
        catch (OperationCanceledException)
        {
            execution.Status ="Отменено";
            execution.Result ="Выполнение было отменено пользователем.";
            execution.EndTime =DateTime.Now.ToString("HH:mm:ss");

            StatusText.Text ="Задача отменена.";
        }
        catch (Exception ex)
        {
            execution.Status = "Ошибка";
            execution.Result = ex.Message;
            execution.EndTime =DateTime.Now.ToString("HH:mm:ss");

            StatusText.Text ="Ошибка выполнения задачи.";


            execution.Log += Environment.NewLine + $"[{DateTime.Now:HH:mm:ss}] Ошибка: {ex.Message}";
        }
        finally
        {
            RunButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            _currentCancellationTokenSource?.Dispose();
            _currentCancellationTokenSource = null;
            ExecutionGrid.Items.Refresh();
        }
    }


    // ОТМЕНА
    private void CancelButton_Click(object sender,RoutedEventArgs e)
    {
        _currentCancellationTokenSource? .Cancel();
    }



    // ВЫБОР ЗАПУСКА В ИСТОРИИ
    private void ExecutionGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ExecutionGrid.SelectedItem
            is not ExecutionItem execution)
        {
            return;
        }
        // Показываем прогресс
        TaskProgressBar.Value = execution.Progress;
        TaskProgressText.Text = execution.ProgressText;
        // Показываем лог
        LogTextBox.Text =  execution.Log;
        LogTextBox.ScrollToEnd();
    }


    // МЕНЮ
    private void ExitMenuItem_Click(object sender,RoutedEventArgs e)
    {
        Close();
    }


    private void ReloadPluginsMenuItem_Click(object sender,RoutedEventArgs e)
    {
        LoadPlugins();
    }
    private void ClearHistoryMenuItem_Click(object sender,RoutedEventArgs e)
    {
        _executions.Clear();
        LogTextBox.Clear();

        TaskProgressBar.Value = 0;
        TaskProgressText.Text = "0%";
        StatusText.Text ="История выполнения очищена.";
    }
    private void AboutMenuItem_Click( object sender,RoutedEventArgs e)
    {
        MessageBox.Show(
            "Модульное приложение\n\n" +
            "Тестовое задание на C# / WPF\n" +
            "Динамическая система плагинов.",
            "О программе",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}