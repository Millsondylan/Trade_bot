using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Internals;

namespace TradingBot.Framework.Logging
{
    /// <summary>
    /// Comprehensive logging system for tracking trades, performance, and debugging
    /// Essential for backtesting validation and live trading monitoring
    /// </summary>
    public class TradingLogger
    {
        private readonly Robot _robot;
        private readonly List<TradeLog> _tradeLogs;
        private readonly List<string> _debugLogs;
        private readonly DateTime _startTime;
        private double _startingBalance;
        private double _peakBalance;
        private double _maxDrawdown;

        public TradingLogger(Robot robot)
        {
            _robot = robot;
            _tradeLogs = new List<TradeLog>();
            _debugLogs = new List<string>();
            _startTime = robot.Server.Time;
            _startingBalance = robot.Account.Balance;
            _peakBalance = robot.Account.Balance;
            _maxDrawdown = 0;
        }

        /// <summary>
        /// Log trade entry with all relevant details
        /// </summary>
        public void LogTradeEntry(Position position, string strategy, string signalDetails)
        {
            var log = new TradeLog
            {
                EntryTime = position.EntryTime,
                Symbol = position.SymbolName,
                TradeType = position.TradeType,
                EntryPrice = position.EntryPrice,
                Volume = position.VolumeInUnits,
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                Strategy = strategy,
                SignalDetails = signalDetails,
                PositionId = position.Id
            };

            _tradeLogs.Add(log);

            _robot.Print($"[TRADE ENTRY] {position.TradeType} {position.VolumeInUnits} units {position.SymbolName} @ {position.EntryPrice:F5}");
            _robot.Print($"  SL: {position.StopLoss:F5} | TP: {position.TakeProfit:F5} | Strategy: {strategy}");
            _robot.Print($"  Signal: {signalDetails}");
        }

        /// <summary>
        /// Log trade exit with profit/loss details
        /// </summary>
        public void LogTradeExit(Position position, string exitReason)
        {
            var log = _tradeLogs.FirstOrDefault(t => t.PositionId == position.Id);
            if (log != null)
            {
                log.ExitTime = _robot.Server.Time;
                log.ExitPrice = position.EntryPrice + (position.NetProfit / position.VolumeInUnits);
                log.NetProfit = position.NetProfit;
                log.GrossProfit = position.GrossProfit;
                log.Commission = position.Commissions;
                log.Swap = position.Swap;
                log.ExitReason = exitReason;
                log.Pips = position.Pips;
                log.Duration = log.ExitTime.HasValue ? log.ExitTime.Value - log.EntryTime : TimeSpan.Zero;

                // Update statistics
                UpdateStatistics();

                _robot.Print($"[TRADE EXIT] {position.TradeType} {position.SymbolName} closed");
                _robot.Print($"  P/L: {position.NetProfit:F2} ({position.Pips:F1} pips) | Reason: {exitReason}");
                _robot.Print($"  Duration: {log.Duration} | Commission: {log.Commission:F2}");
            }
        }

        /// <summary>
        /// Log general debug information
        /// </summary>
        public void LogDebug(string message)
        {
            string timestamped = $"[{_robot.Server.Time:yyyy-MM-dd HH:mm:ss}] {message}";
            _debugLogs.Add(timestamped);
            _robot.Print($"[DEBUG] {message}");
        }

        /// <summary>
        /// Log error with details
        /// </summary>
        public void LogError(string message, Exception ex = null)
        {
            string errorMsg = $"[ERROR] {message}";
            if (ex != null)
            {
                errorMsg += $"\n  Exception: {ex.Message}\n  StackTrace: {ex.StackTrace}";
            }

            _debugLogs.Add(errorMsg);
            _robot.Print(errorMsg);
        }

        /// <summary>
        /// Log warning
        /// </summary>
        public void LogWarning(string message)
        {
            string warningMsg = $"[WARNING] {message}";
            _debugLogs.Add(warningMsg);
            _robot.Print(warningMsg);
        }

        /// <summary>
        /// Get performance statistics summary
        /// </summary>
        public PerformanceStatistics GetPerformanceStatistics()
        {
            var closedTrades = _tradeLogs.Where(t => t.ExitTime.HasValue).ToList();

            if (closedTrades.Count == 0)
            {
                return new PerformanceStatistics();
            }

            int totalTrades = closedTrades.Count;
            int winningTrades = closedTrades.Count(t => t.NetProfit > 0);
            int losingTrades = closedTrades.Count(t => t.NetProfit < 0);
            int breakEvenTrades = closedTrades.Count(t => t.NetProfit == 0);

            double totalProfit = closedTrades.Sum(t => t.NetProfit ?? 0);
            double grossProfit = closedTrades.Where(t => t.NetProfit > 0).Sum(t => t.NetProfit ?? 0);
            double grossLoss = Math.Abs(closedTrades.Where(t => t.NetProfit < 0).Sum(t => t.NetProfit ?? 0));

            double avgWin = winningTrades > 0 ? closedTrades.Where(t => t.NetProfit > 0).Average(t => t.NetProfit ?? 0) : 0;
            double avgLoss = losingTrades > 0 ? closedTrades.Where(t => t.NetProfit < 0).Average(t => t.NetProfit ?? 0) : 0;

            double winRate = totalTrades > 0 ? (double)winningTrades / totalTrades : 0;
            double profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;
            double avgWinLossRatio = avgLoss != 0 ? Math.Abs(avgWin / avgLoss) : 0;

            double currentBalance = _robot.Account.Balance;
            double returnPercent = (currentBalance - _startingBalance) / _startingBalance * 100;

            TimeSpan totalDuration = _robot.Server.Time - _startTime;
            double avgTradeDuration = closedTrades.Any() ? closedTrades.Average(t => t.Duration.TotalHours) : 0;

            // Calculate max consecutive wins/losses
            int maxConsecutiveWins = CalculateMaxConsecutive(closedTrades, true);
            int maxConsecutiveLosses = CalculateMaxConsecutive(closedTrades, false);

            return new PerformanceStatistics
            {
                TotalTrades = totalTrades,
                WinningTrades = winningTrades,
                LosingTrades = losingTrades,
                BreakEvenTrades = breakEvenTrades,
                WinRate = winRate,
                TotalProfit = totalProfit,
                GrossProfit = grossProfit,
                GrossLoss = grossLoss,
                AverageWin = avgWin,
                AverageLoss = avgLoss,
                ProfitFactor = profitFactor,
                AvgWinLossRatio = avgWinLossRatio,
                StartingBalance = _startingBalance,
                CurrentBalance = currentBalance,
                PeakBalance = _peakBalance,
                MaxDrawdown = _maxDrawdown,
                ReturnPercent = returnPercent,
                TotalDuration = totalDuration,
                AverageTradeDuration = avgTradeDuration,
                MaxConsecutiveWins = maxConsecutiveWins,
                MaxConsecutiveLosses = maxConsecutiveLosses
            };
        }

        /// <summary>
        /// Print comprehensive performance report
        /// </summary>
        public void PrintPerformanceReport()
        {
            var stats = GetPerformanceStatistics();

            _robot.Print("\n========================================");
            _robot.Print("      PERFORMANCE REPORT");
            _robot.Print("========================================");
            _robot.Print($"Trading Period: {stats.TotalDuration.TotalDays:F0} days");
            _robot.Print($"Total Trades: {stats.TotalTrades}");
            _robot.Print($"  Winning: {stats.WinningTrades} ({stats.WinRate * 100:F1}%)");
            _robot.Print($"  Losing: {stats.LosingTrades}");
            _robot.Print($"  Break-even: {stats.BreakEvenTrades}");
            _robot.Print("");
            _robot.Print($"Financial Results:");
            _robot.Print($"  Starting Balance: {stats.StartingBalance:F2}");
            _robot.Print($"  Current Balance: {stats.CurrentBalance:F2}");
            _robot.Print($"  Total Profit: {stats.TotalProfit:F2}");
            _robot.Print($"  Return: {stats.ReturnPercent:F2}%");
            _robot.Print($"  Peak Balance: {stats.PeakBalance:F2}");
            _robot.Print($"  Max Drawdown: {stats.MaxDrawdown:F2}%");
            _robot.Print("");
            _robot.Print($"Trade Quality:");
            _robot.Print($"  Profit Factor: {stats.ProfitFactor:F2}");
            _robot.Print($"  Avg Win: {stats.AverageWin:F2}");
            _robot.Print($"  Avg Loss: {stats.AverageLoss:F2}");
            _robot.Print($"  Win/Loss Ratio: {stats.AvgWinLossRatio:F2}");
            _robot.Print($"  Avg Trade Duration: {stats.AverageTradeDuration:F1} hours");
            _robot.Print("");
            _robot.Print($"Consistency:");
            _robot.Print($"  Max Consecutive Wins: {stats.MaxConsecutiveWins}");
            _robot.Print($"  Max Consecutive Losses: {stats.MaxConsecutiveLosses}");
            _robot.Print("========================================\n");
        }

        /// <summary>
        /// Export trade logs to CSV format for external analysis
        /// </summary>
        public string ExportToCSV()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("EntryTime,ExitTime,Symbol,TradeType,Volume,EntryPrice,ExitPrice,StopLoss,TakeProfit," +
                         "NetProfit,GrossProfit,Commission,Swap,Pips,Duration,Strategy,SignalDetails,ExitReason");

            // Data rows
            foreach (var log in _tradeLogs.Where(t => t.ExitTime.HasValue))
            {
                sb.AppendLine($"{log.EntryTime:yyyy-MM-dd HH:mm:ss}," +
                             $"{log.ExitTime:yyyy-MM-dd HH:mm:ss}," +
                             $"{log.Symbol}," +
                             $"{log.TradeType}," +
                             $"{log.Volume}," +
                             $"{log.EntryPrice:F5}," +
                             $"{log.ExitPrice:F5}," +
                             $"{log.StopLoss:F5}," +
                             $"{log.TakeProfit:F5}," +
                             $"{log.NetProfit:F2}," +
                             $"{log.GrossProfit:F2}," +
                             $"{log.Commission:F2}," +
                             $"{log.Swap:F2}," +
                             $"{log.Pips:F1}," +
                             $"{log.Duration.TotalHours:F2}," +
                             $"{log.Strategy}," +
                             $"\"{log.SignalDetails}\"," +
                             $"\"{log.ExitReason}\"");
            }

            return sb.ToString();
        }

        private void UpdateStatistics()
        {
            double currentBalance = _robot.Account.Balance;

            // Update peak balance
            if (currentBalance > _peakBalance)
            {
                _peakBalance = currentBalance;
            }

            // Update max drawdown
            if (_peakBalance > 0)
            {
                double currentDrawdown = ((_peakBalance - currentBalance) / _peakBalance) * 100;
                if (currentDrawdown > _maxDrawdown)
                {
                    _maxDrawdown = currentDrawdown;
                }
            }
        }

        private int CalculateMaxConsecutive(List<TradeLog> trades, bool wins)
        {
            int maxConsecutive = 0;
            int currentConsecutive = 0;

            foreach (var trade in trades.OrderBy(t => t.ExitTime))
            {
                bool isWin = trade.NetProfit > 0;

                if (isWin == wins)
                {
                    currentConsecutive++;
                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                }
                else
                {
                    currentConsecutive = 0;
                }
            }

            return maxConsecutive;
        }
    }

    /// <summary>
    /// Individual trade log entry
    /// </summary>
    public class TradeLog
    {
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public string Symbol { get; set; }
        public TradeType TradeType { get; set; }
        public double Volume { get; set; }
        public double EntryPrice { get; set; }
        public double ExitPrice { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public double? NetProfit { get; set; }
        public double? GrossProfit { get; set; }
        public double? Commission { get; set; }
        public double? Swap { get; set; }
        public double? Pips { get; set; }
        public TimeSpan Duration { get; set; }
        public string Strategy { get; set; }
        public string SignalDetails { get; set; }
        public string ExitReason { get; set; }
        public long PositionId { get; set; }
    }

    /// <summary>
    /// Performance statistics summary
    /// </summary>
    public class PerformanceStatistics
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public int BreakEvenTrades { get; set; }
        public double WinRate { get; set; }
        public double TotalProfit { get; set; }
        public double GrossProfit { get; set; }
        public double GrossLoss { get; set; }
        public double AverageWin { get; set; }
        public double AverageLoss { get; set; }
        public double ProfitFactor { get; set; }
        public double AvgWinLossRatio { get; set; }
        public double StartingBalance { get; set; }
        public double CurrentBalance { get; set; }
        public double PeakBalance { get; set; }
        public double MaxDrawdown { get; set; }
        public double ReturnPercent { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double AverageTradeDuration { get; set; }
        public int MaxConsecutiveWins { get; set; }
        public int MaxConsecutiveLosses { get; set; }
    }
}
