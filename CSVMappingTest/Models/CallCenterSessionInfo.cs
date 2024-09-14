using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVMappingTest.Models
{
    public record CallCenterSessionInfo
    {
        public DateTime SessionStart { get; init; }
        public DateTime SessionEnd { get; init; }
        public string ProjectName { get; init; }
        public string OperatorName { get; init; }
        public OperatorState OperatorState { get; init; }
        public int SessionTime { get; init; }
    }
}
