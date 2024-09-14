﻿using CSVMappingTest.Models;

namespace CSVMappingTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: The file {filePath} does not exist.");
                return;
            }

            await ProcessReports(filePath);
        }

        private static async Task ProcessReports(string path)
        {
            var sessions = await MapCSVToSessionsList(path);

            Console.WriteLine("Первый отчет:");
            await BuildFirstReport(sessions);

            Console.WriteLine("Второй отчет:");
            await BuildSecondReport(sessions);

            Console.ReadKey();
        }

        private static async Task<List<CallCenterSessionInfo>> MapCSVToSessionsList(string filePath)
        {
            var set = new HashSet<CallCenterSessionInfo>();
            string? line;

            using var reader = new StreamReader(filePath);
            {
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    if (line.Contains("Дата начала")) //первая строчка с названиями полей была в середине файла
                        continue;
                    var values = line.Split(';');
                    set.Add(new CallCenterSessionInfo
                    {
                        SessionStart = DateTime.Parse(values[0]),
                        SessionEnd = DateTime.Parse(values[1]),
                        ProjectName = values[2].Trim(),
                        OperatorName = values[3].Trim(),
                        OperatorState = MapToOpState(values[4]),
                        SessionTime = int.Parse(values[5]),
                    });
                }
            }

            return [.. set];
        }

        private static async Task BuildFirstReport(List<CallCenterSessionInfo> sessionList)
        {
            var groupedSessions = sessionList.OrderBy(x => x.SessionStart.Day).GroupBy(s => s.SessionStart.Date);
            var tasks = new HashSet<Task<string>>();
            var taskFactory = new TaskFactory();

            Console.WriteLine("День     Количество сессий");

            foreach (var group in groupedSessions)
            {
                var groupTask = taskFactory.StartNew(() =>
                {
                    var simultaneousSessions = group.SelectMany((s1, index1) => group.Skip(index1 + 1)
                                                .Where((s2) => s1.SessionEnd >= s2.SessionStart && s2.SessionEnd >= s1.SessionStart))
                                                .Distinct()
                                                .ToList();
                    return $"{group.Key:dd.MM.yyyy}     {simultaneousSessions.Count}";
                });
                tasks.Add(groupTask);
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                Console.WriteLine(task.Result);
            }

            Console.WriteLine();
        }

        private static async Task BuildSecondReport(List<CallCenterSessionInfo> sessionList)
        {
            var groupedSessions = sessionList.GroupBy(s => s.OperatorName);
            var tasks = new HashSet<Task<string>>();
            var taskFactory = new TaskFactory();

            Console.WriteLine("ФИО              Пауза   Готов   Разговор    Обработка   Перезвон");

            foreach (var group in groupedSessions)
            {
                var groupTask = taskFactory.StartNew(() =>
                {
                    var line = string.Join(' ',
                    group.Key,
                    group.Where(x => x.OperatorState is OperatorState.Pause).Sum(x => x.SessionTime),
                    group.Where(x => x.OperatorState is OperatorState.Ready).Sum(x => x.SessionTime),
                    group.Where(x => x.OperatorState is OperatorState.Call).Sum(x => x.SessionTime),
                    group.Where(x => x.OperatorState is OperatorState.Processing).Sum(x => x.SessionTime),
                    group.Where(x => x.OperatorState is OperatorState.Recall).Sum(x => x.SessionTime));

                    return line;
                });
                tasks.Add(groupTask);
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                Console.WriteLine(task.Result);
            }

            Console.WriteLine();
        }

        private static OperatorState MapToOpState(string opState) => opState.ToLowerInvariant() switch
        {
            "пауза" => OperatorState.Pause,
            "готов" => OperatorState.Ready,
            "разговор" => OperatorState.Call,
            "обработка" => OperatorState.Processing,
            "перезвон" => OperatorState.Recall,
            _ => throw new NotImplementedException($"unknown state: {opState}")
        };
    }
}
