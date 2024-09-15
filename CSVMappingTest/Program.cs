using CSVMappingTest.Models;

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
            BuildFirstReport(sessions);

            Console.WriteLine("Второй отчет:");
            BuildSecondReport(sessions);

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

        private static void BuildFirstReport(List<CallCenterSessionInfo> sessionList)
        {
            var groupedSessions = sessionList.OrderBy(x => x.SessionStart.Day).GroupBy(s => s.SessionStart.Date);

            Console.WriteLine("День     Количество сессий");

            foreach (var group in groupedSessions)
            {
                var dateTimeTupleList = new List<Tuple<DateTime, int>>();
                dateTimeTupleList.AddRange(group.Select(g => new Tuple<DateTime, int>(g.SessionStart, 1)));
                dateTimeTupleList.AddRange(group.Select(g => new Tuple<DateTime, int>(g.SessionEnd, -1)));
                var maxCumulativeSumOfADay = 0;
                var currentSum = 0;
                foreach (var node in dateTimeTupleList.OrderBy(dt => dt.Item1))
                {
                    currentSum += node.Item2;
                    if (currentSum > maxCumulativeSumOfADay)
                    {
                        maxCumulativeSumOfADay++;
                    }
                }
                Console.WriteLine($"{group.Key:dd.MM.yyyy}     {maxCumulativeSumOfADay}");
            }

            Console.WriteLine();
        }

        private static void BuildSecondReport(List<CallCenterSessionInfo> sessionList)
        {
            var groupedSessions = sessionList.GroupBy(s => s.OperatorName);

            Console.WriteLine("ФИО              Пауза   Готов   Разговор    Обработка   Перезвон");

            foreach (var group in groupedSessions)
            {
                var line = string.Join(' ',
                group.Key,
                group.Where(x => x.OperatorState is OperatorState.Pause).Sum(x => x.SessionTime),
                group.Where(x => x.OperatorState is OperatorState.Ready).Sum(x => x.SessionTime),
                group.Where(x => x.OperatorState is OperatorState.Call).Sum(x => x.SessionTime),
                group.Where(x => x.OperatorState is OperatorState.Processing).Sum(x => x.SessionTime),
                group.Where(x => x.OperatorState is OperatorState.Recall).Sum(x => x.SessionTime));
                Console.WriteLine(line);
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
