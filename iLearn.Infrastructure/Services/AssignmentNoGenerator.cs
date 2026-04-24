using iLearn.Application.Interfaces.Services;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace iLearn.Infrastructure.Services
{
    /// <summary>
    /// Generates a date-based running number that resets each day.
    /// The next number is calculated from existing AssignmentNo values for the current date prefix,
    /// using SQL locking hints to avoid duplicate values under concurrent requests.
    /// Format: AS-yyyyMMdd-NNN
    /// </summary>
    public class AssignmentNoGenerator : IAssignmentNoGenerator
    {
        private readonly AppDbContext _context;
        private readonly IDateTime _dateTime;

        public AssignmentNoGenerator(AppDbContext context, IDateTime dateTime)
        {
            _context = context;
            _dateTime = dateTime;
        }

        public async Task<string> NextAsync()
        {
            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await _context.Database.OpenConnectionAsync();
            }

            try
            {
                string datePrefix = _dateTime.Now.ToString("yyyyMMdd");
                string assignmentPrefix = $"AS-{datePrefix}-";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
SELECT ISNULL(MAX(TRY_CONVERT(int, RIGHT([AssignmentNo], 3))), 0) + 1
FROM [Assignments] WITH (UPDLOCK, HOLDLOCK)
WHERE [AssignmentNo] LIKE @prefix + '[0-9][0-9][0-9]';";

                var prefixParam = cmd.CreateParameter();
                prefixParam.ParameterName = "@prefix";
                prefixParam.Value = assignmentPrefix;
                cmd.Parameters.Add(prefixParam);

                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction is not null)
                {
                    cmd.Transaction = currentTransaction.GetDbTransaction();
                }

                var result = await cmd.ExecuteScalarAsync();
                int seqValue = Convert.ToInt32(result);

                return $"AS-{datePrefix}-{seqValue:D3}";
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }
    }
}
