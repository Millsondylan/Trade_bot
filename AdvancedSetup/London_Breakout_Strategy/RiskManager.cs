using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace TradingBot.Framework.RiskManagement
{
    /// <summary>
    /// Comprehensive risk management system implementing the safety protocols
    /// from the trading guide. Handles drawdown limits, daily loss limits,
    /// position limits, and emergency stops.
    /// </summary>
    public class RiskManager
    {
        private readonly Robot _robot;
        private readonly double _maxDailyLossPercent;
        private readonly double _maxWeeklyLossPercent;
        private readonly double _maxDrawdownPercent;
        private readonly int _maxConcurrentPositions;

        private double _startingDailyEquity;
        private double _startingWeeklyEquity;
        private double _highWaterMark;
        private DateTime _lastDailyReset;
        private DateTime _lastWeeklyReset;
        private bool _tradingEnabled;

        /// <summary>
        /// Initializes the RiskManager with conservative default values
        /// </summary>
        /// <param name="robot">The cTrader Robot instance</param>
        /// <param name="maxDailyLossPercent">Maximum daily loss as % of account (default: 2%)</param>
        /// <param name="maxWeeklyLossPercent">Maximum weekly loss as % of account (default: 5%)</param>
        /// <param name="maxDrawdownPercent">Maximum drawdown from peak (default: 20%)</param>
        /// <param name="maxConcurrentPositions">Maximum number of open positions (default: 5)</param>
        public RiskManager(Robot robot,
            double maxDailyLossPercent = 2.0,
            double maxWeeklyLossPercent = 5.0,
            double maxDrawdownPercent = 20.0,
            int maxConcurrentPositions = 5)
        {
            _robot = robot;
            _maxDailyLossPercent = maxDailyLossPercent;
            _maxWeeklyLossPercent = maxWeeklyLossPercent;
            _maxDrawdownPercent = maxDrawdownPercent;
            _maxConcurrentPositions = maxConcurrentPositions;

            _startingDailyEquity = robot.Account.Equity;
            _startingWeeklyEquity = robot.Account.Equity;
            _highWaterMark = robot.Account.Equity;
            _lastDailyReset = robot.Server.Time.Date;
            _lastWeeklyReset = GetWeekStart(robot.Server.Time);
            _tradingEnabled = true;
        }

        /// <summary>
        /// Must be called on every bar/tick to monitor risk limits
        /// Returns true if trading is allowed, false if limits breached
        /// </summary>
        public bool CheckRiskLimits()
        {
            DateTime currentTime = _robot.Server.Time;
            double currentEquity = _robot.Account.Equity;

            // Reset daily tracking
            if (currentTime.Date > _lastDailyReset)
            {
                _startingDailyEquity = currentEquity;
                _lastDailyReset = currentTime.Date;
                _robot.Print($"[RiskManager] Daily reset - Starting equity: {currentEquity:F2}");
            }

            // Reset weekly tracking
            DateTime weekStart = GetWeekStart(currentTime);
            if (weekStart > _lastWeeklyReset)
            {
                _startingWeeklyEquity = currentEquity;
                _lastWeeklyReset = weekStart;
                _robot.Print($"[RiskManager] Weekly reset - Starting equity: {currentEquity:F2}");
            }

            // Update high water mark
            if (currentEquity > _highWaterMark)
            {
                _highWaterMark = currentEquity;
                _robot.Print($"[RiskManager] New high water mark: {_highWaterMark:F2}");
            }

            // Check daily loss limit
            double dailyLoss = _startingDailyEquity - currentEquity;
            double dailyLossPercent = (dailyLoss / _startingDailyEquity) * 100;

            if (dailyLossPercent >= _maxDailyLossPercent)
            {
                _tradingEnabled = false;
                _robot.Print($"[RiskManager] EMERGENCY STOP: Daily loss limit breached!");
                _robot.Print($"Daily Loss: {dailyLoss:F2} ({dailyLossPercent:F2}%) - Limit: {_maxDailyLossPercent}%");
                CloseAllPositions("Daily loss limit reached");
                return false;
            }

            // Check weekly loss limit
            double weeklyLoss = _startingWeeklyEquity - currentEquity;
            double weeklyLossPercent = (weeklyLoss / _startingWeeklyEquity) * 100;

            if (weeklyLossPercent >= _maxWeeklyLossPercent)
            {
                _tradingEnabled = false;
                _robot.Print($"[RiskManager] EMERGENCY STOP: Weekly loss limit breached!");
                _robot.Print($"Weekly Loss: {weeklyLoss:F2} ({weeklyLossPercent:F2}%) - Limit: {_maxWeeklyLossPercent}%");
                CloseAllPositions("Weekly loss limit reached");
                return false;
            }

            // Check maximum drawdown from peak
            double drawdown = _highWaterMark - currentEquity;
            double drawdownPercent = (drawdown / _highWaterMark) * 100;

            if (drawdownPercent >= _maxDrawdownPercent)
            {
                _tradingEnabled = false;
                _robot.Print($"[RiskManager] EMERGENCY STOP: Maximum drawdown breached!");
                _robot.Print($"Drawdown: {drawdown:F2} ({drawdownPercent:F2}%) - Limit: {_maxDrawdownPercent}%");
                CloseAllPositions("Maximum drawdown reached");
                return false;
            }

            // Warning at 50% of max drawdown
            if (drawdownPercent >= _maxDrawdownPercent * 0.5 && drawdownPercent < _maxDrawdownPercent)
            {
                _robot.Print($"[RiskManager] WARNING: Drawdown at {drawdownPercent:F2}% (50% of limit)");
            }

            // Check position count limit
            int openPositions = _robot.Positions.Count;
            if (openPositions >= _maxConcurrentPositions)
            {
                _robot.Print($"[RiskManager] Position limit reached: {openPositions}/{_maxConcurrentPositions}");
                return false;
            }

            return _tradingEnabled;
        }

        /// <summary>
        /// Checks if a new position can be opened based on current risk exposure
        /// </summary>
        public bool CanOpenNewPosition()
        {
            if (!_tradingEnabled)
            {
                return false;
            }

            if (_robot.Positions.Count >= _maxConcurrentPositions)
            {
                return false;
            }

            return CheckRiskLimits();
        }

        /// <summary>
        /// Gets current risk statistics for monitoring
        /// </summary>
        public RiskStatistics GetRiskStatistics()
        {
            double currentEquity = _robot.Account.Equity;

            return new RiskStatistics
            {
                CurrentEquity = currentEquity,
                HighWaterMark = _highWaterMark,
                DailyLoss = _startingDailyEquity - currentEquity,
                DailyLossPercent = ((_startingDailyEquity - currentEquity) / _startingDailyEquity) * 100,
                WeeklyLoss = _startingWeeklyEquity - currentEquity,
                WeeklyLossPercent = ((_startingWeeklyEquity - currentEquity) / _startingWeeklyEquity) * 100,
                DrawdownFromPeak = _highWaterMark - currentEquity,
                DrawdownPercent = ((_highWaterMark - currentEquity) / _highWaterMark) * 100,
                OpenPositions = _robot.Positions.Count,
                TradingEnabled = _tradingEnabled
            };
        }

        /// <summary>
        /// Manually enable trading after review (use after emergency stop)
        /// </summary>
        public void EnableTrading()
        {
            _tradingEnabled = true;
            _robot.Print("[RiskManager] Trading manually re-enabled");
        }

        /// <summary>
        /// Manually disable trading
        /// </summary>
        public void DisableTrading()
        {
            _tradingEnabled = false;
            _robot.Print("[RiskManager] Trading manually disabled");
        }

        private void CloseAllPositions(string reason)
        {
            _robot.Print($"[RiskManager] Closing all positions: {reason}");

            foreach (var position in _robot.Positions.ToArray())
            {
                var result = position.Close();
                if (!result.IsSuccessful)
                {
                    _robot.Print($"[RiskManager] Failed to close position: {result.Error}");
                }
            }
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }

    /// <summary>
    /// Risk statistics snapshot
    /// </summary>
    public class RiskStatistics
    {
        public double CurrentEquity { get; set; }
        public double HighWaterMark { get; set; }
        public double DailyLoss { get; set; }
        public double DailyLossPercent { get; set; }
        public double WeeklyLoss { get; set; }
        public double WeeklyLossPercent { get; set; }
        public double DrawdownFromPeak { get; set; }
        public double DrawdownPercent { get; set; }
        public int OpenPositions { get; set; }
        public bool TradingEnabled { get; set; }
    }
}
